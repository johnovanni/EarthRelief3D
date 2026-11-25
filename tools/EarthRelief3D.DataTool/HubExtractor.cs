using System.Globalization;
using System.IO.Compression;
using EarthRelief3D.Core.Feeds;

namespace EarthRelief3D.DataTool;

/// <summary>
/// The two logistics-hub extracts of Plan 4 (P4-5; docs/data-sources.md §15 and §16): <c>airports &lt;airports.csv&gt;</c> to
/// <c>data\datasets\ourairports</c> (OurAirports' large and medium airports with scheduled passenger service) and
/// <c>ports &lt;WPI.zip or UpdatedPub150.csv&gt;</c> to <c>data\datasets\wpi-ports</c> (the NGA World Port Index's large and
/// medium harbours). Both are point extracts (<see cref="BundledPoints.Columns"/>): the site's type is "large" or "medium",
/// and <c>detail</c> holds an airport's IATA code or a port's UN/LOCODE. A site whose country the boundary set lacks is left
/// out and listed in the manifest's <c>notPlaced</c> (C16), never dropped silently.
/// </summary>
public static class HubExtractor
{
    public const string AirportsFolder = "ourairports";
    public const string PortsFolder = "wpi-ports";

    public const string AirportsPublisher = "OurAirports";
    public const string AirportsDataset = "OurAirports airport data";
    public const string AirportsDownload = "https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv";
    public const string AirportsPage = "https://ourairports.com/data/";
    public const string AirportsLicence = "The Unlicense (public domain)";
    public const string AirportsLicenceUrl = "https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/LICENSE";
    public const string AirportsAttribution = "OurAirports (ourairports.com), public domain (The Unlicense).";
    public const string AirportsFilter = "large and medium airports with scheduled passenger service";

    public const string PortsPublisher = "US National Geospatial-Intelligence Agency (NGA)";
    public const string PortsDataset = "World Port Index (Pub 150)";
    public const string PortsDownload = "https://msi.nga.mil/apology_objects/WPI.zip";
    public const string PortsPage = "https://msi.nga.mil/Publications/WPI";
    public const string PortsLicence = "Public domain (US government work, 17 U.S.C. 105)";
    public const string PortsAttribution = "US National Geospatial-Intelligence Agency (NGA), World Port Index (Pub 150), public domain.";
    public const string PortsFilter = "ports with a large or medium harbour (the World Port Index's Harbor Size)";
    public const string PortsCsvName = "UpdatedPub150.csv";

    /// <summary>OurAirports' types kept, with the type written to <c>data.csv</c>.</summary>
    private static readonly Dictionary<string, string> AirportTypes = new(StringComparer.Ordinal)
    {
        ["large_airport"] = "large",
        ["medium_airport"] = "medium",
    };

    private static readonly string[] AirportColumns = ["type", "name", "latitude_deg", "longitude_deg", "iso_country", "scheduled_service", "iata_code"];
    private static readonly string[] PortColumns = ["Main Port Name", "UN/LOCODE", "Country Code", "Harbor Size", "Latitude", "Longitude"];

    // ------------------------------------------------------------------------------------------------ airports

    /// <summary>
    /// OurAirports' <c>airports.csv</c>: <c>large_airport</c> and <c>medium_airport</c> rows with <c>scheduled_service</c> "yes".
    /// The file carries no date of its own (it is rebuilt daily), so the as-of is the day it was downloaded.
    /// </summary>
    /// <exception cref="InvalidDataException">The file lacks a column the extract needs (a format change).</exception>
    public static PointExtract Airports(SourceFile src, CountryCodes codes)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(codes);
        using var reader = new StreamReader(src.Path, System.Text.Encoding.UTF8);
        using var rows = CsvReader.Read(reader).GetEnumerator();
        if (!rows.MoveNext()) throw new InvalidDataException("OurAirports: the file is empty");
        var col = Columns(rows.Current, "OurAirports airports.csv", AirportColumns);

