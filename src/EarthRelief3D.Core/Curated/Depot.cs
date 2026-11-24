using AstroCore.Earth;

namespace EarthRelief3D.Core.Curated;

/// <summary>
/// A UN Humanitarian Response Depot (UNHRD, run by the World Food Programme; Plan 4, P4-5): one of the network's five hubs,
/// at the address UNHRD's contacts page gives (<see cref="Address"/>, as the page writes it: a postal address, never a
/// person). UNHRD publishes no coordinates, so the position is that of the airport the depot's address names or sits beside (ruling R21:
/// <see cref="PositionSource"/>, the bundled OurAirports data) and <see cref="Verification"/> says which airport and why, for
/// the owner's review.
/// </summary>
/// <param name="Name">"UNHRD Accra".</param>
/// <param name="City">"Accra".</param>
/// <param name="Address">The postal address as UNHRD's contacts page writes it.</param>
/// <param name="PositionSource">Where the point came from: "OurAirports: &lt;airport&gt; (&lt;ICAO&gt;)", the extract's as-of date.</param>
/// <param name="Source">The page that gives the depot and its address (UNHRD's contacts page, checked on the row's date).</param>
public sealed record Depot(string Id, string Name, string City, string Iso3, string Address, GeoPosition Position, DocumentCitation PositionSource,
    DocumentCitation Source, Review Review, DateOnly CheckedOn)
{
    /// <summary>What was checked, and where, including what the position matched (for the owner's review); never shown in the app.</summary>
    public string? Verification { get; init; }

    public bool Equals(Depot? o) => o is not null && Id == o.Id && Name == o.Name && City == o.City && Iso3 == o.Iso3 && Address == o.Address
        && Position.Equals(o.Position) && PositionSource == o.PositionSource && Source == o.Source && Review == o.Review
        && CheckedOn == o.CheckedOn && Verification == o.Verification;

    public override int GetHashCode() => HashCode.Combine(Id, Iso3, Name, Position);
}
