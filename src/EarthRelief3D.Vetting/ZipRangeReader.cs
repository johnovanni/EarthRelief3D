using System.Buffers.Binary;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace EarthRelief3D.Vetting;

/// <summary>
/// Reads one file out of a zip on a web server without downloading the zip, by HTTP Range requests: the end of the file (the
/// end-of-central-directory record), the central directory (the list of entries), then just that entry. The IRS 990 batch zips
/// are 70 to 260 MB each and hold tens of thousands of returns; a return is a few hundred KB. apps.irs.gov answers Range
/// requests with 206 (checked 2026-11-24, docs/data-sources.md section 20). A server that answers 200 instead (it ignores Range)
/// gets one full download, kept in <c>cacheDir</c> so the next entry from the same zip reads from the copy. Zip64 is supported
/// (a batch can hold more than 65,535 returns).
/// </summary>
public static class ZipRangeReader
{
    private const int TailBytes = 128 * 1024;                  // the end of the file: larger than the biggest possible zip comment
    private const long MaxEntryBytes = 256L * 1024 * 1024;     // a return is never this big: a corrupt zip should fail, not fill memory

    private const uint EocdSig = 0x06054b50, Eocd64Sig = 0x06064b50, Locator64Sig = 0x07064b50, CentralSig = 0x02014b50, LocalSig = 0x04034b50;

    /// <param name="cacheDir">Where a full download goes if the server ignores Range; null for a temporary folder that is cleaned up.</param>
    /// <exception cref="FileNotFoundException">The zip has no entry called <paramref name="entryName"/>.</exception>
    public static async Task<byte[]> ReadEntryAsync(VettingHttp http, Uri zip, string entryName, CancellationToken ct, string? cacheDir = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        var cached = cacheDir is null ? null : Path.Combine(cacheDir, CacheName(zip));
        if (cached is not null && File.Exists(cached)) return ReadFromFile(cached, entryName);

        using var resp = await http.GetAsync(zip, new RangeHeaderValue(null, TailBytes), ct).ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.OK) return await FullDownloadAsync(resp, zip, entryName, cacheDir, ct).ConfigureAwait(false);
        if (resp.StatusCode != HttpStatusCode.PartialContent) throw new HttpRequestException($"{zip.Host} answered HTTP {(int)resp.StatusCode} to a range request for {Path.GetFileName(zip.AbsolutePath)}", null, resp.StatusCode);
        var total = resp.Content.Headers.ContentRange?.Length ?? throw new InvalidDataException("the server gave no total length with its partial answer");
        var tail = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        var tailStart = total - tail.Length;

