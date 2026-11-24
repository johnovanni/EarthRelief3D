namespace EarthRelief3D.Core.Help;

/// <summary>
/// One US senator as the Senate publishes the list (<c>senators_cfm.xml</c>, made into <c>data/countries/US/senators.json</c> by
/// <c>DataTool senators</c>): the state's two-letter code, the name as "First Last", the party's letter, the contact form on the
/// senator's own site and the site itself. No address, phone number or email address is kept (the app links, it never writes to anyone).
/// </summary>
public sealed record Senator(string State, string Name, string Party, Uri ContactForm, Uri Website);

/// <summary>The 50 states by their two-letter codes: the Senate's file gives codes, Settings and the panel show names.</summary>
public static class UsStates
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal)
    {
        ["AL"] = "Alabama", ["AK"] = "Alaska", ["AZ"] = "Arizona", ["AR"] = "Arkansas", ["CA"] = "California", ["CO"] = "Colorado",
        ["CT"] = "Connecticut", ["DE"] = "Delaware", ["FL"] = "Florida", ["GA"] = "Georgia", ["HI"] = "Hawaii", ["ID"] = "Idaho",
        ["IL"] = "Illinois", ["IN"] = "Indiana", ["IA"] = "Iowa", ["KS"] = "Kansas", ["KY"] = "Kentucky", ["LA"] = "Louisiana",
        ["ME"] = "Maine", ["MD"] = "Maryland", ["MA"] = "Massachusetts", ["MI"] = "Michigan", ["MN"] = "Minnesota", ["MS"] = "Mississippi",
        ["MO"] = "Missouri", ["MT"] = "Montana", ["NE"] = "Nebraska", ["NV"] = "Nevada", ["NH"] = "New Hampshire", ["NJ"] = "New Jersey",
        ["NM"] = "New Mexico", ["NY"] = "New York", ["NC"] = "North Carolina", ["ND"] = "North Dakota", ["OH"] = "Ohio", ["OK"] = "Oklahoma",
        ["OR"] = "Oregon", ["PA"] = "Pennsylvania", ["RI"] = "Rhode Island", ["SC"] = "South Carolina", ["SD"] = "South Dakota",
        ["TN"] = "Tennessee", ["TX"] = "Texas", ["UT"] = "Utah", ["VT"] = "Vermont", ["VA"] = "Virginia", ["WA"] = "Washington",
        ["WV"] = "West Virginia", ["WI"] = "Wisconsin", ["WY"] = "Wyoming",
    };

    /// <summary>The 50 codes, sorted by state name.</summary>
    public static IReadOnlyList<string> Codes { get; } = [.. Names.OrderBy(n => n.Value, StringComparer.Ordinal).Select(n => n.Key)];

    /// <summary>The state's full name, or null for a code that is not one of the 50.</summary>
    public static string? Name(string? code) => code is not null && Names.TryGetValue(code, out var n) ? n : null;
}
