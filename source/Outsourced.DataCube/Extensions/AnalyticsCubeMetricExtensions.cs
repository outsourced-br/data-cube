namespace Outsourced.DataCube;

using Metrics;

/// <summary>
/// Provides convenience methods for reading metric values from a cube.
/// </summary>
public static class AnalyticsCubeMetricExtensions
{
  /// <summary>
  /// Gets a metric value for the first fact group containing the supplied dimension value.
  /// </summary>
  public static T GetMetricValue<T>(this AnalyticsCube cube, Metric<T> metric, DimensionValue dimensionValue) where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);
    ArgumentNullException.ThrowIfNull(dimensionValue);

    var metricType = MetricTypeHelper.GetMetricType<T>();

    foreach (var factGroup in cube.FactGroups)
    {
      // Check if this fact group contains the dimension value
      bool hasDimensionValue = false;
      foreach (var kvp in factGroup.DimensionValues)
      {
        if (kvp.Value.Equals(dimensionValue))
        {
          hasDimensionValue = true;
          break;
        }
      }

      if (!hasDimensionValue)
        continue;

      // Check if this fact group has metrics of the right type
      if (!factGroup.MetricCollections.TryGetValue(metricType, out var metricCollection))
        continue;

      // Get the metric value if it exists
      if (metricCollection is MetricCollection<T> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out var value))
          return value;
      }
    }

    return default;
  }

  /// <summary>
  /// Gets a metric value by metric key for the first fact group containing the supplied dimension value.
  /// </summary>
  public static T GetMetricValue<T>(this AnalyticsCube cube, string metricKey, DimensionValue dimensionValue) where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(metricKey);
    ArgumentNullException.ThrowIfNull(dimensionValue);

    var metricType = MetricTypeHelper.GetMetricType<T>();

    foreach (var factGroup in cube.FactGroups)
    {
      // Check if this fact group contains the dimension value
      bool hasDimensionValue = false;
      foreach (var kvp in factGroup.DimensionValues)
      {
        if (kvp.Value.Equals(dimensionValue))
        {
          hasDimensionValue = true;
          break;
        }
      }

      if (!hasDimensionValue)
        continue;

      // Check if this fact group has metrics of the right type
      if (!factGroup.MetricCollections.TryGetValue(metricType, out var metricCollection))
        continue;

      // Get the metric value if it exists
      if (metricCollection is MetricCollection<T> typedCollection)
      {
        if (typedCollection.TryGetValue(metricKey, out var value))
          return value;
      }
    }

    return default;
  }

  /// <summary>
  /// Gets a metric value for a named value within an untyped dimension.
  /// </summary>
  public static T GetMetricValue<T>(this AnalyticsCube cube, Metric<T> metric, Dimension dimension, string dimensionValueKey) where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);
    ArgumentNullException.ThrowIfNull(dimension);
    ArgumentException.ThrowIfNullOrEmpty(dimensionValueKey);

    // Find the dimension value
    var dimensionValue = dimension.GetValue(dimensionValueKey);
    if (dimensionValue == null)
      return default;

    return cube.GetMetricValue(metric, dimensionValue);
  }

  /// <summary>
  /// Gets a metric value for a typed dimension value, creating the dimension value if necessary.
  /// </summary>
  public static T GetMetricValue<T, TValue>(this AnalyticsCube cube, Metric<T> metric, Dimension<TValue> dimension, TValue dimensionValue) where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);
    ArgumentNullException.ThrowIfNull(dimension);

    // Find or create the dimension value
    var dimValue = dimension.GetOrCreateValue(dimensionValue);
    return cube.GetMetricValue(metric, dimValue);
  }

  /// <summary>
  /// Tries to get a metric value for the first fact group containing the supplied dimension value.
  /// </summary>
  public static bool TryGetMetricValue<T>(this AnalyticsCube cube, Metric<T> metric, DimensionValue dimensionValue, out T value) where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);
    ArgumentNullException.ThrowIfNull(dimensionValue);

    var metricType = MetricTypeHelper.GetMetricType<T>();

    foreach (var factGroup in cube.FactGroups)
    {
      // Check if this fact group contains the dimension value
      bool hasDimensionValue = false;
      foreach (var kvp in factGroup.DimensionValues)
      {
        if (kvp.Value.Equals(dimensionValue))
        {
          hasDimensionValue = true;
          break;
        }
      }

      if (!hasDimensionValue)
        continue;

      // Check if this fact group has metrics of the right type
      if (!factGroup.MetricCollections.TryGetValue(metricType, out var metricCollection))
        continue;

      // Get the metric value if it exists
      if (metricCollection is MetricCollection<T> typedCollection)
      {
        if (typedCollection.TryGetValue(metric.Key, out value))
          return true;
      }
    }

    value = default;
    return false;
  }
}
