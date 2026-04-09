namespace Outsourced.DataCube.Serialization.Tests.Shared;

using Outsourced.DataCube;
using Outsourced.DataCube.Builders;
using Outsourced.DataCube.Metrics;
using global::Outsourced.DataCube.Tests.Shared;
using NUnit.Framework;

public abstract class CubeSerializerContractTests : CubeJsonContractTestBase
{
  [Test]
  public void Serialize_and_deserialize_roundtrips_dimensions_fact_groups_and_metrics()
  {
    var cube = SerializationFixtureFactory.CreateAnalyticsCube();

    var (json, result) = RoundTripThroughString(cube);

    CubeJsonAssertions.AssertCubeRoundTrip(cube, result);
    Assert.That(json, Does.Contain("\"duplicateCount\""));
    Assert.That(json, Does.Contain("\"M\""));
    Assert.That(json, Does.Contain("\"D\""));
  }

  [Test]
  public void Serialize_to_stream_and_deserialize_roundtrips_cube_payload()
  {
    var cube = SerializationFixtureFactory.CreateAnalyticsCube();

    using var stream = new MemoryStream();
    Serializer.Serialize(cube, stream);
    stream.Position = 0;

    var result = Serializer.Deserialize(stream);

    CubeJsonAssertions.AssertCubeRoundTrip(cube, result);
  }

  [Test]
  public void WriteAsJsonStream_and_deserialize_roundtrip_cube_payload()
  {
    var cube = SerializationFixtureFactory.CreateAnalyticsCube();

    using var stream = Serializer.WriteAsJsonStream(cube);
    stream.Position = 0;

    var result = Serializer.Deserialize(stream);

    CubeJsonAssertions.AssertCubeRoundTrip(cube, result);
  }

  [Test]
  public void ReadJsonStreamAs_roundtrip_cube_payload()
  {
    var cube = SerializationFixtureFactory.CreateAnalyticsCube();

    using var stream = Serializer.WriteAsJsonStream(cube);
    var result = Serializer.ReadJsonStreamAs<AnalyticsCube>(stream);

    CubeJsonAssertions.AssertCubeRoundTrip(cube, result);
  }

  [Test]
  public void SaveToFile_and_LoadFromFile_roundtrip_cube_payload()
  {
    var cube = SerializationFixtureFactory.CreateAnalyticsCube();
    var filePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{GetType().Name}-{Guid.NewGuid():N}.json");

    try
    {
      Serializer.SaveToFile(cube, filePath);

      var result = Serializer.LoadFromFile(filePath);

      CubeJsonAssertions.AssertCubeRoundTrip(cube, result);
      Assert.That(File.Exists(filePath), Is.True);
    }
    finally
    {
      if (File.Exists(filePath))
        File.Delete(filePath);
    }
  }

  [Test]
  public void Serialize_and_deserialize_roundtrip_typed_and_typed_composite_dimensions()
  {
    var cube = new AnalyticsCube
    {
      Key = "typed-roundtrip",
      Label = "Typed Roundtrip",
      PopulationCount = 1,
    };

    var cohortYear = cube.AddTypedDimension<int>("cohortYear", "Cohort Year");
    var observedOn = cube.AddTypedDimension<DateTime>("observedOn", "Observed On");
    var investigation = cube.CreateCompositeDimension<TypedCompositeRoundTripRow>("investigation", "Investigation")
      .AddProperty(x => x.RecordId)
      .AddProperty(x => x.Region)
      .Build();
    var duplicateCount = cube.AddCountMetric("duplicateCount", "Duplicate Count");

    var observedOnValue = new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Unspecified);
    var row = new TypedCompositeRoundTripRow
    {
      RecordId = "REC-100",
      Region = "NL",
    };

    cube.CreateFactGroup()
      .WithDimensionValue(cohortYear, 2026)
      .WithDimensionValue(observedOn, observedOnValue)
      .WithDimensionValue(investigation, row)
      .WithMetricValue(duplicateCount, 2)
      .Build();

    var (_, result) = RoundTripThroughString(cube);
    var roundTrippedFactGroup = result.FactGroups.Single();
    var roundTrippedObservedOn = (DimensionValue<DateTime>)roundTrippedFactGroup.GetDimensionValue("observedOn");
    var roundTrippedInvestigation = (CompositeDimensionValue<TypedCompositeRoundTripRow>)roundTrippedFactGroup.GetDimensionValue("investigation");

