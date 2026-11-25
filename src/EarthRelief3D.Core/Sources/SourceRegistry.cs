using System.Text.Json;
using System.Text.Json.Serialization;

namespace EarthRelief3D.Core.Sources;

/// <summary>
/// The one list of data sources (<c>data/sources.json</c>, Plan 5 Task 1) that the About card, the Data card and
/// <c>THIRD-PARTY-NOTICES.txt</c> are built from. A bad file is an <see cref="InvalidDataException"/> naming every problem (a packaging
/// fault the tests catch).
/// </summary>
public sealed record SourceRegistry(IReadOnlyList<SourceInfo> Sources)
{
    public const string FileName = "sources.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Reads and validates <c>&lt;dataDir&gt;\sources.json</c>.</summary>
    public static SourceRegistry Load(string dataDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDir);
        var path = Path.Combine(dataDir, FileName);
        SourceRegistry registry;
        try
        {
            var rows = JsonSerializer.Deserialize<List<SourceInfo>>(File.ReadAllText(path), Json) ?? throw new InvalidDataException($"{path} is empty");
            registry = new SourceRegistry(rows);
        }
        catch (JsonException ex) { throw new InvalidDataException($"{path}: {ex.Message}", ex); }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException) { throw new InvalidDataException($"{path} is missing", ex); }
        var problems = Validate(registry);
        if (problems.Count > 0)
            throw new InvalidDataException($"{path}: {problems.Count} problem(s) in the source list:{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
        return registry;
    }

    /// <summary>Every problem, one line each naming the row; empty when the list is sound.</summary>
    public static IReadOnlyList<string> Validate(SourceRegistry r)
    {
        ArgumentNullException.ThrowIfNull(r);
        var problems = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < r.Sources.Count; i++)
        {
            var s = r.Sources[i];
            var who = string.IsNullOrWhiteSpace(s.Id) ? $"row {i + 1}" : s.Id;
            if (string.IsNullOrWhiteSpace(s.Id)) problems.Add($"{who}: the id is empty");
            else if (!seen.Add(s.Id)) problems.Add($"{who}: the id appears more than once");
            if (string.IsNullOrWhiteSpace(s.Name)) problems.Add($"{who}: the name is empty");
            if (string.IsNullOrWhiteSpace(s.What)) problems.Add($"{who}: \"what\" is empty");
            if (string.IsNullOrWhiteSpace(s.Licence)) problems.Add($"{who}: the licence is empty");
            if (string.IsNullOrWhiteSpace(s.Credit)) problems.Add($"{who}: the credit is empty");
            if (s.Page is null || !s.Page.IsAbsoluteUri || s.Page.Scheme != Uri.UriSchemeHttps) problems.Add($"{who}: the page is not an https link");
            if (s.LicenceUrl is { } u && (!u.IsAbsoluteUri || u.Scheme != Uri.UriSchemeHttps)) problems.Add($"{who}: the licence link is not an https link");
            if (s.CheckedOn == default) problems.Add($"{who}: the checked-on date is missing");
            if (s.StatusDots is null || s.DatasetFolders is null) problems.Add($"{who}: the status dots or dataset folders list is missing");
        }
        return problems;
    }

    /// <summary>
    /// The pieces of a credit that must be in docs/data-sources.md word for word: the text either side of each <c>{date}</c>
    /// ("Financial Tracking Service (FTS), " and ", https://fts.unocha.org"); a credit with no date is one piece.
    /// </summary>
    public static IReadOnlyList<string> CreditFragments(string credit)
    {
        ArgumentNullException.ThrowIfNull(credit);
        return [.. credit.Split(SourceInfo.DatePlaceholder).Where(f => f.Length > 0)];
    }

    /// <summary>The sources that feed the status dot <paramref name="dotName"/> (its name as in <c>AppServices.StatusDotNames</c>).</summary>
    public IReadOnlyList<SourceInfo> ForDot(string dotName) => [.. Sources.Where(s => s.StatusDots.Contains(dotName, StringComparer.Ordinal))];
}
