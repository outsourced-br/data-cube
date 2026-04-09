#nullable enable

namespace Outsourced.DataCube;

using System;
using System.Collections.Generic;
using Metrics;

using Collections;

/// <summary>
/// Represents a single grouped observation in the cube, combining dimension values with metric collections.
/// </summary>
public class FactGroup
{
  private IDictionary<string, DimensionValue> _dimensionValues;

  /// <summary>
  /// Gets or sets the dimension values that identify this fact group.
  /// </summary>
  public IDictionary<string, DimensionValue> DimensionValues
  {
    get => _dimensionValues;
    set
    {
      var previousValues = _dimensionValues;
      _dimensionValues = CreateDimensionValues(value);

      try
      {
        NotifyDimensionValuesChanged();
      }
      catch
      {
        _dimensionValues = previousValues;
        throw;
      }
    }
  }

  /// <summary>
  /// Gets or sets the metric collections stored for this fact group, grouped by metric value type.
  /// </summary>
  public IDictionary<MetricType, IMetricCollection> MetricCollections { get; set; } = new MetricTypeDictionary();

  internal event Action<FactGroup>? DimensionValuesChanged;

  public FactGroup()
  {
    _dimensionValues = CreateDimensionValues(null);
  }

  /// <summary>
  /// Returns the dimension keys stored in this fact group.
  /// </summary>
  public IEnumerable<string> GetDimensionValueKeys() => DimensionValues.Keys;

  /// <summary>
  /// Returns the dimension values stored in this fact group.
  /// </summary>
  public IEnumerable<DimensionValue> GetDimensionValues() => DimensionValues.Values;

  /// <summary>
  /// Sets the value for a dimension using the dimension definition as the key source.
  /// </summary>
  public void SetDimensionValue(Dimension dimension, DimensionValue value)
  {
    DimensionValues[dimension.Key] = value;
  }

  /// <summary>
  /// Sets the value for a dimension key directly.
  /// </summary>
  public void SetDimensionValue(string dimensionKey, DimensionValue value)
  {
    DimensionValues[dimensionKey] = value;
  }

  /// <summary>
  /// Gets the stored value for the specified dimension.
  /// </summary>
  public DimensionValue? GetDimensionValue(Dimension dimension)
  {
    return
      DimensionValues.TryGetValue(dimension.Key, out var value)
        ? value
        : null;
  }

  /// <summary>
  /// Gets the stored value for the specified dimension key.
  /// </summary>
  public DimensionValue? GetDimensionValue(string dimensionKey)
  {
    return
      DimensionValues.TryGetValue(dimensionKey, out var value)
        ? value
        : null;
  }

  /// <summary>
  /// Gets the first composite dimension value of the requested entity type.
  /// </summary>
  public CompositeDimensionValue<TEntity>? GetDimensionValue<TEntity>()
    where TEntity : new()
  {
    return DimensionValues.Values.OfType<CompositeDimensionValue<TEntity>>().FirstOrDefault();
  }

  /// <summary>
  /// Gets the metric collection for the supplied value type, creating it if needed.
  /// </summary>
  public MetricCollection<T> GetOrCreateMetricCollection<T>() where T : struct
  {
    var metricType = MetricTypeHelper.GetMetricType<T>();

    if (!MetricCollections.TryGetValue(metricType, out var collection))
    {
      collection = new MetricCollection<T>();
      MetricCollections[metricType] = collection;
    }

    return (MetricCollection<T>)collection;
  }

  /// <summary>
  /// Gets the stored value for a metric.
  /// </summary>
  public T GetMetricValue<T>(Metric metric)
    where T : struct
  {
    var metricType = MetricTypeHelper.GetMetricType<T>();

    if (metric.MetricType != metricType)
      throw new ArgumentException($"Metric {metric.Key} expects {metric.MetricType} value, requested {metricType}", nameof(metric));

    if (!MetricCollections.TryGetValue(metricType, out var collection))
      return default;

    return ((MetricCollection<T>)collection).GetValue(metric.Key);
  }

  /// <summary>
  /// Sets the stored value for a metric.
  /// </summary>
  public void SetMetricValue<T>(Metric metric, T value)
    where T : struct
  {
    var metricType = MetricTypeHelper.GetMetricType<T>();

    if (metric.MetricType != metricType)
      throw new ArgumentException($"Metric {metric.Key} expects {metric.MetricType} value, provided {metricType}", nameof(metric));

    var collection = GetOrCreateMetricCollection<T>();
    collection.SetValue(metric.Key, value);
  }

  /// <summary>
  /// Sets the stored value for a strongly typed metric.
  /// </summary>
  public void SetMetricValue<T>(Metric<T> metric, T value)
    where T : struct
  {
    var metricType = MetricTypeHelper.GetMetricType<T>();

    if (metric.MetricType != metricType)
      throw new ArgumentException($"Metric {metric.Key} expects {metric.MetricType} value, provided {metricType}", nameof(metric));

    var collection = GetOrCreateMetricCollection<T>();
    collection.SetValue(metric.Key, value);
  }

  private IDictionary<string, DimensionValue> CreateDimensionValues(IDictionary<string, DimensionValue>? values)
  {
    return values switch
    {
      ObservableDimensionValueDictionary observableValues => observableValues,
      null => new ObservableDimensionValueDictionary(NotifyDimensionValuesChanged),
      _ => new ObservableDimensionValueDictionary(values, NotifyDimensionValuesChanged)
    };
  }

  private void NotifyDimensionValuesChanged()
  {
    DimensionValuesChanged?.Invoke(this);
  }
}

