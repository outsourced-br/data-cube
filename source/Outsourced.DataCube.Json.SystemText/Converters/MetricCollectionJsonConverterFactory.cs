namespace Outsourced.DataCube.Json.SystemText.Converters;

using Metrics;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;

/// <summary>
/// Creates converters for <see cref="IMetricCollection"/> and <see cref="MetricCollection{T}"/> types.
/// </summary>
public sealed class MetricCollectionJsonConverterFactory : JsonConverterFactory
{
  /// <inheritdoc />
  public override bool CanConvert(Type typeToConvert)
  {
    return typeof(IMetricCollection).IsAssignableFrom(typeToConvert);
  }

  /// <inheritdoc />
  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(MetricCollection<>))
    {
      var valueType = typeToConvert.GetGenericArguments()[0];
      var converterType = typeof(MetricCollectionJsonConverter<>).MakeGenericType(valueType);
      return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    return new MetricCollectionInterfaceJsonConverter();
  }

  private sealed class MetricCollectionInterfaceJsonConverter : JsonConverter<IMetricCollection>
  {
    public override IMetricCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      throw new JsonException("IMetricCollection values are deserialized through FactGroupConverter with metric type context.");
    }

    public override void Write(Utf8JsonWriter writer, IMetricCollection value, JsonSerializerOptions options)
    {
      JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
  }

  private sealed class MetricCollectionJsonConverter<T> : JsonConverter<MetricCollection<T>>
    where T : struct
  {
    public override MetricCollection<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartObject)
        throw new JsonException("Expected a metric collection JSON object.");

      var collection = new MetricCollection<T>();

      while (reader.Read())
      {
        if (reader.TokenType == JsonTokenType.EndObject)
          return collection;

        if (reader.TokenType != JsonTokenType.PropertyName)
          throw new JsonException("Expected a metric key while reading a metric collection.");

        var key = reader.GetString() ?? string.Empty;
        if (!reader.Read())
          throw new JsonException("Unexpected end of JSON while reading a metric collection value.");

        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        collection.SetValue(key, value);
      }

      throw new JsonException("Unexpected end of JSON while reading a metric collection.");
    }

    public override void Write(Utf8JsonWriter writer, MetricCollection<T> value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();

      foreach (var key in value.GetMetricKeys())
      {
        writer.WritePropertyName(key);
        JsonSerializer.Serialize(writer, value.GetValue(key), options);
      }

      writer.WriteEndObject();
    }
  }
}
