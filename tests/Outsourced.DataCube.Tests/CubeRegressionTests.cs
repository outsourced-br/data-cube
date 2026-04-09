namespace Outsourced.DataCube.Tests;

using NUnit.Framework;
using Metrics;

[TestFixture]
public sealed class CubeRegressionTests
{
  [Test]
  public void Dimension_value_hash_set_treats_case_variants_as_duplicates()
  {
    ISet<DimensionValue> values = new HashSet<DimensionValue>();

    values.Add(new DimensionValue("NL", "Netherlands", "NL"));
    values.Add(new DimensionValue("nl", "netherlands", "NL"));

    Assert.That(values.Count, Is.EqualTo(1));
  }

  [Test]
  public void Create_time_dimension_uses_distinct_member_keys()
  {
    var cube = new AnalyticsCube();

    var dimension = cube.CreateTimeDimension(
      new DateTime(2025, 1, 1),
      new DateTime(2025, 1, 3),
      TimeSpan.FromDays(1));

    Assert.That(
      dimension.Values.Select(static value => value.Key).OrderBy(static key => key),
      Is.EqualTo(new[] { "2025-01-01", "2025-01-02", "2025-01-03" }));
  }

  [Test]
  public void Untyped_dimension_helper_uses_member_keys_instead_of_dimension_key()
  {
    var dimension = new Dimension("region", "Region");
    var firstFactGroup = new FactGroup();
    var secondFactGroup = new FactGroup();

    firstFactGroup.SetDimensionValue(dimension, "NL");
    secondFactGroup.SetDimensionValue(dimension, "BE");

    Assert.That(
      dimension.Values.Select(static value => value.Key).OrderBy(static key => key),
      Is.EqualTo(new[] { "BE", "NL" }));
    Assert.That(firstFactGroup.GetDimensionValue(dimension)?.Key, Is.EqualTo("NL"));
    Assert.That(secondFactGroup.GetDimensionValue(dimension)?.Key, Is.EqualTo("BE"));
  }

  [Test]
  public void Aggregate_and_average_skip_missing_metric_keys()
  {
    var cube = new AnalyticsCube();
    var scoreMetric = cube.AddMetric(new AverageMetric("score", "Score"));
    var otherMetric = cube.AddMetric(new AverageMetric("other", "Other"));

    var firstFactGroup = cube.CreateAddFactGroup();
    firstFactGroup.SetMetricValue(scoreMetric, 100d);

    var secondFactGroup = cube.CreateAddFactGroup();
    secondFactGroup.SetMetricValue(otherMetric, 25d);

    Assert.That(cube.Aggregate(scoreMetric), Is.EqualTo(100d));
    Assert.That(cube.Average(scoreMetric), Is.EqualTo(100d));
  }

  [Test]
  public void Try_get_metric_value_returns_false_when_metric_key_is_missing()
  {
    var cube = new AnalyticsCube();
    var dimension = new Dimension<string>("region", "Region");
    var scoreMetric = cube.AddMetric(new AverageMetric("score", "Score"));
    var otherMetric = cube.AddMetric(new AverageMetric("other", "Other"));

    cube.AddDimension(dimension);

    var factGroup = cube.CreateAddFactGroup();
    factGroup.SetDimensionValue(dimension, "NL");
    factGroup.SetMetricValue(otherMetric, 25d);

    var regionValue = dimension.GetOrCreateValue("NL");

    Assert.That(cube.TryGetMetricValue(scoreMetric, regionValue, out var score), Is.False);
    Assert.That(score, Is.EqualTo(default(double)));
  }

