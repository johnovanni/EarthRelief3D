using AstroCore.Earth;
using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.Tests;

internal static class TestData
{
    public static readonly DateTime T = new(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);

    public static SourceCitation Cite(string pub = "UCDP") => new(pub, "Candidate Events", "26.0.9", new Uri("https://ucdp.uu.se/downloads/"),
        "CC BY 4.0", "Uppsala Conflict Data Program", T, T.AddHours(1));

    public static ConflictCrisis Conflict(string id = "ucdp-1", int deaths = 30) => new(id, "SDN", "Sudan: A vs B",
        new NativeSeverity(deaths, "best-estimate deaths", $"{deaths} deaths"), SeverityBands.ForConflict(deaths), T.AddDays(-300),
        new Trend(20, 10, "last 3 months vs the 3 before"), [Cite()], 1234, "A", "B",
        [new ConflictEvent(1, T.AddDays(-5), new GeoPosition(15.5, 32.5), 12, 1), new ConflictEvent(2, T.AddDays(-4), new GeoPosition(-1.5, 179.9), 3, 2)],
        new Dictionary<DateOnly, int> { [new DateOnly(2026, 9, 1)] = 5, [new DateOnly(2026, 10, 1)] = 7 });

    public static FoodCrisis Food() => new("ipc-SOM", "SOM", "Somalia: food insecurity", new NativeSeverity(4, "IPC phase", "Phase 4 (Emergency)"),
        4, null, null, [Cite("IPC")], 4, 1_200_000, 0.25, new DateOnly(2026, 4, 1), new DateOnly(2026, 12, 31), false);

    public static OutbreakCrisis Outbreak() => new("don-2026-DON999", "COD", "Ebola in DRC", new NativeSeverity(0, "WHO Outbreak News", "Outbreak News report"),
        3, T.AddDays(-10), null, [Cite("WHO")], "Ebola", "2026-DON999", T.AddDays(-3), new Uri("https://www.who.int/emergencies/disease-outbreak-news/item/2026-DON999"));

    public static DisplacementFigure Disp() => new("SYR", 2025, 6_000_000, 100_000, 7_000_000, Cite("UNHCR"));
    public static UnplacedReport Unplaced() => new("WHO", "Multi-country event", "no single country", Cite("WHO"));

    public static CrisisSnapshot Snap() => new([Conflict(), Food(), Outbreak()], [Disp()], [Unplaced()]);
}
