namespace Outsourced.DataCube.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using Metrics;

/// <summary>
/// Provides an allocation-light dictionary keyed by <see cref="MetricType"/> values.
/// </summary>
public sealed class MetricTypeDictionary : IDictionary<MetricType, IMetricCollection>
{
  private static readonly int CollectionCapacity = GetCollectionCapacity();

  private IMetricCollection[] _collections = new IMetricCollection[CollectionCapacity];
  private int _count;

  /// <summary>
  /// Gets or sets a metric collection for a metric value type.
  /// </summary>
  public IMetricCollection this[MetricType key]
  {
    get
    {
      ValidateKey(key);

      var value = _collections[(int)key];
      if (value == null) throw new KeyNotFoundException();
      return value;
    }
    set
    {
      ValidateKey(key);
      ArgumentNullException.ThrowIfNull(value);

      if (_collections[(int)key] == null) _count++;
      _collections[(int)key] = value;
    }
  }

  /// <summary>
  /// Gets the stored metric types.
  /// </summary>
  public ICollection<MetricType> Keys => new KeyCollection(this);

  /// <summary>
  /// Gets the stored metric collections.
  /// </summary>
  public ICollection<IMetricCollection> Values => new ValueCollection(this);

  /// <summary>
  /// Gets the number of stored collections.
  /// </summary>
  public int Count => _count;

  /// <summary>
  /// Gets a value indicating whether the dictionary is read-only.
  /// </summary>
  public bool IsReadOnly => false;

  /// <summary>
  /// Adds a metric collection for the supplied metric type.
  /// </summary>
  public void Add(MetricType key, IMetricCollection value)
  {
    ValidateKey(key);
    ArgumentNullException.ThrowIfNull(value);

    if (_collections[(int)key] != null) throw new ArgumentException("Key already exists.");
    _collections[(int)key] = value;
    _count++;
  }

  /// <summary>
  /// Adds a metric collection for the supplied metric type.
  /// </summary>
  public void Add(KeyValuePair<MetricType, IMetricCollection> item) => Add(item.Key, item.Value);

  /// <summary>
  /// Removes all stored metric collections.
  /// </summary>
  public void Clear()
  {
    Array.Clear(_collections, 0, _collections.Length);
    _count = 0;
  }

  /// <summary>
  /// Determines whether the dictionary contains a specific key/value pair.
  /// </summary>
  public bool Contains(KeyValuePair<MetricType, IMetricCollection> item)
  {
    return IsValidKey(item.Key) && EqualityComparer<IMetricCollection>.Default.Equals(_collections[(int)item.Key], item.Value);
  }

  /// <summary>
  /// Determines whether a collection exists for the supplied metric type.
  /// </summary>
  public bool ContainsKey(MetricType key)
  {
    return IsValidKey(key) && _collections[(int)key] != null;
  }

  /// <summary>
  /// Copies the stored collections into an array.
  /// </summary>
  public void CopyTo(KeyValuePair<MetricType, IMetricCollection>[] array, int arrayIndex)
  {
    if (array == null) throw new ArgumentNullException(nameof(array));
    if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
    if (array.Length - arrayIndex < _count) throw new ArgumentException("Array too small");

    for (int i = 0; i < _collections.Length; i++)
    {
      if (_collections[i] != null)
        array[arrayIndex++] = new KeyValuePair<MetricType, IMetricCollection>((MetricType)i, _collections[i]);
    }
  }

  /// <summary>
  /// Returns an enumerator over the stored metric collections.
  /// </summary>
  public IEnumerator<KeyValuePair<MetricType, IMetricCollection>> GetEnumerator()
  {
    for (int i = 0; i < _collections.Length; i++)
    {
      if (_collections[i] != null)
        yield return new KeyValuePair<MetricType, IMetricCollection>((MetricType)i, _collections[i]);
    }
  }

