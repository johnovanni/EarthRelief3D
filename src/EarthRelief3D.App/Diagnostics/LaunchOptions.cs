using System.Globalization;
using AstroCore.Earth;
using EarthRelief3D.App.Presentation;

namespace EarthRelief3D.App.Diagnostics;

/// <summary>
/// Diagnostic command-line options (README, "Diagnostics"):
/// <c>--offline-samples &lt;dir&gt;</c> answers every feed request from the sample files in the folder, with a temporary data
/// folder (so the user's cache and settings are untouched); <c>--display-time &lt;ISO UTC&gt;</c> sets the app's clock to that
/// time (it then runs on in real time, or stands still in a snapshot run);
/// <c>--snapshot-scenes &lt;json&gt; --snapshot-out &lt;dir&gt;</c> (with <c>--offline-samples</c>) takes one picture per scene in the
/// JSON file, saved as <c>&lt;dir&gt;\&lt;name&gt;.png</c>, in one launch;
/// <c>--snapshot &lt;png&gt;</c> takes one picture, with <c>--look-at lat,lon</c>, <c>--layers</c> (comma list of
/// <see cref="AllLayerNames"/>, at most one shading; default conflict, food, outbreaks, displacement) and
/// <c>--open-panel ISO3</c> as the scene's camera, layers and country panel;
/// <c>--delay-ms N</c> waits N ms after the data has been drawn before each picture (default 1500);
/// <c>--show-drafts</c> also shows the curated rows the owner has not approved yet, each labelled "Draft: not yet reviewed" (P3-4).
/// Unknown arguments are ignored; a missing or bad value is reported in <see cref="Error"/>, one line per problem.
/// </summary>
public sealed record LaunchOptions(string? OfflineSamplesDir = null, DateTime? DisplayTimeUtc = null,
                                   string? SnapshotScenesFile = null, string? SnapshotOutDir = null,
                                   string? SnapshotPath = null, GeoPosition? LookAt = null,
                                   IReadOnlyList<string>? Layers = null, string? OpenPanel = null,
                                   int DelayMs = LaunchOptions.DefaultDelayMs, string? Error = null,
                                   bool ShowDrafts = false)
{
    public const int DefaultDelayMs = 1500;
    /// <summary>Added to the window title while drafts are shown, so unreviewed rows are never taken for reviewed ones.</summary>
    public const string DraftsTitleNote = " - DRAFTS SHOWN (not yet reviewed)";
    /// <summary>The Plan 2 layers: the default of <c>--layers</c>, and the names a <c>panelFocus</c> may use.</summary>
    public static readonly string[] LayerNames = ["conflict", "food", "outbreaks", "displacement"];

    /// <summary>Every layer a scene or <c>--layers</c> may name: the shadings (one at a time) and the marker layers.</summary>
    public static IReadOnlyList<string> AllLayerNames => LayerCatalog.AllNames;

    /// <summary>The shading layers among <paramref name="names"/>, in the order given.</summary>
    public static IReadOnlyList<string> ShadingsIn(IEnumerable<string> names) =>
        [.. names.Where(n => LayerCatalog.TryParse(n, out var sh, out _) && sh is not null).Distinct(StringComparer.OrdinalIgnoreCase)];
    public const string DisplayTimeExample = "2026-11-22T12:00:00Z";

    private static readonly string[] UtcFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mmzzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
    ];

    /// <summary>True when a picture is to be taken (one, or a scenes file's worth), after which the app exits.</summary>
    public bool TakesPictures => SnapshotPath != null || SnapshotScenesFile != null;

    /// <summary>True for any run that is not a normal user session (pictures or offline samples).</summary>
    public bool IsDiagnosticRun => TakesPictures || OfflineSamplesDir != null;

    public static LaunchOptions Parse(IReadOnlyList<string> args)
    {
        string? offline = null, scenesFile = null, scenesOut = null, snapshot = null, panel = null;
        DateTime? displayTime = null;
        GeoPosition? lookAt = null;
        List<string>? layers = null;
        int delay = DefaultDelayMs;
        bool showDrafts = false;
        var errors = new List<string>();
        for (int i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--offline-samples":
                    if (Value(args, ref i, errors, "a folder of sample files, such as tests\\Samples") is { } dir) offline = dir;
                    break;
                case "--display-time":
                    if (Value(args, ref i, errors, "a UTC time such as " + DisplayTimeExample) is not { } t) break;
                    if (TryParseUtc(t, out var utc)) displayTime = utc;
                    else errors.Add($"--display-time: '{t}' is not a UTC date and time. Use the ISO form, for example {DisplayTimeExample}.");
                    break;
                case "--snapshot-scenes":
                    if (Value(args, ref i, errors, "a scenes JSON file, such as tests\\SnapshotScenes\\scenes.json") is { } sf) scenesFile = sf;
                    break;
                case "--snapshot-out":
                    if (Value(args, ref i, errors, "a folder for the pictures") is { } so) scenesOut = so;
                    break;
                case "--snapshot":
                    if (Value(args, ref i, errors, "a PNG file to save") is { } sp) snapshot = sp;
                    break;
                case "--look-at":
                    if (Value(args, ref i, errors, "a latitude and longitude such as 15,30") is not { } p) break;
                    if (TryParseLatLon(p, out var pos)) lookAt = pos;
                    else errors.Add($"--look-at: '{p}' is not a latitude,longitude pair in degrees (latitude -90 to 90, longitude -180 to 180, east positive), for example 15,30.");
                    break;
                case "--layers":
                    if (Value(args, ref i, errors, "a comma list from " + string.Join(", ", AllLayerNames)) is not { } list) break;
                    var names = list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .Select(n => n.ToLowerInvariant()).ToList();
                    var bad = names.Where(n => !AllLayerNames.Contains(n)).ToList();
                    if (names.Count == 0 || bad.Count > 0)
                        errors.Add($"--layers: '{(bad.Count > 0 ? string.Join(", ", bad) : list)}' is not a layer. Use any of {string.Join(", ", AllLayerNames)}.");
                    else if (ShadingsIn(names) is { Count: > 1 } two)
                        errors.Add($"--layers: only one shading layer can be on at a time, not {string.Join(" and ", two)}.");
                    else layers = names;
                    break;
                case "--open-panel":
                    if (Value(args, ref i, errors, "a three-letter country code such as SDN") is not { } iso) break;
                    if (IsIso3(iso)) panel = iso.ToUpperInvariant();
                    else errors.Add($"--open-panel: '{iso}' is not a three-letter country code, for example SDN.");
                    break;
                case "--show-drafts":
                    showDrafts = true;
                    break;
                case "--delay-ms" when i + 1 < args.Count
                                       && int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int ms):
                    delay = ms;
                    i++;
                    break;
            }
        }
        if ((scenesFile is null) != (scenesOut is null) && errors.Count == 0)
            errors.Add("--snapshot-scenes and --snapshot-out go together: give both, or neither.");
        if (scenesFile is not null && snapshot is not null && errors.Count == 0)
            errors.Add("--snapshot and --snapshot-scenes cannot be used together.");
        return new LaunchOptions(offline, displayTime, scenesFile, scenesOut, snapshot, lookAt, layers, panel, delay,
                                 errors.Count == 0 ? null : string.Join(Environment.NewLine, errors), showDrafts);
    }

    private static string? Value(IReadOnlyList<string> args, ref int i, List<string> errors, string what)
    {
        if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal)) return args[++i];
        errors.Add($"{args[i]} needs a value: {what}.");
        return null;
    }

    internal static bool IsIso3(string text) => text.Length == 3 && text.All(char.IsAsciiLetter);

    /// <summary>ISO 8601 with a 'Z' or an offset (a time without a zone is refused rather than guessed), as UTC.</summary>
    public static bool TryParseUtc(string text, out DateTime utc)
    {
        if (DateTimeOffset.TryParseExact(text.Trim(), UtcFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var dto))
        {
            utc = dto.UtcDateTime;
            return true;
        }
        utc = default;
        return false;
    }

    private static bool TryParseLatLon(string text, out GeoPosition position)
    {
        position = default;
        var parts = text.Split(',');
        if (parts.Length != 2
            || !double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat)
            || !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon)
            || !(Math.Abs(lat) <= 90) || !(Math.Abs(lon) <= 180)) return false;
        position = new GeoPosition(lat, lon);
        return true;
    }
}
