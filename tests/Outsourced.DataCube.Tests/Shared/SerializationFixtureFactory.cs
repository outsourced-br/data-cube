using Outsourced.DataCube.Metrics;

namespace Outsourced.DataCube.Tests.Shared;

internal static class SerializationFixtureFactory
{
  public static AnalyticsCube CreateAnalyticsCube()
  {
    var cube = new AnalyticsCube
    {
      Key = "legal-entity-uniqueness",
      Label = "Legal Entity Uniqueness",
      PopulationCount = 3,
    };

    var entityType = new Dimension<string>("entityType", "Entity Type");
    var region = new Dimension<string>("region", "Region");
    var year = new Dimension<int>("year", "Year");
    var composite = new CompositeDimension("entityType", "region")
    {
      Key = "Composite",
      Label = "Composite Dimension"
    };

    cube.AddDimension(entityType);
    cube.AddDimension(region);
    cube.AddDimension(year);
    cube.AddDimension(composite);

    var duplicateCount = new CountMetric("duplicateCount", "Duplicate Count");
    var averageScore = new AverageMetric("averageScore", "Average Score");
    var lossAmount = new CurrencyMetric("lossAmount", "Loss Amount", "EUR");

    cube.AddMetric(duplicateCount);
    cube.AddMetric(averageScore);
    cube.AddMetric(lossAmount);

    AddFactGroup(entityType, region, year, composite, duplicateCount, averageScore, lossAmount, "PERSON", "NL", 2025, 2, 97.5d, 1500.25m, cube);
    AddFactGroup(entityType, region, year, composite, duplicateCount, averageScore, lossAmount, "COMPANY", "BE", 2024, 1, 88.25d, 500.00m, cube);

    return cube;
  }

  private static void AddFactGroup(
    Dimension<string> entityTypeDimension,
    Dimension<string> regionDimension,
    Dimension<int> yearDimension,
    CompositeDimension compositeDimension,
    Metric<int> duplicateCountMetric,
    Metric<double> averageScoreMetric,
    Metric<decimal> lossAmountMetric,
    string entityType,
    string region,
    int year,
    int duplicateCount,
    double averageScore,
    decimal lossAmount,
    AnalyticsCube cube)
  {
    var entityTypeValue = entityTypeDimension.GetOrCreateValue(entityType);
    var regionValue = regionDimension.GetOrCreateValue(region);
    var yearValue = yearDimension.GetOrCreateValue(year);
    var compositeValue = compositeDimension.CreateCompositeValue(
      new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
      {
        ["entityType"] = entityType,
        ["region"] = region,
      });

    var factGroup = cube.CreateAddFactGroup();
    factGroup.SetDimensionValue(entityTypeDimension.Key, entityTypeValue);
    factGroup.SetDimensionValue(regionDimension.Key, regionValue);
    factGroup.SetDimensionValue(yearDimension.Key, yearValue);
    factGroup.SetDimensionValue(compositeDimension.Key, compositeValue);
    factGroup.SetMetricValue(duplicateCountMetric, duplicateCount);
    factGroup.SetMetricValue(averageScoreMetric, averageScore);
    factGroup.SetMetricValue(lossAmountMetric, lossAmount);
  }
}
