namespace Outsourced.DataCube.Builders;

using System;
using System.Collections.Generic;
using System.Linq;
using DataCube;
using Metrics;

/// <summary>
/// Builds a custom metric and registers it with a cube.
/// </summary>
/// <typeparam name="T">The metric value type.</typeparam>
public class MetricBuilder<T> where T : struct
{
  private readonly AnalyticsCube _cube;
  private readonly string _key;
  private string _label;
  private MetricType _metricType;
  private AggregationType _aggregationType;
  private string _format;
  private string _unit;
  private Func<IEnumerable<T>, T> _aggregationFunction;
  private Func<T, string> _formatFunction;
  private MetricSemantics _semantics;
  private bool _semanticsConfigured;

  /// <summary>
  /// Initializes a new metric builder for the supplied cube and key.
  /// </summary>
  public MetricBuilder(AnalyticsCube cube, string key)
  {
    _cube = cube ?? throw new ArgumentNullException(nameof(cube));
    _key = key ?? throw new ArgumentNullException(nameof(key));

    // Set defaults based on T
    _metricType = MetricTypeHelper.GetMetricType<T>();

    if (typeof(T) == typeof(int))
    {
      _aggregationType = AggregationType.Count;
      _format = "N0";
    }
    else if (typeof(T) == typeof(double) || typeof(T) == typeof(decimal))
    {
      _aggregationType = AggregationType.Average;
      _format = "F2";
    }
  }

  /// <summary>
  /// Sets the metric label.
  /// </summary>
  public MetricBuilder<T> WithLabel(string label)
  {
    _label = label;
    return this;
  }

  /// <summary>
  /// Sets the aggregation type metadata for the metric.
  /// </summary>
  public MetricBuilder<T> WithAggregationType(AggregationType aggregationType)
  {
    _aggregationType = aggregationType;
    return this;
  }

  /// <summary>
  /// Sets explicit aggregation semantics for the metric.
  /// </summary>
  public MetricBuilder<T> WithSemantics(MetricSemantics semantics)
  {
    ArgumentNullException.ThrowIfNull(semantics);

    _semantics = semantics.Clone();
    _semanticsConfigured = true;
    return this;
  }

  /// <summary>
  /// Marks the metric as fully aggregatable across every dimension.
  /// </summary>
  public MetricBuilder<T> AsAdditive()
  {
    return WithSemantics(MetricSemantics.CreateAdditive());
  }

  /// <summary>
  /// Marks the metric as semi-additive across the supplied time dimension.
  /// </summary>
  public MetricBuilder<T> AsSemiAdditive(
    string timeDimensionKey,
    SemiAdditiveAggregationType aggregation = SemiAdditiveAggregationType.LastValue)
  {
    return WithSemantics(MetricSemantics.CreateSemiAdditive(timeDimensionKey, aggregation));
  }

  /// <summary>
  /// Marks the metric as non-additive so multi-fact-group rollups are rejected.
  /// </summary>
  public MetricBuilder<T> AsNonAdditive()
  {
    return WithSemantics(MetricSemantics.CreateNonAdditive());
  }

  /// <summary>
  /// Sets the display format string for the metric.
  /// </summary>
  public MetricBuilder<T> WithFormat(string format)
  {
    _format = format;
    return this;
  }

  /// <summary>
  /// Sets the display unit for the metric.
  /// </summary>
  public MetricBuilder<T> WithUnit(string unit)
  {
    _unit = unit;
    return this;
  }

  /// <summary>
  /// Sets the aggregation function used by the built metric.
  /// </summary>
  public MetricBuilder<T> WithAggregationFunction(Func<IEnumerable<T>, T> aggregationFunction)
  {
    _aggregationFunction = aggregationFunction;
    return this;
  }

  /// <summary>
  /// Sets the display formatting function used by the built metric.
  /// </summary>
  public MetricBuilder<T> WithFormatFunction(Func<T, string> formatFunction)
  {
    _formatFunction = formatFunction;
    return this;
  }

  /// <summary>
  /// Builds the metric and registers it with the target cube.
  /// </summary>
  public Metric<T> Build()
  {
    // Create a custom metric implementation
    var metric = new CustomMetric<T>(_key, _metricType, _aggregationType, _label, _format, _unit)
    {
      AggregateFunc = _aggregationFunction,
      FormatFunc = _formatFunction
    };

    metric.Semantics = _semanticsConfigured
      ? _semantics.Clone()
      : MetricSemantics.CreateDefault(_aggregationType);

    _cube.AddMetric(metric);
    return metric;
  }

  // Special factory methods for common metric types
  /// <summary>
  /// Creates a builder preconfigured for count metrics.
  /// </summary>
  public static MetricBuilder<int> Count(AnalyticsCube cube, string key)
  {
    return new MetricBuilder<int>(cube, key)
        .WithAggregationType(AggregationType.Count)
        .WithFormat("N0")
        .WithAggregationFunction(values => values.Sum());
  }

  /// <summary>
  /// Creates a builder preconfigured for percentage metrics.
  /// </summary>
  public static MetricBuilder<double> Percentage(AnalyticsCube cube, string key)
  {
    return new MetricBuilder<double>(cube, key)
        .WithAggregationType(AggregationType.Percentage)
        .WithFormat("F2")
        .WithAggregationFunction(values => values.Any() ? values.Average() : 0);
  }

  /// <summary>
  /// Creates a builder preconfigured for currency metrics.
  /// </summary>
  public static MetricBuilder<decimal> Currency(AnalyticsCube cube, string key, string currencyCode = "EUR")
  {
    return new MetricBuilder<decimal>(cube, key)
        .WithAggregationType(AggregationType.Currency)
        .WithFormat("C2")
        .WithUnit(currencyCode)
        .WithAggregationFunction(values => values.Sum());
  }

  /// <summary>
  /// Creates a builder preconfigured for average metrics.
  /// </summary>
  public static MetricBuilder<double> Average(AnalyticsCube cube, string key)
  {
    return new MetricBuilder<double>(cube, key)
      .WithAggregationType(AggregationType.Average)
      .WithFormat("F2")
      .WithAggregationFunction(values => values.Any() ? values.Average() : 0);
  }

  /// <summary>
  /// Creates a builder preconfigured for minimum-value aggregation.
  /// </summary>
  public static MetricBuilder<T> Minimum(AnalyticsCube cube, string key)
  {
    return new MetricBuilder<T>(cube, key)
      .WithAggregationType(AggregationType.Min)
      .WithAggregationFunction(values => values.Any() ? values.Min() : default);
  }

  /// <summary>
  /// Creates a builder preconfigured for maximum-value aggregation.
  /// </summary>
  public static MetricBuilder<T> Maximum(AnalyticsCube cube, string key)
  {
    return new MetricBuilder<T>(cube, key)
      .WithAggregationType(AggregationType.Max)
      .WithAggregationFunction(values => values.Any() ? values.Max() : default);
  }
}

