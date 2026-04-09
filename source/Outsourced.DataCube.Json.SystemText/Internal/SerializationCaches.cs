namespace Outsourced.DataCube.Json.SystemText.Internal;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using DataCube;
using Metrics;

internal static class TypeResolutionCache
{
  private static readonly ConcurrentDictionary<string, Type> Cache = new(StringComparer.Ordinal);

  internal static string GetTypeName(Type type)
  {
    return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
  }

  internal static Type Resolve(string typeName)
  {
    if (string.IsNullOrWhiteSpace(typeName))
      return null;

    if (Cache.TryGetValue(typeName, out var cachedType))
      return cachedType;

    var resolvedType = Type.GetType(typeName, throwOnError: false);
    if (resolvedType == null)
    {
      resolvedType = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(static assembly =>
        {
          try
          {
            return assembly.GetTypes();
          }
          catch (ReflectionTypeLoadException exception)
          {
            return exception.Types.Where(static type => type != null)!;
          }
        })
        .FirstOrDefault(type =>
          string.Equals(type.AssemblyQualifiedName, typeName, StringComparison.Ordinal) ||
          string.Equals(type.FullName, typeName, StringComparison.Ordinal) ||
          string.Equals(type.Name, typeName, StringComparison.Ordinal));
    }

    if (resolvedType != null)
      Cache.TryAdd(typeName, resolvedType);

    return resolvedType;
  }
}

internal static class MetricCollectionAccessorCache
{
  private static readonly ConcurrentDictionary<MetricType, Accessor> Cache = new();

  internal static Accessor Get(MetricType metricType)
  {
    return Cache.GetOrAdd(metricType, CreateAccessor);
  }

  internal sealed class Accessor(
    Type valueType,
    Func<IMetricCollection> createCollection,
    Action<IMetricCollection, string, object> setValue,
    Func<IMetricCollection, string, object> getValue)
  {
    public Type ValueType { get; } = valueType;
    public Func<IMetricCollection> CreateCollection { get; } = createCollection;
    public Action<IMetricCollection, string, object> SetValue { get; } = setValue;
    public Func<IMetricCollection, string, object> GetValue { get; } = getValue;
  }

  private static Accessor CreateAccessor(MetricType metricType)
  {
    var valueType = MetricTypeHelper.GetTypeFromMetricType(metricType);
    var collectionType = typeof(MetricCollection<>).MakeGenericType(valueType);

    var createCollection = Expression
      .Lambda<Func<IMetricCollection>>(Expression.Convert(Expression.New(collectionType), typeof(IMetricCollection)))
      .Compile();

    var collectionParameter = Expression.Parameter(typeof(IMetricCollection), "collection");
    var keyParameter = Expression.Parameter(typeof(string), "key");
    var valueParameter = Expression.Parameter(typeof(object), "value");

    var setValueMethod = collectionType.GetMethod(nameof(MetricCollection<int>.SetValue))!;
    var setValueExpression = Expression.Call(
      Expression.Convert(collectionParameter, collectionType),
      setValueMethod,
      keyParameter,
      Expression.Convert(valueParameter, valueType));

    var setValue = Expression
      .Lambda<Action<IMetricCollection, string, object>>(setValueExpression, collectionParameter, keyParameter, valueParameter)
      .Compile();

    var getValueMethod = collectionType.GetMethod(nameof(MetricCollection<int>.GetValue))!;
    var getValueExpression = Expression.Convert(
      Expression.Call(Expression.Convert(collectionParameter, collectionType), getValueMethod, keyParameter),
      typeof(object));

    var getValue = Expression
      .Lambda<Func<IMetricCollection, string, object>>(getValueExpression, collectionParameter, keyParameter)
      .Compile();

    return new Accessor(valueType, createCollection, setValue, getValue);
  }
}

internal static class MetricFactory
{
  internal static Metric CreateMetric(
    Type runtimeType,
    string key,
    string label,
    MetricType metricType,
    AggregationType aggregationType,
    string format,
    string unit,
    string businessKeyDimensionKey = null)
  {
    Metric result = null;

    if (runtimeType != null)
    {
      if (runtimeType == typeof(CountMetric)) result = new CountMetric(key, label);
      else if (runtimeType == typeof(PercentageMetric)) result = new PercentageMetric(key, label);
      else if (runtimeType == typeof(AverageMetric)) result = new AverageMetric(key, label);
      else if (runtimeType == typeof(CurrencyMetric)) result = new CurrencyMetric(key, label, unit);
      else if (runtimeType == typeof(UniqueMetric)) result = new UniqueMetric(key, label);
      else if (runtimeType == typeof(DistinctCountMetric)) result = new DistinctCountMetric(key, businessKeyDimensionKey, label);
      else if (runtimeType == typeof(DuplicateMetric)) result = new DuplicateMetric(key, label);
      else if (runtimeType == typeof(MissingMetric)) result = new MissingMetric(key, label);
      else if (runtimeType.IsGenericType && string.Equals(runtimeType.GetGenericTypeDefinition().Name, "CustomMetric`1", StringComparison.Ordinal))
      {
        result = CreateCustomMetric(runtimeType, key, label, metricType, aggregationType, format, unit);
      }
    }

    if (result == null)
      result = aggregationType == AggregationType.DistinctCount
        ? new DistinctCountMetric(key, businessKeyDimensionKey, label)
        : new Metric(key, metricType, aggregationType, label, format, unit);
    else
    {
      if (!string.IsNullOrEmpty(format))
        result.Format = format;

      if (!string.IsNullOrEmpty(unit))
        result.Unit = unit;
    }

    return result;
  }

  private static Metric CreateCustomMetric(
    Type runtimeType,
    string key,
    string label,
    MetricType metricType,
    AggregationType aggregationType,
    string format,
    string unit)
  {
    var constructor = runtimeType.GetConstructor(
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
      binder: null,
      types:
      [
        typeof(string),
        typeof(MetricType),
        typeof(AggregationType),
        typeof(string),
        typeof(string),
        typeof(string)
      ],
      modifiers: null);

    if (constructor == null)
      return null;

    var result = (Metric)constructor.Invoke([key, metricType, aggregationType, label, format, unit]);
    var aggregateFuncProperty = runtimeType.GetProperty("AggregateFunc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (aggregateFuncProperty != null)
    {
      var genericType = runtimeType.GetGenericArguments()[0];
      var defaultAggregateFunc = GetDefaultAggregateFunc(genericType, aggregationType);
      if (defaultAggregateFunc != null)
        aggregateFuncProperty.SetValue(result, defaultAggregateFunc);
    }

    return result;
  }

  private static object GetDefaultAggregateFunc(Type type, AggregationType aggregationType)
  {
    if (type == typeof(decimal))
    {
      if (aggregationType is AggregationType.Currency or AggregationType.Count or AggregationType.Sum)
        return (Func<IEnumerable<decimal>, decimal>)(values => values.Sum());

      if (aggregationType == AggregationType.Average)
        return (Func<IEnumerable<decimal>, decimal>)(values => values.Any() ? values.Average() : 0m);
    }
    else if (type == typeof(double))
    {
      if (aggregationType is AggregationType.Average or AggregationType.Percentage)
        return (Func<IEnumerable<double>, double>)(values => values.Any() ? values.Average() : 0d);

      if (aggregationType is AggregationType.Sum or AggregationType.Count)
        return (Func<IEnumerable<double>, double>)(values => values.Sum());
    }
    else if (type == typeof(int))
    {
      return (Func<IEnumerable<int>, int>)(values => values.Sum());
    }

    return null;
  }
}
