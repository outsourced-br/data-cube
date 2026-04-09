namespace Outsourced.DataCube.Builders;

using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Builds an untyped <see cref="CompositeDimension"/> and registers it with a cube.
/// </summary>
public class CompositeDimensionBuilder
{
  private readonly AnalyticsCube _cube;
  private readonly CompositeDimension _dimension;

  /// <summary>
  /// Initializes a new composite dimension builder.
  /// </summary>
  public CompositeDimensionBuilder(AnalyticsCube cube, string key, string label = null, params string[] criteria)
  {
    _cube = cube ?? throw new ArgumentNullException(nameof(cube));
    _dimension = new CompositeDimension(criteria)
    {
      Key = key,
      Label = label ?? key
    };
  }

  /// <summary>
  /// Sets the dimension label.
  /// </summary>
  public CompositeDimensionBuilder WithLabel(string label)
  {
    _dimension.Label = label;
    return this;
  }

  /// <summary>
  /// Adds a composite value to the dimension.
  /// </summary>
  public CompositeDimensionBuilder AddValue(IDictionary<string, object> fieldValues)
  {
    _dimension.CreateCompositeValue(fieldValues);
    return this;
  }

  /// <summary>
  /// Adds multiple composite values to the dimension.
  /// </summary>
  public CompositeDimensionBuilder AddValues(IEnumerable<IDictionary<string, object>> values)
  {
    foreach (var fieldValues in values)
    {
      _dimension.CreateCompositeValue(fieldValues);
    }
    return this;
  }

  /// <summary>
  /// Builds the dimension and registers it with the target cube.
  /// </summary>
  public CompositeDimension Build()
  {
    _cube.AddDimension(_dimension);
    return _dimension;
  }
}

/// <summary>
/// Builds a <see cref="CompositeDimension{TEntity}"/> and registers it with a cube.
/// </summary>
/// <typeparam name="TEntity">The entity type used to create composite values.</typeparam>
public class CompositeDimensionBuilder<TEntity>
  where TEntity : new()
{
  private readonly AnalyticsCube _cube;
  private readonly List<string> _propertyNames = new();
  private string _key;
  private string _label;

  /// <summary>
  /// Initializes a new typed composite dimension builder.
  /// </summary>
  public CompositeDimensionBuilder(AnalyticsCube cube, string key = null, string label = null)
  {
    _cube = cube ?? throw new ArgumentNullException(nameof(cube));
    _key = key ?? typeof(TEntity).Name;
    _label = label ?? _key;
  }

  /// <summary>
  /// Sets the dimension label.
  /// </summary>
  public CompositeDimensionBuilder<TEntity> WithLabel(string label)
  {
    _label = label;
    return this;
  }

  /// <summary>
  /// Adds an entity property by name to the composite key.
  /// </summary>
  public CompositeDimensionBuilder<TEntity> AddProperty(string propertyName)
  {
    if (typeof(TEntity).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) is not null)
    {
      _propertyNames.Add(propertyName);
    }
    else
    {
      throw new ArgumentException($"Property '{propertyName}' not found on type {typeof(TEntity).Name}", nameof(propertyName));
    }
    return this;
  }

  /// <summary>
  /// Adds an entity property selected by expression to the composite key.
  /// </summary>
  public CompositeDimensionBuilder<TEntity> AddProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, bool condition = true)
  {
    if (!condition) return this;

    var propertyName = propertySelector.GetPropertyName();
    _propertyNames.Add(propertyName);
    return this;
  }

  /// <summary>
  /// Adds multiple entity properties by name to the composite key.
  /// </summary>
  public CompositeDimensionBuilder<TEntity> AddProperties(params string[] propertyNames)
  {
    foreach (var propertyName in propertyNames)
    {
      AddProperty(propertyName);
    }
    return this;
  }

  /// <summary>
  /// Adds multiple entity properties selected by expression to the composite key.
  /// </summary>
  public CompositeDimensionBuilder<TEntity> AddProperties(params Expression<Func<TEntity, object>>[] propertySelectors)
  {
    foreach (var selector in propertySelectors)
    {
      var propertyName = selector.GetPropertyName();
      _propertyNames.Add(propertyName);
    }
    return this;
  }

  /// <summary>
  /// Builds the dimension and registers it with the target cube.
  /// </summary>
  public CompositeDimension<TEntity> Build()
  {
    if (_propertyNames.Count == 0)
    {
      throw new InvalidOperationException("At least one property must be added to the composite dimension");
    }

    var dimension = new CompositeDimension<TEntity>(_propertyNames.ToArray())
    {
      Key = _key,
      Label = _label,
    };

    _cube.AddDimension(dimension);
    return dimension;
  }
}

