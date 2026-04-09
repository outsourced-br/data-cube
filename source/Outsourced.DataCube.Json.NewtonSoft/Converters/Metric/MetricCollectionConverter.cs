namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using System;
using global::Newtonsoft.Json;
using Internal;
using Metrics;

public class MetricCollectionConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return typeof(IMetricCollection).IsAssignableFrom(objectType);
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
  {
    if (!objectType.IsGenericType || objectType.GetGenericTypeDefinition() != typeof(MetricCollection<>))
      throw new JsonException($"Expected {nameof(MetricCollection<int>)} type but got {objectType.Name}");

    var valueType = objectType.GetGenericArguments()[0];
    var converter = ClosedGenericJsonConverterCache.Get(typeof(MetricCollectionConverterT<>), valueType);

    return converter.ReadJson(reader, objectType, existingValue, serializer);
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    ArgumentNullException.ThrowIfNull(value);

    var objectType = value.GetType();
    if (!objectType.IsGenericType || objectType.GetGenericTypeDefinition() != typeof(MetricCollection<>))
      throw new JsonException($"Expected {nameof(MetricCollection<int>)} type but got {objectType.Name}");

    var valueType = objectType.GetGenericArguments()[0];
    var converter = ClosedGenericJsonConverterCache.Get(typeof(MetricCollectionConverterT<>), valueType);

    converter.WriteJson(writer, value, serializer);
  }
}
