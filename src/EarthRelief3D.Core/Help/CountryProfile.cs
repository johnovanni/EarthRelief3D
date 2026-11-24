using EarthRelief3D.Core.Curated;

namespace EarthRelief3D.Core.Help;

/// <summary>
/// An evaluator of charities that a country's profile accepts (P4-8): its name, what a charity must hold to qualify ("3 stars
/// or more"), and its site. The app shows "Rated by &lt;name&gt;" with a link to the evaluator's report, never the rating.
/// </summary>
public sealed record Evaluator(string Name, string Qualifies, Uri Site);

/// <summary>The kinds of official contact a profile lists: they decide how the panel writes the line.</summary>
public enum ContactKind
{
    /// <summary>The user's two senators (needs the state chosen in Settings; the senators come from senators.json).</summary>
    Senators,
    /// <summary>A page that finds the user's representative (the House's own finder: the app never asks for a ZIP code).</summary>
    Representative,
    /// <summary>One official page.</summary>
    Link,
    /// <summary>The Blue Book page of missions to the UN (P4-10): "&lt;country&gt;'s mission to the UN: UN Blue Book".</summary>
    UnMission,
}

/// <summary>
/// One official the user may contact (P4-13): a draft until the owner approves it. <paramref name="Url"/> is null for
/// <see cref="ContactKind.Senators"/> (the list is senators.json); <paramref name="Source"/> is the page the row rests on.
/// </summary>
public sealed record ContactRole(string Id, string Title, ContactKind Kind, Uri? Url, string Note, DocumentCitation Source, Review Review, DateOnly CheckedOn);

/// <summary>
/// A country the user may choose in Settings ("Your country"): the registry its charities are checked in, the evaluators it
/// accepts and the officials to contact (<c>data/countries/&lt;ISO2&gt;/profile.json</c>; spec §9.4).
/// </summary>
/// <param name="Registry">"IRS Tax Exempt Organization Search (Publication 78)".</param>
/// <param name="RegistryUrl">The registry's search page.</param>
public sealed record CountryProfile(string Iso2, string Iso3, string Name, string Registry, Uri RegistryUrl, IReadOnlyList<Evaluator> Evaluators,
    IReadOnlyList<ContactRole> Contacts, DateOnly CheckedOn);

/// <summary>A country that has a profile (Settings lists only these).</summary>
public sealed record AvailableCountry(string Iso2, string Name);
