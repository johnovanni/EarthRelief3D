using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>Food crises from the IPC national figures on HDX (CC0, no key). Stale analyses are kept in the store, marked stale.</summary>
public sealed class IpcNationalFeed(FeedHttp http, CrisisStore store, IClock clock, IAppLog log, CountryResolver? countries = null) : IFeed
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
        log.Info($"IPC: {result.Crises.Count} countries with people in Phase 3 or higher ({result.Crises.Count(c => c.IsStale)} stale); "
            + $"{result.BlankPeriod} rows skipped (no current period)");
        return FeedRunResult.Ok;
    }
}