    Assert.That(result.GetDimension("cohortYear"), Is.InstanceOf<Dimension<int>>());
    Assert.That(result.GetDimension("observedOn"), Is.InstanceOf<Dimension<DateTime>>());
    Assert.That(result.GetDimension("investigation"), Is.InstanceOf<CompositeDimension<TypedCompositeRoundTripRow>>());
    Assert.That(((CompositeDimension<TypedCompositeRoundTripRow>)result.GetDimension("investigation")).KeyCriteria, Is.EqualTo(new[] { "RecordId", "Region" }));
    Assert.That(roundTrippedFactGroup.GetDimensionValue("cohortYear"), Is.InstanceOf<DimensionValue<int>>());
    Assert.That(roundTrippedFactGroup.GetDimensionValue("observedOn"), Is.InstanceOf<DimensionValue<DateTime>>());
    Assert.That(roundTrippedFactGroup.GetDimensionValue("investigation"), Is.InstanceOf<CompositeDimensionValue<TypedCompositeRoundTripRow>>());
    Assert.That(roundTrippedObservedOn.Value, Is.EqualTo(observedOnValue));
    Assert.That(roundTrippedInvestigation.GetFieldValue<string>("RecordId"), Is.EqualTo("REC-100"));
    Assert.That(roundTrippedInvestigation.GetFieldValue<string>("Region"), Is.EqualTo("NL"));
    Assert.That(roundTrippedInvestigation.Entity.RecordId, Is.EqualTo("REC-100"));
    Assert.That(roundTrippedInvestigation.Entity.Region, Is.EqualTo("NL"));
    Assert.That(result.GetDimension("investigation").Values.Count, Is.EqualTo(1));
  }

  [Test]
  public void Serialize_and_deserialize_roundtrip_typed_composite_dimensions_backed_by_value_types()
  {
    var cube = new AnalyticsCube
    {
      Key = "typed-roundtrip-struct-composite",
      Label = "Typed Roundtrip Struct Composite",
      PopulationCount = 1,
    };

    var investigation = cube.CreateCompositeDimension<TypedCompositeStructRoundTripRow>("investigation", "Investigation")
      .AddProperty(x => x.RecordId)
      .AddProperty(x => x.Region)
      .Build();
    var duplicateCount = cube.AddCountMetric("duplicateCount", "Duplicate Count");

    cube.CreateFactGroup()
      .WithDimensionValue(investigation, new TypedCompositeStructRoundTripRow("REC-200", "BE"))
      .WithMetricValue(duplicateCount, 3)
      .Build();

    var (_, result) = RoundTripThroughString(cube);
    var roundTrippedInvestigation = result.FactGroups.Single().GetDimensionValue("investigation");

    Assert.That(roundTrippedInvestigation, Is.TypeOf<CompositeDimensionValue<TypedCompositeStructRoundTripRow>>());
    Assert.That(((CompositeDimensionValue<TypedCompositeStructRoundTripRow>)roundTrippedInvestigation).Entity.RecordId, Is.EqualTo("REC-200"));
    Assert.That(((CompositeDimensionValue<TypedCompositeStructRoundTripRow>)roundTrippedInvestigation).Entity.Region, Is.EqualTo("BE"));
  }

  [Test]
  public void WriteAsJsonStream_and_ReadJsonStreamAs_roundtrip_struct_backed_composite_dimension_entity_with_non_key_properties()
  {
    var cube = new AnalyticsCube
    {
      Key = "typed-roundtrip-struct-composite-stream",
      Label = "Typed Roundtrip Struct Composite Stream",
    };

    var investigation = cube.CreateCompositeDimension<TypedCompositeStructWithMetricsRoundTripRow>("investigation", "Investigation")
      .AddProperty(x => x.ClaimId)
      .AddProperty(x => x.AmountType)
      .Build();
    var claimAmount = cube.AddCurrencyMetric(nameof(TypedCompositeStructWithMetricsRoundTripRow.ClaimAmount), "EUR", "Claim Amount");
    var subClaimSum = cube.AddCurrencyMetric(nameof(TypedCompositeStructWithMetricsRoundTripRow.SubClaimSum), "EUR", "Sub Claim Sum");
    var difference = cube.AddCurrencyMetric(nameof(TypedCompositeStructWithMetricsRoundTripRow.Difference), "EUR", "Difference");
    var subClaimCount = cube.AddCountMetric(nameof(TypedCompositeStructWithMetricsRoundTripRow.SubClaimCount), "Sub Claim Count");
    var row = new TypedCompositeStructWithMetricsRoundTripRow("claim-1", "AmountPaid", 125.50m, 100.25m, 25.25m, 2);

    cube.CreateFactGroup()
      .WithDimensionValue(investigation, row)
      .WithMetricValue(claimAmount, row.ClaimAmount)
      .WithMetricValue(subClaimSum, row.SubClaimSum)
      .WithMetricValue(difference, row.Difference)
      .WithMetricValue(subClaimCount, row.SubClaimCount)
      .Build();

    using var stream = Serializer.WriteAsJsonStream(cube);
    var result = Serializer.ReadJsonStreamAs<AnalyticsCube>(stream);
    var roundTrippedInvestigation = result.FactGroups.Single().GetDimensionValue("investigation");

    Assert.That(roundTrippedInvestigation, Is.TypeOf<CompositeDimensionValue<TypedCompositeStructWithMetricsRoundTripRow>>());
    Assert.That(((CompositeDimensionValue<TypedCompositeStructWithMetricsRoundTripRow>)roundTrippedInvestigation).Entity.ClaimId, Is.EqualTo("claim-1"));
    Assert.That(((CompositeDimensionValue<TypedCompositeStructWithMetricsRoundTripRow>)roundTrippedInvestigation).Entity.AmountType, Is.EqualTo("AmountPaid"));
  }

  [Test]
  public void Serialize_and_deserialize_roundtrip_preserves_deduplicated_typed_and_composite_dimension_registries_across_multiple_fact_groups()
  {
    var cube = new AnalyticsCube
    {
      Key = "typed-roundtrip-deduplicated-members",
      Label = "Typed Roundtrip Deduplicated Members",
      PopulationCount = 2,
    };

    var cohortYear = cube.AddTypedDimension<int>("cohortYear", "Cohort Year");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var investigation = cube.CreateCompositeDimension<TypedCompositeRoundTripRow>("investigation", "Investigation")
      .AddProperty(x => x.RecordId)
      .AddProperty(x => x.Region)
      .Build();
    var duplicateCount = cube.AddCountMetric("duplicateCount", "Duplicate Count");

    cube.CreateFactGroup()
      .WithDimensionValue(cohortYear, 2026)
      .WithDimensionValue(channel, "Direct")
      .WithDimensionValue(investigation, new TypedCompositeRoundTripRow
      {
        RecordId = "REC-100",
        Region = "NL",
      })
      .WithMetricValue(duplicateCount, 1)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(cohortYear, 2026)
      .WithDimensionValue(channel, "Partner")
      .WithDimensionValue(investigation, new TypedCompositeRoundTripRow
      {
        RecordId = "REC-100",
        Region = "NL",
      })
      .WithMetricValue(duplicateCount, 2)
      .Build();

    var (_, result) = RoundTripThroughString(cube);
    var roundTrippedCohortYear = (Dimension<int>)result.GetDimension("COHORTYEAR");
    var roundTrippedInvestigation = (CompositeDimension<TypedCompositeRoundTripRow>)result.GetDimension("INVESTIGATION");
    var roundTrippedDuplicateCount = (Metric<int>)result.GetMetric("DUPLICATECOUNT");

    Assert.That(result.FactGroups.Count, Is.EqualTo(2));
    Assert.That(roundTrippedCohortYear.Values.Count, Is.EqualTo(1));
    Assert.That(roundTrippedInvestigation.Values.Count, Is.EqualTo(1));
    Assert.That(result.FactGroups.All(factGroup => factGroup.GetDimensionValue("COHORTYEAR") is DimensionValue<int>), Is.True);
    Assert.That(result.FactGroups.All(factGroup => factGroup.GetDimensionValue("INVESTIGATION") is CompositeDimensionValue<TypedCompositeRoundTripRow>), Is.True);
    Assert.That(result.Aggregate(roundTrippedDuplicateCount), Is.EqualTo(3));
  }

  [Test]
  public void Serialize_and_deserialize_roundtripped_typed_composite_dimensions_do_not_create_new_members()
  {
    var cube = new AnalyticsCube
    {
      Key = "typed-roundtrip-readonly-composite",
      Label = "Typed Roundtrip Readonly Composite",
      PopulationCount = 1,
    };

    var investigation = cube.CreateCompositeDimension<TypedCompositeRoundTripRow>("investigation", "Investigation")
      .AddProperty(x => x.RecordId)
      .AddProperty(x => x.Region)
      .Build();
    var duplicateCount = cube.AddCountMetric("duplicateCount", "Duplicate Count");

    cube.CreateFactGroup()
      .WithDimensionValue(investigation, new TypedCompositeRoundTripRow
      {
        RecordId = "REC-100",
        Region = "NL",
      })
      .WithMetricValue(duplicateCount, 2)
      .Build();

    var (_, result) = RoundTripThroughString(cube);
    var roundTrippedInvestigation = (CompositeDimension<TypedCompositeRoundTripRow>)result.GetDimension("investigation");

    Assert.That(
      () => roundTrippedInvestigation.CreateCompositeValue(new TypedCompositeRoundTripRow
      {
        RecordId = "REC-200",
        Region = "BE",
      }),
      Throws.TypeOf<NotSupportedException>().With.Message.Contains("serialized data"));
  }

  [Test]
  public void Serialize_and_deserialize_roundtrip_dimension_hierarchies()
  {
    var cube = new AnalyticsCube
    {
      Key = "hierarchy-roundtrip",
      Label = "Hierarchy Roundtrip",
    };

    var location = new Dimension<string>("location", "Location");
    cube.AddDimension(location);

    var country = location.CreateValue("country:nl", "Netherlands", "Netherlands");
    var region = location.CreateValue("region:randstad", "Randstad", "Randstad");
    var city = location.CreateValue("city:ams", "Amsterdam", "Amsterdam");

    var hierarchy = location.CreateHierarchy("geography", "Geography");
    hierarchy.AddLevel("country", "Country");
    hierarchy.AddLevel("region", "Region");
    hierarchy.AddLevel("city", "City");
    hierarchy.MapValue("country", country);
    hierarchy.MapValue("region", region, country);
    hierarchy.MapValue("city", city, region);

    var (_, result) = RoundTripThroughString(cube);
    var roundTrippedLocation = result.GetDimension("location");
    var roundTrippedHierarchy = roundTrippedLocation.GetHierarchy("geography");
    var roundTrippedCity = roundTrippedLocation.GetValue("city:ams");

    Assert.That(roundTrippedLocation, Is.InstanceOf<Dimension<string>>());
    Assert.That(roundTrippedHierarchy, Is.Not.Null);
    Assert.That(
      roundTrippedHierarchy.Levels.Select(static level => level.Key),
      Is.EqualTo(new[] { "country", "region", "city" }));
    Assert.That(roundTrippedHierarchy.GetLevel("city")?.Ordinal, Is.EqualTo(2));
    Assert.That(roundTrippedHierarchy.GetParent(roundTrippedCity)?.Key, Is.EqualTo("region:randstad"));
    Assert.That(roundTrippedHierarchy.GetAncestor(roundTrippedCity, "country")?.Key, Is.EqualTo("country:nl"));
    Assert.That(
      roundTrippedHierarchy.GetPath(roundTrippedCity).Select(static value => value.Key),
      Is.EqualTo(new[] { "country:nl", "region:randstad", "city:ams" }));
  }

  [Test]
  public void Serialize_and_deserialize_roundtrip_metric_semantics()
  {
    var cube = new AnalyticsCube
    {
      Key = "metric-semantics-roundtrip",
      Label = "Metric Semantics Roundtrip",
    };

    var month = cube.AddTypedDimension<string>("month", "Month");
    var sales = cube.AddCurrencyMetric("sales", "EUR", "Sales");
    var endingBalance = MetricBuilder<decimal>.Currency(cube, "endingBalance", "EUR")
      .WithLabel("Ending Balance")
      .AsSemiAdditive("month", SemiAdditiveAggregationType.LastNonEmpty)
      .Build();
    var marginPercent = cube.AddPercentageMetric("marginPercent", "Margin Percent");

    cube.CreateFactGroup()
      .WithDimensionValue(month, "2026-02")
      .WithMetricValue(sales, 100m)
      .WithMetricValue(endingBalance, 55m)
      .WithMetricValue(marginPercent, 12.5d)
      .Build();

    var (json, result) = RoundTripThroughString(cube);
    var roundTrippedSales = result.GetMetric("sales");
    var roundTrippedBalance = result.GetMetric("endingBalance");
    var roundTrippedMarginPercent = result.GetMetric("marginPercent");

    Assert.That(roundTrippedSales.Semantics.Additivity, Is.EqualTo(MetricAdditivity.Additive));
    Assert.That(roundTrippedBalance.Semantics.Additivity, Is.EqualTo(MetricAdditivity.SemiAdditive));
    Assert.That(roundTrippedBalance.Semantics.SemiAdditive?.TimeDimensionKey, Is.EqualTo("month"));
    Assert.That(roundTrippedBalance.Semantics.SemiAdditive?.Aggregation, Is.EqualTo(SemiAdditiveAggregationType.LastNonEmpty));
    Assert.That(roundTrippedMarginPercent.Semantics.Additivity, Is.EqualTo(MetricAdditivity.NonAdditive));
    Assert.That(json, Does.Contain("\"Semantics\""));
    Assert.That(json, Does.Contain("\"LastNonEmpty\""));
  }

  [Test]
  public void Serialize_and_deserialize_roundtrip_query_time_distinct_count_metrics()
  {
    var cube = new AnalyticsCube
    {
      Key = "distinct-count-roundtrip",
      Label = "Distinct Count Roundtrip",
    };

    var region = cube.AddTypedDimension<string>("region", "Region");
    var customerId = cube.AddTypedDimension<string>("customerId", "Customer");
    var distinctCustomers = cube.AddDistinctCountMetric("distinctCustomers", customerId, "Distinct Customers");

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(customerId, "C-001")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(customerId, "C-001")
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "South")
      .WithDimensionValue(customerId, "C-002")
      .Build();

    var (json, result) = RoundTripThroughString(cube);
    var roundTrippedMetric = result.GetMetric("distinctCustomers");

    Assert.That(roundTrippedMetric, Is.InstanceOf<DistinctCountMetric>());
    Assert.That(((DistinctCountMetric)roundTrippedMetric).BusinessKeyDimensionKey, Is.EqualTo("customerId"));
    Assert.That(result.Aggregate((DistinctCountMetric)roundTrippedMetric), Is.EqualTo(2));
    Assert.That(json, Does.Contain("\"BusinessKeyDimensionKey\""));
    Assert.That(json, Does.Contain("\"customerId\""));
  }

  [Test]
  public void Serialize_and_deserialize_roundtrip_cube_grain()
  {
    var cube = new AnalyticsCube
    {
      Key = "grain-roundtrip",
      Label = "Grain Roundtrip",
    };

    var region = cube.AddTypedDimension<string>("region", "Region");
    var month = cube.AddTypedDimension<string>("month", "Month");
    var channel = cube.AddTypedDimension<string>("channel", "Channel");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    cube.DefineGrain(grain => grain.Require(region, month).AllowOptional(channel));

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-04")
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(region, "North")
      .WithDimensionValue(month, "2026-05")
      .WithDimensionValue(channel, "Direct")
      .WithMetricValue(revenue, 200m)
      .Build();

    var (json, result) = RoundTripThroughString(cube);
    var roundTrippedGrain = result.Grain;
    var roundTrippedRevenue = (Metric<decimal>)result.GetMetric("revenue");
    var roundTrippedRegion = (Dimension<string>)result.GetDimension("region");

    Assert.That(roundTrippedGrain, Is.Not.Null);
    Assert.That(roundTrippedGrain.RequiredDimensions, Is.EqualTo(new[] { "region", "month" }));
    Assert.That(roundTrippedGrain.OptionalDimensions, Is.EqualTo(new[] { "channel" }));
    Assert.That(result.Aggregate(roundTrippedRevenue), Is.EqualTo(300m));
    Assert.That(
      () => result.CreateFactGroup().WithDimensionValue(roundTrippedRegion, "South").Build(),
      Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Missing required dimensions: month"));
    Assert.That(json, Does.Contain("\"Grain\""));
    Assert.That(json, Does.Contain("\"RequiredDimensions\""));
    Assert.That(json, Does.Contain("\"OptionalDimensions\""));
  }
}

public sealed class TypedCompositeRoundTripRow
{
  public string RecordId { get; set; } = string.Empty;

  public string Region { get; set; } = string.Empty;
}

public readonly record struct TypedCompositeStructRoundTripRow(string RecordId, string Region);

public record struct TypedCompositeStructWithMetricsRoundTripRow(
  string ClaimId,
  string AmountType,
  decimal ClaimAmount,
  decimal SubClaimSum,
  decimal Difference,
  int SubClaimCount);
