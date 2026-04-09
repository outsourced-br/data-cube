namespace Outsourced.DataCube.Json.SystemText.Converters;

using DataCube;
using Internal;
using Metrics;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;
using System;

/// <summary>
/// Serializes <see cref="FactGroup"/> instances using the compact DataCube JSON representation.
/// </summary>
public sealed class FactGroupJsonConverter : JsonConverter<FactGroup>
{
  /// <inheritdoc />
  public override FactGroup Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType == JsonTokenType.Null)
      return null;

    if (reader.TokenType != JsonTokenType.StartObject)
      throw new JsonException("Expected StartObject for FactGroup.");

    var factGroup = new FactGroup();

    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
    {
      if (reader.TokenType == JsonTokenType.PropertyName)
      {
        var propertyName = reader.GetString();
        if (propertyName != null) propertyName = string.Intern(propertyName);

        // Map "D" and legacy names
        if (string.Equals(propertyName, "D", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(propertyName, nameof(FactGroup.DimensionValues), StringComparison.OrdinalIgnoreCase))
        {
          reader.Read(); // move to StartObject
          if (reader.TokenType == JsonTokenType.StartObject)
          {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
              if (reader.TokenType == JsonTokenType.PropertyName)
              {
                var dimKey = reader.GetString();
                if (dimKey != null) dimKey = string.Intern(dimKey);

                reader.Read(); // move to value
                var dimValue = JsonSerializer.Deserialize<DimensionValue>(ref reader, options);
                factGroup.DimensionValues[dimKey] = dimValue;
              }
            }
          }
          else
          {
            reader.Skip();
          }
        }
        // Map "M" and legacy names
        else if (string.Equals(propertyName, "M", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(propertyName, nameof(FactGroup.MetricCollections), StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(propertyName, "MetricValues", StringComparison.OrdinalIgnoreCase))
        {
          reader.Read(); // move to StartObject
          if (reader.TokenType == JsonTokenType.StartObject)
          {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
              if (reader.TokenType == JsonTokenType.PropertyName)
              {
                var metricTypeStr = reader.GetString();
                reader.Read(); // move to StartObject for metric collections

                if (Enum.TryParse<MetricType>(metricTypeStr, true, out var metricType))
                {
                  var accessor = MetricCollectionAccessorCache.Get(metricType);
                  var collection = accessor.CreateCollection();

                  if (reader.TokenType == JsonTokenType.StartObject)
                  {
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                      if (reader.TokenType == JsonTokenType.PropertyName)
                      {
                        var metricKey = reader.GetString();
                        if (metricKey != null) metricKey = string.Intern(metricKey);

                        reader.Read(); // move to value
                        var metricValue = JsonSerializer.Deserialize(ref reader, accessor.ValueType, options);
                        accessor.SetValue(collection, metricKey, metricValue);
                      }
                    }
                  }
                  else
                  {
                    reader.Skip();
                  }

                  factGroup.MetricCollections[metricType] = collection;
                }
                else
                {
                  reader.Skip();
                }
              }
            }
          }
          else
          {
            reader.Skip();
          }
        }
        else
        {
          reader.Read(); // move to value
          reader.Skip(); // skip unknown property
        }
      }
    }

    return factGroup;
  }

  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, FactGroup value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();

    writer.WritePropertyName("D");
    writer.WriteStartObject();
    foreach (var entry in value.DimensionValues)
    {
      writer.WritePropertyName(entry.Key);
      JsonSerializer.Serialize(writer, entry.Value, options);
    }
    writer.WriteEndObject();

    writer.WritePropertyName("M");
    writer.WriteStartObject();
    foreach (var entry in value.MetricCollections)
    {
      writer.WritePropertyName(entry.Key.ToString());
      JsonSerializer.Serialize(writer, entry.Value, entry.Value.GetType(), options);
    }
    writer.WriteEndObject();

    writer.WriteEndObject();
  }
}
