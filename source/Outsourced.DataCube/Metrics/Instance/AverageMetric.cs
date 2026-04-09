namespace Outsourced.DataCube.Metrics;

using System.Globalization;

/// <summary>
/// A metric that aggregates double values by arithmetic mean.
/// </summary>
public class AverageMetric : Metric<double>
{
  /// <summary>
  /// Initializes a new average metric.
  /// </summary>
  public AverageMetric(string key, string label = null)
    : base(key, MetricType.Double, AggregationType.Average, label, "F2") { }

  /// <inheritdoc />
  public override string FormatValue(double value)
  {
    return value.ToString(Format, CultureInfo.InvariantCulture);
  }

  /// <inheritdoc />
  public override double Aggregate(IEnumerable<double> values)
  {
    var list = values.ToList();
    return list.Count != 0 ? list.Average() : 0;
  }
}
