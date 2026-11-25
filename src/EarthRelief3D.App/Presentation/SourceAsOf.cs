using EarthRelief3D.App.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Help;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Sources;
using EarthRelief3D.Core.Store;
using GlobeCore.Regions;

namespace EarthRelief3D.App.Presentation;

/// <summary>
/// What the "as of" text of every source reads from (Plan 5 Task 4): the crisis snapshot (its data and the last successful check of
/// each feed), the country-value series, the bundled extracts' citations by folder name, the curated and help tables, and the
/// boundary set in use.
/// </summary>
/// <param name="Datasets">The newest citation of each bundled extract that loaded, by its folder under <c>data/datasets</c>; a folder that did not load is not in it.</param>
/// <param name="Borders">The boundary set in use; null while none is loaded.</param>
public sealed record SourceAsOfInputs(CrisisSnapshot Crises, IndicatorSnapshot Indicators, IReadOnlyDictionary<string, SourceCitation> Datasets,
    CuratedStore Curated, HelpTables Help, BoundarySet? Borders);

/// <summary>
/// One function for the date of a source, shared by the legend (its crisis lines: <see cref="LegendDate"/>) and the Data card
/// (<see cref="For"/>), so the two never word the same date differently. It never shows a blank, and never a cached date as if it
/// were current: a feed's data date always comes with the day it was last checked, or "not yet checked".
/// </summary>
public static class SourceAsOf
{
    /// <summary>A bundled extract, table or list that did not load.</summary>
    public const string NotLoaded = LayerCatalog.NotLoaded;

    /// <summary>
    /// The legend's date text for a crisis source: the data's date; or, with no data, "none recorded · checked &lt;date&gt;" (WHO: "no
    /// reports in the last 90 days · checked &lt;date&gt;"); or "not yet checked".
    /// </summary>
    public static string LegendDate(SourceSummary s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return s.DateText ?? (s.CheckedUtc is { } t ? SourceDates.NothingFound(s.ShortName) + " · " + SourceDates.Checked(t) : SourceDates.NotChecked);
    }

    /// <summary>The Data card's "As of" for <paramref name="s"/> at <paramref name="nowUtc"/>.</summary>
    public static string For(SourceInfo s, SourceAsOfInputs i, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(i);
        return s.Use switch
        {
            SourceUse.Live => LiveText(s, i, nowUtc),
            SourceUse.Bundled => BundledText(s, i, nowUtc),
            SourceUse.Curated => CuratedText(s, i),
            _ => "links only; checked " + CrisisText.Date(s.CheckedOn),
        };
    }

