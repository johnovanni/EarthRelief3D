using System.Collections.Immutable;
using EarthRelief3D.Core.Curated;

namespace EarthRelief3D.App.Curated;

/// <summary>
/// The curated tables as the user sees them: drafts only in a <c>--show-drafts</c> run (P3-4), and a government's
/// determinations only while that government is chosen in Settings (C15; all offered governments until Settings says
/// otherwise). Read on any thread; <see cref="Changed"/> is raised on the thread that changed the choice (the UI's).
/// </summary>
public sealed class CuratedStore
{
    private volatile State _state;

    private sealed record State(CuratedTables Tables, bool ShowDrafts, ImmutableHashSet<string> Governments,
        IReadOnlyList<Designation> Visible, IReadOnlyList<CampSite> VisibleCamps, IReadOnlyList<Depot> VisibleDepots,
        IReadOnlyList<Explainer> VisibleExplainers);

    /// <param name="showDrafts">Show the rows the owner has not approved yet, labelled (a diagnostic run only).</param>
    public CuratedStore(CuratedTables tables, bool showDrafts)
    {
        ArgumentNullException.ThrowIfNull(tables);
        _state = Make(tables, showDrafts, tables.OfferedGovernments.Select(g => g.Iso3));
    }

    /// <summary>No tables (tests, or a packaging fault that was logged): every curated layer is empty.</summary>
    public static CuratedStore Empty() => new(CuratedTables.Empty, showDrafts: false);

    public bool ShowDrafts => _state.ShowDrafts;

    public CuratedTables Tables => _state.Tables;

    /// <summary>The governments whose determinations are shown (always a subset of the offered ones).</summary>
    public IReadOnlySet<string> GovernmentsOn => _state.Governments;

    /// <summary>The designations the user sees, in file order.</summary>
    public IReadOnlyList<Designation> Visible => _state.Visible;

    /// <summary>The camp sites the user sees.</summary>
    public IReadOnlyList<CampSite> VisibleCamps => _state.VisibleCamps;

    /// <summary>The UN depots the user sees: approved ones, and drafts in a <c>--show-drafts</c> run (P4-13).</summary>
    public IReadOnlyList<Depot> VisibleDepots => _state.VisibleDepots;

    /// <summary>The explainers the user sees: approved ones, and drafts in a <c>--show-drafts</c> run (P4-13).</summary>
    public IReadOnlyList<Explainer> VisibleExplainers => _state.VisibleExplainers;

    /// <summary>The explainer <paramref name="id"/> if the user sees it (approved, or drafts shown); otherwise null.</summary>
    public Explainer? VisibleExplainer(string? id) =>
        id is null ? null : _state.VisibleExplainers.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.Ordinal));

    public event EventHandler? Changed;

    /// <summary>Chooses whose determinations are shown; names not offered (C15) are ignored.</summary>
    public void SetGovernments(IEnumerable<string> iso3)
    {
        ArgumentNullException.ThrowIfNull(iso3);
        var next = Make(_state.Tables, _state.ShowDrafts, iso3);
        if (next.Governments.SetEquals(_state.Governments)) return;
        _state = next;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Replaces the tables, keeping the chosen governments that are still offered.</summary>
    public void Replace(CuratedTables tables)
    {
        ArgumentNullException.ThrowIfNull(tables);
        _state = Make(tables, _state.ShowDrafts, _state.Governments);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Shows or hides the drafts (a <c>--snapshot-scenes</c> scene with <c>"showDrafts": true</c> turns them on for that
    /// picture only, as <c>--show-drafts</c> does for a whole run). Raises <see cref="Changed"/> when it changes anything.
    /// </summary>
    public void SetShowDrafts(bool showDrafts)
    {
        if (showDrafts == _state.ShowDrafts) return;
        _state = Make(_state.Tables, showDrafts, _state.Governments);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static State Make(CuratedTables tables, bool showDrafts, IEnumerable<string> governments)
    {
        var offered = tables.OfferedGovernments.Select(g => g.Iso3).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var on = governments.Where(offered.Contains).Select(g => g.ToUpperInvariant()).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        var filter = new CuratedFilter(showDrafts, on);
        return new State(tables, showDrafts, on, tables.VisibleDesignations(filter), tables.VisibleCamps(filter), tables.VisibleDepots(filter),
            tables.VisibleExplainers(filter));
    }
}
