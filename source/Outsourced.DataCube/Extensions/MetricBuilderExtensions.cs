namespace Outsourced.DataCube;

using System;
using System.Collections.Generic;
using Builders;
using Metrics;

/// <summary>
/// Provides convenience methods for adding common metric types to a cube.
/// </summary>
public static class MetricBuilderExtensions
{
  /// <summary>
  /// Creates and registers a count metric.
  /// </summary>
  public static Metric<int> AddCountMetric(this AnalyticsCube cube, string key, string label = null)
  {
    return MetricBuilder<int>.Count(cube, key)
      .WithLabel(label ?? $"{key} Count")
      .Build();
  }

  /// <summary>
  /// Creates and registers a query-time distinct-count metric based on the supplied business-key dimension.
  /// </summary>
  public static DistinctCountMetric AddDistinctCountMetric(
    this AnalyticsCube cube,
    string key,
    string businessKeyDimensionKey,
    string label = null)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(businessKeyDimensionKey);

    var metric = new DistinctCountMetric(key, businessKeyDimensionKey, label ?? $"{key} Distinct Count");
    return (DistinctCountMetric)cube.AddMetric(metric);
  }

  /// <summary>
  /// Creates and registers a query-time distinct-count metric based on the supplied business-key dimension.
  /// </summary>
  public static DistinctCountMetric AddDistinctCountMetric(
    this AnalyticsCube cube,
    string key,
    Dimension businessKeyDimension,
    string label = null)
  {
    ArgumentNullException.ThrowIfNull(businessKeyDimension);
    return cube.AddDistinctCountMetric(key, businessKeyDimension.Key, label);
  }

  /// <summary>
  /// Creates and registers a query-time distinct-count metric based on the supplied business-key dimension.
  /// </summary>
  public static DistinctCountMetric AddDistinctCountMetric<TValue>(
    this AnalyticsCube cube,
    string key,
    Dimension<TValue> businessKeyDimension,
    string label = null)
  {
    ArgumentNullException.ThrowIfNull(businessKeyDimension);
    return cube.AddDistinctCountMetric(key, businessKeyDimension.Key, label);
  }

  /// <summary>
  /// Creates and registers a percentage metric.
  /// </summary>
  public static Metric<double> AddPercentageMetric(this AnalyticsCube cube, string key, string label = null)
  {
    return MetricBuilder<double>.Percentage(cube, key)
      .WithLabel(label ?? $"{key} Percentage")
      .Build();
  }

  /// <summary>
  /// Creates and registers a currency metric.
  /// </summary>
  public static Metric<decimal> AddCurrencyMetric(this AnalyticsCube cube, string key, string currencyCode = "EUR", string label = null)
  {
    return MetricBuilder<decimal>.Currency(cube, key, currencyCode)
      .WithLabel(label ?? $"{key} Amount")
      .Build();
  }

  /// <summary>
  /// Creates and registers a calculated metric that is evaluated from aggregated dependency values.
  /// </summary>
  public static CalculatedMetric<T> AddCalculatedMetric<T>(
    this AnalyticsCube cube,
    string key,
    IEnumerable<Metric> dependencies,
    Func<CalculatedMetricContext, T> calculation,
    string label = null,
    string format = null,
    string unit = null)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);

    var metric = new CalculatedMetric<T>(key, dependencies, calculation, label, format, unit);
    return (CalculatedMetric<T>)cube.AddMetric(metric);
  }

  /// <summary>
  /// Creates and registers a calculated metric that is evaluated from aggregated dependency values.
  /// </summary>
  public static CalculatedMetric<T> AddCalculatedMetric<T>(
    this AnalyticsCube cube,
    string key,
    Func<CalculatedMetricContext, T> calculation,
    string label = null,
    string format = null,
    string unit = null,
    params Metric[] dependencies)
    where T : struct
  {
    return cube.AddCalculatedMetric(key, (IEnumerable<Metric>)dependencies, calculation, label, format, unit);
  }
}
