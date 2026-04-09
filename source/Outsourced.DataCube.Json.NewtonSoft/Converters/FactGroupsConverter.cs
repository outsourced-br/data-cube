namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using System;
using System.Collections.Generic;
using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using DataCube;
using Internal;
using Metrics;

public class FactGroupsConverter : JsonConverter<IList<FactGroup>>
{
  public override IList<FactGroup> ReadJson(JsonReader reader, Type objectType, IList<FactGroup> existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    if (reader.TokenType != JsonToken.StartArray)
      throw new JsonException("Expected StartArray token.");

    var factGroups = new List<FactGroup>();
    var jsonArray = JArray.Load(reader);

    foreach (var item in jsonArray)
    {
      if (item is JObject factGroupObj)
      {
        var factGroup = new FactGroup();

        if (factGroupObj.TryGetValue("DimensionValues", out var dimensionValues) && dimensionValues is JObject dimensionValuesObj)
        {
          foreach (var property in dimensionValuesObj.Properties())
          {
            factGroup.DimensionValues[property.Name] = property.Value.ToObject<DimensionValue>(serializer);
          }
        }

        if (factGroupObj.TryGetValue("MetricValues", out var metricValues) && metricValues is JObject metricValuesObj)
        {
          foreach (var metricTypeProperty in metricValuesObj.Properties())
          {
            if (!Enum.TryParse(metricTypeProperty.Name, true, out MetricType metricType))
              continue;

            var accessor = MetricCollectionAccessorCache.Get(metricType);
            var collection = accessor.CreateCollection();

            if (metricTypeProperty.Value is JObject metricsObj)
            {
              foreach (var metricProperty in metricsObj.Properties())
              {
                var metricKey = metricProperty.Name;
                var metricValue = metricProperty.Value.ToObject(accessor.ValueType, serializer);
                accessor.SetValue(collection, metricKey, metricValue);
              }
            }

            factGroup.MetricCollections[metricType] = collection;
          }
        }

        factGroups.Add(factGroup);
      }
    }

    return factGroups;
  }

  public override void WriteJson(JsonWriter writer, IList<FactGroup> value, JsonSerializer serializer)
  {
    writer.WriteStartArray();

    foreach (var factGroup in value)
    {
      writer.WriteStartObject();

      writer.WritePropertyName("DimensionValues");
      serializer.Serialize(writer, factGroup.DimensionValues);

      writer.WritePropertyName("MetricValues");
      writer.WriteStartObject();

      foreach (var collection in factGroup.MetricCollections)
      {
        writer.WritePropertyName(collection.Key.ToString());
        writer.WriteStartObject();

        var metricType = collection.Key;
        var metricCollection = collection.Value;
        var accessor = MetricCollectionAccessorCache.Get(metricType);
        var metricKeys = metricCollection.GetMetricKeys();

        foreach (var metricKey in metricKeys)
        {
          writer.WritePropertyName(metricKey);
          var metricValue = accessor.GetValue(metricCollection, metricKey);
          serializer.Serialize(writer, metricValue);
        }

        writer.WriteEndObject();
      }

      writer.WriteEndObject();
      writer.WriteEndObject();
    }

    writer.WriteEndArray();
  }
}
