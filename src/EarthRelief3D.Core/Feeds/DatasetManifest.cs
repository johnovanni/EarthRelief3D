using System.Text.Json;
using System.Text.Json.Serialization;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// A bundled extract's <c>manifest.json</c> (ruling P3-10), written by the DataTool next to <c>data.csv</c>: where the
/// figures come from, under what licence, the original file's SHA-256 and the command that made the extract, and what the
/// tool left out (aggregates, codes with no country) and saw (qualifiers, flags), with the checks made in <see cref="Notes"/>.
/// </summary>
public sealed record DatasetManifest
{
    /// <summary>The folder name under <c>data\datasets</c> ("fao-food-security").</summary>
    public required string Id { get; init; }
    public required string Publisher { get; init; }
    public required string Dataset { get; init; }
    public string? Version { get; init; }
    /// <summary>The file the maintainer downloads.</summary>
    public required string SourceUrl { get; init; }
    /// <summary>The source's page, the link the app shows.</summary>
    public required string PageUrl { get; init; }
    public required string Licence { get; init; }
    public required string Attribution { get; init; }
    /// <summary>A <see cref="Model.CitationDate"/> name: what a figure's year means ("EstimateYear").</summary>
    public required string DateMeaning { get; init; }
    /// <summary>What the figures are as of, in words (each country's latest year, and the range).</summary>
    public required string AsOf { get; init; }
    /// <summary>The source file's own date (its Last-Modified, yyyy-MM-dd): the publication date the attribution quotes, never the as-of.</summary>
    public required string Published { get; init; }
    /// <summary>When the maintainer downloaded the file (yyyy-MM-dd): the citation's "retrieved" date, never the as-of.</summary>
    public required string Downloaded { get; init; }
    public required string FileName { get; init; }
    public required string FileSha256 { get; init; }
    public required string Command { get; init; }
    public IReadOnlyList<ManifestSeries> Series { get; init; } = [];
    /// <summary>Codes with figures but no country in the boundary set: listed "Not placed on the map" by the app.</summary>
    public IReadOnlyList<ManifestCode> NotPlaced { get; init; } = [];
    /// <summary>Rows left out on purpose (aggregates, regions, former countries), each with the reason.</summary>
    public IReadOnlyList<ManifestCode> Dropped { get; init; } = [];
    /// <summary>The source's value qualifiers seen ("&lt;2.5"), or WRI's (cat, label) pairs, with how often.</summary>
    public IReadOnlyDictionary<string, int> QualifiersSeen { get; init; } = new Dictionary<string, int>();
    /// <summary>The source's flags seen (FAO's E, O; EIA's frequency and "--" marker), with how often.</summary>
    public IReadOnlyDictionary<string, int> FlagsSeen { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<string> Notes { get; init; } = [];

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // keep "<2.5" and "Côte" readable
    };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    /// <exception cref="InvalidDataException">The text is not a manifest.</exception>
    public static DatasetManifest Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DatasetManifest>(json, Options) ?? throw new InvalidDataException("the manifest is empty");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("the manifest is not valid: " + ex.Message, ex);
        }
    }
}

/// <summary>One series in an extract: its store id, what it measures (the legend's words, without the year) and its unit.</summary>
/// <param name="SourceItem">The source's own name for it (FAO's item, EIA's series, WRI's indicator and weight).</param>
/// <param name="Countries">How many countries have a row.</param>
public sealed record ManifestSeries(string Id, string Measures, string Unit, string SourceItem, int Countries);

/// <summary>A source code that is not in the extract, with the source's name for it and why.</summary>
public sealed record ManifestCode(string Code, string Name, string Reason);
