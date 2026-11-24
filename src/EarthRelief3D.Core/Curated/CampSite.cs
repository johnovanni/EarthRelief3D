using AstroCore.Earth;

namespace EarthRelief3D.Core.Curated;

/// <summary>A cited document: its title, who published it, where, and its own date (not a web page's upload date).</summary>
public sealed record DocumentCitation(string Title, string Publisher, Uri Url, DateOnly Date);

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

    /// <summary>What was checked, and where (for the owner's review); never shown in the app.</summary>
    public string? Verification { get; init; }

    public bool Equals(CampSite? o) => o is not null && Id == o.Id && Iso3 == o.Iso3 && Name == o.Name && LocalName == o.LocalName
        && Position.Equals(o.Position) && PositionSource == o.PositionSource && Description == o.Description
        && DocumentedBy.SequenceEqual(o.DocumentedBy) && Review == o.Review && CheckedOn == o.CheckedOn && Verification == o.Verification;

    public override int GetHashCode() => HashCode.Combine(Id, Iso3, Name, Position, DocumentedBy.Count);
}
