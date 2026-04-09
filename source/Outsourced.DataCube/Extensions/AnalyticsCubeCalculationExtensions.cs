namespace Outsourced.DataCube;

using Metrics;

/// <summary>
/// Provides convenience aggregation helpers over an <see cref="AnalyticsCube"/>.
/// </summary>
public static class AnalyticsCubeCalculationExtensions
{
  /// <summary>
  /// Collects all values for a metric and applies a custom aggregation function.
  /// </summary>
  /// <typeparam name="T">The metric value type.</typeparam>
  /// <param name="cube">The cube to read from.</param>
  /// <param name="metric">The metric to aggregate.</param>
  /// <param name="aggregation">The aggregation to apply to the collected values.</param>
  /// <returns>The aggregated result, or the default value when no matching values are present.</returns>
  public static T Calculate<T>(this AnalyticsCube cube, Metric<T> metric, Func<IEnumerable<T>, T> aggregation)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);
    ArgumentNullException.ThrowIfNull(aggregation);

    var values = cube.EnumerateValues(metric);

    return values.Count > 0 ? aggregation(values) : default;
  }

  /// <summary>
  /// Collects all values for a metric key and applies a custom aggregation function.
  /// </summary>
  /// <typeparam name="T">The metric value type.</typeparam>
  /// <param name="cube">The cube to read from.</param>
  /// <param name="metricKey">The metric key to aggregate.</param>
  /// <param name="aggregation">The aggregation to apply to the collected values.</param>
  /// <returns>The aggregated result, or the default value when no matching values are present.</returns>
  public static T Calculate<T>(this AnalyticsCube cube, string metricKey, Func<IEnumerable<T>, T> aggregation)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(metricKey);
    ArgumentNullException.ThrowIfNull(aggregation);

    var values = cube.EnumerateValues<T>(metricKey);

    return values.Count > 0 ? aggregation(values) : default;
  }

  /// <summary>
  /// Collects values for a metric across fact groups that contain the supplied dimension and applies a custom aggregation.
  /// </summary>
  /// <typeparam name="T">The metric value type.</typeparam>
  /// <param name="cube">The cube to read from.</param>
  /// <param name="dimension">The dimension that must be present on each matching fact group.</param>
  /// <param name="metric">The metric to aggregate.</param>
  /// <param name="aggregation">The aggregation to apply to the collected values.</param>
  /// <returns>The aggregated result, or the default value when no matching values are present.</returns>
  public static T Calculate<T>(this AnalyticsCube cube, Dimension dimension, Metric<T> metric, Func<IEnumerable<T>, T> aggregation)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(dimension);
    ArgumentNullException.ThrowIfNull(metric);
    ArgumentNullException.ThrowIfNull(aggregation);

    var values = cube.EnumerateValues(dimension, metric);

    return values.Count > 0 ? aggregation(values) : default;
  }

  private static List<T> EnumerateValues<T>(this AnalyticsCube cube, Metric<T> metric) where T : struct
  {
    var metricType = MetricTypeHelper.GetMetricType<T>();
    var factGroups = cube.FactGroups;
    var result = new List<T>(factGroups.Count);

    foreach (var factGroup in factGroups)
    {
      if (factGroup.MetricCollections.TryGetValue(metricType, out var metricCollection) && metricCollection is MetricCollection<T> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out var value))
          result.Add(value);
      }
    }
    return result;
  }

  private static List<T> EnumerateValues<T>(this AnalyticsCube cube, string metricKey) where T : struct
  {
    var metricType = MetricTypeHelper.GetMetricType<T>();
    var factGroups = cube.FactGroups;
    var result = new List<T>(factGroups.Count);

    foreach (var factGroup in factGroups)
    {
      if (factGroup.MetricCollections.TryGetValue(metricType, out var metricCollection) && metricCollection is MetricCollection<T> typedCollection)
      {
        if (typedCollection.TryGetValue(metricKey, out var value))
          result.Add(value);
      }
    }
    return result;
  }

  private static List<T> EnumerateValues<T>(this AnalyticsCube cube, Dimension dimension, Metric<T> metric) where T : struct
  {
    var metricType = MetricTypeHelper.GetMetricType<T>();
    var factGroups = cube.FactGroups;
    var result = new List<T>(factGroups.Count);

    foreach (var factGroup in factGroups)
    {
      if (!factGroup.DimensionValues.ContainsKey(dimension.Key))
        continue;

      if (factGroup.MetricCollections.TryGetValue(metricType, out var metricCollection) && metricCollection is MetricCollection<T> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out var value))
          result.Add(value);
      }
    }
    return result;
  }

  #region Common Aggregations

  /// <summary>
  /// Sums all integer values recorded for a metric.
  /// </summary>
  public static int Sum(this AnalyticsCube cube, Metric<int> metric)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    var metricType = MetricTypeHelper.GetMetricType<int>();
    int sum = 0;
    foreach (var factGroup in cube.FactGroups)
    {
      if (factGroup.MetricCollections.TryGetValue(metricType, out var collection) && collection is MetricCollection<int> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out var value))
          sum += value;
      }
    }
    return sum;
  }

  /// <summary>
  /// Sums all double values recorded for a metric.
  /// </summary>
  public static double Sum(this AnalyticsCube cube, Metric<double> metric)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    var metricType = MetricTypeHelper.GetMetricType<double>();
    double sum = 0;
    foreach (var factGroup in cube.FactGroups)
    {
      if (factGroup.MetricCollections.TryGetValue(metricType, out var collection) && collection is MetricCollection<double> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out var value))
          sum += value;
      }
    }
    return sum;
  }

  /// <summary>
  /// Sums all decimal values recorded for a metric.
  /// </summary>
  public static decimal Sum(this AnalyticsCube cube, Metric<decimal> metric)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    var metricType = MetricTypeHelper.GetMetricType<decimal>();
    decimal sum = 0;
    foreach (var factGroup in cube.FactGroups)
    {
      if (factGroup.MetricCollections.TryGetValue(metricType, out var collection) && collection is MetricCollection<decimal> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out var value))
          sum += value;
      }
    }
    return sum;
  }

  /// <summary>
  /// Computes the arithmetic mean of all integer values recorded for a metric.
  /// </summary>
  public static double Average(this AnalyticsCube cube, Metric<int> metric)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    var metricType = MetricTypeHelper.GetMetricType<int>();
    long sum = 0;
    int count = 0;

    foreach (var factGroup in cube.FactGroups)
    {
      if (factGroup.MetricCollections.TryGetValue(metricType, out var collection) && collection is MetricCollection<int> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out var value))
        {
          sum += value;
          count++;
        }
      }
    }

    return count == 0 ? 0d : (double)sum / count;
  }

  /// <summary>
  /// Computes the arithmetic mean of all double values recorded for a metric.
  /// </summary>
  public static double Average(this AnalyticsCube cube, Metric<double> metric)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    var metricType = MetricTypeHelper.GetMetricType<double>();
    double sum = 0d;
    int count = 0;

    foreach (var factGroup in cube.FactGroups)
    {
      if (factGroup.MetricCollections.TryGetValue(metricType, out var collection) && collection is MetricCollection<double> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out var value))
        {
          sum += value;
          count++;
        }
      }
    }

    return count == 0 ? 0d : sum / count;
  }

  /// <summary>
  /// Computes the arithmetic mean of all decimal values recorded for a metric.
  /// </summary>
  public static decimal Average(this AnalyticsCube cube, Metric<decimal> metric)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    var metricType = MetricTypeHelper.GetMetricType<decimal>();
    decimal sum = 0m;
    int count = 0;

    foreach (var factGroup in cube.FactGroups)
    {
      if (factGroup.MetricCollections.TryGetValue(metricType, out var collection) && collection is MetricCollection<decimal> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out var value))
        {
          sum += value;
          count++;
        }
      }
    }

    return count == 0 ? 0m : sum / count;
  }

  /// <summary>
  /// Returns the minimum value recorded for a metric.
  /// </summary>
  public static T Min<T>(this AnalyticsCube cube, Metric<T> metric) where T : struct, IComparable<T>
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    return cube.Calculate(metric, static values => values.Min());
  }

  /// <summary>
  /// Returns the maximum value recorded for a metric.
  /// </summary>
  public static T Max<T>(this AnalyticsCube cube, Metric<T> metric) where T : struct, IComparable<T>
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    return cube.Calculate(metric, static values => values.Max());
  }

  #endregion
}
