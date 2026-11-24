namespace EarthRelief3D.Vetting;

/// <summary>
/// Reads the IRS auto-revocation list (<c>data-download-revocation.zip</c>, pipe-delimited, 12 fields:
/// <c>EIN|Legal name|Doing business as|Address|City|State|ZIP|Country|Exemption type|Revocation date|Posting date|Reinstatement date</c>).
/// An EIN is revoked when at least one of its rows has no reinstatement date: a row with one means the exemption was reinstated.
/// </summary>
public static class RevocationReader
{
    public static IReadOnlySet<string> Read(Stream zip, IReadOnlySet<string>? only = null)
    {
        var revoked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in ZipText.Lines(zip))
        {
            var bar = line.IndexOf('|');
            if (bar < 9) continue;
            var ein = Ein.Normalize(line[..bar]);
            if (ein is null || (only is not null && !only.Contains(ein))) continue;
            var f = line.Split('|');
            var reinstated = f.Length > 11 && f[11].Trim().Length > 0;
            if (!reinstated) revoked.Add(ein);
        }
        return revoked;
    }
}