        // End of central directory: the last 22+ bytes, found by its signature.
        var eocd = -1;
        for (var i = tail.Length - 22; i >= 0; i--)
            if (BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i)) == EocdSig && i + 22 + BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(i + 20)) == tail.Length) { eocd = i; break; }
        if (eocd < 0) throw new InvalidDataException("the file is not a zip (no end-of-central-directory record)");
        long entries = BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(eocd + 10));
        long cdSize = BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(eocd + 12));
        long cdOffset = BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(eocd + 16));
        if (entries == 0xFFFF || cdSize == 0xFFFFFFFF || cdOffset == 0xFFFFFFFF)
        {
            if (eocd < 20 || BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(eocd - 20)) != Locator64Sig) throw new InvalidDataException("a zip64 zip with no zip64 locator");
            var recordAt = (long)BinaryPrimitives.ReadUInt64LittleEndian(tail.AsSpan(eocd - 20 + 8));
            var record = await SliceAsync(http, zip, tail, tailStart, total, recordAt, 56, ct).ConfigureAwait(false);
            if (BinaryPrimitives.ReadUInt32LittleEndian(record) != Eocd64Sig) throw new InvalidDataException("bad zip64 end-of-central-directory record");
            cdSize = (long)BinaryPrimitives.ReadUInt64LittleEndian(record.AsSpan(40));
            cdOffset = (long)BinaryPrimitives.ReadUInt64LittleEndian(record.AsSpan(48));
        }
        if (cdOffset < 0 || cdSize <= 0 || cdOffset + cdSize > total) throw new InvalidDataException("the zip's central directory is out of range");

        var cd = await SliceAsync(http, zip, tail, tailStart, total, cdOffset, cdSize, ct).ConfigureAwait(false);
        var entry = FindEntry(cd, entryName) ?? throw new FileNotFoundException($"{Path.GetFileName(zip.AbsolutePath)} has no entry {entryName}");
        if (entry.CompressedSize > MaxEntryBytes || entry.Size > MaxEntryBytes) throw new InvalidDataException($"{entryName} is larger than {MaxEntryBytes / (1024 * 1024)} MB");

        // The local header (30 bytes, then name and extra, which may differ from the central copy), then the data.
        var want = Math.Min(30 + entry.NameLength + 1024 + entry.CompressedSize, total - entry.Offset);
        var chunk = await SliceAsync(http, zip, tail, tailStart, total, entry.Offset, want, ct).ConfigureAwait(false);
        if (BinaryPrimitives.ReadUInt32LittleEndian(chunk) != LocalSig) throw new InvalidDataException("bad local header");
        var dataStart = 30L + BinaryPrimitives.ReadUInt16LittleEndian(chunk.AsSpan(26)) + BinaryPrimitives.ReadUInt16LittleEndian(chunk.AsSpan(28));
        var localLength = dataStart + entry.CompressedSize;
        var local = localLength <= chunk.Length ? chunk.AsSpan(0, (int)localLength).ToArray()
            : await SliceAsync(http, zip, tail, tailStart, total, entry.Offset, localLength, ct).ConfigureAwait(false);
        return Inflate(entry, local, (int)dataStart);
    }

    // ---------------------------------------------------------------- full download (the server ignores Range)

    private static async Task<byte[]> FullDownloadAsync(HttpResponseMessage resp, Uri zip, string entryName, string? cacheDir, CancellationToken ct)
    {
        var dir = cacheDir ?? Path.Combine(Path.GetTempPath(), "er3d-vet-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, CacheName(zip));
        try
        {
            await VettingHttp.SaveAsync(resp, path, ct).ConfigureAwait(false);
            return ReadFromFile(path, entryName);
        }
        finally
        {
            if (cacheDir is null && Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    private static byte[] ReadFromFile(string path, string entryName)
    {
        using var archive = ZipFile.OpenRead(path);
        var e = archive.GetEntry(entryName) ?? archive.Entries.FirstOrDefault(x => string.Equals(x.FullName, entryName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"{Path.GetFileName(path)} has no entry {entryName}");
        return ReadAll(e);
    }

    /// <summary>One file in a zip, from a file on disk (the offline folder).</summary>
    public static byte[] ReadEntryFromFile(string zipPath, string entryName) => ReadFromFile(zipPath, entryName);

    private static byte[] ReadAll(ZipArchiveEntry e)
    {
        if (e.Length > MaxEntryBytes) throw new InvalidDataException($"{e.FullName} is larger than {MaxEntryBytes / (1024 * 1024)} MB");
        using var s = e.Open();
        using var ms = new MemoryStream((int)e.Length);
        s.CopyTo(ms);
        return ms.ToArray();
    }

    private static string CacheName(Uri zip)
    {
        var name = new string(Path.GetFileName(zip.AbsolutePath).Select(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '_').ToArray());
        return name.Length == 0 ? "download.zip" : name;
    }

    // ---------------------------------------------------------------- ranges

    /// <summary>Bytes [offset, offset+length) of the zip: from the tail already read when they lie in it, otherwise by one range request.</summary>
    private static async Task<byte[]> SliceAsync(VettingHttp http, Uri zip, byte[] tail, long tailStart, long total, long offset, long length, CancellationToken ct)
    {
        if (length > int.MaxValue) throw new InvalidDataException("a part of the zip is too large to read");
        if (offset >= tailStart && offset + length <= tailStart + tail.Length) return tail.AsSpan((int)(offset - tailStart), (int)length).ToArray();
        using var resp = await http.GetAsync(zip, new RangeHeaderValue(offset, offset + length - 1), ct).ConfigureAwait(false);
        if (resp.StatusCode != HttpStatusCode.PartialContent) throw new HttpRequestException($"{zip.Host} did not honour a later range request (HTTP {(int)resp.StatusCode})", null, resp.StatusCode);
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        if (bytes.Length != length) throw new InvalidDataException($"asked for {length} bytes at {offset} of {total}, got {bytes.Length}");
        return bytes;
    }

    // ---------------------------------------------------------------- central directory

    private sealed record CentralEntry(string Name, int NameLength, ushort Method, uint Crc, long CompressedSize, long Size, long Offset, byte[] Record);

    private static CentralEntry? FindEntry(byte[] cd, string entryName)
    {
        var pos = 0;
        while (pos + 46 <= cd.Length && BinaryPrimitives.ReadUInt32LittleEndian(cd.AsSpan(pos)) == CentralSig)
        {
            var flags = BinaryPrimitives.ReadUInt16LittleEndian(cd.AsSpan(pos + 8));
            var method = BinaryPrimitives.ReadUInt16LittleEndian(cd.AsSpan(pos + 10));
            var crc = BinaryPrimitives.ReadUInt32LittleEndian(cd.AsSpan(pos + 16));
            long comp = BinaryPrimitives.ReadUInt32LittleEndian(cd.AsSpan(pos + 20));
            long size = BinaryPrimitives.ReadUInt32LittleEndian(cd.AsSpan(pos + 24));
            int nameLen = BinaryPrimitives.ReadUInt16LittleEndian(cd.AsSpan(pos + 28));
            int extraLen = BinaryPrimitives.ReadUInt16LittleEndian(cd.AsSpan(pos + 30));
            int commentLen = BinaryPrimitives.ReadUInt16LittleEndian(cd.AsSpan(pos + 32));
            long offset = BinaryPrimitives.ReadUInt32LittleEndian(cd.AsSpan(pos + 42));
            if (pos + 46 + nameLen + extraLen + commentLen > cd.Length) break;
            var name = ((flags & 0x800) != 0 ? Encoding.UTF8 : Encoding.Latin1).GetString(cd, pos + 46, nameLen);
            if (string.Equals(name, entryName, StringComparison.OrdinalIgnoreCase))
            {
                // Zip64 extra field (id 1): the values that did not fit in 32 bits, in this order.
                var x = pos + 46 + nameLen;
                var end = x + extraLen;
                while (x + 4 <= end)
                {
                    var id = BinaryPrimitives.ReadUInt16LittleEndian(cd.AsSpan(x));
                    var len = BinaryPrimitives.ReadUInt16LittleEndian(cd.AsSpan(x + 2));
                    if (id == 1)
                    {
                        var p = x + 4;
                        if (size == 0xFFFFFFFF) { size = (long)BinaryPrimitives.ReadUInt64LittleEndian(cd.AsSpan(p)); p += 8; }
                        if (comp == 0xFFFFFFFF) { comp = (long)BinaryPrimitives.ReadUInt64LittleEndian(cd.AsSpan(p)); p += 8; }
                        if (offset == 0xFFFFFFFF) offset = (long)BinaryPrimitives.ReadUInt64LittleEndian(cd.AsSpan(p));
                    }
                    x += 4 + len;
                }
                return new CentralEntry(name, nameLen, method, crc, comp, size, offset, cd.AsSpan(pos, 46 + nameLen).ToArray());
            }
            pos += 46 + nameLen + extraLen + commentLen;
        }
        return null;
    }

    /// <param name="local">The local header and the compressed data, as they lie in the zip.</param>
    private static byte[] Inflate(CentralEntry e, byte[] local, int dataStart)
    {
        byte[] result;
        switch (e.Method)
        {
            case 0: result = local.AsSpan(dataStart, (int)e.CompressedSize).ToArray(); break;
            case 8:
                using (var src = new MemoryStream(local, dataStart, (int)e.CompressedSize))
                using (var deflate = new DeflateStream(src, CompressionMode.Decompress))
                using (var ms = new MemoryStream((int)e.Size))
                {
                    deflate.CopyTo(ms);
                    result = ms.ToArray();
                }
                break;
            default:
                // Another method (the IRS zips use Deflate64, method 9, for some returns): wrap the one entry in a tiny zip of its
                // own and let System.IO.Compression read it.
                result = ReadFromMiniZip(e, local);
                break;
        }
        if (result.Length != e.Size) throw new InvalidDataException($"{e.Name}: expected {e.Size} bytes, got {result.Length}");
        if (Crc32(result) != e.Crc) throw new InvalidDataException($"{e.Name}: the checksum does not match (a damaged download)");
        return result;
    }

    private static byte[] ReadFromMiniZip(CentralEntry e, byte[] local)
    {
        var record = (byte[])e.Record.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(20), (uint)e.CompressedSize);
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(24), (uint)e.Size);
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(30), 0);          // no extra field
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(32), 0);          // no comment
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(42), 0);          // the local header is first in the mini zip
        var eocd = new byte[22];
        BinaryPrimitives.WriteUInt32LittleEndian(eocd, EocdSig);
        BinaryPrimitives.WriteUInt16LittleEndian(eocd.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(eocd.AsSpan(10), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(eocd.AsSpan(12), (uint)record.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(eocd.AsSpan(16), (uint)local.Length);
        using var mini = new MemoryStream(local.Length + record.Length + eocd.Length);
        mini.Write(local);
        mini.Write(record);
        mini.Write(eocd);
        mini.Position = 0;
        using var archive = new ZipArchive(mini, ZipArchiveMode.Read);
        return ReadAll(archive.Entries[0]);
    }
    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    private static uint Crc32(byte[] data)
    {
        var c = 0xFFFFFFFFu;
        foreach (var b in data) c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }
}

