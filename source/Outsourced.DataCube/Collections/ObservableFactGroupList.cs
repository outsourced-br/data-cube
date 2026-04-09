namespace Outsourced.DataCube.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

internal sealed class ObservableFactGroupList : IList<FactGroup>
{
  private readonly List<FactGroup> _items;
  private readonly Action<FactGroup> _onAdded;
  private readonly Action<FactGroup> _onRemoved;

  public ObservableFactGroupList(
    IEnumerable<FactGroup> items,
    Action<FactGroup> onAdded,
    Action<FactGroup> onRemoved)
  {
    _items = items?.ToList() ?? new List<FactGroup>();
    _onAdded = onAdded ?? throw new ArgumentNullException(nameof(onAdded));
    _onRemoved = onRemoved ?? throw new ArgumentNullException(nameof(onRemoved));
  }

  public FactGroup this[int index]
  {
    get => _items[index];
    set
    {
      ArgumentNullException.ThrowIfNull(value);

      var previous = _items[index];
      if (ReferenceEquals(previous, value))
        return;

      _onRemoved(previous);
      _items[index] = value;

      try
      {
        _onAdded(value);
      }
      catch
      {
        _items[index] = previous;
        _onAdded(previous);
        throw;
      }
    }
  }

  public int Count => _items.Count;

  public bool IsReadOnly => false;

  public void Add(FactGroup item)
  {
    ArgumentNullException.ThrowIfNull(item);

    _items.Add(item);

    try
    {
      _onAdded(item);
    }
    catch
    {
      _items.RemoveAt(_items.Count - 1);
      throw;
    }
  }

  public void Clear()
  {
    foreach (var item in _items)
    {
      _onRemoved(item);
    }

    _items.Clear();
  }

  public bool Contains(FactGroup item) => _items.Contains(item);

  public void CopyTo(FactGroup[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

  public IEnumerator<FactGroup> GetEnumerator() => _items.GetEnumerator();

  public int IndexOf(FactGroup item) => _items.IndexOf(item);

  public void Insert(int index, FactGroup item)
  {
    ArgumentNullException.ThrowIfNull(item);

    _items.Insert(index, item);

    try
    {
      _onAdded(item);
    }
    catch
    {
      _items.RemoveAt(index);
      throw;
    }
  }

  public bool Remove(FactGroup item)
  {
    if (!_items.Remove(item))
      return false;

    _onRemoved(item);
    return true;
  }

  public void RemoveAt(int index)
  {
    var item = _items[index];
    _items.RemoveAt(index);
    _onRemoved(item);
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
