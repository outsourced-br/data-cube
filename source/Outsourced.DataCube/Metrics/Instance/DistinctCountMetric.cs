namespace Outsourced.DataCube.Metrics;

using System.Globalization;

/// <summary>
/// A metric that counts distinct business keys across the contributing fact groups at query time.
/// </summary>
public sealed class DistinctCountMetric : Metric<int>
{
  /// <summary>
  /// Gets the dimension key that supplies the business key being counted distinctly.
  /// </summary>
  public string BusinessKeyDimensionKey { get; set; }

  /// <summary>
  /// Initializes a new distinct-count metric.
  /// </summary>
  public DistinctCountMetric(string key, string businessKeyDimensionKey, string label = null)
    : base(key, MetricType.Int, AggregationType.DistinctCount, label, "N0")
  {
    ArgumentException.ThrowIfNullOrEmpty(businessKeyDimensionKey);

    BusinessKeyDimensionKey = businessKeyDimensionKey;
    Semantics = MetricSemantics.CreateAdditive();
  }

  /// <inheritdoc />
  public override string FormatValue(int value)
  {
    return value.ToString(Format, CultureInfo.InvariantCulture);
  }

  /// <inheritdoc />
  public override int Aggregate(IEnumerable<int> values)
  {
    throw new NotSupportedException(
      $"Metric '{Key}' is a query-time distinct count and must be evaluated through AnalyticsCube query APIs.");
  }

  internal bool TryGetBusinessKey(FactGroup factGroup, out string businessKey)
  {
    ArgumentNullException.ThrowIfNull(factGroup);

    if (factGroup.DimensionValues.TryGetValue(BusinessKeyDimensionKey, out var dimensionValue) &&
        dimensionValue is not null &&
        !string.IsNullOrEmpty(dimensionValue.Key))
    {
      businessKey = dimensionValue.Key;
      return true;
    }

    businessKey = null;
    return false;
  }
}