        var kept = new List<PointRow>();
        var notPlaced = new Dictionary<string, int>(StringComparer.Ordinal);
        var typeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        int total = 0, line = 1;
        while (rows.MoveNext())
        {
            line++;
            var r = rows.Current;
            total++;
            if (r.Length <= col.Values.Max()) throw new InvalidDataException($"OurAirports airports.csv line {line} is short");
            if (!AirportTypes.TryGetValue(r[col["type"]], out var type) || r[col["scheduled_service"]] != "yes") continue;
            var iso2 = r[col["iso_country"]].Trim();
            var (lat, lon) = Position(r[col["latitude_deg"]], r[col["longitude_deg"]], $"OurAirports airports.csv line {line}");
            if (codes.FromIso2(iso2) is not { } iso3)
            {
                Tally.Add(notPlaced, iso2.Length == 0 ? "(blank)" : iso2);
                continue;
            }
            Tally.Add(typeCounts, type);
            kept.Add(new PointRow(r[col["name"]].Trim(), iso3, lat, lon, null, "", "", type, Detail(r[col["iata_code"]].Trim())));
        }

        var published = src.Downloaded;   // no file date: the as-of is the download day
        var countries = kept.Select(k => k.Iso3).Distinct(StringComparer.Ordinal).Count();
        var unplaced = notPlaced.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new ManifestCode(kv.Key, kv.Key,
            string.Create(CultureInfo.InvariantCulture, $"no unit in the boundary set for this iso_country: {kv.Value} {(kv.Value == 1 ? "airport" : "airports")} not drawn"))).ToList();
        var notes = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture,
                $"Counts (checked {src.Downloaded:yyyy-MM-dd}): {kept.Count:N0} airports kept in {countries} countries (large {typeCounts.GetValueOrDefault("large"):N0}, "
                + $"medium {typeCounts.GetValueOrDefault("medium"):N0}) of {total:N0} rows in the file; {notPlaced.Values.Sum():N0} more met the filter but have no country in the boundary set."),
            "Licence: the repository's LICENSE begins \"This is free and unencumbered software released into the public domain\" (The Unlicense). "
                + "The data is community-edited; the file has no date of its own, so its as-of is the day it was downloaded.",
            "Filter: type large_airport or medium_airport, and scheduled_service \"yes\" (P4-5; small airstrips used by aid flights are not shown). "
                + "iso_country is ISO 3166 alpha-2, mapped to the boundary set's units (C16).",
        };
        var manifest = new DatasetManifest
        {
            Id = AirportsFolder,
            Publisher = AirportsPublisher,
            Dataset = AirportsDataset,
            SourceUrl = AirportsDownload,
            PageUrl = AirportsPage,
            Licence = AirportsLicence,
            LicenceUrl = AirportsLicenceUrl,
            Attribution = AirportsAttribution,
            DateMeaning = nameof(EarthRelief3D.Core.Model.CitationDate.AsOf),
            AsOf = $"OurAirports' airports.csv as downloaded on {src.Downloaded:yyyy-MM-dd} (rebuilt daily; the file has no date)",
            Published = published.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Downloaded = src.Downloaded.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            FileName = src.FileName,
            FileSha256 = src.Sha256(),
            Command = $"DataTool airports <downloads>/{src.FileName} --downloaded {src.Downloaded:yyyy-MM-dd}",
            Series = [new ManifestSeries(AirportsFolder, "Large and medium airports with scheduled passenger service", "airports", "airports.csv", countries)],
            NotPlaced = unplaced,
            Dropped = [],
            QualifiersSeen = Tally.Sorted(typeCounts),
            Notes = notes,
            Filter = AirportsFilter,
            Changes = "filtered to large and medium airports with scheduled service; name, position, type and IATA code kept; the country as the boundary set's unit",
        };
        return new PointExtract(manifest, kept.OrderBy(k => k.Iso3, StringComparer.Ordinal).ThenBy(k => k.Name, StringComparer.Ordinal).ToList(), [BundledPoints.DetailColumn]);
    }

    // ------------------------------------------------------------------------------------------------ ports

    /// <summary>
    /// The World Port Index: <c>Harbor Size</c> Large or Medium (a blank, Small or Very Small is left out). The country is the
    /// first two letters of the UN/LOCODE ("SD PZU" is Sudan, as ISO 3166 alpha-2); a port without a LOCODE falls back to the
    /// <c>Country Code</c> column, which holds the country's <em>name</em>.
    /// </summary>
    /// <param name="src">The <c>WPI.zip</c> (its <c>UpdatedPub150.csv</c> is read) or the CSV itself.</param>
    /// <param name="published">The CSV's own date; null: the zip entry's date (a CSV needs <see cref="SourceFile.Published"/>).</param>
    /// <param name="portalNote">What was seen on NGA's portal, in the licence note as given (P5-13); null: the note says the portal was not re-checked for this extract, with no date.</param>
    /// <exception cref="InvalidDataException">The file lacks a column the extract needs (a format change).</exception>
    public static PointExtract Ports(SourceFile src, CountryCodes codes, DateOnly? published = null, string? portalNote = null)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(codes);
        bool zipped = src.Path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        ZipArchive? zip = null;
        try
        {
            TextReader reader;
            DateOnly asOf;
            if (zipped)
            {
                zip = ZipFile.OpenRead(src.Path);
                var entry = zip.Entries.FirstOrDefault(e => string.Equals(e.Name, PortsCsvName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidDataException($"WPI: the zip has no {PortsCsvName}");
                asOf = published ?? DateOnly.FromDateTime(entry.LastWriteTime.DateTime);
                reader = new StreamReader(entry.Open(), new System.Text.UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
            }
            else
            {
                asOf = published ?? src.Published;
                reader = new StreamReader(src.Path, new System.Text.UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
            }
            using (reader) return ReadPorts(src, codes, reader, asOf, portalNote);
        }
        finally { zip?.Dispose(); }
    }

    private static PointExtract ReadPorts(SourceFile src, CountryCodes codes, TextReader reader, DateOnly asOf, string? portalNote)
    {
        using var rows = CsvReader.Read(reader).GetEnumerator();
        if (!rows.MoveNext()) throw new InvalidDataException("WPI: the file is empty");
        var col = Columns(rows.Current, "WPI UpdatedPub150.csv", PortColumns);
        var kept = new List<PointRow>();
        var notPlaced = new Dictionary<string, (string Name, int Count)>(StringComparer.Ordinal);
        var sizes = new Dictionary<string, int>(StringComparer.Ordinal);
        int total = 0, line = 1, byLocode = 0, byName = 0;
        while (rows.MoveNext())
        {
            line++;
            var r = rows.Current;
            total++;
            if (r.Length <= col.Values.Max()) throw new InvalidDataException($"WPI line {line} is short");
            var size = r[col["Harbor Size"]].Trim();
            Tally.Add(sizes, size.Length == 0 ? "(blank)" : size);
            string? type = size switch { "Large" => "large", "Medium" => "medium", _ => null };
            if (type is null) continue;
            var name = r[col["Main Port Name"]].Trim();
            var locode = r[col["UN/LOCODE"]].Trim();
            var country = r[col["Country Code"]].Trim();
            string? iso3 = null;
            if (locode.Length >= 2)
            {
                iso3 = codes.FromIso2(locode[..2]);
                if (iso3 is not null) byLocode++;
            }
            if (iso3 is null && country.Length > 0)
            {
                iso3 = codes.FromName(country);
                if (iso3 is not null) byName++;
            }
            if (iso3 is null)
            {
                var key = locode.Length >= 2 ? locode[..2].ToUpperInvariant() : country.Length > 0 ? country : "(blank)";
                var shown = country.Length > 0 ? country : key;
                notPlaced[key] = (shown, notPlaced.GetValueOrDefault(key).Count + 1);
                continue;
            }
            var (lat, lon) = Position(r[col["Latitude"]], r[col["Longitude"]], $"WPI line {line}");
            kept.Add(new PointRow(name, iso3, lat, lon, null, "", "", type, Detail(Locode(locode))));
        }

        var countries = kept.Select(k => k.Iso3).Distinct(StringComparer.Ordinal).Count();
        var unplaced = notPlaced.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new ManifestCode(kv.Key, kv.Value.Name,
            string.Create(CultureInfo.InvariantCulture,
                $"no unit in the boundary set for this country: {kv.Value.Count} {(kv.Value.Count == 1 ? "port" : "ports")} not drawn"))).ToList();
        var large = kept.Count(k => k.Type == "large");
        var medium = kept.Count - large;
        var notes = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture,
                $"Counts (checked {src.Downloaded:yyyy-MM-dd}): {kept.Count:N0} ports kept in {countries} countries (large {large:N0}, medium {medium:N0}) of {total:N0} rows in the file "
                + $"(Harbor Size: {string.Join(", ", sizes.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key} {kv.Value:N0}"))}); "
                + $"{notPlaced.Values.Sum(v => v.Count):N0} large or medium ports have no country in the boundary set."),
            string.Create(CultureInfo.InvariantCulture,
                $"Country: {byLocode:N0} ports by the first two letters of the UN/LOCODE (ISO 3166 alpha-2), {byName:N0} with no usable UN/LOCODE by the country name in the Country Code column."),
            (string.IsNullOrWhiteSpace(portalNote)
                ? "Licence: NGA's portal was not re-checked for this extract, so no statement was read there"
                : $"Licence: no statement could be read on NGA's portal ({portalNote.Trim()})")
                + "; a US federal government work (17 U.S.C. section 105), not confirmed from NGA's own page.",
            "The file is UTF-8 with a byte order mark. Its date is the zip entry's (the CSV inside WPI.zip); the portal states no edition number.",
        };
        var manifest = new DatasetManifest
        {
            Id = PortsFolder,
            Publisher = PortsPublisher,
            Dataset = PortsDataset,
            SourceUrl = PortsDownload,
            PageUrl = PortsPage,
            Licence = PortsLicence,
            Attribution = PortsAttribution,
            DateMeaning = nameof(EarthRelief3D.Core.Model.CitationDate.AsOf),
            AsOf = $"the World Port Index file dated {asOf:yyyy-MM-dd}",
            Published = asOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Downloaded = src.Downloaded.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            FileName = src.FileName,
            FileSha256 = src.Sha256(),
            Command = $"DataTool ports <downloads>/{src.FileName} --downloaded {src.Downloaded:yyyy-MM-dd}" + (string.IsNullOrWhiteSpace(portalNote) ? "" : $" --portal-note \"{portalNote.Trim()}\""),
            Series = [new ManifestSeries(PortsFolder, "Ports with a large or medium harbour", "ports", PortsCsvName, countries)],
            NotPlaced = unplaced,
            Dropped = [],
            QualifiersSeen = Tally.Sorted(sizes),
            Notes = notes,
            Filter = PortsFilter,
            Changes = "filtered to ports with a large or medium harbour; name, position, harbour size and UN/LOCODE kept; the country as the boundary set's unit",
        };
        return new PointExtract(manifest, kept.OrderBy(k => k.Iso3, StringComparer.Ordinal).ThenBy(k => k.Name, StringComparer.Ordinal).ToList(), [BundledPoints.DetailColumn]);
    }

    // ------------------------------------------------------------------------------------------------ shared

    /// <summary>The UN/LOCODE in its usual form, "SD PZU" (WPI writes some without the space, "NGBON"); anything else as written.</summary>
    public static string Locode(string text)
    {
        var t = text.Trim();
        return t.Length == 5 && t[2] != ' ' ? t[..2] + " " + t[2..] : t;
    }

    private static Dictionary<string, string> Detail(string text) => new(StringComparer.Ordinal) { [BundledPoints.DetailColumn] = text };

    private static Dictionary<string, int> Columns(string[] header, string source, string[] required)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < header.Length; i++) map.TryAdd(header[i].Trim().TrimStart('﻿'), i);
        foreach (var name in required)
            if (!map.ContainsKey(name)) throw new InvalidDataException($"{source} has no \"{name}\" column (a format change)");
        return map.Where(kv => required.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
    }

    private static (double Lat, double Lon) Position(string lat, string lon, string where) =>
        double.TryParse(lat.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var la)
        && double.TryParse(lon.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lo)
        && la is >= -90 and <= 90 && lo is >= -180 and <= 180
            ? (Math.Round(la, 6), Math.Round(lo, 6))
            : throw new InvalidDataException($"{where}: \"{lat}\", \"{lon}\" is not a position");
}
