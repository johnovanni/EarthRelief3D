namespace EarthRelief3D.App.Presentation;

/// <summary>
/// Hover tooltips show plain text in GlobeCore's dark tooltip, which does not wrap. A long line is broken here instead, so a
/// tooltip stays well inside the window (Task 8 measured Palestine's: one line of 215 characters, about 1,140 px wide).
/// </summary>
public static class TooltipText
{
    /// <summary>A line longer than this is wrapped at a space (about 600 px of 12 px text).</summary>
    public const int MaxLineChars = 110;

    private const string Indent = "    ";

    /// <summary>
    /// Breaks each line of <paramref name="text"/> at spaces into lines of at most <see cref="MaxLineChars"/> characters (a
    /// longer word stays whole); the continuation lines are indented, so each entry still starts at the margin.
    /// </summary>
    public static string Wrap(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return string.Join(Environment.NewLine, text.Split(Environment.NewLine).Select(WrapLine));
    }

    private static string WrapLine(string line)
    {
        if (line.Length <= MaxLineChars) return line;
        var lines = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var word in line.Split(' '))
        {
            int limit = lines.Count == 0 ? MaxLineChars : MaxLineChars - Indent.Length;
            if (current.Length > 0 && current.Length + 1 + word.Length > limit)
            {
                lines.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }
        lines.Add(current.ToString());
        return string.Join(Environment.NewLine + Indent, lines);
    }
}
