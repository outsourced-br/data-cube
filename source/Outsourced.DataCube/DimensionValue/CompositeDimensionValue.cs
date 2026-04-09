namespace Outsourced.DataCube;

/// <summary>
/// Represents a dimension value composed of multiple named fields.
/// </summary>
public class CompositeDimensionValue : DimensionValue
{
  /// <summary>
  /// Gets a value indicating whether this value is composite.
  /// </summary>
  public override bool IsComposite => true;

  /// <summary>
  /// Gets or sets the component field values for this composite value.
  /// </summary>
  public new IDictionary<string, object> Value
  {
    get => (IDictionary<string, object>)base.Value;
    set => base.Value = value;
  }

  /// <summary>
  /// Initializes a new composite dimension value.
  /// </summary>
  public CompositeDimensionValue(string key, string displayName, IDictionary<string, object> fieldValues)
    : base(key, displayName, fieldValues ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase))
  {
  }

  /// <summary>
  /// Gets a field value by key.
  /// </summary>
  public object GetFieldValue(string fieldKey)
  {
    return Value.TryGetValue(fieldKey, out var fieldValue) ? fieldValue : null;
  }

  /// <summary>
  /// Gets a field value by key and casts it to the requested type when possible.
  /// </summary>
  public T GetFieldValue<T>(string fieldKey)
  {
    if (Value.TryGetValue(fieldKey, out var fieldValue) && fieldValue is T typedValue)
      return typedValue;

    return default;
  }

  public override bool Equals(object obj)
  {
    if (obj is CompositeDimensionValue other)
      return string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);

    return base.Equals(obj);
  }

  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Key);
}

/// <summary>
/// Represents a composite dimension value associated with an entity instance.
/// </summary>
/// <typeparam name="TEntity">The entity type represented by this value.</typeparam>
public class CompositeDimensionValue<TEntity> : CompositeDimensionValue
  where TEntity : new()
{
  private TEntity _entity;

  /// <summary>
  /// Gets the entity represented by this value, materializing it from the field dictionary when needed.
  /// </summary>
  public TEntity Entity
  {
    get
    {
      if (EqualityComparer<TEntity>.Default.Equals(_entity, default))
        _entity = Value.ToEntity<TEntity>();

      return _entity;
    }
  }

  /// <summary>
  /// Initializes a new entity-backed composite dimension value.
  /// </summary>
  public CompositeDimensionValue(string key, string displayName, IDictionary<string, object> fieldValues, TEntity entity)
    : base(key, displayName, fieldValues)
  {
    _entity = entity;
  }

  public override bool Equals(object obj)
  {
    if (obj is CompositeDimensionValue<TEntity> other)
      return string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);

    return base.Equals(obj);
  }

  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Key);
}

