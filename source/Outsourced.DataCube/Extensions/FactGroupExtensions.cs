namespace Outsourced.DataCube;

using Builders;
using Metrics;

/// <summary>
/// Provides helpers for building and populating <see cref="FactGroup"/> instances.
/// </summary>
public static class FactGroupExtensions
{
  /// <summary>
  /// Creates a fact-group builder for the supplied cube.
  /// </summary>
  public static FactGroupBuilder CreateFactGroup(this AnalyticsCube cube)
  {
    return new FactGroupBuilder(cube);
  }

  #region Set

  /// <summary>
  /// Sets a composite dimension value from an entity instance.
  /// </summary>
  public static void SetDimensionValue<TEntity>(
    this FactGroup factGroup,
    CompositeDimension<TEntity> dimension,
    TEntity data)
      where TEntity : new()
  {
    var matchValue = dimension.CreateCompositeValue(data);
    factGroup.SetDimensionValue(dimension, matchValue);
  }

  /// <summary>
  /// Sets an untyped dimension value from a raw object.
  /// </summary>
  public static void SetDimensionValue(
    this FactGroup factGroup,
    Dimension dimension,
    object value)
  {
    var matchValue = dimension.GetValueByRawValue(value) ?? new DimensionValue(GetDimensionValueKey(value), GetDimensionValueLabel(value), value);
    dimension.AddValue(matchValue);
    factGroup.SetDimensionValue(dimension, matchValue);
  }

  /// <summary>
  /// Sets a metric value for a fact group associated with a composite dimension.
  /// </summary>
  public static void SetMetricValue<TComposition, TMetric>(
    this FactGroup factGroup,
    CompositeDimension<TComposition> dimension,
    TComposition data,
    Metric<TMetric> metric,
    TComposition instance,
    TMetric value)
      where TComposition : new()
      where TMetric : struct
  {
    factGroup.SetMetricValue(metric, value);
  }

  // Set dimension values
  /// <summary>
  /// Sets a typed dimension value.
  /// </summary>
  public static void SetDimensionValue<TValue>(
    this FactGroup factGroup,
    Dimension<TValue> dimension,
    TValue value)
  {
    var dimensionValue = dimension.GetOrCreateValue(value);
    factGroup.SetDimensionValue(dimension.Key, dimensionValue);
  }

  /// <summary>
  /// Sets a composite dimension value from a field dictionary.
  /// </summary>
  public static void SetDimensionValue(
    this FactGroup factGroup,
    CompositeDimension dimension,
    IDictionary<string, object> values)
  {
    var dimensionValue = dimension.CreateCompositeValue(values);
    factGroup.SetDimensionValue(dimension.Key, dimensionValue);
  }

  #endregion

  #region With

  // Fluent methods for adding dimension values and metrics in one go
  /// <summary>
  /// Builds a fact group from a composite dimension value and metric value in one call.
  /// </summary>
  public static FactGroup WithDimensionAndMetric<TEntity, TMetric>(
    this FactGroupBuilder builder,
    CompositeDimension<TEntity> dimension,
    TEntity entity,
    Metric<TMetric> metric,
    TMetric value)
      where TEntity : new()
      where TMetric : struct
  {
    var factGroup = builder.WithDimensionValue(dimension, entity).Build();
    factGroup.SetMetricValue(metric, value);
    return factGroup;
  }

  /// <summary>
  /// Builds a fact group from a typed dimension value and metric value in one call.
  /// </summary>
  public static FactGroup WithDimensionAndMetric<TValue, TMetric>(
    this FactGroupBuilder builder,
    Dimension<TValue> dimension,
    TValue value,
    Metric<TMetric> metric,
    TMetric metricValue)
    where TMetric : struct
  {
    var dimensionValue = dimension.GetOrCreateValue(value);
    var factGroup = builder.WithDimensionValue(dimension, dimensionValue).Build();
    factGroup.SetMetricValue(metric, metricValue);
    return factGroup;
  }

  #endregion

  private static string GetDimensionValueKey(object value)
  {
    var valueString = value?.ToString();
    if (valueString == null) return Constants.NULL_LABEL;
    return string.IsNullOrEmpty(valueString) ? Constants.EMPTY_LABEL : valueString;
  }

  private static string GetDimensionValueLabel(object value) => GetDimensionValueKey(value);
}

