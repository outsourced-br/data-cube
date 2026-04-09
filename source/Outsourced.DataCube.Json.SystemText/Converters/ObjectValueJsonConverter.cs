namespace Outsourced.DataCube.Json.SystemText.Converters;

using System.Buffers;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;

/// <summary>
/// Reads and writes loosely typed object values used inside composite dimension payloads.
/// </summary>
public sealed class ObjectValueJsonConverter : JsonConverter<object>
{
  /// <inheritdoc />
  public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    return ReadInferredValue(ref reader, options);
  }

  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
  {
    WriteValue(writer, value, options);
  }

  internal static object ReadInferredValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
  {
    switch (reader.TokenType)
    {
      case JsonTokenType.StartObject:
        return ReadObject(ref reader, options);
      case JsonTokenType.StartArray:
        return ReadArray(ref reader, options);
      case JsonTokenType.String:
        return reader.GetString();
      case JsonTokenType.Number:
        return ReadNumber(ref reader);
      case JsonTokenType.True:
        return true;
      case JsonTokenType.False:
        return false;
      case JsonTokenType.Null:
        return null;
      default:
        throw new JsonException($"Unsupported token {reader.TokenType} for inferred object deserialization.");
    }
  }

  internal static Dictionary<string, object> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
  {
    var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    while (reader.Read())
    {
      if (reader.TokenType == JsonTokenType.EndObject)
        return values;

      if (reader.TokenType != JsonTokenType.PropertyName)
        throw new JsonException("Expected a property name while reading an inferred object.");

      var propertyName = reader.GetString() ?? string.Empty;
      if (!reader.Read())
        throw new JsonException("Unexpected end of JSON while reading an inferred object value.");

      values[propertyName] = ReadInferredValue(ref reader, options);
    }

    throw new JsonException("Unexpected end of JSON while reading an inferred object.");
  }

  internal static List<object> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
  {
    var values = new List<object>();

    while (reader.Read())
    {
      if (reader.TokenType == JsonTokenType.EndArray)
        return values;

      values.Add(ReadInferredValue(ref reader, options));
    }

    throw new JsonException("Unexpected end of JSON while reading an inferred array.");
  }

  internal static void WriteValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
  {
    switch (value)
    {
      case null:
        writer.WriteNullValue();
        return;
      case JsonElement element:
        element.WriteTo(writer);
        return;
      case string stringValue:
        writer.WriteStringValue(stringValue);
        return;
      case bool boolValue:
        writer.WriteBooleanValue(boolValue);
        return;
      case byte byteValue:
        writer.WriteNumberValue(byteValue);
        return;
      case sbyte sbyteValue:
        writer.WriteNumberValue(sbyteValue);
        return;
      case short shortValue:
        writer.WriteNumberValue(shortValue);
        return;
      case ushort ushortValue:
        writer.WriteNumberValue(ushortValue);
        return;
      case int intValue:
        writer.WriteNumberValue(intValue);
        return;
      case uint uintValue:
        writer.WriteNumberValue(uintValue);
        return;
      case long longValue:
        writer.WriteNumberValue(longValue);
        return;
      case ulong ulongValue:
        writer.WriteNumberValue(ulongValue);
        return;
      case float floatValue:
        writer.WriteNumberValue(floatValue);
        return;
      case double doubleValue:
        writer.WriteNumberValue(doubleValue);
        return;
      case decimal decimalValue:
        writer.WriteNumberValue(decimalValue);
        return;
      case IDictionary<string, object> dictionary:
        writer.WriteStartObject();
        foreach (var entry in dictionary)
        {
          writer.WritePropertyName(entry.Key);
          WriteValue(writer, entry.Value, options);
        }
        writer.WriteEndObject();
        return;
      case IEnumerable<object> sequence:
        writer.WriteStartArray();
        foreach (var item in sequence)
        {
          WriteValue(writer, item, options);
        }
        writer.WriteEndArray();
        return;
      default:
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
        return;
    }
  }

  private static object ReadNumber(ref Utf8JsonReader reader)
  {
    ReadOnlySpan<byte> span = reader.HasValueSequence
      ? reader.ValueSequence.ToArray()
      : reader.ValueSpan;

    var looksFloatingPoint = span.Contains((byte)'.') || span.Contains((byte)'e') || span.Contains((byte)'E');

    if (!looksFloatingPoint)
    {
      if (reader.TryGetInt32(out var intValue))
        return intValue;

      if (reader.TryGetInt64(out var longValue))
        return longValue;
    }

    if (reader.TryGetDecimal(out var decimalValue))
      return decimalValue;

    if (reader.TryGetDouble(out var doubleValue))
      return doubleValue;

    throw new JsonException("Unable to infer a CLR numeric type from the JSON number token.");
  }
}
