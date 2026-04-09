#nullable enable

namespace Outsourced.DataCube;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a cube dimension and its available values.
/// </summary>
public class Dimension
{
  /// <summary>
  /// Gets the serialized/runtime type name for the dimension.
  /// </summary>
  public virtual string Type => GetType().Name;

  /// <summary>
  /// Gets or sets the dimension key.
  /// </summary>
  public virtual string Key { get; set; }

  /// <summary>
  /// Gets or sets the display label for the dimension.
  /// </summary>
  public virtual string? Label { get; set; }

  /// <summary>
  /// Gets the values registered for this dimension.
  /// </summary>
  public virtual ISet<DimensionValue> Values { get; } = new HashSet<DimensionValue>();

  /// <summary>
  /// Gets the hierarchies registered for this dimension.
  /// </summary>
  public virtual IList<Hierarchy.Hierarchy> Hierarchies { get; } = new List<Hierarchy.Hierarchy>();

  /// <summary>
  /// Initializes a new dimension.
  /// </summary>
  /// <param name="key">The unique key for the dimension.</param>
  /// <param name="label">The display label for the dimension.</param>
  public Dimension(string key, string? label = null)
  {
    if (key == null)
      throw new ArgumentException("Dimension key cannot be null or empty", nameof(key));

    if (string.IsNullOrEmpty(key))
      key = Constants.EMPTY_LABEL;

    Key = key;
    Label = label;
  }

  /// <summary>
  /// Adds an existing value to the dimension.
  /// </summary>
  public virtual void AddValue(DimensionValue value)
  {
    ArgumentNullException.ThrowIfNull(value);
    Values.Add(value);
  }

  /// <summary>
  /// Adds an existing hierarchy to the dimension.
  /// </summary>
  public virtual void AddHierarchy(Hierarchy.Hierarchy hierarchy)
  {
    ArgumentNullException.ThrowIfNull(hierarchy);
    ArgumentException.ThrowIfNullOrEmpty(hierarchy.Key);

    if (GetHierarchy(hierarchy.Key) != null)
      throw new ArgumentException($"Hierarchy {hierarchy.Key} already exists", nameof(hierarchy));

    hierarchy.Attach(this);
    Hierarchies.Add(hierarchy);
  }

  /// <summary>
  /// Creates, attaches, and returns a new hierarchy for the dimension.
  /// </summary>
  public virtual Hierarchy.Hierarchy CreateHierarchy(string key, string? label = null)
  {
    var hierarchy = new Hierarchy.Hierarchy(key, label);
    AddHierarchy(hierarchy);
    return hierarchy;
  }

  /// <summary>
  /// Creates and adds a new untyped dimension value.
  /// </summary>
  public virtual DimensionValue CreateValue(string key, string? label, object? value)
  {
    var dimensionValue = new DimensionValue(key, label, value);
    AddValue(dimensionValue);
    return dimensionValue;
  }

  /// <summary>
  /// Finds a value by its key.
  /// </summary>
  public virtual DimensionValue? GetValue(string key)
  {
    foreach (var value in Values)
    {
      if (string.Equals(value.Key, key, StringComparison.OrdinalIgnoreCase))
        return value;
    }

    return null;
  }

  /// <summary>
  /// Finds a value by comparing its raw value.
  /// </summary>
  public virtual DimensionValue? GetValueByRawValue(object? value)
  {
    foreach (var dimensionValue in Values)
    {
      if (Equals(dimensionValue.Value, value))
        return dimensionValue;
    }

    return null;
  }

  /// <summary>
  /// Finds a hierarchy by its key.
  /// </summary>
  public virtual Hierarchy.Hierarchy? GetHierarchy(string key)
  {
    foreach (var hierarchy in Hierarchies)
    {
      if (string.Equals(hierarchy.Key, key, StringComparison.OrdinalIgnoreCase))
        return hierarchy;
    }

    return null;
  }

  /// <summary>
  /// Returns the registered values for this dimension.
  /// </summary>
  public virtual IEnumerable<DimensionValue> GetValues() => Values;

  /// <summary>
  /// Returns the hierarchies registered for this dimension.
  /// </summary>
  public virtual IEnumerable<Hierarchy.Hierarchy> GetHierarchies() => Hierarchies;
}

/// <summary>
/// Represents a strongly typed cube dimension.
/// </summary>
/// <typeparam name="TValue">The CLR type used for the dimension's raw values.</typeparam>
public class Dimension<TValue> : Dimension
{
  /// <summary>
  /// Gets the serialized/runtime type name for the dimension.
  /// </summary>
  public override string Type => $"Dimension<{typeof(TValue).Name}>";

  /// <summary>
  /// Initializes a new typed dimension.
  /// </summary>
  public Dimension(string key, string? label = null)
    : base(key, label)
  {
  }

  /// <summary>
  /// Creates and adds a new typed dimension value using generated key and label text.
  /// </summary>
  public virtual DimensionValue<TValue> CreateValue(TValue value)
  {
    string key = ConvertValueToKey(value);
    string label = FormatValueForDisplay(value);

    var dimensionValue = new DimensionValue<TValue>(key, label, value);
    AddValue(dimensionValue);
    return dimensionValue;
  }

  /// <summary>
  /// Creates and adds a new typed dimension value.
  /// </summary>
  public virtual DimensionValue<TValue> CreateValue(string key, string? label, TValue value)
  {
    var dimensionValue = new DimensionValue<TValue>(key, label, value);
    AddValue(dimensionValue);
    return dimensionValue;
  }

  /// <summary>
  /// Finds a typed value by key.
  /// </summary>
  public new virtual DimensionValue<TValue>? GetValue(string key)
  {
    foreach (var value in Values)
    {
      if (string.Equals(value.Key, key, StringComparison.OrdinalIgnoreCase) && value is DimensionValue<TValue> typedValue)
        return typedValue;
    }

    return null;
  }

  /// <summary>
  /// Finds a typed value by its raw value.
  /// </summary>
  public virtual DimensionValue<TValue>? GetValueByRawValue(TValue value)
  {
    foreach (var dimensionValue in Values)
    {
      if (dimensionValue is DimensionValue<TValue> typedValue && Equals(typedValue.Value, value))
        return typedValue;
    }

    return null;
  }

  /// <summary>
  /// Gets an existing value or creates a new one when the raw value has not been registered yet.
  /// </summary>
  public virtual DimensionValue<TValue> GetOrCreateValue(TValue value)
  {
    return GetValueByRawValue(value) ?? CreateValue(value);
  }

  protected virtual string ConvertValueToKey(TValue value)
  {
    var valueString = value?.ToString();
    if (valueString == null) valueString = Constants.NULL_LABEL;
    else if (string.IsNullOrEmpty(valueString)) valueString = Constants.EMPTY_LABEL;

    return valueString;
  }

  protected virtual string FormatValueForDisplay(TValue value)
  {
    var valueString = value?.ToString();
    if (valueString == null) valueString = Constants.NULL_LABEL;
    else if (string.IsNullOrEmpty(valueString)) valueString = Constants.EMPTY_LABEL;

    return valueString;
  }
}

