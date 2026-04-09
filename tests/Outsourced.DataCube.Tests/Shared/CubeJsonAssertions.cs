namespace Outsourced.DataCube.Tests.Shared;

using Metrics;
using NUnit.Framework;

internal static class CubeJsonAssertions
{
  public static void AssertCubeRoundTrip(AnalyticsCube expected, AnalyticsCube actual)
  {
    Assert.That(actual.Key, Is.EqualTo(expected.Key));
    Assert.That(actual.Label, Is.EqualTo(expected.Label));
    Assert.That(actual.PopulationCount, Is.EqualTo(expected.PopulationCount));
    Assert.That(actual.Dimensions.Count, Is.EqualTo(expected.Dimensions.Count));
    Assert.That(actual.Metrics.Count, Is.EqualTo(expected.Metrics.Count));
    Assert.That(actual.FactGroups.Count, Is.EqualTo(expected.FactGroups.Count));

    Assert.That(actual.GetDimension("entityType"), Is.Not.Null);
    Assert.That(actual.GetDimension("year"), Is.Not.Null);
    Assert.That(actual.GetDimension("Composite"), Is.Not.Null);

    AssertDimensionRegistryCount(expected, actual, "entityType");
    AssertDimensionRegistryCount(expected, actual, "region");
    AssertDimensionRegistryCount(expected, actual, "year");
    AssertDimensionRegistryCount(expected, actual, "Composite");

    var duplicateMetric = actual.GetMetric("duplicateCount") as CountMetric;
    Assert.That(duplicateMetric, Is.Not.Null);

    var averageMetric = actual.GetMetric("averageScore") as AverageMetric;
    Assert.That(averageMetric, Is.Not.Null);

    var lossMetric = actual.GetMetric("lossAmount") as CurrencyMetric;
    Assert.That(lossMetric, Is.Not.Null);

    Assert.That(actual.FactGroups[0].GetMetricValue<int>(duplicateMetric), Is.EqualTo(2));
    Assert.That(actual.FactGroups[0].GetMetricValue<double>(averageMetric), Is.EqualTo(97.5d));
    Assert.That(actual.FactGroups[0].GetMetricValue<decimal>(lossMetric), Is.EqualTo(1500.25m));

    var compositeValue = actual.FactGroups[0].GetDimensionValue("Composite") as CompositeDimensionValue;
    Assert.That(compositeValue, Is.Not.Null);
    Assert.That(compositeValue.GetFieldValue<string>("entityType"), Is.EqualTo("PERSON"));
    Assert.That(compositeValue.GetFieldValue<string>("region"), Is.EqualTo("NL"));
  }

  private static void AssertDimensionRegistryCount(AnalyticsCube expected, AnalyticsCube actual, string dimensionKey)
  {
    Assert.That(actual.GetDimension(dimensionKey).Values.Count, Is.EqualTo(expected.GetDimension(dimensionKey).Values.Count), $"Dimension registry mismatch for '{dimensionKey}'.");
  }
}
