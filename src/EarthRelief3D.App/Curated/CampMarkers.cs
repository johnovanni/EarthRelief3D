using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.App.Curated;

/// <summary>
/// The prison camps marker layer's points (a square each, <see cref="PointMarkerLayer"/>) from the camp sites the user
/// sees, each at its published position with its name, its position's source and date in the tooltip. The legend names the
/// sources and the date the list was checked; with nothing visible it says why.
/// </summary>
public static class CampMarkers
{
    public const string Publisher = "UN Commission of Inquiry on the DPRK and HRNK";
    /// <summary>The legend's note when no camp is shown (P3-4).</summary>
    public const string NoneApprovedNote = "None approved yet: each site is shown after the owner's review";
    /// <summary>The legend's note in a <c>--show-drafts</c> run.</summary>
    public const string DraftsNote = "Drafts shown: not yet reviewed (diagnostic run)";
    /// <summary>The UN Commission of Inquiry's report on the DPRK, A/HRC/25/63 (7 Feb 2014), on the UN's document system.</summary>
    public static readonly Uri CoiReport = new("https://documents.un.org/doc/undoc/gen/g14/108/66/pdf/g1410866.pdf");

    public static PointSet For(CuratedStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var camps = store.VisibleCamps;
        var items = camps.Select(c => new PointItem(c.Position, PointMarkerLayer.DefaultSizePx + 1, Tooltip(c), c.Iso3)).ToList();
        var all = store.Tables.Camps;
        var checkedOn = all.Count == 0 ? DateOnly.FromDateTime(DateTime.UtcNow) : all.Max(c => c.CheckedOn);
        var newest = camps.SelectMany(c => c.DocumentedBy).Select(r => r.Date).DefaultIfEmpty().Max();
        var at = checkedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dataset = newest == default ? "curated camp sites" : $"curated camp sites, newest report {DesignationText.Date(newest)}";
        var url = camps.FirstOrDefault()?.DocumentedBy.LastOrDefault()?.Url ?? CoiReport;
        var citation = new SourceCitation(Publisher, dataset, null, url, "Facts with citations", Publisher, at, at);
        string? note = camps.Count == 0 ? NoneApprovedNote : store.ShowDrafts && camps.Any(c => !c.Review.IsApproved) ? DraftsNote : null;
        return new PointSet(items, citation, note);
    }

    /// <summary>"Kwan-li-so No. 14 (Kaechon) · political prison camp · position: HRNK, 22 Dec 2021"; drafts are marked first.</summary>
    public static string Tooltip(CampSite c) =>
        (c.Review.IsApproved ? "" : DesignationText.DraftLabel + " · ")
        + $"{c.Name}{(c.LocalName is { } l ? $" ({l})" : "")} · position: {c.PositionSource.Publisher}, {DesignationText.Date(c.PositionSource.Date)}";
}
