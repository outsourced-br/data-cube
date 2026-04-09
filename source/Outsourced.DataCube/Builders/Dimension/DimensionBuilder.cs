namespace Outsourced.DataCube.Builders;

using System;
using System.Collections.Generic;

/// <summary>
/// Builds an untyped <see cref="Dimension"/> and registers it with a cube.
/// </summary>
public class DimensionBuilder
{
  private readonly AnalyticsCube _cube;
  private readonly Dimension _dimension;

  /// <summary>
  /// Initializes a new untyped dimension builder.
  /// </summary>
  public DimensionBuilder(AnalyticsCube cube, string key, string label = null)
  {
    _cube = cube ?? throw new ArgumentNullException(nameof(cube));
    _dimension = new Dimension(key, label);
  }

  /// <summary>
  /// Sets the dimension label.
  /// </summary>
  public DimensionBuilder WithLabel(string label)
  {
    _dimension.Label = label;
    return this;
  }

  /// <summary>
  /// Adds a value to the dimension.
  /// </summary>
  public DimensionBuilder AddValue(string key, string label, object value)
  {
    var dimensionValue = new DimensionValue(key, label, value);
    _dimension.AddValue(dimensionValue);
    return this;
  }

  /// <summary>
  /// Adds multiple values to the dimension.
  /// </summary>
  public DimensionBuilder AddValueRange(IEnumerable<(string Key, string Label, object Value)> values)
  {
    foreach (var (key, label, value) in values)
    {
      AddValue(key, label, value);
    }
    return this;
  }

  /// <summary>
  /// Builds the dimension and registers it with the target cube.
  /// </summary>
  public Dimension Build()
  {
    _cube.AddDimension(_dimension);
    return _dimension;
  }
}

/// <summary>
/// Builds a strongly typed <see cref="Dimension{TValue}"/> and registers it with a cube.
/// </summary>
/// <typeparam name="TValue">The raw value type stored by the dimension.</typeparam>

public class DimensionBuilder<TValue>
{
  private readonly AnalyticsCube _cube;
  private Dimension<TValue> _dimension;

  /// <summary>
  /// Initializes a new typed dimension builder.
  /// </summary>
  public DimensionBuilder(AnalyticsCube cube, string key, string label = null)
  {
    _cube = cube ?? throw new ArgumentNullException(nameof(cube));
    _dimension = new Dimension<TValue>(key, label);
  }

  /// <summary>
  /// Sets the dimension label.
  /// </summary>
  public DimensionBuilder<TValue> WithLabel(string label)
  {
    _dimension.Label = label;
    return this;
  }

  /// <summary>
  /// Adds a value to the dimension using generated key and label text.
  /// </summary>
  public DimensionBuilder<TValue> AddValue(TValue value)
  {
    _dimension.CreateValue(value);
    return this;
  }

  /// <summary>
  /// Adds a value to the dimension.
  /// </summary>
  public DimensionBuilder<TValue> AddValue(string key, string displayName, TValue value)
  {
    _dimension.CreateValue(key, displayName, value);
    return this;
  }

  /// <summary>
  /// Adds multiple values to the dimension using generated key and label text.
  /// </summary>
  public DimensionBuilder<TValue> AddValues(IEnumerable<TValue> values)
  {
    foreach (var value in values)
    {
      _dimension.CreateValue(value);
    }
    return this;
  }

  /// <summary>
  /// Adds multiple values to the dimension.
  /// </summary>
  public DimensionBuilder<TValue> AddValues(IEnumerable<(string Key, string DisplayName, TValue Value)> values)
  {
    foreach (var (key, displayName, value) in values)
    {
      _dimension.CreateValue(key, displayName, value);
    }
    return this;
  }

  /// <summary>
  /// Replaces the dimension with a formatter-aware variant for display labels.
  /// </summary>
  public DimensionBuilder<TValue> WithFormatter(Func<TValue, string> formatter)
  {
    // Create a custom dimension that overrides the formatting method
    var formattingDimension = new FormattingTypedDimension<TValue>(_dimension.Key, _dimension.Label, formatter);

    // Copy existing values
    foreach (var value in _dimension.GetValues())
    {
      if (value is DimensionValue<TValue> typedValue)
      {
        formattingDimension.CreateValue(typedValue.Value);
      }
    }

    _dimension = formattingDimension;
    return this;
  }

  /// <summary>
  /// Builds the dimension and registers it with the target cube.
  /// </summary>
  public Dimension<TValue> Build()
  {
    _cube.AddDimension(_dimension);
    return _dimension;
  }

  private sealed class FormattingTypedDimension<T> : Dimension<T>
  {
    private readonly Func<T, string> _formatter;

    public FormattingTypedDimension(string key, string label, Func<T, string> formatter)
      : base(key, label)
    {
      _formatter = formatter;
    }

    protected override string FormatValueForDisplay(T value)
    {
      return _formatter(value);
    }
  }
}

