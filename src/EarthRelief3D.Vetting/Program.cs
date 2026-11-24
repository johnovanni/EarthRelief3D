namespace EarthRelief3D.Vetting;

internal static class Program
{
    private static Task<int> Main(string[] args) => VettingRun.RunAsync(args, Console.Out, Console.Error);
}
