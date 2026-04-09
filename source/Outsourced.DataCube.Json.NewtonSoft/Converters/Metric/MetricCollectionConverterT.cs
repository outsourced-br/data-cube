namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using System;
using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using Metrics;

public class MetricCollectionConverterT<T> : JsonConverter<MetricCollection<T>> where T : struct
{
  public override MetricCollection<T> ReadJson(JsonReader reader, Type objectType, MetricCollection<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var collection = new MetricCollection<T>();

    if (reader.TokenType == JsonToken.StartObject)
    {
      var jObject = JObject.Load(reader);

      foreach (var property in jObject.Properties())
      {
        var key = property.Name;
        var value = property.Value.ToObject<T>(serializer);
        collection.SetValue(key, value);
      }
    }

    return collection;
  }

  public override void WriteJson(JsonWriter writer, MetricCollection<T> value, JsonSerializer serializer)
  {
    writer.WriteStartObject();

    foreach (var key in value.GetMetricKeys())
    {
      writer.WritePropertyName(key);
      serializer.Serialize(writer, value.GetValue(key));
    }

    writer.WriteEndObject();
  }
}
