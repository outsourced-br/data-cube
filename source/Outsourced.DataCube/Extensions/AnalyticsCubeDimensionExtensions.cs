namespace Outsourced.DataCube;

using System.Globalization;
using System.Linq.Expressions;
using Builders;
using Metrics;

/// <summary>
/// Provides convenience methods for creating, registering, and querying dimensions on a cube.
/// </summary>
public static class AnalyticsCubeDimensionExtensions
{
  #region Dimension

  /// <summary>
  /// Creates an untyped dimension builder for the cube.
  /// </summary>
  public static DimensionBuilder CreateDimension(this AnalyticsCube cube, string key, string label = null)
  {
    return new DimensionBuilder(cube, key, label);
  }

  /// <summary>
  /// Creates and registers an untyped dimension.
  /// </summary>
  public static Dimension AddDimension(this AnalyticsCube cube, string key, string label = null)
  {
    var dimension = new Dimension(key, label ?? key);
    cube.AddDimension(dimension);
    return dimension;
  }


  // Create standard dimensions more easily
  /// <summary>
  /// Creates and registers a date-based dimension with values across an inclusive time range.
  /// </summary>
  public static Dimension CreateTimeDimension(this AnalyticsCube cube, DateTime start, DateTime end, TimeSpan interval, string key = "Time", string label = "Time Period")
  {
    var builder = new DimensionBuilder(cube, key, label);

    for (var date = start; date <= end; date += interval)
    {
      var keyValue = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
      builder.AddValue(keyValue, keyValue, date);
    }

    return builder.Build();
  }

  #endregion

  #region TypedDimension

  /// <summary>
  /// Creates a typed dimension builder for the cube.
  /// </summary>
  public static DimensionBuilder<TValue> CreateTypedDimension<TValue>(this AnalyticsCube cube, string key, string label = null)
  {
    return new DimensionBuilder<TValue>(cube, key, label);
  }

  /// <summary>
  /// Creates and registers a typed dimension.
  /// </summary>
  public static Dimension<TValue> AddTypedDimension<TValue>(this AnalyticsCube cube, string key, string label = null)
  {
    var dimension = new Dimension<TValue>(key, label);
    cube.AddDimension(dimension);
    return dimension;
  }

  /// <summary>
  /// Creates a typed dimension builder based on a selected entity property.
  /// </summary>
  public static DimensionBuilder<TProperty> CreatePropertyDimension<TEntity, TProperty>(
    this AnalyticsCube cube,
    TEntity entity,
    Expression<Func<TEntity, TProperty>> propertySelector,
    string label = null)
  {
    var propertyValue = entity.GetPropertyValueAsString(propertySelector);
    return cube.CreateTypedDimension<TProperty>(propertyValue, label);
  }

  /// <summary>
  /// Creates a typed dimension builder using the selected property name as the dimension key.
  /// </summary>
  public static DimensionBuilder<TProperty> CreatePropertyDimension<TEntity, TProperty>(
    this AnalyticsCube cube,
    IEnumerable<TEntity> entity,
    Expression<Func<TEntity, TProperty>> propertySelector,
    string label = null)
  {
    var name = propertySelector.GetPropertyName();
    return cube.CreateTypedDimension<TProperty>(name, label);
  }

  /// <summary>
  /// Creates a typed dimension builder using an explicit key.
  /// </summary>
  public static DimensionBuilder<TProperty> CreatePropertyDimension<TProperty>(
    this AnalyticsCube cube,
    string key,
    string label = null)
  {
    return cube.CreateTypedDimension<TProperty>(key, label);
  }

  /// <summary>
  /// Creates and registers a composite dimension for the supplied entity properties.
  /// </summary>
  public static CompositeDimension<TEntity> CreateEntityDimension<TEntity>(
    this AnalyticsCube cube,
    string label = null,
    params Expression<Func<TEntity, object>>[] propertySelectors)
      where TEntity : new()
  {
    var propertyNames = propertySelectors
      .Select(selector => selector.GetPropertyName())
      .ToArray();

    return cube.AddCompositeDimension<TEntity>(
      typeof(TEntity).Name,
      label,
      propertyNames);
  }

  /// <summary>
  /// Creates and registers a typed date dimension with values across an inclusive time range.
  /// </summary>
  public static Dimension<DateTime> CreateTimeTypedDimension(this AnalyticsCube cube, DateTime start, DateTime end, TimeSpan interval, string key = "Time", string label = "Time Period")
  {
    var dimension = new Dimension<DateTime>(key, label);

    for (var date = start; date <= end; date += interval)
    {
      dimension.CreateValue(date);
    }

    cube.AddDimension(dimension);
    return dimension;
  }

