namespace Outsourced.DataCube.Metrics;

using System.Globalization;

/// <summary>
/// A metric that stores unique-value counts.
/// </summary>
public class UniqueMetric : Metric<int>
{
  /// <summary>
  /// Initializes a new unique-count metric.
  /// </summary>
  public UniqueMetric(string key, string label = null)
    : base(key, MetricType.Int, AggregationType.Unique, label, "N0") { }

  /// <inheritdoc />
  public override string FormatValue(int value)
  {
    return value.ToString(Format, CultureInfo.InvariantCulture);
  }

  /// <inheritdoc />
  public override int Aggregate(IEnumerable<int> values)
  {
    return values.Sum();
  }
}
