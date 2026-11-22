using System.Text.RegularExpressions;

namespace EarthRelief3D.Core.Feeds;

/// <summary>Splits a Disease Outbreak News title ("Disease - Place, Place and Place") into disease and places.</summary>
public static partial class WhoTitleParser
{
    // A hyphen or en dash with a space after it; the space before is optional, because live titles also read
    // "Disease- Place". A dash inside a word ("cholera-like", "COVID-19") has no space after it, so it is not a separator.
    [GeneratedRegex(@"\s*[-–]\s+")]
    private static partial Regex Separator();

    [GeneratedRegex(@"^(update|situation update)\s*(no\.?\s*)?\d*$", RegexOptions.IgnoreCase)]
    private static partial Regex UpdateSuffix();

    [GeneratedRegex(@"\s+and\s+", RegexOptions.IgnoreCase)]
    private static partial Regex And();

    /// <summary>
    /// Splits at the last " - " or " – " (or "- ", "– "). A trailing "update 2" segment is a revision marker, not a place, so it is dropped
    /// first. With no separator the whole title is the disease and there are no places.
    /// </summary>
    public static (string Disease, IReadOnlyList<string> Places) Parse(string title)
    {
        title = title.Trim();
        var cut = LastSeparator(title);
        if (cut is { } c && UpdateSuffix().IsMatch(title[c.End..].Trim()))
        {
            title = title[..c.Start].TrimEnd();
            cut = LastSeparator(title);
        }
        if (cut is not { } k) return (title, []);
        var places = title[k.End..].Split(',', ';').SelectMany(part => And().Split(part))
            .Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        return (title[..k.Start].Trim(), places);
    }

    private static (int Start, int End)? LastSeparator(string s)
    {
        Match? last = null;
        foreach (Match m in Separator().Matches(s)) last = m;
        return last is null ? null : (last.Index, last.Index + last.Length);
    }
}
