using EarthRelief3D.App.Presentation;

namespace EarthRelief3D.Tests;

/// <summary>Plan 4 Task 5: distances in whole kilometres or miles (P4-6, P4-15).</summary>
public class DistanceTextTests
{
    [Fact]
    public void KilometresAndMiles()
    {
        Assert.Equal("812 km", CrisisText.Distance(812.4, DistanceUnit.Kilometres));
        Assert.Equal("505 mi", CrisisText.Distance(812.4, DistanceUnit.Miles));   // 812.4 / 1.609344 = 504.8
        Assert.Equal("12,345 km", CrisisText.Distance(12345, DistanceUnit.Kilometres));
        Assert.Equal("7,671 mi", CrisisText.Distance(12345, DistanceUnit.Miles));
        Assert.Equal("0 km", CrisisText.Distance(0.2, DistanceUnit.Kilometres));
        Assert.Equal(DistanceUnit.Kilometres, default(DistanceUnit));   // the default unit
    }
}
