namespace Outsourced.DataCube.Converters;

using Metrics;
using Newtonsoft.Json;
using System;
using Json.NewtonSoft.Internal;

public class FactGroupConverter : JsonConverter<FactGroup>
{
  public override FactGroup ReadJson(JsonReader reader, Type objectType, FactGroup existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    if (reader.TokenType == JsonToken.Null) return null;

    var factGroup = new FactGroup();

    if (reader.TokenType != JsonToken.StartObject)
    {
      reader.Read(); // Advance if not already at start
    }

    while (reader.Read() && reader.TokenType != JsonToken.EndObject)
    {
      if (reader.TokenType == JsonToken.PropertyName)
      {
        var propertyName = reader.Value as string;
        if (propertyName != null) propertyName = string.Intern(propertyName);

        // Map "D" and legacy "DimensionValues" to dimension loading
        if (string.Equals(propertyName, "D", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(propertyName, nameof(FactGroup.DimensionValues), StringComparison.OrdinalIgnoreCase))
        {
          reader.Read(); // move to StartObject
          if (reader.TokenType == JsonToken.StartObject)
          {
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
              if (reader.TokenType == JsonToken.PropertyName)
              {
                var dimKey = reader.Value as string;
                if (dimKey != null) dimKey = string.Intern(dimKey);

                reader.Read(); // move to value
                var dimValue = serializer.Deserialize<DimensionValue>(reader);
                factGroup.DimensionValues[dimKey] = dimValue;
              }
            }
          }
          else
          {
            serializer.Deserialize(reader); // consume whatever it is to skip
          }
        }
        // Map "M" and legacy "MetricCollections" to metric loading
        else if (string.Equals(propertyName, "M", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(propertyName, nameof(FactGroup.MetricCollections), StringComparison.OrdinalIgnoreCase))
        {
          reader.Read(); // move to StartObject
          if (reader.TokenType == JsonToken.StartObject)
          {
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
              if (reader.TokenType == JsonToken.PropertyName)
              {
                var metricTypeStr = reader.Value as string;
                reader.Read(); // move to StartObject for metric collections

                if (Enum.TryParse<MetricType>(metricTypeStr, true, out var metricType))
                {
                  var accessor = MetricCollectionAccessorCache.Get(metricType);
                  var collection = accessor.CreateCollection();

                  if (reader.TokenType == JsonToken.StartObject)
                  {
                    while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                    {
                      if (reader.TokenType == JsonToken.PropertyName)
                      {
                        var metricKey = reader.Value as string;
                        if (metricKey != null) metricKey = string.Intern(metricKey);

                        reader.Read(); // move to value
                        var metricValue = serializer.Deserialize(reader, accessor.ValueType);
                        accessor.SetValue(collection, metricKey, metricValue);
                      }
                    }
                  }
                  else
                  {
                    serializer.Deserialize(reader);
                  }

                  factGroup.MetricCollections[metricType] = collection;
                }
                else
                {
                  serializer.Deserialize(reader); // skip unknown metric type object
                }
              }
            }
          }
          else
          {
            serializer.Deserialize(reader);
          }
        }
        else
        {
          reader.Read(); // move to value
          serializer.Deserialize(reader); // skip unknown property
        }
      }
    }

    return factGroup;
  }

  public override void WriteJson(JsonWriter writer, FactGroup value, JsonSerializer serializer)
  {
    writer.WriteStartObject();

    // Write dimension values very compactly
    writer.WritePropertyName("D");
    serializer.Serialize(writer, value.DimensionValues);

    // Write metric collections very compactly
    writer.WritePropertyName("M");
    writer.WriteStartObject();

    foreach (var collection in value.MetricCollections)
    {
      writer.WritePropertyName(collection.Key.ToString());
      serializer.Serialize(writer, collection.Value);
    }

    writer.WriteEndObject(); // End metric collections

    writer.WriteEndObject(); // End fact group
  }
}