    /// <summary>
    /// The date that goes where the source's credit has <c>{date}</c> (<see cref="SourceInfo.CreditFor"/>): the date alone, without
    /// "as of" ("FTS, 20 Nov 2026, https://..."), worded as the "As of" column words it (a date not yet reached reads "retrieved ..."); null
    /// when the source has no data yet, so the credit drops the placeholder.
    /// </summary>
    public static string? CreditDate(SourceInfo s, SourceAsOfInputs i, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(i);
        switch (s.Id)
        {
            case "ipc-areas":
                return Newest(i.Crises.IpcAreas)?.AnalysisDate;
            case "acaps":
                return NewestCitation(AccessCitations(i.Indicators)) is { } a ? Plain(CrisisText.DataDate(a, nowUtc)) : null;
            case "fts":
                return NewestCitation(i.Indicators.Appeals.Select(x => x.Citation)) is { } f ? Plain(CrisisText.DataDate(f, nowUtc)) : null;
            case "eia":
                // The credit names the month the figures were published; the extract's download is that month (its manifest).
                return DatasetCitation(s, i) is { } e ? e.RetrievedUtc.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture) : null;
            case "us-senate":
                return i.Help.Senators.Count > 0 && i.Help.SenatorsAsOf != default ? CrisisText.Date(i.Help.SenatorsAsOf) : null;
            default:
                return null;
        }
    }

    private static string Plain(string dataDate) => dataDate.StartsWith("as of ", StringComparison.Ordinal) ? dataDate["as of ".Length..] : dataDate;

    // ---------------------------------------------------------------- fetched by the app

    private static string LiveText(SourceInfo s, SourceAsOfInputs i, DateTime now)
    {
        var snapshot = i.Crises.AsSeenAt(now);   // WHO's 90 days and IPC's 12 months judged now, even for cached data
        if (LayerOf(s.Id) is { } layer)
        {
            var summary = SourceDates.For(layer, snapshot, now);
            return summary.DateText is null ? LegendDate(summary) : WithCheck(summary.DateText, summary.CheckedUtc);
        }
        DateTime? checkedAt = s.StatusDots.Count > 0 && snapshot.CheckedUtc.TryGetValue(s.StatusDots[0], out var t) ? t : null;
        var data = LiveData(s, i, snapshot, now);
        if (data is not null) return WithCheck(data, checkedAt);
        return checkedAt is { } c ? (s.Id == "ipc-areas" ? "no area analyses loaded" : "no figures") + " · " + SourceDates.Checked(c) : SourceDates.NotChecked;
    }

    private static CrisisLayer? LayerOf(string id) => id switch
    {
        "ucdp" => CrisisLayer.Conflict,
        "ipc" => CrisisLayer.Food,
        "who-don" => CrisisLayer.Outbreaks,
        "unhcr" => CrisisLayer.Displacement,
        _ => null,
    };

    private static string? LiveData(SourceInfo s, SourceAsOfInputs i, CrisisSnapshot snapshot, DateTime now)
    {
        switch (s.Id)
        {
            case "ipc-areas":
                // Each set carries IPC's own "Date of analysis" ("Sep 2026"), as the IPC line does.
                return Newest(snapshot.IpcAreas) is { } set ? set.AnalysisDate is { Length: > 0 } d ? "as of " + d : "analysis date not given" : null;
            case "who-gho":
                return SeriesText(i.Indicators.Series.Where(x => x.Key.StartsWith("gho.", StringComparison.Ordinal)).SelectMany(x => x.Value.Values).Select(v => v.Citation), now);
            case "ocha-hno":
                return SeriesText(i.Indicators.Get(HnoFeed.ShelterSeriesId)?.Values.Select(v => v.Citation) ?? [], now);
            case "acaps":
                return SeriesText(AccessCitations(i.Indicators), now);
            case "fts":
                return SeriesText(i.Indicators.Appeals.Select(x => x.Citation), now);
            default:
                return null;
        }
    }

    private static IEnumerable<SourceCitation> AccessCitations(IndicatorSnapshot ind) =>
        ind.Access.Select(a => a.Citation).Concat(ind.Get(AcapsAccessFeed.ScoreSeriesId)?.Values.Select(v => v.Citation) ?? []);

    private static string? SeriesText(IEnumerable<SourceCitation> citations, DateTime now) =>
        NewestCitation(citations) is { } c ? CrisisText.SeriesDate(c, now) : null;

    private static string WithCheck(string dataDate, DateTime? checkedUtc) =>
        dataDate + " · " + (checkedUtc is { } t ? SourceDates.Checked(t) : SourceDates.NotChecked);

    private static IpcAreaSet? Newest(IReadOnlyList<IpcAreaSet> sets) =>
        sets.Count == 0 ? null : sets.MaxBy(a => SourceDates.AnalysisDate(a.AnalysisDate));

    private static SourceCitation? NewestCitation(IEnumerable<SourceCitation> citations) => citations.MaxBy(c => c.AsOfUtc);

    // ---------------------------------------------------------------- bundled with the app

    private static string BundledText(SourceInfo s, SourceAsOfInputs i, DateTime now)
    {
        switch (s.Id)
        {
            case "natural-earth":
                return i.Borders?.Edition is { Length: > 0 } edition ? edition : NotLoaded;
            case "us-senate":
                // Loaded with the user's country (the United States' list): without it the list is not loaded.
                return i.Help.Senators.Count > 0 && i.Help.SenatorsAsOf != default ? CrisisText.AsOf(i.Help.SenatorsAsOf.ToDateTime(TimeOnly.MinValue)) : NotLoaded;
            case "blue-marble":
                return "July 2004 image";   // the image's own date, in its credit (docs/data-sources.md)
        }
        return DatasetCitation(s, i) is { } c ? CrisisText.SeriesDate(c, now) : NotLoaded;
    }

    /// <summary>The newest citation among the extracts in the source's dataset folders; null when none of them loaded.</summary>
    private static SourceCitation? DatasetCitation(SourceInfo s, SourceAsOfInputs i) =>
        NewestCitation(s.DatasetFolders.Select(f => i.Datasets.TryGetValue(f, out var c) ? c : null).OfType<SourceCitation>());

    // ---------------------------------------------------------------- written for the app

    private const string EachEntry = "each entry gives its own date; ";

    private static string CuratedText(SourceInfo s, SourceAsOfInputs i)
    {
        var (rows, newest) = s.Id switch
        {
            "ocha-pooled-funds" => Reviews(i.Help.Funds.Select(f => f.Review)),
            "unhrd" => Reviews(i.Curated.Tables.Depots.Select(d => d.Review)),
            _ => Reviews(i.Curated.Tables.Designations.Select(d => d.Review)
                .Concat(i.Curated.Tables.Camps.Select(c => c.Review)).Concat(i.Curated.Tables.Explainers.Select(e => e.Review))),
        };
        if (rows == 0) return NotLoaded;
        return EachEntry + (newest is { } day ? "last reviewed " + CrisisText.Date(day) : "none reviewed yet");
    }

    private static (int Rows, DateOnly? Newest) Reviews(IEnumerable<Core.Curated.Review> reviews)
    {
        int n = 0;
        DateOnly? newest = null;
        foreach (var r in reviews)
        {
            n++;
            if (r.IsApproved && r.ApprovedOn is { } d && (newest is null || d > newest)) newest = d;
        }
        return (n, newest);
    }
}
