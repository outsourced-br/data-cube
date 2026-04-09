namespace Outsourced.DataCube.Builders;

using System;
using System.Collections.Generic;
using Metrics;

/// <summary>
/// Builds a <see cref="FactGroup"/> and adds it to an <see cref="AnalyticsCube"/>.
/// </summary>
public class FactGroupBuilder
{
  private readonly AnalyticsCube _cube;
  private readonly FactGroup _factGroup;

  /// <summary>
  /// Initializes a new fact-group builder for the supplied cube.
  /// </summary>
  public FactGroupBuilder(AnalyticsCube cube)
  {
    _cube = cube ?? throw new ArgumentNullException(nameof(cube));
    _factGroup = new FactGroup();
  }

  // Base dimension value methods
  /// <summary>
  /// Adds a dimension value to the fact group.
  /// </summary>
  public FactGroupBuilder WithDimensionValue(Dimension dimension, DimensionValue value)
  {
    _factGroup.SetDimensionValue(dimension, value);
    return this;
  }

  /// <summary>
  /// Adds a dimension value using a dimension key directly.
  /// </summary>
  public FactGroupBuilder WithDimensionValue(string dimensionKey, DimensionValue value)
  {
    _factGroup.SetDimensionValue(dimensionKey, value);
    return this;
  }

  // Typed dimension value methods
  /// <summary>
  /// Adds a typed dimension value, creating it on the dimension when necessary.
  /// </summary>
  public FactGroupBuilder WithDimensionValue<TValue>(Dimension<TValue> dimension, TValue value)
  {
    var dimensionValue = dimension.GetOrCreateValue(value);
    _factGroup.SetDimensionValue(dimension, dimensionValue);
    return this;
  }

  /// <summary>
  /// Adds an existing typed dimension value.
  /// </summary>
  public FactGroupBuilder WithDimensionValue<TValue>(Dimension<TValue> dimension, DimensionValue<TValue> value)
  {
    _factGroup.SetDimensionValue(dimension, value);
    return this;
  }

  // Composite dimension value methods
  /// <summary>
  /// Adds a composite dimension value from a field dictionary.
  /// </summary>
  public FactGroupBuilder WithDimensionValue(CompositeDimension dimension, IDictionary<string, object> values)
  {
    var value = dimension.CreateCompositeValue(values);
    _factGroup.SetDimensionValue(dimension, value);
    return this;
  }

  /// <summary>
  /// Adds a composite dimension value from an entity instance.
  /// </summary>
  public FactGroupBuilder WithDimensionValue<TEntity>(CompositeDimension<TEntity> dimension, TEntity entity)
    where TEntity : new()
  {
    var value = dimension.CreateCompositeValue(entity);
    _factGroup.SetDimensionValue(dimension, value);
    return this;
  }

  // Simple value auto-conversion
  /// <summary>
  /// Creates an untyped dimension value from a raw value and adds it to the fact group.
  /// </summary>
  public FactGroupBuilder WithSimpleValue<TValue>(Dimension dimension, TValue value)
  {
    string key = value?.ToString() ?? "null";
    string displayName = key;

    var dimensionValue = new DimensionValue(key, displayName, value);
    dimension.AddValue(dimensionValue);
    _factGroup.SetDimensionValue(dimension, dimensionValue);

    return this;
  }

  // Metric value methods
  /// <summary>
  /// Adds a metric value to the fact group.
  /// </summary>
  public FactGroupBuilder WithMetricValue<T>(Metric<T> metric, T value) where T : struct
  {
    _factGroup.SetMetricValue(metric, value);
    return this;
  }

  // Multiple values at once
  /// <summary>
  /// Adds multiple dimension values to the fact group.
  /// </summary>
  public FactGroupBuilder WithValues(IEnumerable<(Dimension Dimension, DimensionValue Value)> dimensionValues)
  {
    foreach (var (dimension, value) in dimensionValues)
    {
      WithDimensionValue(dimension, value);
    }
    return this;
  }

  /// <summary>
  /// Adds multiple values for the same typed dimension.
  /// </summary>
  public FactGroupBuilder WithValues<TValue>(Dimension<TValue> dimension, IEnumerable<TValue> values)
  {
    foreach (var value in values)
    {
      WithDimensionValue(dimension, value);
    }
    return this;
  }

  // Finalize and add to cube
  /// <summary>
  /// Finalizes the fact group and adds it to the target cube.
  /// </summary>
  public FactGroup Build()
  {
    _cube.FactGroups.Add(_factGroup);
    return _factGroup;
  }
}

