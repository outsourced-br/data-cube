namespace Outsourced.DataCube;

using Metrics;

/// <summary>
/// Provides helpers for reading dimension values from fact groups.
/// </summary>
public static class FactGroupDimensionValueExtensions
{
  /// <summary>
  /// Finds the first dimension value associated with the supplied dimension and metric pair.
  /// </summary>
  public static DimensionValue GetDimensionValue(
    this IList<FactGroup> factGroups,
    Dimension dimension,
    Metric metric)
  {
    ArgumentNullException.ThrowIfNull(factGroups);
    ArgumentNullException.ThrowIfNull(dimension);
    ArgumentNullException.ThrowIfNull(metric);

    // Look across all fact groups for the intersection of this dimension and metric
    foreach (var factGroup in factGroups)
    {
      // Check if this fact group contains the dimension
      if (!factGroup.DimensionValues.TryGetValue(dimension.Key, out var dimensionValue))
        continue;

      // Check if any metric collection contains the specified metric
      var metricType = metric.MetricType;
      bool hasMetric = false;
      if (factGroup.MetricCollections.TryGetValue(metricType, out var collection))
      {
        hasMetric = collection.GetMetricKeys().Contains(metric.Key, StringComparer.OrdinalIgnoreCase);
      }

      // If this fact group contains both dimension and metric, return the dimension value
      if (hasMetric)
        return dimensionValue;
    }

    return null;
  }

  /// <summary>
  /// Finds the first typed dimension value associated with the supplied dimension and metric pair.
  /// </summary>
  public static DimensionValue<T> GetDimensionValue<T>(
    this IList<FactGroup> factGroups,
    Dimension<T> dimension,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(factGroups);
    ArgumentNullException.ThrowIfNull(dimension);
    ArgumentNullException.ThrowIfNull(metric);

    var metricType = MetricTypeHelper.GetMetricType<T>();

    // Look across all fact groups for the intersection of this dimension and metric
    foreach (var factGroup in factGroups)
    {
      // Check if this fact group contains the dimension
      if (!factGroup.DimensionValues.TryGetValue(dimension.Key, out var dimensionValue))
        continue;

      // Check if the fact group has the right metric collection type
      if (!factGroup.MetricCollections.TryGetValue(metricType, out var metricCollection))
        continue;

      // Check if the metric collection contains the specific metric
      if (!metricCollection.GetMetricKeys().Contains(metric.Key, StringComparer.OrdinalIgnoreCase))
        continue;

      // Found a match - return the dimension value if it's the right type
      if (dimensionValue is DimensionValue<T> typedValue)
        return typedValue;
    }

    return null;
  }

  /// <summary>
  /// Gets the raw value of the first dimension entry in a fact group.
  /// </summary>
  public static object GetFirstDimensionValue(this FactGroup factGroup)
  {
    ArgumentNullException.ThrowIfNull(factGroup);

    if (factGroup.DimensionValues.Count == 0)
      return null;

    var firstDimensionValue = factGroup.DimensionValues.Values.FirstOrDefault();
    return firstDimensionValue?.Value;
  }

  /// <summary>
  /// Gets the first dimension value in a fact group cast to the requested type when possible.
  /// </summary>
  public static T GetFirstDimensionValue<T>(this FactGroup factGroup)
  {
    ArgumentNullException.ThrowIfNull(factGroup);

    if (factGroup.DimensionValues.Count == 0)
      return default;

    var firstDimensionValue = factGroup.DimensionValues.Values.FirstOrDefault();

    if (firstDimensionValue is DimensionValue<T> typedValue)
      return typedValue.Value;

    if (firstDimensionValue?.Value is T value)
      return value;

    return default;
  }

  /// <summary>
  /// Gets the raw value for a specific dimension key.
  /// </summary>
  public static object GetFirstDimensionValue(this FactGroup factGroup, string dimensionKey)
  {
    ArgumentNullException.ThrowIfNull(factGroup);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);

    if (!factGroup.DimensionValues.TryGetValue(dimensionKey, out var dimensionValue))
      return null;

    return dimensionValue.Value;
  }

  /// <summary>
  /// Gets the value for a specific dimension key cast to the requested type when possible.
  /// </summary>
  public static T GetFirstDimensionValue<T>(this FactGroup factGroup, string dimensionKey)
  {
    ArgumentNullException.ThrowIfNull(factGroup);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);

    if (!factGroup.DimensionValues.TryGetValue(dimensionKey, out var dimensionValue))
      return default;

    if (dimensionValue is DimensionValue<T> typedValue)
      return typedValue.Value;

    if (dimensionValue.Value is T value)
      return value;

    return default;
  }
}
