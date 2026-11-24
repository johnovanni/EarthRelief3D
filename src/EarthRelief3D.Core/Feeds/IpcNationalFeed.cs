using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Food crises from the IPC national figures on HDX (CC0, no key). Stale analyses are kept in the store, marked stale;
/// analyses with only a projection are kept as projection-only records (not crises). After the national records are stored,
/// the area step (<see cref="IpcAreaFeed"/>, P3-2) looks for the current countries' IPC areas; it reports through this
/// feed's status dot, and a country whose areas cannot be had keeps the 20% rule without failing the national figures.
/// </summary>
/// <param name="areas">The area step; null: no areas (every country shaded by the 20% rule).</param>
public sealed class IpcNationalFeed(FeedHttp http, CrisisStore store, IClock clock, IAppLog log, CountryResolver? countries = null,
    IpcAreaFeed? areas = null) : IFeed
{
    public static readonly Uri PackageUrl = new("https://data.humdata.org/api/3/action/package_show?id=global-acute-food-insecurity-country-data");

    private readonly CountryResolver _countries = countries ?? CountryResolver.LoadDefault();

    public string Name => "IPC";
    public TimeSpan Interval => TimeSpan.FromHours(12);

    public async Task<FeedRunResult> RunOnceAsync(CancellationToken ct)
    {
        var package = await http.GetStringAsync(PackageUrl, ct).ConfigureAwait(false);
        var csvUrl = IpcNationalReader.FindResourceUrl(package);
        var csv = await http.GetBytesAsync(csvUrl, ct).ConfigureAwait(false);
        var result = IpcNationalReader.Read(csv, _countries, clock.UtcNow);

        store.ReplaceKind(CrisisKind.FoodCrisis, result.Crises);
        store.MarkChecked(Name, clock.UtcNow);   // R12: checked, whatever was found
        var current = result.Crises.Where(c => !c.ProjectionOnly).ToList();
        log.Info($"IPC: {current.Count} countries with people in Phase 3 or higher ({current.Count(c => c.IsStale)} stale); "
            + $"{result.ProjectionOnly} with a projection only (no current period); "
            + $"{result.BlankPeriod} rows skipped (no current period or projection)");
        if (areas is not null)
        {
            try { await areas.RunAsync(result.Crises, ct).ConfigureAwait(false); }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // The national figures are stored and good; the countries keep the 20% rule until the next run.
                log.Error($"{IpcAreaFeed.LogName}: the area step failed; every country keeps the 20% rule", e);
            }
        }
        return FeedRunResult.Ok;
    }
}
