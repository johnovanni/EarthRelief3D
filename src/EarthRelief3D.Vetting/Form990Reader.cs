using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace EarthRelief3D.Vetting;

/// <summary>
/// Reads a Form 990 e-file XML return (IRS schema, namespace <c>http://www.irs.gov/efile</c>). The programme share is Part IX line 25:
/// <c>IRS990/TotalFunctionalExpensesGrp/ProgramServicesAmt</c> over <c>/TotalAmt</c>. A 990-EZ or 990-PF has other elements (and
/// other rules for what counts), so it gives no share.
/// </summary>
public static class Form990Reader
{
    /// <summary>Parses the XML; a DOCTYPE is refused (the download is untrusted).</summary>
    public static XDocument Parse(byte[] xml)
    {
        using var stream = new MemoryStream(xml);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return XDocument.Load(reader);
    }

    /// <returns>
    /// The programme services expenses, the total functional expenses (both whole dollars) and the tax year (the year the return's
    /// tax period ends, as the index's TAX_PERIOD gives it); null for a return with no Part IX line 25, or total expenses of zero.
    /// </returns>
    public static (long Programme, long Total, int TaxYear)? ProgrammeShare(XDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var form = doc.Root?.Descendants().FirstOrDefault(e => e.Name.LocalName == "IRS990");
        var grp = form?.Elements().FirstOrDefault(e => e.Name.LocalName == "TotalFunctionalExpensesGrp");
        if (grp is null) return null;
        if (!Money(grp, "ProgramServicesAmt", out var programme) || !Money(grp, "TotalAmt", out var total) || total <= 0) return null;

        return Year(doc) is { } year ? (programme, total, year) : null;
    }

    /// <summary>
    /// The tax year (ruling R13): the return's own <c>ReturnHeader/TaxYr</c>; when that is absent, the calendar year of
    /// <c>TaxPeriodBeginDt</c>. Null when the return states neither.
    /// </summary>
    public static int? Year(XDocument doc)
    {
        if (int.TryParse(HeaderValue(doc, "TaxYr"), NumberStyles.None, CultureInfo.InvariantCulture, out var y) && y is > 1900 and < 3000) return y;
        var begin = HeaderValue(doc, "TaxPeriodBeginDt");
        return begin is { Length: >= 4 } && int.TryParse(begin.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var by) ? by : null;
    }

    /// <summary>The tax period's first day, "2024-07-01", as the return states it; null when it does not.</summary>
    public static string? TaxPeriodBegin(XDocument doc) => HeaderValue(doc, "TaxPeriodBeginDt");

    /// <summary>The tax period's last day, "2025-06-30", as the return states it; null when it does not.</summary>
    public static string? TaxPeriodEnd(XDocument doc) => HeaderValue(doc, "TaxPeriodEndDt");

    private static string? HeaderValue(XDocument doc, string name) =>
        doc.Root?.Descendants().FirstOrDefault(e => e.Name.LocalName == "ReturnHeader")?.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim() is { Length: > 0 } v ? v : null;
    private static bool Money(XElement parent, string name, out long value)
    {
        value = 0;
        var text = parent.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim();
        return text is { Length: > 0 } && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }
}

