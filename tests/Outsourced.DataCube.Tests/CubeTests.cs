using NUnit.Framework;

namespace Outsourced.DataCube.Tests;

[TestFixture]
public class CubeTests
{
  [Test]
  public void Slice_ShouldFilterByDimensionValue()
  {
    // Arrange
    var cube = new AnalyticsCube { Key = "sales", Label = "Sales cube" };
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
        .WithDimensionValue(region, "EU")
        .WithDimensionValue(month, "2026-04")
        .WithMetricValue(revenue, 1200m)
        .Build();

    cube.CreateFactGroup()
        .WithDimensionValue(region, "LATAM")
        .WithDimensionValue(month, "2026-04")
        .WithMetricValue(revenue, 800m)
        .Build();

    cube.CreateFactGroup()
        .WithDimensionValue(region, "EU")
        .WithDimensionValue(month, "2026-05")
        .WithMetricValue(revenue, 1500m)
        .Build();

    // Act
    var euCube = cube.Slice("region", "EU");
    var totalEuRevenue = euCube.Aggregate(revenue);

    // Assert
    Assert.That(totalEuRevenue, Is.EqualTo(2700m));
  }

  [Test]
  public void Dice_ShouldFilterByMultipleDimensions()
  {
    // Arrange
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR");

    cube.CreateFactGroup().WithDimensionValue(region, "EU").WithDimensionValue(month, "Jan").WithMetricValue(revenue, 100).Build();
    cube.CreateFactGroup().WithDimensionValue(region, "EU").WithDimensionValue(month, "Feb").WithMetricValue(revenue, 200).Build();
    cube.CreateFactGroup().WithDimensionValue(region, "US").WithDimensionValue(month, "Jan").WithMetricValue(revenue, 300).Build();

    var filter = new Dictionary<string, object>
        {
            { "region", "EU" },
            { "month", "Jan" }
        };

    // Act
    var dicedCube = cube.Dice(filter);
    var result = dicedCube.Aggregate(revenue);

    // Assert
    Assert.That(result, Is.EqualTo(100m));
  }

  [Test]
  public void Pivot_ShouldGroupByDimension()
  {
    // Arrange
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR");

    cube.CreateFactGroup().WithDimensionValue(region, "EU").WithMetricValue(revenue, 1000).Build();
    cube.CreateFactGroup().WithDimensionValue(region, "EU").WithMetricValue(revenue, 500).Build();
    cube.CreateFactGroup().WithDimensionValue(region, "LATAM").WithMetricValue(revenue, 800).Build();

    // Act
    var result = cube.Pivot("region", revenue);

    // Assert
    Assert.That(result.Count, Is.EqualTo(2));
    Assert.That(result["EU"], Is.EqualTo(1500m));
    Assert.That(result["LATAM"], Is.EqualTo(800m));
  }
}
