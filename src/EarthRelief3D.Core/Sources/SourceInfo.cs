namespace EarthRelief3D.Core.Sources;

/// <summary>How the app uses a source: fetched at run time, bundled with the app, written by hand from cited pages, or only linked to.</summary>
public enum SourceUse { Live, Bundled, Curated, LinksOnly }

/// <summary>
/// One data source, as the About card, the Data card and the notices file all say it (Plan 5, Task 1; <c>data/sources.json</c>).
/// <see cref="Credit"/> is the wording from <c>docs/data-sources.md</c>; <c>{date}</c> in it stands for the source's own date.
/// </summary>
/// <param name="Id">A short stable id such as <c>gem-gipt</c>.</param>
/// <param name="Name">What people call the source.</param>
/// <param name="What">Plain words: what the app takes from it.</param>
/// <param name="Licence">The licence as the source states it, in words.</param>
/// <param name="LicenceUrl">Where the licence is read; null when the source gives none.</param>
/// <param name="Credit">The credit text, with <c>{date}</c> where a date belongs.</param>
/// <param name="Page">The source's page, opened in the user's browser.</param>
/// <param name="StatusDots">The status dots (<c>AppServices.StatusDotNames</c>) this source feeds.</param>
/// <param name="DatasetFolders">The folders under <c>data/datasets</c> that hold its bundled extracts.</param>
/// <param name="CheckedOn">The day its section of docs/data-sources.md was checked.</param>
public sealed record SourceInfo(string Id, string Name, string What, SourceUse Use, string Licence, Uri? LicenceUrl, string Credit, Uri Page,
    IReadOnlyList<string> StatusDots, IReadOnlyList<string> DatasetFolders, DateOnly CheckedOn)
{
    public const string DatePlaceholder = "{date}";

    /// <summary>
    /// The credit with <c>{date}</c> replaced by <paramref name="dateText"/>. With no date the placeholder goes, together with
    /// the comma or the brackets that set it off ("FTS, {date}, url" becomes "FTS, url"; ", analysis {date}" and a bracketed " ({date})" are dropped whole).
    /// </summary>
    public string CreditFor(string? dateText)
    {
        if (!Credit.Contains(DatePlaceholder, StringComparison.Ordinal)) return Credit;
        if (!string.IsNullOrWhiteSpace(dateText)) return Credit.Replace(DatePlaceholder, dateText.Trim(), StringComparison.Ordinal);
        return Credit.Replace(" (" + DatePlaceholder + ")", "", StringComparison.Ordinal)
            .Replace(", analysis " + DatePlaceholder, "", StringComparison.Ordinal)
            .Replace(", " + DatePlaceholder, "", StringComparison.Ordinal)
            .Replace(DatePlaceholder, "", StringComparison.Ordinal)
            .Trim();
    }
}
