namespace Outsourced.DataCube.Json.NewtonSoft.Internal;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using global::Newtonsoft.Json;
using Metrics;

internal static class ClosedGenericJsonConverterCache
{
  private static readonly ConcurrentDictionary<(Type OpenGenericType, Type TypeArgument), JsonConverter> Cache = new();

  internal static JsonConverter Get(Type openGenericType, Type typeArgument)
  {
    return Cache.GetOrAdd((openGenericType, typeArgument), static key =>
    {
      var closedType = key.OpenGenericType.MakeGenericType(key.TypeArgument);
      return (JsonConverter)Activator.CreateInstance(closedType);
    });
  }
}

internal static class TypeResolutionCache
{
  private static readonly ConcurrentDictionary<string, Type> Cache = new(StringComparer.Ordinal);

  internal static string GetTypeName(Type type)
  {
    return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
  }

  internal static Type Resolve(string typeName, Type expectedBaseType)
  {
    var resolvedType = Resolve(typeName);
    if (resolvedType == null)
      return null;

    return expectedBaseType == null || expectedBaseType.IsAssignableFrom(resolvedType)
      ? resolvedType
      : null;
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

    var setValueMethod = collectionType.GetMethod(nameof(MetricCollection<int>.SetValue));
    var setValueExpression = Expression.Call(
      Expression.Convert(collectionParameter, collectionType),
      setValueMethod,
      keyParameter,
      Expression.Convert(valueParameter, valueType));

    var setValue = Expression
      .Lambda<Action<IMetricCollection, string, object>>(setValueExpression, collectionParameter, keyParameter, valueParameter)
      .Compile();

    var getValueMethod = collectionType.GetMethod(nameof(MetricCollection<int>.GetValue));
    var getValueExpression = Expression.Convert(
      Expression.Call(Expression.Convert(collectionParameter, collectionType), getValueMethod, keyParameter),
      typeof(object));

    var getValue = Expression
      .Lambda<Func<IMetricCollection, string, object>>(getValueExpression, collectionParameter, keyParameter)
      .Compile();

    return new Accessor(valueType, createCollection, setValue, getValue);
  }
}
