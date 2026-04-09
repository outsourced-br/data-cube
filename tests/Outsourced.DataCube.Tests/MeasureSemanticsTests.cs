namespace Outsourced.DataCube.Tests;

using Builders;
using NUnit.Framework;

[TestFixture]
public sealed class MeasureSemanticsTests
{
  [Test]
  public void Additive_sales_metrics_continue_to_sum_across_dimensions()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var sales = cube.AddCurrencyMetric("sales", "EUR", "Sales");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-01")
      .WithMetricValue(sales, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-02")
      .WithMetricValue(sales, 150m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(month, "2026-02")
      .WithMetricValue(sales, 90m)
      .Build();

    var grouped = cube.GroupBy(new[] { "region" }, sales)
      .ToDictionary(result => result.Key["region"].Key, result => result.Value);

    Assert.That(sales.Semantics.Additivity, Is.EqualTo(MetricAdditivity.Additive));
    Assert.That(cube.Aggregate(sales), Is.EqualTo(340m));
    Assert.That(grouped["North"], Is.EqualTo(250m));
    Assert.That(grouped["South"], Is.EqualTo(90m));
  }

  [Test]
  public void Semi_additive_last_value_rolls_up_balances_from_the_latest_time_member()
  {
    var cube = new AnalyticsCube();
    var account = cube.AddTypedDimension<string>("account", "Account");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var balance = MetricBuilder<decimal>.Currency(cube, "endingBalance", "EUR")
      .WithLabel("Ending Balance")
      .AsSemiAdditive("month", SemiAdditiveAggregationType.LastValue)
      .Build();

    var year2026 = month.CreateValue("2026", "2026", "2026");
    var january = month.CreateValue("2026-01", "January 2026", "2026-01");
    var february = month.CreateValue("2026-02", "February 2026", "2026-02");
    var calendar = month.CreateHierarchy("calendar", "Calendar");
    calendar.AddLevel("year", "Year");
    calendar.AddLevel("month", "Month");
    calendar.MapValue("year", year2026);
    calendar.MapValue("month", january, year2026);
    calendar.MapValue("month", february, year2026);

    cube.CreateFactGroup()
      .WithDimensionValue(account, "A-100")
      .WithDimensionValue(month, january)
      .WithMetricValue(balance, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(account, "A-100")
      .WithDimensionValue(month, february)
      .WithMetricValue(balance, 120m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(account, "B-200")
      .WithDimensionValue(month, january)
      .WithMetricValue(balance, 50m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(account, "B-200")
      .WithDimensionValue(month, february)
      .WithMetricValue(balance, 70m)
      .Build();

    var byYear = cube.AggregateAtLevel("month", "calendar", "year", balance);

    Assert.That(balance.Semantics.Additivity, Is.EqualTo(MetricAdditivity.SemiAdditive));
    Assert.That(balance.Semantics.SemiAdditive?.Aggregation, Is.EqualTo(SemiAdditiveAggregationType.LastValue));
    Assert.That(cube.Aggregate(balance), Is.EqualTo(190m));
    Assert.That(byYear.Single().Value, Is.EqualTo(190m));
  }

  [Test]
  public void Semi_additive_last_non_empty_skips_latest_missing_snapshots()
  {
    var cube = new AnalyticsCube();
    var account = cube.AddTypedDimension<string>("account", "Account");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var lastValueBalance = MetricBuilder<decimal>.Currency(cube, "lastValueBalance", "EUR")
      .WithLabel("Last Value Balance")
      .AsSemiAdditive("month", SemiAdditiveAggregationType.LastValue)
      .Build();
    var lastNonEmptyBalance = MetricBuilder<decimal>.Currency(cube, "lastNonEmptyBalance", "EUR")
      .WithLabel("Last Non Empty Balance")
      .AsSemiAdditive("month", SemiAdditiveAggregationType.LastNonEmpty)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(account, "A-100")
      .WithDimensionValue(month, "2026-01")
      .WithMetricValue(lastValueBalance, 100m)
      .WithMetricValue(lastNonEmptyBalance, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(account, "A-100")
      .WithDimensionValue(month, "2026-02")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(account, "B-200")
      .WithDimensionValue(month, "2026-01")
      .WithMetricValue(lastValueBalance, 50m)
      .WithMetricValue(lastNonEmptyBalance, 50m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(account, "B-200")
      .WithDimensionValue(month, "2026-02")
      .WithMetricValue(lastValueBalance, 70m)
      .WithMetricValue(lastNonEmptyBalance, 70m)
      .Build();

    Assert.That(cube.Aggregate(lastValueBalance), Is.EqualTo(70m));
    Assert.That(cube.Aggregate(lastNonEmptyBalance), Is.EqualTo(170m));
  }

  [Test]
  public void Non_additive_ratio_metrics_reject_multi_fact_group_rollups()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var marginPercent = cube.AddPercentageMetric("marginPercent", "Margin Percent");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithMetricValue(marginPercent, 10d)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithMetricValue(marginPercent, 20d)
      .Build();

    var north = cube.Slice("region", "North");

    Assert.That(marginPercent.Semantics.Additivity, Is.EqualTo(MetricAdditivity.NonAdditive));
    Assert.That(north.Aggregate(marginPercent), Is.EqualTo(10d));
    Assert.That(
      () => cube.Aggregate(marginPercent),
      Throws.TypeOf<InvalidOperationException>().With.Message.Contains("non-additive"));
  }
}
