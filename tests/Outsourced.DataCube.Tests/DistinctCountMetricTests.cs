namespace Outsourced.DataCube.Tests;

using Metrics;
using NUnit.Framework;

[TestFixture]
public sealed class DistinctCountMetricTests
{
  [Test]
  public void Distinct_count_recomputes_group_totals_from_business_keys_instead_of_summing_leaf_counts()
  {
    var (cube, _, month, distinctCustomers) = CreateCustomerActivityCube();

    var february = cube.Slice("month", "2026-02");
    var grouped = cube.GroupBy(
      new[] { "region", "month" },
      distinctCustomers,
      new GroupTotalsOptions
      {
        IncludeSubtotals = true,
        IncludeGrandTotal = true,
      });

    var northJanuary = FindGroupResult(grouped, RollupKind.Leaf, ("region", "North"), ("month", "2026-01"));
    var northFebruary = FindGroupResult(grouped, RollupKind.Leaf, ("region", "North"), ("month", "2026-02"));
    var southJanuary = FindGroupResult(grouped, RollupKind.Leaf, ("region", "South"), ("month", "2026-01"));
    var southFebruary = FindGroupResult(grouped, RollupKind.Leaf, ("region", "South"), ("month", "2026-02"));
    var northSubtotal = FindGroupResult(grouped, RollupKind.Subtotal, ("region", "North"), ("month", Constants.ALL_LABEL));
    var southSubtotal = FindGroupResult(grouped, RollupKind.Subtotal, ("region", "South"), ("month", Constants.ALL_LABEL));
    var grandTotal = grouped.Single(result => result.Kind == RollupKind.GrandTotal);

    Assert.That(february.Aggregate(distinctCustomers), Is.EqualTo(3));
    Assert.That(cube.Aggregate(distinctCustomers), Is.EqualTo(3));
    Assert.That(northJanuary.Value, Is.EqualTo(1));
    Assert.That(northFebruary.Value, Is.EqualTo(2));
    Assert.That(southJanuary.Value, Is.EqualTo(1));
    Assert.That(southFebruary.Value, Is.EqualTo(2));
    Assert.That(northSubtotal.Value, Is.EqualTo(2));
    Assert.That(southSubtotal.Value, Is.EqualTo(2));
    Assert.That(grandTotal.Value, Is.EqualTo(3));

    var northLeafSum = northJanuary.Value + northFebruary.Value;
    var southLeafSum = southJanuary.Value + southFebruary.Value;
    var visibleLeafSum = grouped
      .Where(static result => result.Kind == RollupKind.Leaf)
      .Sum(static result => result.Value);

    Assert.That(northLeafSum, Is.EqualTo(3));
    Assert.That(southLeafSum, Is.EqualTo(3));
    Assert.That(northSubtotal.Value, Is.LessThan(northLeafSum));
    Assert.That(southSubtotal.Value, Is.LessThan(southLeafSum));
    Assert.That(grandTotal.Value, Is.LessThan(visibleLeafSum));
  }

  [Test]
  public void Distinct_count_pivot_totals_use_the_underlying_fact_groups_for_each_total_cell()
  {
    var (cube, _, _, distinctCustomers) = CreateCustomerActivityCube();

    var result = cube.Pivot(
      new[] { "region" },
      new[] { "month" },
      distinctCustomers,
      new PivotTotalsOptions
      {
        IncludeRowTotals = true,
        IncludeColumnTotals = true,
        IncludeGrandTotal = true,
      });

    var northRow = FindCoordinate(result.Rows, ("region", "North"));
    var southRow = FindCoordinate(result.Rows, ("region", "South"));
    var grandTotalRow = FindCoordinate(result.Rows, ("region", Constants.ALL_LABEL));
    var januaryColumn = FindCoordinate(result.Columns, ("month", "2026-01"));
    var februaryColumn = FindCoordinate(result.Columns, ("month", "2026-02"));
    var grandTotalColumn = FindCoordinate(result.Columns, ("month", Constants.ALL_LABEL));

    Assert.That(GetRequiredValue(result, northRow, januaryColumn), Is.EqualTo(1));
    Assert.That(GetRequiredValue(result, northRow, februaryColumn), Is.EqualTo(2));
    Assert.That(GetRequiredValue(result, northRow, grandTotalColumn), Is.EqualTo(2));
    Assert.That(GetRequiredValue(result, southRow, grandTotalColumn), Is.EqualTo(2));
    Assert.That(GetRequiredValue(result, grandTotalRow, januaryColumn), Is.EqualTo(2));
    Assert.That(GetRequiredValue(result, grandTotalRow, februaryColumn), Is.EqualTo(3));
    Assert.That(GetRequiredValue(result, grandTotalRow, grandTotalColumn), Is.EqualTo(3));

    var northVisibleLeafSum = GetRequiredValue(result, northRow, januaryColumn) + GetRequiredValue(result, northRow, februaryColumn);
    var grandVisibleColumnSum = GetRequiredValue(result, grandTotalRow, januaryColumn) + GetRequiredValue(result, grandTotalRow, februaryColumn);

    Assert.That(GetRequiredValue(result, northRow, grandTotalColumn), Is.LessThan(northVisibleLeafSum));
    Assert.That(GetRequiredValue(result, grandTotalRow, grandTotalColumn), Is.LessThan(grandVisibleColumnSum));
  }

