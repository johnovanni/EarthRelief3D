using EarthRelief3D.Core.Logic;

namespace EarthRelief3D.Tests;

public class TrendTests
{
    private static Dictionary<DateOnly, int> Months(int count, DateOnly last, Func<int, int> value) =>
        Enumerable.Range(0, count).ToDictionary(i => last.AddMonths(-i), value);

    [Fact]
    public void TrendNeedsSixMonths()
    {
        var last = new DateOnly(2026, 10, 1);
        Assert.Null(Trend.FromMonths(Months(5, last, _ => 1), last));
        Assert.Null(Trend.FromMonths(new Dictionary<DateOnly, int>(), last));
        var gap = Months(6, last, _ => 1);
        gap.Remove(last.AddMonths(-2));
        Assert.Null(Trend.FromMonths(gap, last));
        Assert.NotNull(Trend.FromMonths(Months(6, last, _ => 1), last));
    }

    [Fact]
    public void TrendComparesLastThreeWithThreeBefore()
    {
        var last = new DateOnly(2026, 2, 1);    // crosses the year boundary
        var data = Months(8, last, i => i < 3 ? 100 : i < 6 ? 10 : 9999);
        var t = Trend.FromMonths(data, new DateOnly(2026, 2, 17))!;   // any day of the month
        Assert.Equal(300, t.Recent);
        Assert.Equal(30, t.Previous);
    }
}