  [Test]
  public void Cube_registries_treat_dimension_and_metric_keys_case_insensitively()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    Assert.That(cube.GetDimension("REGION"), Is.SameAs(region));
    Assert.That(cube.GetMetric("REVENUE"), Is.SameAs(revenue));
    Assert.That(() => cube.AddDimension(new Dimension("REGION", "Other Region")), Throws.ArgumentException);
    Assert.That(() => cube.AddMetric(new CurrencyMetric("REVENUE", "Revenue", "EUR")), Throws.ArgumentException);
  }

  [Test]
  public void Typed_dimension_reuses_existing_member_for_exact_duplicate_values()
  {
    var region = new Dimension<string>("region", "Region");

    var first = region.GetOrCreateValue("EU");
    var second = region.GetOrCreateValue("EU");

    Assert.That(second, Is.SameAs(first));
    Assert.That(region.GetValue("eu"), Is.SameAs(first));
    Assert.That(region.Values.Count, Is.EqualTo(1));
  }

  [Test]
  public void Composite_dimension_registry_collapses_identical_member_payloads()
  {
    var reviewPair = new CompositeDimension("leftId", "rightId")
    {
      Key = "reviewPair",
      Label = "Review Pair"
    };

    reviewPair.CreateCompositeValue(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      ["leftId"] = "REC-001",
      ["rightId"] = "REC-002",
    });
    reviewPair.CreateCompositeValue(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      ["leftId"] = "REC-001",
      ["rightId"] = "REC-002",
    });

    Assert.That(reviewPair.Values.Count, Is.EqualTo(1));
  }

  [Test]
  public void Fact_group_rejects_metric_values_with_the_wrong_clr_type()
  {
    Metric revenue = new CurrencyMetric("revenue", "Revenue", "EUR");
    var factGroup = new FactGroup();

    Assert.That(
      () => factGroup.SetMetricValue(revenue, 5),
      Throws.TypeOf<ArgumentException>().With.Message.Contains("revenue"));
  }

  [Test]
  public void Fact_group_rejects_metric_reads_with_the_wrong_clr_type()
  {
    Metric revenue = new CurrencyMetric("revenue", "Revenue", "EUR");
    var factGroup = new FactGroup();
    factGroup.SetMetricValue(revenue, 12.5m);

    Assert.That(
      () => factGroup.GetMetricValue<int>(revenue),
      Throws.TypeOf<ArgumentException>().With.Message.Contains("revenue"));
  }

  [Test]
  public void Aggregate_returns_default_when_requested_metric_is_missing_from_every_fact_group()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var count = cube.AddCountMetric("count", "Count");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithMetricValue(count, 2)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "US")
      .WithMetricValue(count, 3)
      .Build();

    Assert.That(cube.Aggregate(revenue), Is.EqualTo(0m));
    Assert.That(cube.Average(revenue), Is.EqualTo(0m));
  }

  [Test]
  public void Pivot_ignores_fact_groups_without_the_requested_metric()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");
    var count = cube.AddCountMetric("count", "Count");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithMetricValue(count, 2)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "US")
      .WithMetricValue(revenue, 50m)
      .Build();

    var result = cube.Pivot("region", revenue);

    Assert.That(result["EU"], Is.EqualTo(100m));
    Assert.That(result["US"], Is.EqualTo(50m));
  }

  [Test]
  public void Group_by_and_structured_one_axis_pivot_support_multiple_dimension_keys()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");
    var count = cube.AddCountMetric("count", "Count");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(month, "2026-05")
      .WithMetricValue(revenue, 90m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(revenue, 120m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(month, "2026-05")
      .WithMetricValue(revenue, 10m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-06")
      .WithMetricValue(revenue, 75m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(count, 2)
      .Build();

    var grouped = cube.GroupBy(new[] { "region", "month" }, revenue);
    var pivoted = cube.Pivot(new[] { "region", "month" }, revenue);

    Assert.That(grouped.Count, Is.EqualTo(3));
    Assert.That(
      grouped.Select(static result => (
        Region: result.Key["region"].Key,
        Month: result.Key["month"].Key,
        Revenue: result.Value)),
      Is.EqualTo(new[]
      {
        ("South", "2026-05", 100m),
        ("North", "2026-04", 120m),
        ("North", "2026-06", 75m),
      }));
    Assert.That(grouped[0].Key.Parts.Select(static part => part.DimensionKey), Is.EqualTo(new[] { "region", "month" }));
    Assert.That(
      pivoted.Select(static result => (
        Region: result.Key["region"].Key,
        Month: result.Key["month"].Key,
        Revenue: result.Value)),
      Is.EqualTo(grouped.Select(static result => (
        Region: result.Key["region"].Key,
        Month: result.Key["month"].Key,
        Revenue: result.Value))));
  }

  [Test]
  public void Pivot_supports_two_axis_cross_tab_results()
  {
    var cube = new AnalyticsCube();
    var category = cube.AddTypedDimension<string>("category", "Category");
    var quarter = cube.AddTypedDimension<string>("quarter", "Quarter");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");
    var count = cube.AddCountMetric("count", "Count");

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Hardware")
      .WithDimensionValue(quarter, "Q1")
      .WithMetricValue(revenue, 150m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Software")
      .WithDimensionValue(quarter, "Q1")
      .WithMetricValue(revenue, 200m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Hardware")
      .WithDimensionValue(quarter, "Q2")
      .WithMetricValue(revenue, 175m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Hardware")
      .WithDimensionValue(quarter, "Q1")
      .WithMetricValue(revenue, 50m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Services")
      .WithDimensionValue(quarter, "Q1")
      .WithMetricValue(count, 3)
      .Build();

    var result = cube.Pivot(new[] { "category" }, new[] { "quarter" }, revenue);

    Assert.That(result.Rows.Select(static row => row["category"].Key), Is.EqualTo(new[] { "Hardware", "Software" }));
    Assert.That(result.Columns.Select(static column => column["quarter"].Key), Is.EqualTo(new[] { "Q1", "Q2" }));
    Assert.That(result.Cells.Select(static cell => (
      Category: cell.RowKey["category"].Key,
      Quarter: cell.ColumnKey["quarter"].Key,
      Revenue: cell.Value)),
      Is.EqualTo(new[]
      {
        ("Hardware", "Q1", 200m),
        ("Hardware", "Q2", 175m),
        ("Software", "Q1", 200m),
      }));
    Assert.That(result.TryGetValue(result.Rows[0], result.Columns[0], out var hardwareQ1), Is.True);
    Assert.That(hardwareQ1, Is.EqualTo(200m));
    Assert.That(result.TryGetValue(result.Rows[1], result.Columns[1], out _), Is.False);
  }

  [Test]
  public void Group_by_can_return_subtotals_and_grand_total_with_explicit_all_members()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(month, "2026-05")
      .WithMetricValue(revenue, 90m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(revenue, 120m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(month, "2026-05")
      .WithMetricValue(revenue, 10m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-06")
      .WithMetricValue(revenue, 75m)
      .Build();

    var results = cube.GroupBy(
      new[] { "region", "month" },
      revenue,
      new GroupTotalsOptions
      {
        IncludeSubtotals = true,
        IncludeGrandTotal = true,
      });

    Assert.That(results.Count, Is.EqualTo(6));
    Assert.That(results.Count(static result => result.Kind == RollupKind.Leaf), Is.EqualTo(3));
    Assert.That(results.Count(static result => result.Kind == RollupKind.Subtotal), Is.EqualTo(2));
    Assert.That(results.Count(static result => result.Kind == RollupKind.GrandTotal), Is.EqualTo(1));

    var northSubtotal = results.Single(result =>
      result.Kind == RollupKind.Subtotal &&
      result.Key["region"].Key == "North" &&
      result.Key["month"].IsTotal);
    var southSubtotal = results.Single(result =>
      result.Kind == RollupKind.Subtotal &&
      result.Key["region"].Key == "South" &&
      result.Key["month"].IsTotal);
    var grandTotal = results.Single(result => result.Kind == RollupKind.GrandTotal);

    Assert.That(northSubtotal.Key["month"].Key, Is.EqualTo(Constants.ALL_LABEL));
    Assert.That(northSubtotal.Value, Is.EqualTo(195m));
    Assert.That(southSubtotal.Value, Is.EqualTo(100m));
    Assert.That(grandTotal.Key["region"].IsTotal, Is.True);
    Assert.That(grandTotal.Key["month"].IsTotal, Is.True);
    Assert.That(grandTotal.Value, Is.EqualTo(295m));

    var northLeafSum = results
      .Where(result => result.Kind == RollupKind.Leaf && result.Key["region"].Key == "North")
      .Sum(static result => result.Value);
    var southLeafSum = results
      .Where(result => result.Kind == RollupKind.Leaf && result.Key["region"].Key == "South")
      .Sum(static result => result.Value);
    var grandLeafSum = results
      .Where(result => result.Kind == RollupKind.Leaf)
      .Sum(static result => result.Value);

    Assert.That(northSubtotal.Value, Is.EqualTo(northLeafSum));
    Assert.That(southSubtotal.Value, Is.EqualTo(southLeafSum));
    Assert.That(grandTotal.Value, Is.EqualTo(grandLeafSum));
  }

  [Test]
  public void Pivot_can_return_axis_subtotals_row_totals_column_totals_and_grand_total()
  {
    var cube = new AnalyticsCube();
    var category = cube.AddTypedDimension<string>("category", "Category");
    var region = cube.AddTypedDimension<string>("region", "Region");
    var quarter = cube.AddTypedDimension<string>("quarter", "Quarter");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Hardware")
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(quarter, "Q1")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Hardware")
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(quarter, "Q1")
      .WithDimensionValue(channel, "Partner")
      .WithMetricValue(revenue, 50m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Hardware")
      .WithDimensionValue(region, "US")
      .WithDimensionValue(quarter, "Q1")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 75m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Hardware")
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(quarter, "Q2")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 10m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Software")
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(quarter, "Q2")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 200m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(category, "Software")
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(quarter, "Q2")
      .WithDimensionValue(channel, "Partner")
      .WithMetricValue(revenue, 25m)
      .Build();

    var result = cube.Pivot(
      new[] { "category", "region" },
      new[] { "quarter", "channel" },
      revenue,
      new PivotTotalsOptions
      {
        IncludeRowSubtotals = true,
        IncludeColumnSubtotals = true,
        IncludeRowTotals = true,
        IncludeColumnTotals = true,
        IncludeGrandTotal = true,
      });

    Assert.That(result.Rows.Count, Is.EqualTo(6));
    Assert.That(result.Columns.Count, Is.EqualTo(7));

    var hardwareEuRow = FindCoordinate(result.Rows, ("category", "Hardware"), ("region", "EU"));
    var hardwareUsRow = FindCoordinate(result.Rows, ("category", "Hardware"), ("region", "US"));
    var hardwareTotalRow = FindCoordinate(result.Rows, ("category", "Hardware"), ("region", Constants.ALL_LABEL));
    var grandTotalRow = FindCoordinate(result.Rows, ("category", Constants.ALL_LABEL), ("region", Constants.ALL_LABEL));

    var q1DirectColumn = FindCoordinate(result.Columns, ("quarter", "Q1"), ("channel", "Direct"));
    var q1PartnerColumn = FindCoordinate(result.Columns, ("quarter", "Q1"), ("channel", "Partner"));
    var q1TotalColumn = FindCoordinate(result.Columns, ("quarter", "Q1"), ("channel", Constants.ALL_LABEL));
    var allColumnsTotalColumn = FindCoordinate(result.Columns, ("quarter", Constants.ALL_LABEL), ("channel", Constants.ALL_LABEL));

    Assert.That(hardwareTotalRow.Kind, Is.EqualTo(RollupKind.Subtotal));
    Assert.That(hardwareTotalRow["region"].IsTotal, Is.True);
    Assert.That(q1TotalColumn.Kind, Is.EqualTo(RollupKind.Subtotal));
    Assert.That(q1TotalColumn["channel"].IsTotal, Is.True);
    Assert.That(grandTotalRow.Kind, Is.EqualTo(RollupKind.GrandTotal));
    Assert.That(allColumnsTotalColumn.Kind, Is.EqualTo(RollupKind.GrandTotal));

    Assert.That(GetRequiredValue(result, hardwareEuRow, q1DirectColumn), Is.EqualTo(100m));
    Assert.That(GetRequiredValue(result, hardwareTotalRow, q1DirectColumn), Is.EqualTo(175m));
    Assert.That(GetRequiredValue(result, hardwareEuRow, q1TotalColumn), Is.EqualTo(150m));
    Assert.That(GetRequiredValue(result, hardwareEuRow, allColumnsTotalColumn), Is.EqualTo(160m));
    Assert.That(GetRequiredValue(result, grandTotalRow, q1DirectColumn), Is.EqualTo(175m));
    Assert.That(GetRequiredValue(result, grandTotalRow, allColumnsTotalColumn), Is.EqualTo(460m));

    var hardwareVisibleChildRowTotals =
      GetRequiredValue(result, hardwareEuRow, allColumnsTotalColumn) +
      GetRequiredValue(result, hardwareUsRow, allColumnsTotalColumn);
    var q1VisibleChildColumnTotals =
      GetRequiredValue(result, grandTotalRow, q1DirectColumn) +
      GetRequiredValue(result, grandTotalRow, q1PartnerColumn);
    var visibleLeafRowTotals = result.Rows
      .Where(static row => row.Kind == RollupKind.Leaf)
      .Sum(row => GetRequiredValue(result, row, allColumnsTotalColumn));
    var visibleLeafColumnTotals = result.Columns
      .Where(static column => column.Kind == RollupKind.Leaf)
      .Sum(column => GetRequiredValue(result, grandTotalRow, column));

    Assert.That(GetRequiredValue(result, hardwareTotalRow, allColumnsTotalColumn), Is.EqualTo(hardwareVisibleChildRowTotals));
    Assert.That(GetRequiredValue(result, grandTotalRow, q1TotalColumn), Is.EqualTo(q1VisibleChildColumnTotals));
    Assert.That(GetRequiredValue(result, grandTotalRow, allColumnsTotalColumn), Is.EqualTo(visibleLeafRowTotals));
    Assert.That(GetRequiredValue(result, grandTotalRow, allColumnsTotalColumn), Is.EqualTo(visibleLeafColumnTotals));
    Assert.That(
      result.Cells.Single(cell => ReferenceEquals(cell.RowKey, grandTotalRow) && ReferenceEquals(cell.ColumnKey, allColumnsTotalColumn)).Kind,
      Is.EqualTo(RollupKind.GrandTotal));
  }

  [Test]
  public void Dice_can_apply_multiple_filters_before_pivoting()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 1200m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 300m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Partner")
      .WithMetricValue(revenue, 800m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-05")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 400m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "US")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 900m)
      .Build();

    var filtered = cube.Dice(new Dictionary<string, object>
    {
      ["REGION"] = "EU",
      ["Month"] = "2026-04",
    });
    var revenueByChannel = filtered.Pivot("CHANNEL", revenue);

    Assert.That(filtered.FactGroups.Count, Is.EqualTo(3));
    Assert.That(revenueByChannel.Count, Is.EqualTo(2));
    Assert.That(revenueByChannel["Direct"], Is.EqualTo(1500m));
    Assert.That(revenueByChannel["Partner"], Is.EqualTo(800m));
  }

  [Test]
  public void Add_metric_value_reuses_the_same_fact_group_for_the_same_coordinate()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.AddMetricValue(
      new Dictionary<string, DimensionValue>(StringComparer.OrdinalIgnoreCase)
      {
        ["region"] = region.GetOrCreateValue("EU"),
        ["month"] = month.GetOrCreateValue("2026-04"),
      },
      revenue,
      100m);

    cube.AddMetricValue(
      new Dictionary<string, DimensionValue>(StringComparer.OrdinalIgnoreCase)
      {
        ["MONTH"] = month.GetOrCreateValue("2026-04"),
        ["REGION"] = new DimensionValue("eu", "Europe", "EU"),
      },
      revenue,
      250m);

    Assert.That(cube.FactGroups.Count, Is.EqualTo(1));
    Assert.That(cube.FactGroups[0].GetMetricValue<decimal>(revenue), Is.EqualTo(250m));
  }

  [Test]
  public void Add_metric_value_finds_fact_groups_inserted_before_their_dimensions_are_set()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    var factGroup = cube.CreateAddFactGroup();
    factGroup.SetDimensionValue(region, "EU");
    factGroup.SetDimensionValue(month, "2026-04");

    cube.AddMetricValue(
      new Dictionary<string, DimensionValue>(StringComparer.OrdinalIgnoreCase)
      {
        ["region"] = region.GetOrCreateValue("EU"),
        ["month"] = month.GetOrCreateValue("2026-04"),
      },
      revenue,
      125m);

    Assert.That(cube.FactGroups.Count, Is.EqualTo(1));
    Assert.That(cube.FactGroups[0], Is.SameAs(factGroup));
    Assert.That(factGroup.GetMetricValue<decimal>(revenue), Is.EqualTo(125m));
  }

  [Test]
  public void Slice_uses_exact_coordinate_lookup_when_each_fact_group_has_one_dimension()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithMetricValue(revenue, 50m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "US")
      .WithMetricValue(revenue, 25m)
      .Build();

    var sliced = cube.Slice("region", "EU");

    Assert.That(sliced.FactGroups.Count, Is.EqualTo(2));
    Assert.That(sliced.Aggregate(revenue), Is.EqualTo(150m));
  }

  [Test]
  public void Slice_supports_dimension_value_predicates_for_numeric_ranges()
  {
    var cube = new AnalyticsCube();
    var quantity = cube.AddTypedDimension<int>("quantity", "Quantity");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(quantity, 5)
      .WithMetricValue(revenue, 10m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(quantity, 15)
      .WithMetricValue(revenue, 20m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(quantity, 25)
      .WithMetricValue(revenue, 30m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(quantity, 35)
      .WithMetricValue(revenue, 40m)
      .Build();

    var filtered = cube.Slice("quantity", value => value.Value is int numericValue && numericValue > 10 && numericValue < 30);

    Assert.That(filtered.FactGroups.Count, Is.EqualTo(2));
    Assert.That(filtered.Aggregate(revenue), Is.EqualTo(50m));
  }

  [Test]
  public void Slice_supports_typed_predicates_for_date_ranges()
  {
    var cube = new AnalyticsCube();
    var orderDate = cube.AddTypedDimension<DateTime>("orderDate", "Order Date");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(orderDate, new DateTime(2026, 3, 31))
      .WithMetricValue(revenue, 90m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(orderDate, new DateTime(2026, 4, 1))
      .WithMetricValue(revenue, 120m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(orderDate, new DateTime(2026, 4, 15))
      .WithMetricValue(revenue, 180m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(orderDate, new DateTime(2026, 5, 1))
      .WithMetricValue(revenue, 210m)
      .Build();

    var filtered = cube.Slice<DateTime>(
      "orderDate",
      value => value >= new DateTime(2026, 4, 1) && value <= new DateTime(2026, 4, 30));

    Assert.That(filtered.FactGroups.Count, Is.EqualTo(2));
    Assert.That(filtered.Aggregate(revenue), Is.EqualTo(300m));
  }

  [Test]
  public void Dice_supports_string_inclusion_and_exclusion_helpers()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var segment = cube.AddTypedDimension<string>("segment", "Segment");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(segment, "Retail")
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "LATAM")
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(segment, "Retail")
      .WithMetricValue(revenue, 80m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(channel, "Partner")
      .WithDimensionValue(segment, "Retail")
      .WithMetricValue(revenue, 50m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "US")
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(segment, "Retail")
      .WithMetricValue(revenue, 120m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(segment, "Wholesale")
      .WithMetricValue(revenue, 60m)
      .Build();

    var filtered = cube.Dice(new Dictionary<string, Func<DimensionValue, bool>>
    {
      ["region"] = DimensionFilters.In("EU", "LATAM"),
      ["channel"] = DimensionFilters.NotIn("Partner"),
      ["segment"] = DimensionFilters.EqualTo("Retail"),
    });

    Assert.That(filtered.FactGroups.Count, Is.EqualTo(2));
    Assert.That(filtered.Aggregate(revenue), Is.EqualTo(180m));
  }

  [Test]
  public void Dice_supports_numeric_comparison_helpers()
  {
    var cube = new AnalyticsCube();
    var units = cube.AddTypedDimension<int>("units", "Units");
    var score = cube.AddTypedDimension<int>("score", "Score");
    var discount = cube.AddTypedDimension<decimal>("discount", "Discount");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(units, 15)
      .WithDimensionValue(score, 80)
      .WithDimensionValue(discount, 0.10m)
      .WithMetricValue(revenue, 150m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(units, 20)
      .WithDimensionValue(score, 75)
      .WithDimensionValue(discount, 0.05m)
      .WithMetricValue(revenue, 200m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(units, 9)
      .WithDimensionValue(score, 90)
      .WithDimensionValue(discount, 0.05m)
      .WithMetricValue(revenue, 90m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(units, 12)
      .WithDimensionValue(score, 45)
      .WithDimensionValue(discount, 0.05m)
      .WithMetricValue(revenue, 120m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(units, 16)
      .WithDimensionValue(score, 90)
      .WithDimensionValue(discount, 0.20m)
      .WithMetricValue(revenue, 160m)
      .Build();

    var filtered = cube.Dice(new Dictionary<string, Func<DimensionValue, bool>>
    {
      ["units"] = DimensionFilters.Between(10, 20),
      ["score"] = DimensionFilters.GreaterThan(50),
      ["discount"] = DimensionFilters.LessThan(0.15m),
    });

    Assert.That(filtered.FactGroups.Count, Is.EqualTo(2));
    Assert.That(filtered.Aggregate(revenue), Is.EqualTo(350m));
  }

  [Test]
  public void Dice_exact_coordinate_filters_keep_duplicate_fact_groups_for_the_same_coordinate()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(revenue, 120m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(revenue, 80m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-05")
      .WithMetricValue(revenue, 200m)
      .Build();

    var filtered = cube.Dice(new Dictionary<string, object>
    {
      ["region"] = "EU",
      ["month"] = "2026-04",
    });

    Assert.That(filtered.FactGroups.Count, Is.EqualTo(2));
    Assert.That(filtered.Aggregate(revenue), Is.EqualTo(200m));
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
