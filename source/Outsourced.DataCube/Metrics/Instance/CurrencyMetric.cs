namespace Outsourced.DataCube.Metrics;

using System.Globalization;

/// <summary>
/// A metric for monetary amounts aggregated by sum.
/// </summary>
public class CurrencyMetric : Metric<decimal>
{
  /// <summary>
  /// Initializes a new currency metric.
  /// </summary>
  public CurrencyMetric(string key, string label = null, string currency = "XXX")
    : base(key, MetricType.Decimal, AggregationType.Currency, label, "C2", currency) { }

  /// <inheritdoc />
  public override string FormatValue(decimal value)
  {
    return value.ToString(Format, CultureInfo.InvariantCulture);
  }

  /// <inheritdoc />
  public override decimal Aggregate(IEnumerable<decimal> values)
  {
    return values.Sum();
  }
}
