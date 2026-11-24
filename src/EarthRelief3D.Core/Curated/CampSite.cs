using AstroCore.Earth;

namespace EarthRelief3D.Core.Curated;

/// <summary>A cited document: its title, who published it, where, and its own date (not a web page's upload date).</summary>
public sealed record DocumentCitation(string Title, string Publisher, Uri Url, DateOnly Date);

/// <summary>
/// A source's statement about a camp's status ("reported closed in 2006 (A/HRC/25/CRP.1, para. 739)", "parts remain
/// active"), quoted or closely paraphrased, with its citation. Shown as "&lt;ShortSource&gt; (&lt;year&gt;): &lt;Statement&gt;".
/// </summary>
/// <param name="ShortSource">The source's short name in labels ("UN Commission of Inquiry", "HRNK").</param>
public sealed record CampStatement(string Statement, string ShortSource, DocumentCitation Source)
{
    /// <summary>"UN Commission of Inquiry (2014): reported closed in 2006 (...)".</summary>
    public string Text => $"{ShortSource} ({Source.Date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)}): {Statement}";
}

/// <summary>
/// A documented prison camp (spec §10.2). Its name and description come from the UN Commission of Inquiry; its position
/// only from a coordinate or location map published in a cited report (<see cref="PositionSource"/>), never estimated.
/// </summary>
/// <param name="LocalName">The local name, if the sources give one ("Kwan-li-so No. 14"); null for none.</param>
/// <param name="DocumentedBy">The reports that document the camp, newest first.</param>
public sealed record CampSite(string Id, string Iso3, string Name, string? LocalName, GeoPosition Position, DocumentCitation PositionSource,
    string Description, IReadOnlyList<DocumentCitation> DocumentedBy, Review Review)
{
    /// <summary>When the row's facts were last checked in its sources.</summary>
    public DateOnly CheckedOn { get; init; }

    /// <summary>
    /// The cited statement that the camp has closed, from its newest cited report (ruling R14b: validation refuses a closure
    /// older than the newest report in <see cref="DocumentedBy"/>); null when the newest report does not say so. A closed camp
    /// has no marker and is no crisis: the panel lists it as history.
    /// </summary>
    public CampStatement? ReportedClosed { get; init; }

    /// <summary>
    /// Cited statements about the camp's status shown with it, oldest first, when sources differ (Camp 18: the COI reported it
    /// closed, HRNK's newer report says parts remain active). They do not change whether it has a marker.
    /// </summary>
    public IReadOnlyList<CampStatement> StatusNotes { get; init; } = [];

    /// <summary>A question for the owner's review (docs/curated-review.md); never shown in the app.</summary>
    public string? OwnerQuestion { get; init; }

    /// <summary>Not reported closed: drawn on the map and a crisis (C11, R14).</summary>
    public bool IsActive => ReportedClosed is null;

    /// <summary>What was checked, and where (for the owner's review); never shown in the app.</summary>
    public string? Verification { get; init; }

    public bool Equals(CampSite? o) => o is not null && Id == o.Id && Iso3 == o.Iso3 && Name == o.Name && LocalName == o.LocalName
        && Position.Equals(o.Position) && PositionSource == o.PositionSource && Description == o.Description
        && DocumentedBy.SequenceEqual(o.DocumentedBy) && Review == o.Review && CheckedOn == o.CheckedOn && Verification == o.Verification
        && ReportedClosed == o.ReportedClosed && StatusNotes.SequenceEqual(o.StatusNotes);

    public override int GetHashCode() => HashCode.Combine(Id, Iso3, Name, Position, DocumentedBy.Count);
}
