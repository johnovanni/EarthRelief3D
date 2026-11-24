using System.Text.RegularExpressions;
using GlobeCore.Geo;

namespace EarthRelief3D.Core.Curated;

/// <summary>
/// A short plain-language explainer (spec §1 "Teaching", §4, §11): what a kind of crisis is and why it happens, opened from
/// the country panel's "What is this, and why does it happen?" link ("Why these?" for the step section). Drafted from the
/// authority's own pages, paraphrased, never copied; a draft until the owner approves it (P4-13).
/// </summary>
/// <param name="Id">One of <see cref="Ids"/>.</param>
/// <param name="Paragraphs">The text, <see cref="MaxWords"/> words or fewer in all, with no quotation longer than <see cref="MaxQuoteWords"/> words.</param>
/// <param name="Sources">At least two pages on the authority's own site, each dated (its own date, or the day it was opened when it shows none).</param>
/// <param name="ReviewedOn">The day the text was last checked against its sources.</param>
public sealed record Explainer(string Id, string Title, IReadOnlyList<string> Paragraphs, IReadOnlyList<DocumentCitation> Sources, Review Review,
    DateOnly ReviewedOn)
{
    /// <summary>The eight explainers: one per crisis kind, then access ("Getting help in") and "why these" (the step section).</summary>
    public static IReadOnlyList<string> Ids { get; } = ["conflict", "atrocities", "camps", "food", "outbreaks", "displacement", "access", "why-these"];

    public const int MaxWords = 300;
    public const int MaxQuoteWords = 15;
    public const int MinSources = 2;

    /// <summary>What was opened, where and when, for the owner's review (docs/curated-review.md); never shown in the app.</summary>
    public string? Verification { get; init; }

    /// <summary>The words in all the paragraphs.</summary>
    public int WordCount => Paragraphs?.Sum(p => Words(p ?? "")) ?? 0;

    /// <summary>Words separated by white space ("non-state" is one word).</summary>
    public static int Words(string text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Count(w => w.Any(char.IsLetterOrDigit));

    private static readonly Regex QuoteRx = new("“([^”]*)”|\"([^\"]*)\"", RegexOptions.CultureInvariant);

    /// <summary>The text inside each pair of quotation marks (straight or curly).</summary>
    public static IReadOnlyList<string> Quotations(string text) =>
        [.. QuoteRx.Matches(text ?? "").Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)];

    public bool Equals(Explainer? o) => o is not null && Id == o.Id && Title == o.Title && ListEquality.Equal(Paragraphs, o.Paragraphs)
        && ListEquality.Equal(Sources, o.Sources) && Review == o.Review && ReviewedOn == o.ReviewedOn && Verification == o.Verification;

    public override int GetHashCode() => HashCode.Combine(Id, Title, ReviewedOn, Review);
}
