namespace Outsourced.DataCube.Metrics;

using System.Globalization;

/// <summary>
/// A metric that aggregates integer values by summing counts.
/// </summary>
public class CountMetric : Metric<int>
{
  /// <summary>
  /// Initializes a new count metric.
  /// </summary>
  public CountMetric(string key, string label = null)
    : base(key, MetricType.Int, AggregationType.Count, label, "N0") { }

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
