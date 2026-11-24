using System.Text.Json;

namespace EarthRelief3D.Core.Help;

/// <summary>One Logistics Cluster country page: the country, the page's address, its title as the page gives it, and the day it was checked.</summary>
public sealed record LogisticsClusterLink(string Iso3, Uri Url, string Title, DateOnly CheckedOn);

/// <summary>
/// The Logistics Cluster's country pages (<c>data/help/logistics-cluster.json</c>, docs/data-sources.md section 19): links
/// only, one per country that has an operation page. The panel's Getting help in section shows the link for the open country.
/// Every row has an ISO3 code, an absolute https address, a title and a real date; a bad file is an
/// <see cref="InvalidDataException"/> naming each problem (a packaging fault the tests catch).
/// </summary>
public sealed class LogisticsClusterLinks
{
    public const string FileName = "logistics-cluster.json";

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    private readonly Dictionary<string, LogisticsClusterLink> _byIso3;

    public static LogisticsClusterLinks Empty { get; } = new([]);

    public LogisticsClusterLinks(IEnumerable<LogisticsClusterLink> links) =>
        _byIso3 = links.ToDictionary(l => l.Iso3, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<LogisticsClusterLink> All => _byIso3.Values;

    /// <summary>The country's Logistics Cluster page, or null when the cluster lists none.</summary>
    public LogisticsClusterLink? For(string iso3) => _byIso3.GetValueOrDefault(iso3);

    private sealed record Row(string? Iso3, string? Url, string? Title, DateOnly? CheckedOn);

    /// <summary>Reads <c>logistics-cluster.json</c> from <paramref name="helpDir"/> (the folder <c>data/help</c>).</summary>
    public static LogisticsClusterLinks Load(string helpDir)
    {
        var path = Path.Combine(helpDir, FileName);
        List<Row> rows;
        try { rows = JsonSerializer.Deserialize<List<Row>>(File.ReadAllText(path), Json) ?? throw new InvalidDataException($"{path} is empty"); }
        catch (JsonException ex) { throw new InvalidDataException($"{path}: {ex.Message}", ex); }

        var problems = new List<string>();
        var links = new List<LogisticsClusterLink>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            var id = r.Iso3 ?? "(no iso3)";
            if (r.Iso3 is not { Length: 3 } || !r.Iso3.All(char.IsAsciiLetterUpper)) problems.Add($"{id}: iso3 must be three capital letters");
            else if (!seen.Add(r.Iso3)) problems.Add($"{id}: listed twice");
            if (!Uri.TryCreate(r.Url, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps) problems.Add($"{id}: url must be an absolute https address");
            if (string.IsNullOrWhiteSpace(r.Title)) problems.Add($"{id}: title is missing");
            if (r.CheckedOn is not { } day || day.Year < 2000) problems.Add($"{id}: checkedOn is missing or not a real date");
            if (problems.Count == 0 || problems.All(p => !p.StartsWith(id + ":", StringComparison.Ordinal)))
                links.Add(new LogisticsClusterLink(r.Iso3!, url!, r.Title!, r.CheckedOn!.Value));
        }
        if (problems.Count > 0)
            throw new InvalidDataException($"{path}: {problems.Count} problem(s):" + string.Concat(problems.Select(p => "\n  " + p)));
        return new LogisticsClusterLinks(links);
    }
}
