using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.App.Curated;

/// <summary>
/// The prison camps marker layer's points (a square each, <see cref="PointMarkerLayer"/>) from the camp sites the user
/// sees that are not reported closed (ruling R14), each at its published position with its name and its position's source
/// and date in the tooltip. The legend names the sources, the date the list was checked and links the newest report; with
/// nothing visible it says why.
/// </summary>
public static class CampMarkers
{
    public const string Publisher = "UN Commission of Inquiry on the DPRK and HRNK";
    /// <summary>The legend's note when no camp is shown (P3-4).</summary>
    public const string NoneApprovedNote = "None approved yet: each site is shown after the owner's review";
    /// <summary>The legend's note in a <c>--show-drafts</c> run.</summary>
    public const string DraftsNote = "Drafts shown: not yet reviewed (diagnostic run)";

    /// <summary>The layer's points, or null when there are no camp tables at all (the legend then says "Data not loaded yet", R2).</summary>
    public static PointSet? For(CuratedStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var all = store.Tables.Camps;
        if (all.Count == 0) return null;
        var camps = store.VisibleCamps.Where(c => c.IsActive).ToList();
        var items = camps.Select(c => new PointItem(c.Position, PointMarkerLayer.DefaultSizePx + 1, Tooltip(c), c.Iso3)).ToList();
        var at = all.Max(c => c.CheckedOn).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var newest = camps.SelectMany(c => c.DocumentedBy).OrderByDescending(r => r.Date).FirstOrDefault();
        var dataset = newest is null ? "curated camp sites" : $"curated camp sites, newest report {DesignationText.Date(newest.Date)}";
        // The newest report shown, or with none shown the newest report in the tables (every row has one).
        var url = (newest ?? all.SelectMany(c => c.DocumentedBy).OrderByDescending(r => r.Date).First()).Url;
        var citation = new SourceCitation(Publisher, dataset, null, url, "Facts with citations", Publisher, at, at);
        string? note = store.VisibleCamps.Count == 0 ? NoneApprovedNote
            : store.ShowDrafts && store.VisibleCamps.Any(c => !c.Review.IsApproved) ? DraftsNote : null;
        return new PointSet(items, citation, note);
    }

    /// <summary>"Political Prison Camp No. 14 (Kaechon) (Kwan-li-so No. 14) · position: HRNK, 22 Dec 2021"; drafts are marked first.</summary>
    /// <remarks>Status notes follow, each with its source and year ("UN Commission of Inquiry (2014): reported closed in 2006 (...)").</remarks>
    public static string Tooltip(CampSite c) =>
        (c.Review.IsApproved ? "" : DesignationText.DraftLabel + " · ")
        + $"{c.Name}{(c.LocalName is { } l ? $" ({l})" : "")} · position: {c.PositionSource.Publisher}, {DesignationText.Date(c.PositionSource.Date)}"
        + string.Concat(c.StatusNotes.Select(n => Environment.NewLine + n.Text));

    /// <summary>"reported closed (UN Commission of Inquiry, 2014)" (ruling R14b).</summary>
    public static string ClosedLabel(CampStatement closed) =>
        $"reported closed ({closed.ShortSource}, {closed.Source.Date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
}
