namespace Outsourced.DataCube.Metrics;

using System;
using System.Collections.Generic;
using System.Globalization;

// Custom metric class for the builder
internal sealed class CustomMetric<T> : Metric<T> where T : struct
{
  public Func<IEnumerable<T>, T> AggregateFunc { get; set; }
  public Func<T, string> FormatFunc { get; set; }

  public CustomMetric(string key, MetricType type, AggregationType aggregation, string label = null, string format = null, string unit = null)
      : base(key, type, aggregation, label, format, unit)
  {
  }

  public override T Aggregate(IEnumerable<T> values)
  {
    return AggregateFunc != null ? AggregateFunc(values) : default;
  }

  public override string FormatValue(T value)
  {
    if (FormatFunc != null)
      return FormatFunc(value);

    return string.Create(CultureInfo.InvariantCulture, $"{value}");
  }
}

