using EarthRelief3D.Core.Curated;

namespace EarthRelief3D.Core.Help;

/// <summary>
/// An OCHA country-based pooled fund, or CERF (<c>data/help/pooled-funds.json</c>; P4-11, P4-13): the countries it covers
/// (a regional fund's are its sub-funds' in OCHA's CBPF API; CERF names none: it is the line for every other country),
/// its OCHA page (the row's citation), the page that says how to give and what that page says (<paramref name="GiveNote"/>).
/// The app never takes a gift: it links. A draft until the owner approves it.
/// </summary>
/// <param name="Id">"sudan-humanitarian-fund", or "cerf".</param>
/// <param name="Iso3">The countries the fund covers; empty for CERF.</param>
/// <param name="Page">The fund's page on OCHA's site (CERF: cerf.un.org); the row's citation.</param>
/// <param name="GiveUrl">Where to read how to give (OCHA's "Our funding"; CERF: its donate page).</param>
/// <param name="CheckedOn">The day the page was read.</param>
public sealed record PooledFund(string Id, string Name, IReadOnlyList<string> Iso3, Uri Page, Uri GiveUrl, string GiveNote, Review Review, DateOnly CheckedOn);
