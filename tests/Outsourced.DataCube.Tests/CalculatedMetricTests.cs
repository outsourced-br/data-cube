namespace Outsourced.DataCube.Tests;

using Metrics;
using NUnit.Framework;

[TestFixture]
public sealed class CalculatedMetricTests
{
  [Test]
  public void Margin_percent_is_computed_from_aggregated_components_after_slice_and_dice()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var profit = cube.AddCurrencyMetric("profit", "EUR", "Profit");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");
    var marginPercent = cube.AddCalculatedMetric(
      "marginPercent",
      dependencies: [profit, revenue],
      calculation: context => context.DivideAsDouble(profit, revenue) * 100d,
      label: "Margin Percent",
      format: "F2");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(profit, 20m)
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(profit, 20m)
      .WithMetricValue(revenue, 300m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-05")
      .WithMetricValue(profit, 45m)
      .WithMetricValue(revenue, 150m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "US")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(profit, 50m)
      .WithMetricValue(revenue, 100m)
      .Build();

    var sliced = cube.Slice("region", "EU");
    var diced = cube.Dice(new Dictionary<string, object>
    {
      ["region"] = "EU",
      ["month"] = "2026-04",
    });

    Assert.That(sliced.Aggregate(marginPercent), Is.EqualTo(15.4545454545d).Within(0.000001d));
    Assert.That(diced.Aggregate(marginPercent), Is.EqualTo(10d).Within(0.000001d));
  }

