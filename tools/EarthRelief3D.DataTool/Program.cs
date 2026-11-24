using System.Globalization;
using EarthRelief3D.DataTool;

// Argument parsing only; the extractors are the library the tests use.
const string Usage = """
    EarthRelief3D DataTool: makes the bundled extracts in data\datasets from files downloaded into .downloads\ (never committed).

      DataTool fao      <zip>      --published yyyy-MM-dd [--downloaded yyyy-MM-dd] [--out <folder>] [--note <text>]...
      DataTool eia      <INTL.zip> --published yyyy-MM-dd [--downloaded yyyy-MM-dd] [--out <folder>] [--note <text>]...
      DataTool aqueduct <zip>      --published yyyy-MM-dd [--downloaded yyyy-MM-dd] [--out <folder>] [--note <text>]...
      DataTool simplify <geojson> <tolerance in degrees> [--out <file>]

    --published: the source file's own date (its Last-Modified). --downloaded: the day it was downloaded (default: the
    file's modified date). --out: default data\datasets\<id> in the repository. --note: added to the manifest's notes.
    """;

if (args.Length < 2) { Console.Error.WriteLine(Usage); return 2; }
var verb = args[0];
var path = args[1];
string? published = null, downloaded = null, output = null;
var notes = new List<string>();
var positional = new List<string>();
for (int i = 2; i < args.Length; i++)
{
    string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
    switch (args[i])
    {
        case "--published": published = Next(); break;
        case "--downloaded": downloaded = Next(); break;
        case "--out": output = Next(); break;
        case "--note": notes.Add(Next()); break;
        default: positional.Add(args[i]); break;
    }
}

try
{
    if (!File.Exists(path)) throw new FileNotFoundException($"{path} not found");
    if (verb == "simplify")
    {
        if (positional.Count != 1) { Console.Error.WriteLine(Usage); return 2; }
        var simplified = GeoJsonSimplifier.Simplify(File.ReadAllText(path), GeoJsonSimplifier.ParseTolerance(positional[0]));
        var target = output ?? Path.ChangeExtension(path, ".simplified.geojson");
        File.WriteAllText(target, simplified);
        Console.WriteLine($"{target}: {new FileInfo(target).Length:N0} bytes (from {new FileInfo(path).Length:N0})");
        return 0;
    }
    if (positional.Count != 0 || published is null) { Console.Error.WriteLine(Usage); return 2; }

    var src = new SourceFile(path, Day(published), downloaded is null ? DateOnly.FromDateTime(File.GetLastWriteTimeUtc(path)) : Day(downloaded));
    var codes = CountryCodes.Default;
    Extract extract = verb switch
    {
        "fao" => FaoExtractor.Run(src, codes),
        "eia" => EiaExtractor.Run(src, codes),
        "aqueduct" => AqueductExtractor.Run(src, codes),
        _ => throw new ArgumentException($"unknown command \"{verb}\""),
    };
    if (notes.Count > 0) extract = extract with { Manifest = extract.Manifest with { Notes = [.. extract.Manifest.Notes, .. notes] } };
    var folder = output ?? Path.Combine(RepoRoot(), "data", "datasets", extract.Manifest.Id);
    DatasetWriter.Write(extract, folder);

    var m = extract.Manifest;
    Console.WriteLine($"{folder}:");
    foreach (var s in m.Series) Console.WriteLine($"  {s.Id}: {s.Countries} countries ({s.Unit})");
    Console.WriteLine($"  as of: {m.AsOf}");
    Console.WriteLine($"  not placed: {m.NotPlaced.Count}{(m.NotPlaced.Count > 0 ? " (" + string.Join(", ", m.NotPlaced.Select(n => $"{n.Code} {n.Name}")) + ")" : "")}");
    Console.WriteLine($"  dropped: {m.Dropped.Count}");
    foreach (var n in m.Notes.Where(n => n.StartsWith("Check", StringComparison.Ordinal))) Console.WriteLine("  " + n);
    return 0;
}
catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or UnauthorizedAccessException)
{
    Console.Error.WriteLine("DataTool: " + ex.Message);
    return 1;
}

static DateOnly Day(string text) =>
    DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
        ? d : throw new ArgumentException($"\"{text}\" is not yyyy-MM-dd");

static string RepoRoot()
{
    for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, "EarthRelief3D.sln"))) return dir.FullName;
    throw new DirectoryNotFoundException("Run the DataTool inside the repository, or pass --out.");
}