  /// <summary>
  /// Removes the collection associated with the supplied metric type.
  /// </summary>
  public bool Remove(MetricType key)
  {
    if (IsValidKey(key) && _collections[(int)key] != null)
    {
      _collections[(int)key] = null;
      _count--;
      return true;
    }
    return false;
  }

  /// <summary>
  /// Removes a specific key/value pair when present.
  /// </summary>
  public bool Remove(KeyValuePair<MetricType, IMetricCollection> item)
  {
    if (IsValidKey(item.Key) && EqualityComparer<IMetricCollection>.Default.Equals(_collections[(int)item.Key], item.Value))
    {
      _collections[(int)item.Key] = null;
      _count--;
      return true;
    }
    return false;
  }

  /// <summary>
  /// Attempts to retrieve the collection associated with the supplied metric type.
  /// </summary>
  public bool TryGetValue(MetricType key, out IMetricCollection value)
  {
    if (IsValidKey(key))
    {
      value = _collections[(int)key];
      return value != null;
    }
    value = null;
    return false;
  }

  private sealed class KeyCollection : ICollection<MetricType>
  {
    private readonly MetricTypeDictionary _dictionary;
    public KeyCollection(MetricTypeDictionary dictionary) => _dictionary = dictionary;
    public int Count => _dictionary._count;
    public bool IsReadOnly => true;
    public void Add(MetricType item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Contains(MetricType item) => _dictionary.ContainsKey(item);
    public void CopyTo(MetricType[] array, int arrayIndex)
    {
      if (array == null) throw new ArgumentNullException(nameof(array));
      if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
      if (array.Length - arrayIndex < _dictionary._count) throw new ArgumentException("Array too small");

      for (int i = 0; i < _dictionary._collections.Length; i++)
        if (_dictionary._collections[i] != null) array[arrayIndex++] = (MetricType)i;
    }
    public IEnumerator<MetricType> GetEnumerator()
    {
      for (int i = 0; i < _dictionary._collections.Length; i++)
        if (_dictionary._collections[i] != null) yield return (MetricType)i;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool Remove(MetricType item) => throw new NotSupportedException();
  }

  private sealed class ValueCollection : ICollection<IMetricCollection>
  {
    private readonly MetricTypeDictionary _dictionary;
    public ValueCollection(MetricTypeDictionary dictionary) => _dictionary = dictionary;
    public int Count => _dictionary._count;
    public bool IsReadOnly => true;
    public void Add(IMetricCollection item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Contains(IMetricCollection item)
    {
      for (int i = 0; i < _dictionary._collections.Length; i++)
        if (EqualityComparer<IMetricCollection>.Default.Equals(_dictionary._collections[i], item)) return true;
      return false;
    }
    public void CopyTo(IMetricCollection[] array, int arrayIndex)
    {
      if (array == null) throw new ArgumentNullException(nameof(array));
      if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
      if (array.Length - arrayIndex < _dictionary._count) throw new ArgumentException("Array too small");

      for (int i = 0; i < _dictionary._collections.Length; i++)
        if (_dictionary._collections[i] != null) array[arrayIndex++] = _dictionary._collections[i];
    }
    public IEnumerator<IMetricCollection> GetEnumerator()
    {
      for (int i = 0; i < _dictionary._collections.Length; i++)
        if (_dictionary._collections[i] != null) yield return _dictionary._collections[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool Remove(IMetricCollection item) => throw new NotSupportedException();
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  private static int GetCollectionCapacity()
  {
    int highestValue = 0;
    foreach (var metricType in Enum.GetValues<MetricType>())
    {
      highestValue = Math.Max(highestValue, (int)metricType);
    }

    return highestValue + 1;
  }

  private static bool IsValidKey(MetricType key) => (uint)key < (uint)CollectionCapacity;

  private static void ValidateKey(MetricType key)
  {
    if (!IsValidKey(key))
      throw new ArgumentOutOfRangeException(nameof(key), key, "The metric type is outside the supported range.");
  }
}
