using System.Text.Json;
using System.Text.Json.Serialization;
using EarthRelief3D.Core.Curated;

namespace EarthRelief3D.Core.Help;

/// <summary>
/// The help catalog's files (spec §9.4, §10; Plan 4): the OCHA pooled funds and CERF (<c>data/help/pooled-funds.json</c>, for every
/// country), and for the user's country <c>data/countries/&lt;ISO2&gt;/profile.json</c> (registry, evaluators, official contacts),
/// <c>charities.json</c> and <c>senators.json</c>. Every row is cited and a draft until the owner approves it (P4-13). A bad
/// file is an <see cref="InvalidDataException"/> naming every problem (a packaging fault the tests catch); the app then runs with
/// <see cref="Empty"/>.
/// </summary>
/// <param name="Profile">The chosen country's profile; null for a country that has none.</param>
/// <param name="SenatorsAsOf">The Senate list's own date (its <c>last_updated</c>).</param>
public sealed record HelpTables(CountryProfile? Profile, IReadOnlyList<Charity> Charities, IReadOnlyList<PooledFund> Funds,
    IReadOnlyList<Senator> Senators, DateOnly SenatorsAsOf)
{
    public const string FundsFile = "pooled-funds.json";
    public const string ProfileFile = "profile.json";
    public const string CharitiesFile = "charities.json";
    public const string SenatorsFile = "senators.json";
    /// <summary>The fund that stands in for every country without a pooled fund of its own (P4-11).</summary>
    public const string CerfId = "cerf";

    public static HelpTables Empty { get; } = new(null, [], [], [], default);

    /// <summary>Every country that has a <c>profile.json</c>, by name: what "Your country" in Settings offers.</summary>
    public IReadOnlyList<AvailableCountry> AvailableCountries { get; init; } = [];

    // ---------------------------------------------------------------- loading

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>The shape of <c>senators.json</c>: where the list came from, its own date, and the senators.</summary>
    public sealed record SenatorsDocument(string? Source, DateOnly AsOf, IReadOnlyList<Senator> Senators);

    /// <summary>
    /// Reads the pooled funds from <c>&lt;dataDir&gt;\help</c> and, when <c>&lt;dataDir&gt;\countries\&lt;iso2&gt;\profile.json</c>
    /// exists, that country's profile, charities and senators; validates them. A country with no profile has none (not an error).
    /// </summary>
    public static HelpTables Load(string dataDir, string iso2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDir);
        var funds = Read<List<PooledFund>>(Path.Combine(dataDir, "help", FundsFile));
        CountryProfile? profile = null;
        IReadOnlyList<Charity> charities = [];
        IReadOnlyList<Senator> senators = [];
        DateOnly asOf = default;
        var code = iso2 is { Length: 2 } && iso2.All(char.IsAsciiLetter) ? iso2.ToUpperInvariant() : null;   // a folder name: letters only
        var countryDir = code is null ? null : Path.Combine(dataDir, "countries", code);
        if (countryDir is not null && File.Exists(Path.Combine(countryDir, ProfileFile)))
        {
            profile = Read<CountryProfile>(Path.Combine(countryDir, ProfileFile));
            charities = Read<List<Charity>>(Path.Combine(countryDir, CharitiesFile));
            var senatorsPath = Path.Combine(countryDir, SenatorsFile);
            if (File.Exists(senatorsPath)) (senators, asOf) = ReadSenators(senatorsPath);
        }
        var tables = new HelpTables(profile, charities, funds, senators, asOf) { AvailableCountries = Countries(dataDir) };
        var problems = Validate(tables);
        if (problems.Count > 0)
            throw new InvalidDataException($"{dataDir}: {problems.Count} problem(s) in the help tables:{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
        return tables;
    }

    /// <summary>Reads a <c>senators.json</c>: the senators and the Senate list's own date.</summary>
    public static (IReadOnlyList<Senator> Senators, DateOnly AsOf) ReadSenators(string path)
    {
        var doc = Read<SenatorsDocument>(path);
        return (doc.Senators ?? [], doc.AsOf);
    }

    /// <summary>The text <c>DataTool senators</c> writes (indented, LF, one trailing newline).</summary>
    public static string WriteSenators(SenatorsDocument doc) => JsonSerializer.Serialize(doc, Json).ReplaceLineEndings("\n") + "\n";

    private static IReadOnlyList<AvailableCountry> Countries(string dataDir)
    {
        var root = Path.Combine(dataDir, "countries");
        if (!Directory.Exists(root)) return [];
        var found = new List<AvailableCountry>();
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var path = Path.Combine(dir, ProfileFile);
            if (!File.Exists(path)) continue;
            var profile = Read<CountryProfile>(path);
            if (profile.Iso2 is { Length: 2 } && !string.IsNullOrWhiteSpace(profile.Name)) found.Add(new AvailableCountry(profile.Iso2, profile.Name));
        }
        return [.. found.OrderBy(c => c.Name, StringComparer.Ordinal)];
    }

    private static T Read<T>(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) ?? throw new InvalidDataException($"{path} is empty");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{path}: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new InvalidDataException($"{path} is missing", ex);
        }
    }

    // ---------------------------------------------------------------- validation

    /// <summary>Every problem in the tables, one line each ("&lt;id&gt;: ..."); empty when they are sound.</summary>
    public static IReadOnlyList<string> Validate(HelpTables t)
    {
        ArgumentNullException.ThrowIfNull(t);
        var problems = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        if (t.Profile is { } p)
        {
            Required(problems, "profile", p.Iso2 is { Length: 2 } && p.Iso2.All(char.IsAsciiLetterUpper) ? "x" : null, "iso2 (two capital letters)");
            if (!IsIso3(p.Iso3)) problems.Add("profile: iso3 must be three capital letters");
            Required(problems, "profile", p.Name, "name");
            Required(problems, "profile", p.Registry, "registry");
            Https(problems, "profile", "registryUrl", p.RegistryUrl);
            if (!IsRealDate(p.CheckedOn)) problems.Add("profile: no checkedOn date");
            if (p.Evaluators is not { Count: > 0 }) problems.Add("profile: no evaluator is listed (a charity cannot be rated)");
            foreach (var e in p.Evaluators ?? [])
            {
                var who = $"evaluator {e?.Name}";
                if (e is null) { problems.Add("profile: an empty evaluator"); continue; }
                Required(problems, who, e.Name, "name");
                Required(problems, who, e.Qualifies, "qualifies text");
                Https(problems, who, "site", e.Site);
            }
            foreach (var c in p.Contacts ?? [])
            {
                if (c is null) { problems.Add("contacts: an empty entry"); continue; }
                var id = string.IsNullOrWhiteSpace(c.Id) ? "(contact with no id)" : c.Id;
                if (!ids.Add(id)) problems.Add($"{id}: id used twice");
                Required(problems, id, c.Title, "title");
                Required(problems, id, c.Note, "note");
                if (c.Kind == ContactKind.Senators) { if (c.Url is not null) problems.Add($"{id}: the senators row has no url (its list is senators.json)"); }
                else if (c.Url is null) problems.Add($"{id}: no url");
                else Https(problems, id, "url", c.Url);
                Cited(problems, id, "source", c.Source?.Title, c.Source?.Url, c.Source?.Date ?? default, c.Source?.Publisher);
                if (!IsRealDate(c.CheckedOn)) problems.Add($"{id}: no checkedOn date");
                ReviewOk(problems, id, c.Review);
            }
        }

        foreach (var f in t.Funds ?? [])
        {
            if (f is null) { problems.Add("funds: an empty entry"); continue; }
            var id = string.IsNullOrWhiteSpace(f.Id) ? "(fund with no id)" : f.Id;
            if (!ids.Add(id)) problems.Add($"{id}: id used twice");
            Required(problems, id, f.Name, "name");
            Required(problems, id, f.GiveNote, "giveNote");
            Https(problems, id, "page", f.Page);
            Https(problems, id, "giveUrl", f.GiveUrl);
            if (!IsRealDate(f.CheckedOn)) problems.Add($"{id}: no checkedOn date");
            foreach (var iso in f.Iso3 ?? []) if (!IsIso3(iso)) problems.Add($"{id}: iso3 \"{iso}\" must be three capital letters");
            if (id != CerfId && f.Iso3 is not { Count: > 0 }) problems.Add($"{id}: covers no country (iso3 is empty)");
            ReviewOk(problems, id, f.Review);
        }

        foreach (var c in t.Charities ?? [])
        {
            if (c is null) { problems.Add("charities: an empty entry"); continue; }
            var id = string.IsNullOrWhiteSpace(c.Id) ? "(charity with no id)" : c.Id;
            if (!ids.Add(id)) problems.Add($"{id}: id used twice");
            Required(problems, id, c.Name, "name");
            Required(problems, id, c.Reviewer, "reviewer");
            if (c.Ein is not { Length: 10 } || c.Ein[2] != '-' || !c.Ein.Where((ch, i) => i != 2).All(char.IsAsciiDigit)) problems.Add($"{id}: the EIN must read 12-3456789");
            Https(problems, id, "website", c.Website);
            Https(problems, id, "donateUrl", c.DonateUrl);
            if (!IsRealDate(c.ReviewedOn)) problems.Add($"{id}: no reviewedOn date");
            ReviewOk(problems, id, c.Review);
            foreach (var l in c.ProfileLinks ?? []) Https(problems, id, "profile link", l);
            foreach (var r in c.Ratings ?? [])
            {
                Required(problems, id, r?.Evaluator, "rating evaluator");
                Https(problems, id, "rating report url", r?.ReportUrl);
                if (r is null || !IsRealDate(r.CheckedOn)) problems.Add($"{id}: a rating has no checkedOn date");
                Required(problems, id, r?.CheckedBy, "rating checkedBy");
            }
            foreach (var w in c.Countries ?? [])
            {
                if (!IsIso3(w?.Iso3)) problems.Add($"{id}: a country row's iso3 must be three capital letters");
                Cited(problems, id, "evidence", w?.EvidenceTitle, w?.Evidence, w?.CheckedOn ?? default);
                if (w?.EvidenceKind is not ("charity's own page" or "OCHA 3W")) problems.Add($"{id}: evidenceKind must be \"charity's own page\" or \"OCHA 3W\"");
            }
            if (c.Legal is { } legal && (!IsRealDate(legal.CheckedOn) || string.IsNullOrWhiteSpace(legal.Pub78File)))
                problems.Add($"{id}: the Publication 78 check needs its date and file name");
            // An approved row that the rules would still hide is a mistake the owner should see, not a silent gap (P4-8).
            if (c.Review is { IsApproved: true } && t.Profile is { } profile)
            {
                var iso3 = c.Countries?.FirstOrDefault()?.Iso3 ?? "";
                foreach (var reason in VettingRules.Check(c, iso3, profile.Evaluators ?? [], c.ReviewedOn, showDrafts: true).Reasons)
                    problems.Add($"{id}: approved but would be hidden: {reason}");
            }
        }
        if (t.Charities is { Count: > 0 } && t.Profile is null) problems.Add("charities: listed for a country with no profile");

        var perState = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in t.Senators ?? [])
        {
            if (s is null) { problems.Add("senators: an empty entry"); continue; }
            var id = $"senator {s.Name} ({s.State})";
            if (UsStates.Name(s.State) is null) problems.Add($"{id}: {s.State} is not one of the 50 states");
            Required(problems, id, s.Name, "name");
            Required(problems, id, s.Party, "party");
            Https(problems, id, "contact form", s.ContactForm);
            Https(problems, id, "website", s.Website);
            perState[s.State ?? ""] = perState.GetValueOrDefault(s.State ?? "") + 1;
        }
        foreach (var (state, n) in perState.Where(kv => kv.Value > 2)) problems.Add($"{state}: {n} senators listed; a state has two");
        if (t.Senators is { Count: > 0 } && !IsRealDate(t.SenatorsAsOf)) problems.Add("senators: no asOf date");
        return problems;
    }

    private static bool IsIso3(string? s) => s is { Length: 3 } && s.All(char.IsAsciiLetterUpper);

    private static bool IsRealDate(DateOnly d) => d > new DateOnly(1900, 1, 1);

    private static void Required(List<string> problems, string id, string? value, string what)
    {
        if (string.IsNullOrWhiteSpace(value)) problems.Add($"{id}: no {what}");
    }

    private static void Https(List<string> problems, string id, string what, Uri? url)
    {
        if (url is null || !url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps) problems.Add($"{id}: the {what} needs an absolute https URL");
    }

    private static void Cited(List<string> problems, string id, string what, string? title, Uri? url, DateOnly date, string? publisher = "-")
    {
        if (string.IsNullOrWhiteSpace(title)) problems.Add($"{id}: the {what} has no title");
        Https(problems, id, what, url);
        if (!IsRealDate(date)) problems.Add($"{id}: the {what} has no date");
        if (string.IsNullOrWhiteSpace(publisher)) problems.Add($"{id}: the {what} has no publisher");
    }

    private static void ReviewOk(List<string> problems, string id, Review? r)
    {
        if (r is null) { problems.Add($"{id}: no review status"); return; }
        if (r.Status == Review.Approved)
        {
            if (r.ApprovedOn is null) problems.Add($"{id}: an approved row needs review.approvedOn");
        }
        else if (r.Status == Review.Draft)
        {
            if (r.ApprovedOn is not null) problems.Add($"{id}: a draft row has review.approvedOn");
        }
        else problems.Add($"{id}: review status \"{r.Status}\" must be \"{Review.Draft}\" or \"{Review.Approved}\"");
    }
}
