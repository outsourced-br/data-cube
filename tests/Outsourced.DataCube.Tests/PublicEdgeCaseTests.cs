namespace Outsourced.DataCube.Tests;

using Metrics;
using NUnit.Framework;

[TestFixture]
public sealed class PublicEdgeCaseTests
{
  [Test]
  public void Composite_dimension_registry_collapses_identical_members_even_when_field_order_differs()
  {
    var duplicatePair = new CompositeDimension("leftId", "rightId")
    {
      Key = "duplicatePair",
      Label = "Duplicate Pair",
    };

    duplicatePair.CreateCompositeValue(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      ["leftId"] = "REC-001",
      ["rightId"] = "REC-002",
    });
    duplicatePair.CreateCompositeValue(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      ["rightId"] = "REC-002",
      ["leftId"] = "REC-001",
    });

    Assert.That(duplicatePair.Values.Count, Is.EqualTo(1));
  }

  [Test]
  public void Typed_composite_dimension_registry_collapses_identical_entity_members()
  {
    var cube = new AnalyticsCube();
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var duplicateCount = cube.AddCountMetric("duplicateCount", "Duplicate Count");
    var investigation = cube.CreateCompositeDimension<TypedCompositeDuplicateRow>("investigation", "Investigation")
      .AddProperty(x => x.RecordId)
      .AddProperty(x => x.Region)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(investigation, new TypedCompositeDuplicateRow { RecordId = "REC-100", Region = "NL" })
      .WithMetricValue(duplicateCount, 1)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(channel, "Partner")
      .WithDimensionValue(investigation, new TypedCompositeDuplicateRow { RecordId = "REC-100", Region = "NL" })
      .WithMetricValue(duplicateCount, 2)
      .Build();

    Assert.That(investigation.Values.Count, Is.EqualTo(1));
    Assert.That(cube.Aggregate(duplicateCount), Is.EqualTo(3));
  }

  [Test]
  public void Dice_with_multiple_filters_returns_empty_when_one_member_is_not_registered()
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
      .WithDimensionValue(region, "US")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(revenue, 90m)
      .Build();

    var filtered = cube.Dice(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      ["REGION"] = "EU",
      ["MONTH"] = "2026-06",
    });

    Assert.That(filtered.FactGroups, Is.Empty);
    Assert.That(filtered.Aggregate(revenue), Is.EqualTo(0m));
  }

  [Test]
  public void Pivot_totals_ignore_missing_metric_fact_groups_after_dice_with_multiple_filters()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var segment = cube.AddTypedDimension<string>("segment", "Segment");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");
    var count = cube.AddCountMetric("count", "Count");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(segment, "Retail")
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(segment, "Retail")
      .WithMetricValue(count, 1)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(segment, "Wholesale")
      .WithMetricValue(revenue, 40m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Partner")
      .WithDimensionValue(segment, "Retail")
      .WithMetricValue(revenue, 50m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Partner")
      .WithDimensionValue(segment, "Wholesale")
      .WithMetricValue(count, 1)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "US")
      .WithDimensionValue(month, "2026-04")
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(segment, "Retail")
      .WithMetricValue(revenue, 999m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "EU")
      .WithDimensionValue(month, "2026-05")
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(segment, "Retail")
      .WithMetricValue(revenue, 888m)
      .Build();

    var filtered = cube.Dice(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      ["REGION"] = "EU",
      ["MONTH"] = "2026-04",
    });

    var result = filtered.Pivot(
      new[] { "channel" },
      new[] { "segment" },
      revenue,
      new PivotTotalsOptions
      {
        IncludeRowTotals = true,
        IncludeColumnTotals = true,
        IncludeGrandTotal = true,
      });

    var directRow = FindCoordinate(result.Rows, "channel", "Direct");
    var partnerRow = FindCoordinate(result.Rows, "channel", "Partner");
    var totalRow = FindCoordinate(result.Rows, "channel", Constants.ALL_LABEL);
    var retailColumn = FindCoordinate(result.Columns, "segment", "Retail");
    var wholesaleColumn = FindCoordinate(result.Columns, "segment", "Wholesale");
    var totalColumn = FindCoordinate(result.Columns, "segment", Constants.ALL_LABEL);

    Assert.That(GetRequiredValue(result, directRow, retailColumn), Is.EqualTo(100m));
    Assert.That(GetRequiredValue(result, directRow, wholesaleColumn), Is.EqualTo(40m));
    Assert.That(GetRequiredValue(result, partnerRow, retailColumn), Is.EqualTo(50m));
    Assert.That(result.TryGetValue(partnerRow, wholesaleColumn, out _), Is.False);
    Assert.That(GetRequiredValue(result, directRow, totalColumn), Is.EqualTo(140m));
    Assert.That(GetRequiredValue(result, partnerRow, totalColumn), Is.EqualTo(50m));
    Assert.That(GetRequiredValue(result, totalRow, retailColumn), Is.EqualTo(150m));
    Assert.That(GetRequiredValue(result, totalRow, wholesaleColumn), Is.EqualTo(40m));
    Assert.That(GetRequiredValue(result, totalRow, totalColumn), Is.EqualTo(190m));
  }

  [Test]
  public void Composite_dimension_entity_materialization_converts_enum_string_and_numeric_fields()
  {
    var value = new CompositeDimensionValue<ConvertedCompositeEntity>(
      "converted",
      "Converted",
      new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
      {
        ["recordid"] = "REC-300",
        ["status"] = "closed",
        ["quantity"] = 5L,
        ["amount"] = 12.5d,
      },
      null);

    var entity = value.Entity;

    Assert.That(entity.RecordId, Is.EqualTo("REC-300"));
    Assert.That(entity.Status, Is.EqualTo(TestCompositeStatus.Closed));
    Assert.That(entity.Quantity, Is.EqualTo(5));
    Assert.That(entity.Amount, Is.EqualTo(12.5m));
  }

  [Test]
  public void Composite_dimension_entity_materialization_defaults_nulls_and_handles_numeric_enum_values()
  {
    var value = new CompositeDimensionValue<NullableConvertedCompositeEntity>(
      "nullable",
      "Nullable",
      new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
      {
        ["isFlagged"] = null,
        ["attemptCount"] = null,
        ["status"] = 1,
        ["optionalStatus"] = null,
      },
      null);

    var entity = value.Entity;

    Assert.That(entity.IsFlagged, Is.False);
    Assert.That(entity.AttemptCount, Is.Null);
    Assert.That(entity.Status, Is.EqualTo(TestCompositeStatus.Open));
    Assert.That(entity.OptionalStatus, Is.Null);
  }

  [Test]
  public void Property_dimension_uses_entity_value_and_null_label_for_missing_entities()
  {
    var withValueCube = new AnalyticsCube();
    var valueDimension = withValueCube
      .CreatePropertyDimension(new PropertySelectionEntity { Region = "EMEA" }, x => x.Region, "Region")
      .Build();

    var nullEntityCube = new AnalyticsCube();
    var nullDimension = nullEntityCube
      .CreatePropertyDimension((PropertySelectionEntity)null, x => x.Region, "Region")
      .Build();

    Assert.That(valueDimension.Key, Is.EqualTo("EMEA"));
    Assert.That(valueDimension.Label, Is.EqualTo("Region"));
    Assert.That(nullDimension.Key, Is.EqualTo(Constants.NULL_LABEL));
    Assert.That(nullDimension.Label, Is.EqualTo("Region"));
  }

  [Test]
  public void Composite_dimension_builder_supports_reference_and_value_type_property_selectors()
  {
    var cube = new AnalyticsCube();
    var dimension = cube
      .CreateCompositeDimension<PropertySelectionEntity>("selection", "Selection")
      .AddProperty(x => x.Region)
      .AddProperties(x => x.Score)
      .Build();

    Assert.That(dimension.KeyCriteria, Is.EqualTo(new[] { "Region", "Score" }));
  }

  private static DimensionCoordinate FindCoordinate(
    IEnumerable<DimensionCoordinate> coordinates,
    string dimensionKey,
    string valueKey)
  {
    return coordinates.Single(coordinate => coordinate[dimensionKey].Key == valueKey);
  }

  private static T GetRequiredValue<T>(PivotResult<T> result, DimensionCoordinate row, DimensionCoordinate column)
    where T : struct
  {
    Assert.That(result.TryGetValue(row, column, out var value), Is.True);
    return value;
  }
}

public sealed class TypedCompositeDuplicateRow
{
  public string RecordId { get; set; } = string.Empty;

  public string Region { get; set; } = string.Empty;
}

public sealed class ConvertedCompositeEntity
{
  public string RecordId { get; set; } = string.Empty;

  public TestCompositeStatus Status { get; set; }

  public int Quantity { get; set; }

  public decimal Amount { get; set; }
}

public sealed class NullableConvertedCompositeEntity
{
  public bool IsFlagged { get; set; }

  public int? AttemptCount { get; set; }

  public TestCompositeStatus Status { get; set; }

  public TestCompositeStatus? OptionalStatus { get; set; }
}

public sealed class PropertySelectionEntity
{
  public string Region { get; set; } = string.Empty;

  public int Score { get; set; }
}

public enum TestCompositeStatus
{
  Open = 1,
  Closed = 2,
}
