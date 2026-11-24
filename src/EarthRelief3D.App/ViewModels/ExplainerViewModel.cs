using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.App.Curated;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Help;

namespace EarthRelief3D.App.ViewModels;

/// <summary>
/// The explainer card (Plan 4 Task 9; spec §11): opened from a panel section's "What is this, and why does it happen?" link
/// (the step section's "Why these?"), it shows one explainer's title, paragraphs and sources (each a link opened in the
/// browser), and whether the owner has reviewed it. Only an explainer the user sees opens: approved, or a draft in a
/// <c>--show-drafts</c> run (P4-13). When drafts are turned off while a draft is open, the card closes.
/// </summary>
public sealed partial class ExplainerViewModel : ObservableObject
{
    private static readonly IReadOnlyList<string> NoParagraphs = [];
    private static readonly IReadOnlyList<PanelLink> NoSources = [];
    private readonly CuratedStore? _curated;

    [ObservableProperty] private bool _isOpen;
    /// <summary>The open explainer's id ("food"), or empty.</summary>
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private IReadOnlyList<string> _paragraphs = NoParagraphs;
    /// <summary>One link per source: "&lt;title&gt; (&lt;publisher&gt;, &lt;date&gt;)".</summary>
    [ObservableProperty] private IReadOnlyList<PanelLink> _sources = NoSources;
    /// <summary>"Reviewed 24 Nov 2026" for an approved explainer; <see cref="DesignationText.DraftLabel"/> for a draft.</summary>
    [ObservableProperty] private string _reviewedText = "";

    /// <param name="curated">The curated tables as the user sees them; null: no explainers (nothing opens).</param>
    public ExplainerViewModel(CuratedStore? curated)
    {
        _curated = curated;
        if (_curated is not null) _curated.Changed += OnCuratedChanged;
    }

    /// <summary>Opens the explainer <paramref name="id"/>; false (and nothing changes) when the user does not see it.</summary>
    public bool Open(string? id)
    {
        if (_curated?.VisibleExplainer(id) is not { } e) return false;
        Show(e);
        IsOpen = true;
        return true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        Id = "";
        Title = "";
        Paragraphs = NoParagraphs;
        Sources = NoSources;
        ReviewedText = "";
    }

    /// <summary>"Definitions of Genocide and Related Crimes (United Nations, 24 Nov 2026)".</summary>
    public static string SourceText(DocumentCitation c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return $"{c.Title} ({c.Publisher}, {DesignationText.Date(c.Date)})";
    }

    /// <summary>"Reviewed 24 Nov 2026", or "Draft: not yet reviewed".</summary>
    public static string ReviewText(Explainer e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return e.Review.IsApproved ? "Reviewed " + DesignationText.Date(e.ReviewedOn) : DesignationText.DraftLabel;
    }

    private void Show(Explainer e)
    {
        Id = e.Id;
        Title = e.Title;
        Paragraphs = e.Paragraphs;
        Sources = [.. e.Sources.Select(s => new PanelLink(SourceText(s), s.Url))];
        ReviewedText = ReviewText(e);
    }

    // The tables or the draft setting changed: follow the open explainer, or close the card when the user no longer sees it.
    private void OnCuratedChanged(object? sender, EventArgs e)
    {
        if (!IsOpen) return;
        if (_curated?.VisibleExplainer(Id) is { } current) Show(current);
        else Close();
    }
}