  [Test]
  public void Distinct_count_supports_hierarchy_roll_up_and_drill_down_without_double_counting_shared_members()
  {
    var cube = new AnalyticsCube { Key = "activity", Label = "Customer Activity" };
    var month = cube.AddTypedDimension<string>("month", "Month");
    var customerId = cube.AddTypedDimension<string>("customerId", "Customer");
    var distinctCustomers = cube.AddDistinctCountMetric("distinctCustomers", customerId, "Distinct Customers");

    var year2026 = month.CreateValue("2026", "2026", "2026");
    var quarterOne = month.CreateValue("2026-Q1", "Q1 2026", "Q1 2026");
    var quarterTwo = month.CreateValue("2026-Q2", "Q2 2026", "Q2 2026");
    var january = month.CreateValue("2026-01", "January 2026", "2026-01");
    var february = month.CreateValue("2026-02", "February 2026", "2026-02");
    var april = month.CreateValue("2026-04", "April 2026", "2026-04");
    var may = month.CreateValue("2026-05", "May 2026", "2026-05");

    var calendar = month.CreateHierarchy("calendar", "Calendar");
    calendar.AddLevel("year", "Year");
    calendar.AddLevel("quarter", "Quarter");
    calendar.AddLevel("month", "Month");
    calendar.MapValue("year", year2026);
    calendar.MapValue("quarter", quarterOne, year2026);
    calendar.MapValue("quarter", quarterTwo, year2026);
    calendar.MapValue("month", january, quarterOne);
    calendar.MapValue("month", february, quarterOne);
    calendar.MapValue("month", april, quarterTwo);
    calendar.MapValue("month", may, quarterTwo);

    cube.CreateFactGroup()
      .WithDimensionValue(month, january)
      .WithDimensionValue(customerId, "C-001")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(month, february)
      .WithDimensionValue(customerId, "C-001")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(month, february)
      .WithDimensionValue(customerId, "C-002")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(month, april)
      .WithDimensionValue(customerId, "C-002")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(month, may)
      .WithDimensionValue(customerId, "C-003")
      .Build();

    var byQuarter = cube.AggregateAtLevel("month", "calendar", "quarter", distinctCustomers);
    var yearTotal = cube.AggregateAtLevel("month", "calendar", "year", distinctCustomers).Single();
    var drilled = cube.DrillDown(month, calendar, year2026, distinctCustomers);
    var februaryRollUp = cube.RollUp("month", "calendar", "2026-02", distinctCustomers);

    Assert.That(
      byQuarter.Select(static result => (result.Member.Key, result.Value)),
      Is.EqualTo(new[]
      {
        ("2026-Q1", 2),
        ("2026-Q2", 2),
      }));
    Assert.That(yearTotal.Value, Is.EqualTo(3));
    Assert.That(
      drilled.Select(static result => (result.Member.Key, result.Value)),
      Is.EqualTo(new[]
      {
        ("2026-Q1", 2),
        ("2026-Q2", 2),
      }));
    Assert.That(februaryRollUp, Is.Not.Null);
    Assert.That(februaryRollUp!.Member.Key, Is.EqualTo("2026-Q1"));
    Assert.That(februaryRollUp.Value, Is.EqualTo(2));
    Assert.That(drilled.Sum(static result => result.Value), Is.GreaterThan(yearTotal.Value));
  }

  private static (AnalyticsCube Cube, Dimension<string> Region, Dimension<string> Month, DistinctCountMetric DistinctCustomers) CreateCustomerActivityCube()
  {
    var cube = new AnalyticsCube { Key = "activity", Label = "Customer Activity" };
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var customerId = cube.AddTypedDimension<string>("customerId", "Customer");
    var distinctCustomers = cube.AddDistinctCountMetric("distinctCustomers", customerId, "Distinct Customers");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-01")
      .WithDimensionValue(customerId, "C-001")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-02")
      .WithDimensionValue(customerId, "C-001")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-02")
      .WithDimensionValue(customerId, "C-002")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(month, "2026-01")
      .WithDimensionValue(customerId, "C-002")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(month, "2026-02")
      .WithDimensionValue(customerId, "C-002")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(month, "2026-02")
      .WithDimensionValue(customerId, "C-003")
      .Build();

    return (cube, region, month, distinctCustomers);
  }

  private static GroupedMetricResult<int> FindGroupResult(
    IEnumerable<GroupedMetricResult<int>> results,
    RollupKind kind,
    params (string DimensionKey, string ValueKey)[] parts)
  {
    return results.Single(result =>
      result.Kind == kind &&
      parts.All(part => result.Key[part.DimensionKey].Key == part.ValueKey));
  }

  private static DimensionCoordinate FindCoordinate(
    IEnumerable<DimensionCoordinate> coordinates,
    params (string DimensionKey, string ValueKey)[] parts)
  {
    return coordinates.Single(coordinate =>
      parts.All(part => coordinate[part.DimensionKey].Key == part.ValueKey));
  }

  private static T GetRequiredValue<T>(PivotResult<T> result, DimensionCoordinate row, DimensionCoordinate column)
    where T : struct
  {
    Assert.That(result.TryGetValue(row, column, out var value), Is.True);
    return value;
  }
}
