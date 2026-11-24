using AstroCore.Earth;

namespace EarthRelief3D.Core.Curated;

/// <summary>A cited document: its title, who published it, where, and its own date (not a web page's upload date).</summary>
public sealed record DocumentCitation(string Title, string Publisher, Uri Url, DateOnly Date);

/// <summary>A source's statement that a camp has closed (ruling R14), quoted or closely paraphrased, with its citation.</summary>
/// <param name="ShortSource">The source's short name in labels ("UN Commission of Inquiry").</param>
public sealed record CampClosure(string Statement, string ShortSource, DocumentCitation Source);

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
    /// The cited statement that the camp has closed; null when no source says so (ruling R14). A closed camp has no marker
    /// and is no crisis: the panel lists it as history.
    /// </summary>
    public CampClosure? ReportedClosed { get; init; }

    /// <summary>A question for the owner's review (docs/curated-review.md); never shown in the app.</summary>
    public string? OwnerQuestion { get; init; }

    /// <summary>Not reported closed: drawn on the map and a crisis (C11, R14).</summary>
    public bool IsActive => ReportedClosed is null;

    /// <summary>What was checked, and where (for the owner's review); never shown in the app.</summary>
    public string? Verification { get; init; }

    public bool Equals(CampSite? o) => o is not null && Id == o.Id && Iso3 == o.Iso3 && Name == o.Name && LocalName == o.LocalName
        && Position.Equals(o.Position) && PositionSource == o.PositionSource && Description == o.Description
        && DocumentedBy.SequenceEqual(o.DocumentedBy) && Review == o.Review && CheckedOn == o.CheckedOn && Verification == o.Verification
        && ReportedClosed == o.ReportedClosed;

    public override int GetHashCode() => HashCode.Combine(Id, Iso3, Name, Position, DocumentedBy.Count);
}
