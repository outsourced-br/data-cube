namespace Outsourced.DataCube.Metrics;

using System.Globalization;

/// <summary>
/// A metric that stores missing-value counts.
/// </summary>
public class MissingMetric : Metric<int>
{
  /// <summary>
  /// Initializes a new missing-count metric.
  /// </summary>
  public MissingMetric(string key, string label = null)
    : base(key, MetricType.Int, AggregationType.Missing, label, "N0") { }

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
