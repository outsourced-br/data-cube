namespace Outsourced.DataCube.Serialization.Tests.Shared;

using Metrics;
using global::Outsourced.DataCube.Tests.Shared;
using NUnit.Framework;

public abstract class CubeGuideFoundationsJsonContractTests : CubeJsonContractTestBase
{
  [Test]
  public void A_cube_can_keep_grouped_uniqueness_metrics_together_for_each_cohort_and_roundtrip_them_as_json()
  {
    // Scenario: each segment needs both total volume and uniqueness quality for the same shared identifier field.
    // Why a cube helps: one fact group per segment can carry total count, unique count, and uniqueness percentage together.
    var rows = BusinessNeutralUseCaseData.GetCohortIdentifierRows();
    var groupedRows = rows.GroupBy(x => x.Segment, StringComparer.OrdinalIgnoreCase);

    var cube = new AnalyticsCube
    {
      Key = "cohort-uniqueness",
      Label = "Cohort Uniqueness",
      PopulationCount = rows.Length,
    };

    var segment = cube.AddTypedDimension<string>("segment", "Segment");
    var totalCount = cube.AddCountMetric("totalCount", "Total Count");
    var uniqueCount = cube.AddCountMetric("uniqueCount", "Unique Count");
    var uniquenessPercentage = cube.AddMetric(new PercentageMetric("uniquenessPercentage", "Uniqueness Percentage"));

    foreach (var group in groupedRows)
    {
      var total = group.Count();
      var unique = group.Select(x => x.AccountReference).Distinct(StringComparer.OrdinalIgnoreCase).Count();
      var percentage = unique / (double)total * 100d;

      cube.CreateFactGroup()
        .WithDimensionValue(segment, group.Key)
        .WithMetricValue(totalCount, total)
        .WithMetricValue(uniqueCount, unique)
        .WithMetricValue(uniquenessPercentage, percentage)
        .Build();
    }

    var (json, roundTripped) = RoundTripThroughString(cube);
    var roundTrippedUniqueCount = (Metric<int>)roundTripped.GetMetric("uniqueCount");
    var roundTrippedUniqueness = (Metric<double>)roundTripped.GetMetric("uniquenessPercentage");
    var uniqueCountBySegment = roundTripped.Pivot("segment", roundTrippedUniqueCount);
    var uniquenessBySegment = roundTripped.Pivot("segment", roundTrippedUniqueness);

    Assert.That(cube.Aggregate(totalCount), Is.EqualTo(rows.Length));
    Assert.That(uniqueCountBySegment["Starter"], Is.EqualTo(2));
    Assert.That(uniqueCountBySegment["Growth"], Is.EqualTo(3));
    Assert.That(uniqueCountBySegment["Enterprise"], Is.EqualTo(3));
    Assert.That(uniquenessBySegment["Starter"], Is.EqualTo(66.66666666666667d).Within(0.0001d));
    Assert.That(uniquenessBySegment["Growth"], Is.EqualTo(100d).Within(0.0001d));
    Assert.That(uniquenessBySegment["Enterprise"], Is.EqualTo(75d).Within(0.0001d));
    Assert.That(roundTripped.FactGroups.Count, Is.EqualTo(3));
    Assert.That(roundTripped.GetDimension("segment").Values.Count, Is.EqualTo(3));
    Assert.That(json, Does.Contain("\"uniquenessPercentage\""));
  }

  [Test]
  public void A_cube_can_store_top_category_rankings_with_count_share_and_value_ranges_in_the_same_rows()
  {
    // Scenario: a summary screen needs a top-category ranking with both frequency and amount context.
    // Why a cube helps: each category row can hold count, share, and range metrics together instead of splitting them across reports.
    var rows = BusinessNeutralUseCaseData.GetCategoryAmountRows();
    var groupedRows = rows
      .GroupBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
      .OrderByDescending(g => g.Count())
      .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
      .Take(3)
      .ToArray();

    var cube = new AnalyticsCube
    {
      Key = "top-categories",
      Label = "Top Categories",
      PopulationCount = rows.Length,
    };

    var category = cube.AddTypedDimension<string>("category", "Category");
    var count = cube.AddCountMetric("count", "Count");
    var share = cube.AddMetric(new PercentageMetric("share", "Share"));
    var minValue = cube.AddCurrencyMetric("minValue", "EUR", "Minimum Value");
    var maxValue = cube.AddCurrencyMetric("maxValue", "EUR", "Maximum Value");

    foreach (var group in groupedRows)
    {
      cube.CreateFactGroup()
        .WithDimensionValue(category, group.Key)
        .WithMetricValue(count, group.Count())
        .WithMetricValue(share, group.Count() / (double)rows.Length * 100d)
        .WithMetricValue(minValue, group.Min(x => x.Amount))
        .WithMetricValue(maxValue, group.Max(x => x.Amount))
        .Build();
    }

    var (json, roundTripped) = RoundTripThroughString(cube);
    var roundTrippedCount = (Metric<int>)roundTripped.GetMetric("count");
    var roundTrippedMinValue = (Metric<decimal>)roundTripped.GetMetric("minValue");
    var roundTrippedMaxValue = (Metric<decimal>)roundTripped.GetMetric("maxValue");
    var roundTrippedCountByCategory = roundTripped.Pivot("category", roundTrippedCount);
    var priorityFactGroup = roundTripped.FactGroups.Single(factGroup => string.Equals(factGroup.GetDimensionValue("category")?.Value?.ToString(), "Priority", StringComparison.OrdinalIgnoreCase));
    var standardFactGroup = roundTripped.FactGroups.Single(factGroup => string.Equals(factGroup.GetDimensionValue("category")?.Value?.ToString(), "Standard", StringComparison.OrdinalIgnoreCase));

    Assert.That(cube.Pivot("category", count)["Flexible"], Is.EqualTo(4));
    Assert.That(cube.Pivot("category", count)["Standard"], Is.EqualTo(3));
    Assert.That(cube.Pivot("category", count)["Priority"], Is.EqualTo(2));
    Assert.That(roundTrippedCountByCategory["Flexible"], Is.EqualTo(4));
    Assert.That(priorityFactGroup.GetMetricValue<decimal>(roundTrippedMinValue), Is.EqualTo(300m));
    Assert.That(standardFactGroup.GetMetricValue<decimal>(roundTrippedMaxValue), Is.EqualTo(180m));
    Assert.That(roundTripped.FactGroups.Count, Is.EqualTo(3));
    Assert.That(roundTripped.GetDimension("category").Values.Count, Is.EqualTo(3));
    Assert.That(json, Does.Contain("\"M\""));
  }
}
