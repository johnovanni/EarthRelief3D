using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using EarthRelief3D.Core.Help;

namespace EarthRelief3D.DataTool;

/// <summary>The Senate's contact list as the app keeps it: the senators and the list's own date (its <c>last_updated</c>).</summary>
public sealed record SenatorsExtract(IReadOnlyList<Senator> Senators, DateOnly AsOf);

/// <summary>
/// The <c>senators &lt;senators_cfm.xml&gt;</c> verb (Plan 4 Task 6; docs/data-sources.md section 17): reads the US Senate's
/// contact list (https://www.senate.gov/general/contact_information/senators_cfm.xml, a US government work) and writes
/// <c>data\countries\US\senators.json</c>. Per member it keeps the state code, the name as "First Last", the party's letter, the
/// contact form (the file's <c>email</c> element, which holds a web address, not an email address) and the website. The street
/// address, phone number, class and bioguide id are not kept. The download is untrusted: a DOCTYPE is refused, and a contact form
/// or site that is not https is refused rather than quietly upgraded.
/// </summary>
public static class SenatorsExtractor
{
    public const string SourceUrl = "https://www.senate.gov/general/contact_information/senators_cfm.xml";

    public static SenatorsExtract Read(string path)
    {
        XDocument doc;
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(path, settings);
            doc = XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException($"{path}: not the Senate's contact list: {ex.Message}", ex);
        }
        var root = doc.Root;
        if (root is null || root.Name.LocalName != "contact_information") throw new InvalidDataException($"{path}: the root element is not contact_information");

        var updated = root.Element("last_updated")?.Value.Trim();
        if (updated is null || updated.Length < 10 || !DateOnly.TryParseExact(updated[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var asOf))
            throw new InvalidDataException($"{path}: no last_updated date (the list's own date)");

        var members = root.Elements("member").ToList();
        if (members.Count == 0) throw new InvalidDataException($"{path}: no member elements");
        var problems = new List<string>();
        var rows = new List<(string Sort, Senator Senator)>();
        foreach (var m in members)
        {
            string? Field(string name) => m.Element(name)?.Value.Trim() is { Length: > 0 } v ? v : null;
            var last = Field("last_name");
            var first = Field("first_name");
            var who = $"{first} {last}".Trim();
            var state = Field("state");
            var party = Field("party");
            if (last is null || first is null || state is null || party is null) { problems.Add($"{(who.Length > 0 ? who : "(a member)")}: needs first_name, last_name, state and party"); continue; }
            if (!HttpsUrl(Field("email"), out var form)) { problems.Add($"{who}: the contact form (email element) is not an https address"); continue; }
            if (!HttpsUrl(Field("website"), out var site)) { problems.Add($"{who}: the website is not an https address"); continue; }
            rows.Add(($"{state}|{last}|{first}", new Senator(state, who, party, form!, site!)));
        }
        if (problems.Count > 0) throw new InvalidDataException($"{path}: {problems.Count} problem(s):" + string.Concat(problems.Select(p => "\n  " + p)));
        return new SenatorsExtract([.. rows.OrderBy(r => r.Sort, StringComparer.OrdinalIgnoreCase).Select(r => r.Senator)], asOf);
    }

    private static bool HttpsUrl(string? text, out Uri? url)
    {
        url = null;
        return text is not null && Uri.TryCreate(text, UriKind.Absolute, out url) && url.Scheme == Uri.UriSchemeHttps;
    }

    /// <summary>Writes <c>senators.json</c> (UTF-8 without a BOM, LF).</summary>
    public static void Write(SenatorsExtract x, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, HelpTables.WriteSenators(new HelpTables.SenatorsDocument(SourceUrl, x.AsOf, x.Senators)), new System.Text.UTF8Encoding(false));
    }
}
