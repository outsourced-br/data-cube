namespace Outsourced.DataCube.Json.NewtonSoft;

using Outsourced.DataCube.Converters;
using System;
using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Serialization;
using DataCube;
using Converters;
using Internal;
using Metrics;
using System.Reflection;

/// <summary>
/// Resolves the specialized Json.NET converters required for DataCube model types.
/// </summary>
public class CubeContractResolver : DefaultContractResolver
{
  private static readonly JsonConverter DimensionConverterInstance = new DimensionConverter();
  private static readonly JsonConverter CompositeDimensionConverterInstance = new CompositeDimensionConverter();
  private static readonly JsonConverter DimensionValueConverterInstance = new DimensionValueConverter();
  private static readonly JsonConverter CompositeDimensionValueConverterInstance = new CompositeDimensionValueConverter();
  private static readonly JsonConverter MetricConverterInstance = new MetricConverter();
  private static readonly JsonConverter FactGroupConverterInstance = new FactGroupConverter();

  /// <inheritdoc />
  protected override JsonConverter ResolveContractConverter(Type objectType)
  {
    #region Dimension

    if (typeof(Dimension).IsAssignableFrom(objectType))
    {
      if (objectType == typeof(Dimension))
        return DimensionConverterInstance;

      if (objectType == typeof(CompositeDimension))
        return CompositeDimensionConverterInstance;

      if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Dimension<>))
      {
        var valueType = objectType.GetGenericArguments()[0];
        return ClosedGenericJsonConverterCache.Get(typeof(DimensionConverter<>), valueType);
      }

      if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(CompositeDimension<>))
      {
        var entityType = objectType.GetGenericArguments()[0];
        return ClosedGenericJsonConverterCache.Get(typeof(CompositeDimensionConverterT<>), entityType);
      }
    }

    #endregion

    #region DimensionValue

    // Handle dimension value types
    if (typeof(DimensionValue).IsAssignableFrom(objectType))
    {
      if (objectType == typeof(DimensionValue))
        return DimensionValueConverterInstance;

      if (objectType == typeof(CompositeDimensionValue))
        return CompositeDimensionValueConverterInstance;

      if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(DimensionValue<>))
      {
        var valueType = objectType.GetGenericArguments()[0];
        return ClosedGenericJsonConverterCache.Get(typeof(DimensionValueConverter<>), valueType);
      }

      if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(CompositeDimensionValue<>))
      {
        var valueType = objectType.GetGenericArguments()[0];
        return ClosedGenericJsonConverterCache.Get(typeof(CompositeDimensionValueConverter<>), valueType);
      }

      //if (objectType == typeof(DimensionCompositeValue))
      //  return new CustomCompositeDimensionValueConverter();
    }

    #endregion

    #region Metrics

    // Handle metric types
    if (typeof(Metric).IsAssignableFrom(objectType))
    {
      return MetricConverterInstance;
    }

    // Handle metric collections
    if (typeof(IMetricCollection).IsAssignableFrom(objectType))
    {
      if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(MetricCollection<>))
      {
        var valueType = objectType.GetGenericArguments()[0];
        return ClosedGenericJsonConverterCache.Get(typeof(MetricCollectionConverterT<>), valueType);
      }
    }

    #endregion

    // Handle fact group
    if (objectType == typeof(FactGroup))
      return FactGroupConverterInstance;

    return null;
  }

  /// <inheritdoc />
  protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
  {
    var property = base.CreateProperty(member, memberSerialization);

    // if (member.DeclaringType == typeof(AnalyticsCube) && string.Equals(member.Name, nameof(AnalyticsCube.Dimensions), StringComparison.OrdinalIgnoreCase))
    //   property.TypeNameHandling = TypeNameHandling.All;
    //
    // if (member.DeclaringType == typeof(FactGroup) && string.Equals(member.Name, nameof(FactGroup.DimensionValues), StringComparison.OrdinalIgnoreCase))
    //   property.TypeNameHandling = TypeNameHandling.All;
    //
    // if (member.DeclaringType == typeof(FactGroup) && string.Equals(member.Name, nameof(FactGroup.MetricCollections), StringComparison.OrdinalIgnoreCase))
    //   property.TypeNameHandling = TypeNameHandling.All;

    if (typeof(Dimension).IsAssignableFrom(member.DeclaringType) && string.Equals(property.PropertyName, "Values", StringComparison.OrdinalIgnoreCase))
      property.ShouldSerialize = instance => false;

    return property;
  }
}
