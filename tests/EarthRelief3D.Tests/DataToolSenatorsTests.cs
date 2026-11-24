using System.IO;
using EarthRelief3D.Core.Help;
using EarthRelief3D.DataTool;

namespace EarthRelief3D.Tests;

/// <summary>Plan 4 Task 6: the <c>senators</c> verb, run on the real excerpt of the Senate's contact list (tests/Samples, six members).</summary>
public class DataToolSenatorsTests
{
    [Fact]
    public void ReadsExcerpt()
    {
        var x = SenatorsExtractor.Read(Samples.Path("senators_cfm_excerpt.xml"));

        Assert.Equal(6, x.Senators.Count);
        Assert.Equal(new DateOnly(2026, 11, 18), x.AsOf);   // the file's last_updated, 2026-11-18T12:08-05:00
        Assert.All(x.Senators, s =>
        {
            Assert.Equal(Uri.UriSchemeHttps, s.ContactForm.Scheme);
            Assert.Equal(Uri.UriSchemeHttps, s.Website.Scheme);
        });
        // Two each for Maine, Maryland and Wisconsin, in state then surname order; the name as "First Last".
        Assert.Equal(["MD", "MD", "ME", "ME", "WI", "WI"], x.Senators.Select(s => s.State));
        Assert.Equal("Angela D. Alsobrooks", x.Senators[0].Name);
        Assert.Equal("D", x.Senators[0].Party);
        Assert.Equal("https://www.alsobrooks.senate.gov/contact/", x.Senators[0].ContactForm.AbsoluteUri);
        Assert.Equal("https://alsobrooks.senate.gov/", x.Senators[0].Website.AbsoluteUri);
        Assert.Contains(x.Senators, s => s.Name == "Susan M. Collins" && s.Party == "R" && s.State == "ME");
    }

    [Fact]
    public void WrittenFileIsReadBack()
    {
        using var dir = new TempDir();
        var path = dir.File("senators.json");
        var x = SenatorsExtractor.Read(Samples.Path("senators_cfm_excerpt.xml"));
        SenatorsExtractor.Write(x, path);

        var (senators, asOf) = HelpTables.ReadSenators(path);
        Assert.Equal(x.Senators, senators);
        Assert.Equal(x.AsOf, asOf);
        var text = File.ReadAllText(path);
        Assert.Contains("\"asOf\": \"2026-11-18\"", text);
        Assert.Contains("https://www.senate.gov/general/contact_information/senators_cfm.xml", text);
        Assert.DoesNotContain("\r", text);
    }

    [Fact]
    public void RefusesAFileThatIsNotTheSenatesList()
    {
        using var dir = new TempDir();
        string Write(string xml) { var p = dir.File(Guid.NewGuid().ToString("N") + ".xml"); File.WriteAllText(p, xml); return p; }

        Assert.Throws<InvalidDataException>(() => SenatorsExtractor.Read(Write("<nope/>")));
        Assert.Throws<InvalidDataException>(() => SenatorsExtractor.Read(Write("<contact_information><member><state>MD</state></member></contact_information>")));
        // No last_updated: the as-of date is unknown, so the file is refused.
        var member = "<member><last_name>X</last_name><first_name>Y</first_name><party>D</party><state>MD</state><email>https://x.senate.gov/c</email><website>https://x.senate.gov</website></member>";
        Assert.Throws<InvalidDataException>(() => SenatorsExtractor.Read(Write("<contact_information>" + member + "</contact_information>")));
        // A downloaded file is untrusted: a DOCTYPE is refused, not expanded.
        Assert.Throws<InvalidDataException>(() => SenatorsExtractor.Read(Write("<!DOCTYPE a [<!ENTITY e \"x\">]><contact_information>" + member + "<last_updated>2026-11-18T12:08-05:00</last_updated></contact_information>")));
        // A contact form that is not https is refused, not quietly upgraded.
        var http = member.Replace("https://x.senate.gov/c", "http://x.senate.gov/c", StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => SenatorsExtractor.Read(Write("<contact_information>" + http + "<last_updated>2026-11-18T12:08-05:00</last_updated></contact_information>")));
    }
}
