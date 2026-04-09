namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using System;
using System.Reflection;
using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using Metrics;

public class CustomMetricCollectionConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return typeof(IMetricCollection).IsAssignableFrom(objectType) ||
           (objectType.IsGenericType &&
            objectType.GetGenericTypeDefinition() == typeof(MetricCollection<>));
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
  {
    if (!objectType.IsGenericType || objectType.GetGenericTypeDefinition() != typeof(MetricCollection<>))
      throw new JsonException($"Expected {nameof(MetricCollection<int>)} type but got {objectType.Name}");

    var valueType = objectType.GetGenericArguments()[0];
    var collection = Activator.CreateInstance(objectType) as IMetricCollection;

    if (reader.TokenType == JsonToken.StartObject)
    {
      var jObject = JObject.Load(reader);

      // Get the SetValue method via reflection
      var setValueMethod = objectType.GetMethod("SetValue", BindingFlags.Public | BindingFlags.Instance);

      foreach (var property in jObject.Properties())
      {
        var key = property.Name;
        var value = property.Value.ToObject(valueType, serializer);

        // Call SetValue via reflection
        setValueMethod?.Invoke(collection, new[] { key, value });
      }
    }

    return collection;
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    if (value == null)
    {
      writer.WriteNull();
      return;
    }

    writer.WriteStartObject();

    var objectType = value.GetType();

    if (value is IMetricCollection metricCollection)
    {
      // Get the GetValue method using reflection
      var getValueMethod = objectType.GetMethod("GetValue",
          BindingFlags.Public | BindingFlags.Instance,
          null,
          new[] { typeof(string) },
          null);

      // Get all metric keys in this collection
      var metricKeys = metricCollection.GetMetricKeys();

      foreach (var metricKey in metricKeys)
      {
        writer.WritePropertyName(metricKey);

        // Call GetValue via reflection
        var metricValue = getValueMethod?.Invoke(metricCollection, new[] { metricKey });
        serializer.Serialize(writer, metricValue);
      }
    }

    writer.WriteEndObject();
  }
}
