#nullable enable

namespace Outsourced.DataCube;

/// <summary>
/// Represents a value that can be assigned to a dimension in a fact group.
/// </summary>
public class DimensionValue
{
  /// <summary>
  /// Gets a value indicating whether this value represents an all-members total.
  /// </summary>
  public virtual bool IsTotal => false;

  /// <summary>
  /// Gets a value indicating whether this value contains multiple named fields.
  /// </summary>
  public virtual bool IsComposite => false;

  /// <summary>
  /// Gets or sets the stable key for the value.
  /// </summary>
  public virtual string Key { get; set; }

  /// <summary>
  /// Gets or sets the display label for the value.
  /// </summary>
  public virtual string? Label { get; set; }

  /// <summary>
  /// Gets or sets the underlying raw value.
  /// </summary>
  public virtual object? Value { get; set; }

  /// <summary>
  /// Initializes a new dimension value.
  /// </summary>
  public DimensionValue(string key, string? label, object? value)
  {
    if (key == null)
      throw new ArgumentException("DimensionValue key cannot be null or empty", nameof(key));

    if (string.IsNullOrEmpty(key))
      key = Constants.EMPTY_LABEL;

    Key = key;
    Label = label;
    Value = value;
  }

  public override string ToString() => Label == null ? Key : $"{Key} ({Label})";

  public override bool Equals(object? obj)
  {
    if (obj is DimensionValue other)
      return string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);

    return false;
  }

  public override int GetHashCode() => Key is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Key);
}

/// <summary>
/// Represents a strongly typed dimension value.
/// </summary>
/// <typeparam name="TValue">The CLR type used for the raw value.</typeparam>
public class DimensionValue<TValue> : DimensionValue
{
  /// <summary>
  /// Gets or sets the strongly typed raw value.
  /// </summary>
  public new TValue Value
  {
    get => (TValue)base.Value!;
    set => base.Value = value;
  }

  /// <summary>
  /// Initializes a new typed dimension value.
  /// </summary>
  public DimensionValue(string key, string? displayName, TValue value)
    : base(key, displayName, value)
  {
  }

  public override bool Equals(object? obj)
  {
    if (obj is DimensionValue<TValue> other)
      return string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase) && EqualityComparer<TValue>.Default.Equals(Value, other.Value);

    return base.Equals(obj);
  }

  public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value);
}

