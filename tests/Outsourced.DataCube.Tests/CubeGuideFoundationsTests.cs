using Outsourced.DataCube.Tests.Shared;
using NUnit.Framework;

namespace Outsourced.DataCube.Tests;

[TestFixture]
public sealed class CubeGuideFoundationsTests
{
  [Test]
  public void A_single_cube_can_power_slice_dice_pivot_and_totals_without_rebuilding_the_dataset()
  {
    // Scenario: one team needs regional totals, a period-specific cut, and a channel comparison from the same data.
    // Why a cube helps: we load the data once and ask several OLAP-style questions without reshaping it into new tables each time.
    var rows = BusinessNeutralUseCaseData.GetRegionalPerformanceRows();

    var cube = new AnalyticsCube
    {
      Key = "regional-performance",
      Label = "Regional Performance",
      PopulationCount = rows.Length,
    };

    var region = cube.AddTypedDimension<string>("region", "Region");
    var period = cube.AddTypedDimension<string>("period", "Period");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    foreach (var row in rows)
    {
      cube.CreateFactGroup()
        .WithDimensionValue(region, row.Region)
        .WithDimensionValue(period, row.Period)
        .WithDimensionValue(channel, row.Channel)
        .WithMetricValue(revenue, row.Revenue)
        .Build();
    }

    var northSlice = cube.Slice("region", "North");
    var aprilNorthDirect = cube.Dice(new Dictionary<string, object>
    {
      ["region"] = "North",
      ["period"] = "2026-04",
      ["channel"] = "Direct",
    });
    var revenueByRegion = cube.Pivot("region", revenue);

    Assert.That(northSlice.Aggregate(revenue), Is.EqualTo(3500m));
    Assert.That(aprilNorthDirect.Aggregate(revenue), Is.EqualTo(1200m));
    Assert.That(revenueByRegion["North"], Is.EqualTo(3500m));
    Assert.That(revenueByRegion["South"], Is.EqualTo(2200m));
    Assert.That(cube.FactGroups.Count, Is.EqualTo(rows.Length));
    Assert.That(cube.GetDimension("region").Values.Count, Is.EqualTo(2));
  }

}
