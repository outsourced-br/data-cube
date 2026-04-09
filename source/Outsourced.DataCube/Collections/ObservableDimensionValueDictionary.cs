namespace Outsourced.DataCube.Collections;

using System;
using System.Collections;
using System.Collections.Generic;

internal sealed class ObservableDimensionValueDictionary : IDictionary<string, DimensionValue>
{
  private readonly FlatStringDictionary<DimensionValue> _inner = new();
  private readonly Action _onChanged;

  public ObservableDimensionValueDictionary(Action onChanged)
    : this(Array.Empty<KeyValuePair<string, DimensionValue>>(), onChanged)
  {
  }

  public ObservableDimensionValueDictionary(IEnumerable<KeyValuePair<string, DimensionValue>> values, Action onChanged)
  {
    _onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));

    foreach (var (key, value) in values)
    {
      _inner[key] = value;
    }
  }

  public DimensionValue this[string key]
  {
    get => _inner[key];
    set
    {
      var existed = _inner.TryGetValue(key, out var previousValue);
      _inner[key] = value;

      try
      {
        _onChanged();
      }
      catch
      {
        if (existed)
          _inner[key] = previousValue;
        else
          _inner.Remove(key);

        throw;
      }
    }
  }

  public ICollection<string> Keys => _inner.Keys;

  public ICollection<DimensionValue> Values => _inner.Values;

  public int Count => _inner.Count;

  public bool IsReadOnly => false;

  public void Add(string key, DimensionValue value)
  {
    if (_inner.ContainsKey(key))
      throw new ArgumentException($"An item with the same key has already been added. Key: {key}", nameof(key));

    _inner.Add(key, value);

    try
    {
      _onChanged();
    }
    catch
    {
      _inner.Remove(key);
      throw;
    }
  }

  public void Add(KeyValuePair<string, DimensionValue> item) => Add(item.Key, item.Value);

  public void Clear()
  {
    if (_inner.Count == 0)
      return;

    var previousItems = _inner.ToArray();
    _inner.Clear();

    try
    {
      _onChanged();
    }
    catch
    {
      foreach (var (key, value) in previousItems)
      {
        _inner[key] = value;
      }

      throw;
    }
  }

  public bool Contains(KeyValuePair<string, DimensionValue> item) => _inner.Contains(item);

  public bool ContainsKey(string key) => _inner.ContainsKey(key);

  public void CopyTo(KeyValuePair<string, DimensionValue>[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

  public IEnumerator<KeyValuePair<string, DimensionValue>> GetEnumerator() => _inner.GetEnumerator();

  public bool Remove(string key)
  {
    if (!_inner.TryGetValue(key, out var previousValue))
      return false;

    _inner.Remove(key);

    try
    {
      _onChanged();
      return true;
    }
    catch
    {
      _inner[key] = previousValue;
      throw;
    }
  }

  public bool Remove(KeyValuePair<string, DimensionValue> item)
  {
    if (!_inner.Remove(item))
      return false;

    try
    {
      _onChanged();
      return true;
    }
    catch
    {
      _inner[item.Key] = item.Value;
      throw;
    }
  }

  public bool TryGetValue(string key, out DimensionValue value) => _inner.TryGetValue(key, out value);

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
