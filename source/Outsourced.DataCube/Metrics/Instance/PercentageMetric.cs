namespace Outsourced.DataCube.Metrics;

using System.Globalization;

/// <summary>
/// A metric that stores percentage values and aggregates them by average.
/// </summary>
public class PercentageMetric : Metric<double>
{
  /// <summary>
  /// Initializes a new percentage metric.
  /// </summary>
  public PercentageMetric(string key, string label = null)
    : base(key, MetricType.Double, AggregationType.Percentage, label, "F2") { }

  /// <inheritdoc />
  public override string FormatValue(double value)
  {
    return value.ToString(Format, CultureInfo.InvariantCulture);
  }

  /// <inheritdoc />
  public override double Aggregate(IEnumerable<double> values)
  {
    var list = values.ToList();
    return list.Count != 0 ? list.Average() : 0d;
  }
}
