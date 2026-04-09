namespace Outsourced.DataCube.Tests;

using NUnit.Framework;

[TestFixture]
public sealed class CubeGrainTests
{
  [Test]
  public void Define_grain_allows_fact_groups_matching_required_and_optional_dimensions()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.DefineGrain(grain => grain.Require(region, month).AllowOptional(channel));

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-05")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 200m)
      .Build();

    Assert.That(cube.Aggregate(revenue), Is.EqualTo(300m));
    Assert.That(cube.FactGroups.Count, Is.EqualTo(2));
  }

  [Test]
  public void Create_fact_group_rejects_missing_required_dimensions()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");

    cube.DefineGrain(grain => grain.Require(region, month));

    var exception = Assert.Throws<InvalidOperationException>(() =>
      cube.CreateFactGroup()
        .WithDimensionValue(region, "EU")
        .Build());

    Assert.That(exception?.Message, Does.Contain("Missing required dimensions: month"));
    Assert.That(exception?.Message, Does.Contain("Expected grain: required [region, month]"));
    Assert.That(cube.FactGroups, Is.Empty);
  }

  [Test]
  public void Create_fact_group_rejects_unexpected_dimensions_outside_the_declared_grain()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");

    cube.DefineGrain(grain => grain.Require(region, month));

    var exception = Assert.Throws<InvalidOperationException>(() =>
      cube.CreateFactGroup()
        .WithDimensionValue(region, "EU")
        .WithDimensionValue(month, "2026-04")
        .WithDimensionValue(channel, "Direct")
        .Build());

    Assert.That(exception?.Message, Does.Contain("Unexpected dimensions: channel"));
    Assert.That(exception?.Message, Does.Contain("Fact group dimensions: [region, month, channel]"));
    Assert.That(cube.FactGroups, Is.Empty);
  }

  [Test]
  public void Add_metric_value_rejects_dimension_coordinates_that_do_not_match_the_grain()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    cube.AddTypedDimension<string>("month", "Month");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.DefineGrain(grain => grain.Require("region", "month"));

    var coordinates = new Dictionary<string, DimensionValue>(StringComparer.OrdinalIgnoreCase)
    {
      ["region"] = region.GetOrCreateValue("EU"),
    };

    var exception = Assert.Throws<InvalidOperationException>(() => cube.AddMetricValue(coordinates, revenue, 100m));

    Assert.That(exception?.Message, Does.Contain("Missing required dimensions: month"));
    Assert.That(cube.FactGroups, Is.Empty);
  }

  [Test]
  public void Tracked_dimension_changes_roll_back_when_they_break_the_grain()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");

    cube.DefineGrain(grain => grain.Require(region, month));

    var factGroup = cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .Build();

    var exception = Assert.Throws<InvalidOperationException>(() => factGroup.DimensionValues.Remove("month"));
    var matchingCube = cube.Dice(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      ["region"] = "EU",
      ["month"] = "2026-04",
    });

    Assert.That(exception?.Message, Does.Contain("Missing required dimensions: month"));
    Assert.That(factGroup.GetDimensionValue("month")?.Key, Is.EqualTo("2026-04"));
    Assert.That(cube.FactGroups.Count, Is.EqualTo(1));
    Assert.That(matchingCube.FactGroups.Count, Is.EqualTo(1));
  }

  [Test]
  public void Setting_grain_rejects_existing_fact_groups_that_do_not_match_it()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    cube.AddTypedDimension<string>("month", "Month");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .Build();

    var exception = Assert.Throws<InvalidOperationException>(() => cube.SetGrain(new CubeGrain().Require("region", "month")));

    Assert.That(exception?.Message, Does.Contain("Missing required dimensions: month"));
    Assert.That(cube.Grain, Is.Null);
    Assert.That(cube.FactGroups.Count, Is.EqualTo(1));
  }
}