  [Test]
  public void Average_order_value_supports_group_by_and_two_axis_pivot()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");
    var orders = cube.AddCountMetric("orders", "Orders");
    var averageOrderValue = cube.AddCalculatedMetric(
      "averageOrderValue",
      dependencies: [revenue, orders],
      calculation: context => context.Divide(revenue, orders),
      label: "Average Order Value",
      format: "F2");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 120m)
      .WithMetricValue(orders, 2)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 80m)
      .WithMetricValue(orders, 4)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(channel, "Partner")
      .WithMetricValue(revenue, 90m)
      .WithMetricValue(orders, 3)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 50m)
      .WithMetricValue(orders, 1)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 70m)
      .WithMetricValue(orders, 1)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(channel, "Partner")
      .WithMetricValue(revenue, 200m)
      .WithMetricValue(orders, 10)
      .Build();

    var grouped = cube.GroupBy(new[] { "region" }, averageOrderValue);
    var groupedByRegion = grouped.ToDictionary(result => result.Key["region"].Key, result => result.Value);
    var pivot = cube.Pivot(
      new[] { "region" },
      new[] { "channel" },
      averageOrderValue,
      new PivotTotalsOptions
      {
        IncludeRowTotals = true,
        IncludeColumnTotals = true,
        IncludeGrandTotal = true,
      });

    Assert.That(groupedByRegion["North"], Is.EqualTo(32.2222222222m).Within(0.000001m));
    Assert.That(groupedByRegion["South"], Is.EqualTo(26.6666666667m).Within(0.000001m));

    var northRow = pivot.Rows.Single(row => row["region"].Key == "North");
    var southRow = pivot.Rows.Single(row => row["region"].Key == "South");
    var totalRow = pivot.Rows.Single(row => row["region"].IsTotal);
    var directColumn = pivot.Columns.Single(column => column["channel"].Key == "Direct");
    var partnerColumn = pivot.Columns.Single(column => column["channel"].Key == "Partner");
    var totalColumn = pivot.Columns.Single(column => column["channel"].IsTotal);

    Assert.That(GetRequiredValue(pivot, northRow, directColumn), Is.EqualTo(33.3333333333m).Within(0.000001m));
    Assert.That(GetRequiredValue(pivot, northRow, partnerColumn), Is.EqualTo(30m).Within(0.000001m));
    Assert.That(GetRequiredValue(pivot, southRow, directColumn), Is.EqualTo(60m).Within(0.000001m));
    Assert.That(GetRequiredValue(pivot, southRow, partnerColumn), Is.EqualTo(20m).Within(0.000001m));
    Assert.That(GetRequiredValue(pivot, totalRow, totalColumn), Is.EqualTo(29.0476190476m).Within(0.000001m));
  }

  [Test]
  public void Conversion_rate_rolls_up_from_child_members_using_aggregated_totals()
  {
    var cube = new AnalyticsCube();
    var time = cube.AddTypedDimension<string>("time", "Time");
    var conversions = cube.AddCountMetric("conversions", "Conversions");
    var visits = cube.AddCountMetric("visits", "Visits");
    var conversionRate = cube.AddCalculatedMetric(
      "conversionRate",
      dependencies: [conversions, visits],
      calculation: context => context.DivideAsDouble(conversions, visits) * 100d,
      label: "Conversion Rate",
      format: "F2");

    var year = time.CreateValue("2026", "2026", "2026");
    var q1 = time.CreateValue("2026-Q1", "Q1 2026", "Q1 2026");
    var q2 = time.CreateValue("2026-Q2", "Q2 2026", "Q2 2026");
    var january = time.CreateValue("2026-01", "January 2026", "2026-01");
    var february = time.CreateValue("2026-02", "February 2026", "2026-02");
    var april = time.CreateValue("2026-04", "April 2026", "2026-04");

    var calendar = time.CreateHierarchy("calendar", "Calendar");
    calendar.AddLevel("year", "Year");
    calendar.AddLevel("quarter", "Quarter");
    calendar.AddLevel("month", "Month");
    calendar.MapValue("year", year);
    calendar.MapValue("quarter", q1, year);
    calendar.MapValue("quarter", q2, year);
    calendar.MapValue("month", january, q1);
    calendar.MapValue("month", february, q1);
    calendar.MapValue("month", april, q2);

    cube.CreateFactGroup()
      .WithDimensionValue(time, january)
      .WithMetricValue(conversions, 1)
      .WithMetricValue(visits, 10)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, february)
      .WithMetricValue(conversions, 9)
      .WithMetricValue(visits, 30)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, april)
      .WithMetricValue(conversions, 1)
      .WithMetricValue(visits, 5)
      .Build();

    var q1RollUp = cube.RollUp(time, calendar, january, conversionRate);
    var yearlyRate = cube.AggregateAtLevel("time", "calendar", "year", conversionRate);

    Assert.That(q1RollUp, Is.Not.Null);
    Assert.That(q1RollUp.Value, Is.EqualTo(25d).Within(0.000001d));
    Assert.That(yearlyRate.Single().Value, Is.EqualTo(24.4444444444d).Within(0.000001d));
  }

  [Test]
  public void Calculated_metrics_define_missing_value_and_divide_by_zero_behavior()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");
    var orders = cube.AddCountMetric("orders", "Orders");
    var safeAverageOrderValue = cube.AddCalculatedMetric(
      "safeAverageOrderValue",
      dependencies: [revenue, orders],
      calculation: context => context.Divide(
        revenue,
        orders,
        CalculatedMetricMissingValueBehavior.ReturnDefault,
        CalculatedMetricDivideByZeroBehavior.ReturnDefault),
      label: "Safe Average Order Value",
      format: "F2");
    var strictAverageOrderValue = cube.AddCalculatedMetric(
      "strictAverageOrderValue",
      dependencies: [revenue, orders],
      calculation: context => context.Divide(
        revenue,
        orders,
        CalculatedMetricMissingValueBehavior.Throw,
        CalculatedMetricDivideByZeroBehavior.Throw),
      label: "Strict Average Order Value",
      format: "F2");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "zero-orders")
      .WithMetricValue(revenue, 100m)
      .WithMetricValue(orders, 0)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "missing-revenue")
      .WithMetricValue(orders, 2)
      .Build();

    var zeroOrders = cube.Slice("region", "zero-orders");
    var missingRevenue = cube.Slice("region", "missing-revenue");

    Assert.That(zeroOrders.Aggregate(safeAverageOrderValue), Is.EqualTo(0m));
    Assert.That(missingRevenue.Aggregate(safeAverageOrderValue), Is.EqualTo(0m));
    Assert.That(
      () => zeroOrders.Aggregate(strictAverageOrderValue),
      Throws.TypeOf<DivideByZeroException>().With.Message.Contains("orders"));
    Assert.That(
      () => missingRevenue.Aggregate(strictAverageOrderValue),
      Throws.TypeOf<InvalidOperationException>().With.Message.Contains("missing"));
  }

  private static T GetRequiredValue<T>(PivotResult<T> result, DimensionCoordinate row, DimensionCoordinate column)
    where T : struct
  {
    Assert.That(result.TryGetValue(row, column, out var value), Is.True);
    return value;
  }
}
