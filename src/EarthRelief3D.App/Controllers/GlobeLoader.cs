using EarthRelief3D.App.Layers;
using GlobeCore.Earth;
using GlobeCore.Imaging;
using GlobeCore.Regions;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Controllers;

/// <summary>The globe's start-up loads (as EarthWatch3D's GlobeLoader): the Blue Marble and the coastlines and borders.</summary>
internal static class GlobeLoader
{
    /// <summary>
    /// Decodes the Blue Marble at <paramref name="width"/> and reads the Natural Earth lines in parallel on the pool, then
    /// rasterizes the lines (the borders from the live boundary set) at the image's size on <paramref name="sta"/>.
    /// </summary>
    public static async Task<(Bgra32Image BaseImage, SparseOverlay CoastAndBorders)> LoadAsync(int width, StaWorker sta, BoundarySet boundaries)
    {
        var decodeTask = Task.Run(() =>
        {
            using var s = EarthAssets.Open(EarthAssets.BlueMarbleFile);
            return ImageConversions.DecodeToBgra32(s, width);   // scales while decoding
        });
        var linesTask = Task.Run(() => (Coast: NaturalEarthLines.Coastlines(), Borders: NaturalEarthLines.LandBoundaries(boundaries)));
        await Task.WhenAll(decodeTask, linesTask);   // observes both exceptions
        var baseImage = decodeTask.Result;
        var (coast, borders) = linesTask.Result;
        int w = baseImage.Width, h = baseImage.Height;
        var lines = await sta.InvokeAsync(() => BordersImageLayer.RasterizeCoastAndBorders(coast, borders, w, h));
        return (baseImage, lines);
    }
}