  /// <summary>
  /// Populates a typed dimension from a sequence of entities.
  /// </summary>
  public static void PopulateDimension<TEntity, TProperty>(
    this Dimension<TProperty> dimension,
    IEnumerable<TEntity> entities,
    Func<TEntity, TProperty> valueSelector)
  {
    foreach (var entity in entities)
    {
      var value = valueSelector(entity);
      dimension.GetOrCreateValue(value);
    }
  }


  #endregion

  #region CompositeDimension

  /// <summary>
  /// Creates a composite dimension builder.
  /// </summary>
  public static CompositeDimensionBuilder CreateCompositeDimension(this AnalyticsCube cube, string key, string label = null, params string[] criteria)
  {
    return new CompositeDimensionBuilder(cube, key, label, criteria);
  }

  /// <summary>
  /// Creates a typed composite dimension builder.
  /// </summary>
  public static CompositeDimensionBuilder<TEntity> CreateCompositeDimension<TEntity>(this AnalyticsCube cube, string key = null, string label = null)
    where TEntity : new()
  {
    return new CompositeDimensionBuilder<TEntity>(cube, key, label);
  }

  /// <summary>
  /// Creates and registers a composite entity dimension using the selected properties.
  /// </summary>
  public static CompositeDimension<TEntity> CreateEntityDimension<TEntity>(
    this AnalyticsCube cube,
    params Expression<Func<TEntity, object>>[] propertySelectors)
      where TEntity : new()
  {
    var propertyNames = propertySelectors
      .Select(selector => selector.GetPropertyName())
      .ToArray();

    return cube.AddCompositeDimension<TEntity>(
      typeof(TEntity).Name,
      typeof(TEntity).Name,
      propertyNames);
  }

  /// <summary>
  /// Creates and registers an untyped composite dimension.
  /// </summary>
  public static CompositeDimension AddCompositeDimension(this AnalyticsCube cube, string key, string label = null, params string[] criteria)
  {
    var dimension = new CompositeDimension(criteria)
    {
      Key = key,
      Label = label ?? key
    };
    cube.AddDimension(dimension);
    return dimension;
  }

  /// <summary>
  /// Creates and registers a typed composite dimension.
  /// </summary>
  public static CompositeDimension<TEntity> AddCompositeDimension<TEntity>(this AnalyticsCube cube, string key, string label = null, params string[] propertyNames)
    where TEntity : new()
  {
    var dimension = new CompositeDimension<TEntity>(propertyNames)
    {
      Key = key,
      Label = label ?? key
    };
    cube.AddDimension(dimension);
    return dimension;
  }

  /// <summary>
  /// Populates a typed composite dimension from a sequence of entities.
  /// </summary>
  public static void PopulateDimension<TEntity>(
    this CompositeDimension<TEntity> dimension,
    IEnumerable<TEntity> entities)
      where TEntity : new()
  {
    foreach (var entity in entities)
    {
      dimension.CreateCompositeValue(entity);
    }
  }

  #endregion

  #region Get

  /// <summary>
  /// Finds the first dimension value associated with the supplied dimension and metric pair.
  /// </summary>
  public static DimensionValue GetDimensionValue(
    this AnalyticsCube cube,
    Dimension dimension,
    Metric metric)
  {
    ArgumentNullException.ThrowIfNull(cube);
    if (cube.FactGroups is null) throw new ArgumentNullException(nameof(cube), "FactGroups is null");
    ArgumentNullException.ThrowIfNull(dimension);
    ArgumentNullException.ThrowIfNull(metric);

    return cube.FactGroups.GetDimensionValue(dimension, metric);
  }

  /// <summary>
  /// Finds the first typed dimension value associated with the supplied dimension and metric pair.
  /// </summary>
  public static DimensionValue<T> GetDimensionValue<T>(
    this AnalyticsCube cube,
    Dimension<T> dimension,
    Metric<T> metric)
      where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    if (cube.FactGroups is null) throw new ArgumentNullException(nameof(cube), "FactGroups is null");
    ArgumentNullException.ThrowIfNull(dimension);
    ArgumentNullException.ThrowIfNull(metric);

    return cube.FactGroups.GetDimensionValue(dimension, metric);
  }

  #endregion
}
