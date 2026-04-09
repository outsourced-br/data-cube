namespace Outsourced.DataCube.Metrics;

using System.Collections.Generic;

/// <summary>
/// Stores metric values for a specific CLR value type.
/// </summary>
/// <typeparam name="T">The metric value type.</typeparam>
public class MetricCollection<T> : IMetricCollection where T : struct
{
  private readonly Collections.FlatStringDictionary<T> _values = new();

  /// <summary>
  /// Gets the metric value type stored by this collection.
  /// </summary>
  public MetricType Type => MetricTypeHelper.GetMetricType<T>();

  /// <summary>
  /// Returns the metric keys currently stored in the collection.
  /// </summary>
  public IEnumerable<string> GetMetricKeys() => _values.Keys;

  /// <summary>
  /// Sets a metric value by key.
  /// </summary>
  public void SetValue(string key, T value)
  {
    _values[key] = value;
  }

  /// <summary>
  /// Gets a metric value by key, or the default value when the key is not present.
  /// </summary>
  public T GetValue(string key)
  {
    return _values.TryGetValue(key, out var value) ? value : default;
  }

  /// <summary>
  /// Tries to get a metric value by key.
  /// </summary>
  public bool TryGetValue(string key, out T value)
  {
    return _values.TryGetValue(key, out value);
  }
}
