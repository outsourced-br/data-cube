namespace Outsourced.DataCube.Collections;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A low-allocation, flat dictionary for tiny collections (for example, &lt; 10 items) optimized for fast linear search.
/// Does not compute hash codes, improving CPU cache locality and speed for small, case-insensitive string-keyed datasets.
/// </summary>
public sealed class FlatStringDictionary<T> : IDictionary<string, T>
{
  private struct Entry
  {
    public string Key;
    public T Value;
  }

  private Entry[] _entries;
  private int _count;

  /// <summary>
  /// Initializes a new dictionary with the requested initial capacity.
  /// </summary>
  /// <param name="capacity">The number of entries to allocate space for initially.</param>
  public FlatStringDictionary(int capacity = 4)
  {
    _entries = capacity == 0 ? Array.Empty<Entry>() : new Entry[capacity];
    _count = 0;
  }

  /// <summary>
  /// Gets or sets a value by key using case-insensitive lookup semantics.
  /// </summary>
  public T this[string key]
  {
    get
    {
      ArgumentNullException.ThrowIfNull(key);

      if (TryGetValue(key, out var value)) return value;
      throw new KeyNotFoundException($"Key '{key}' was not found.");
    }
    set
    {
      ArgumentNullException.ThrowIfNull(key);

      for (int i = 0; i < _count; i++)
      {
        if (string.Equals(_entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
        {
          _entries[i].Value = value;
          return;
        }
      }
      Add(key, value);
    }
  }

  /// <summary>
  /// Gets the keys stored in the dictionary.
  /// </summary>
  public ICollection<string> Keys => new KeyCollection(this);

  /// <summary>
  /// Gets the values stored in the dictionary.
  /// </summary>
  public ICollection<T> Values => new ValueCollection(this);

  /// <summary>
  /// Gets the number of stored entries.
  /// </summary>
  public int Count => _count;

  /// <summary>
  /// Gets a value indicating whether the dictionary is read-only.
  /// </summary>
  public bool IsReadOnly => false;

  /// <summary>
  /// Adds a key/value pair to the dictionary.
  /// </summary>
  /// <param name="key">The entry key.</param>
  /// <param name="value">The entry value.</param>
  public void Add(string key, T value)
  {
    ArgumentNullException.ThrowIfNull(key);

    for (int i = 0; i < _count; i++)
    {
      if (string.Equals(_entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
      {
        throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
      }
    }

    // Ensure capacity
    if (_count == _entries.Length)
    {
      var newEntries = new Entry[_entries.Length == 0 ? 4 : _entries.Length * 2];
      if (_count > 0)
      {
        Array.Copy(_entries, newEntries, _count);
      }
      _entries = newEntries;
    }

    _entries[_count] = new Entry { Key = key, Value = value };
    _count++;
  }

  /// <summary>
  /// Adds a key/value pair to the dictionary.
  /// </summary>
  public void Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);

  /// <summary>
  /// Removes all entries from the dictionary.
  /// </summary>
  public void Clear()
  {
    Array.Clear(_entries, 0, _count);
    _count = 0;
  }

  /// <summary>
  /// Determines whether the dictionary contains a specific key/value pair.
  /// </summary>
  public bool Contains(KeyValuePair<string, T> item)
  {
    ArgumentNullException.ThrowIfNull(item.Key);

    for (int i = 0; i < _count; i++)
    {
      if (string.Equals(_entries[i].Key, item.Key, StringComparison.OrdinalIgnoreCase) &&
          EqualityComparer<T>.Default.Equals(_entries[i].Value, item.Value))
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Determines whether the dictionary contains the specified key.
  /// </summary>
  public bool ContainsKey(string key)
  {
    ArgumentNullException.ThrowIfNull(key);

    for (int i = 0; i < _count; i++)
    {
      if (string.Equals(_entries[i].Key, key, StringComparison.OrdinalIgnoreCase)) return true;
    }
    return false;
  }

  /// <summary>
  /// Copies the stored entries into an array.
  /// </summary>
  public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
  {
    if (array == null) throw new ArgumentNullException(nameof(array));
    if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
    if (array.Length - arrayIndex < _count) throw new ArgumentException("Array too small");

    for (int i = 0; i < _count; i++)
    {
      array[arrayIndex + i] = new KeyValuePair<string, T>(_entries[i].Key, _entries[i].Value);
    }
  }

  /// <summary>
  /// Returns an enumerator over the stored entries.
  /// </summary>
  public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
  {
    for (int i = 0; i < _count; i++)
    {
      yield return new KeyValuePair<string, T>(_entries[i].Key, _entries[i].Value);
    }
  }

  /// <summary>
  /// Removes an entry by key.
  /// </summary>
  public bool Remove(string key)
  {
    ArgumentNullException.ThrowIfNull(key);

    for (int i = 0; i < _count; i++)
    {
      if (string.Equals(_entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
      {
        _count--;
        if (i < _count)
        {
          _entries[i] = _entries[_count]; // Swap with last element
        }
        _entries[_count] = default; // Clear reference
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Removes a specific key/value pair when present.
  /// </summary>
  public bool Remove(KeyValuePair<string, T> item)
  {
    ArgumentNullException.ThrowIfNull(item.Key);

    for (int i = 0; i < _count; i++)
    {
      if (string.Equals(_entries[i].Key, item.Key, StringComparison.OrdinalIgnoreCase) &&
          EqualityComparer<T>.Default.Equals(_entries[i].Value, item.Value))
      {
        _count--;
        if (i < _count)
        {
          _entries[i] = _entries[_count];
        }
        _entries[_count] = default;
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Attempts to retrieve a value for the specified key.
  /// </summary>
  public bool TryGetValue(string key, out T value)
  {
    ArgumentNullException.ThrowIfNull(key);

    for (int i = 0; i < _count; i++)
    {
      if (string.Equals(_entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
      {
        value = _entries[i].Value;
        return true;
      }
    }
    value = default;
    return false;
  }

  private sealed class KeyCollection : ICollection<string>
  {
    private readonly FlatStringDictionary<T> _dictionary;
    public KeyCollection(FlatStringDictionary<T> dictionary) => _dictionary = dictionary;
    public int Count => _dictionary._count;
    public bool IsReadOnly => true;
    public void Add(string item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Contains(string item) => _dictionary.ContainsKey(item);
    public void CopyTo(string[] array, int arrayIndex)
    {
      if (array == null) throw new ArgumentNullException(nameof(array));
      if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
      if (array.Length - arrayIndex < _dictionary._count) throw new ArgumentException("Array too small");

      for (int i = 0; i < _dictionary._count; i++) array[arrayIndex + i] = _dictionary._entries[i].Key;
    }
    public IEnumerator<string> GetEnumerator()
    {
      for (int i = 0; i < _dictionary._count; i++) yield return _dictionary._entries[i].Key;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool Remove(string item) => throw new NotSupportedException();
  }

  private sealed class ValueCollection : ICollection<T>
  {
    private readonly FlatStringDictionary<T> _dictionary;
    public ValueCollection(FlatStringDictionary<T> dictionary) => _dictionary = dictionary;
    public int Count => _dictionary._count;
    public bool IsReadOnly => true;
    public void Add(T item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Contains(T item)
    {
      var comparer = EqualityComparer<T>.Default;
      for (int i = 0; i < _dictionary._count; i++) if (comparer.Equals(_dictionary._entries[i].Value, item)) return true;
      return false;
    }
    public void CopyTo(T[] array, int arrayIndex)
    {
      if (array == null) throw new ArgumentNullException(nameof(array));
      if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
      if (array.Length - arrayIndex < _dictionary._count) throw new ArgumentException("Array too small");

      for (int i = 0; i < _dictionary._count; i++) array[arrayIndex + i] = _dictionary._entries[i].Value;
    }
    public IEnumerator<T> GetEnumerator()
    {
      for (int i = 0; i < _dictionary._count; i++) yield return _dictionary._entries[i].Value;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool Remove(T item) => throw new NotSupportedException();
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
