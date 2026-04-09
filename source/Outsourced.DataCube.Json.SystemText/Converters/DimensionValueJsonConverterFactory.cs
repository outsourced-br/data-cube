namespace Outsourced.DataCube.Json.SystemText.Converters;

using DataCube;
using Internal;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;

/// <summary>
/// Creates converters for <see cref="DimensionValue"/> and <see cref="CompositeDimensionValue"/> model types.
/// </summary>
public sealed class DimensionValueJsonConverterFactory : JsonConverterFactory
{
  /// <inheritdoc />
  public override bool CanConvert(Type typeToConvert)
  {
    return typeof(DimensionValue).IsAssignableFrom(typeToConvert);
  }

  /// <inheritdoc />
  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    if (typeToConvert == typeof(DimensionValue))
      return new DimensionValueJsonConverter();

    if (typeToConvert == typeof(CompositeDimensionValue))
      return new CompositeDimensionValueJsonConverter();

    if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(DimensionValue<>))
    {
      var valueType = typeToConvert.GetGenericArguments()[0];
      var converterType = typeof(TypedDimensionValueJsonConverter<>).MakeGenericType(valueType);
      return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(CompositeDimensionValue<>))
    {
      var entityType = typeToConvert.GetGenericArguments()[0];
      var converterType = typeof(TypedCompositeDimensionValueJsonConverter<>).MakeGenericType(entityType);
      return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    throw new NotSupportedException($"Unsupported dimension value type {typeToConvert.FullName}.");
  }

  private sealed class DimensionValueJsonConverter : JsonConverter<DimensionValue>
  {
    public override DimensionValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      var root = document.RootElement;

      if (root.TryGetProperty("$type", out var typeElement))
      {
        var runtimeType = TypeResolutionCache.Resolve(typeElement.GetString());
        if (runtimeType != null && runtimeType != typeof(DimensionValue))
          return (DimensionValue)JsonSerializer.Deserialize(root.GetRawText(), runtimeType, options)!;
      }

      var key = GetRequiredString(root, nameof(DimensionValue.Key));
      var label = GetOptionalString(root, nameof(DimensionValue.Label));
      var value = root.TryGetProperty(nameof(DimensionValue.Value), out var valueElement)
        ? JsonSerializer.Deserialize<object>(valueElement.GetRawText(), options)
        : null;

      return new DimensionValue(key, label, value);
    }

    public override void Write(Utf8JsonWriter writer, DimensionValue value, JsonSerializerOptions options)
    {
      if (value.GetType() != typeof(DimensionValue))
      {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
        return;
      }

      writer.WriteStartObject();
      writer.WriteString("$type", TypeResolutionCache.GetTypeName(value.GetType()));
      writer.WriteString(nameof(DimensionValue.Key), value.Key);

      if (!string.IsNullOrEmpty(value.Label))
        writer.WriteString(nameof(DimensionValue.Label), value.Label);

      writer.WritePropertyName(nameof(DimensionValue.Value));
      ObjectValueJsonConverter.WriteValue(writer, value.Value, options);
      writer.WriteEndObject();
    }
  }

  private sealed class TypedDimensionValueJsonConverter<T> : JsonConverter<DimensionValue<T>>
  {
    public override DimensionValue<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      var root = document.RootElement;

      var key = GetRequiredString(root, nameof(DimensionValue.Key));
      var label = GetOptionalString(root, nameof(DimensionValue.Label));
      var value = root.GetProperty(nameof(DimensionValue.Value)).Deserialize<T>(options);

      return new DimensionValue<T>(key, label, value);
    }

    public override void Write(Utf8JsonWriter writer, DimensionValue<T> value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();
      writer.WriteString("$type", TypeResolutionCache.GetTypeName(value.GetType()));
      writer.WriteString(nameof(DimensionValue.Key), value.Key);

      if (!string.IsNullOrEmpty(value.Label))
        writer.WriteString(nameof(DimensionValue.Label), value.Label);

      writer.WritePropertyName(nameof(DimensionValue.Value));
      JsonSerializer.Serialize(writer, value.Value, options);
      writer.WriteEndObject();
    }
  }

  private sealed class CompositeDimensionValueJsonConverter : JsonConverter<CompositeDimensionValue>
  {
    public override CompositeDimensionValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      var root = document.RootElement;

      var key = GetRequiredString(root, nameof(DimensionValue.Key));
      var label = GetOptionalString(root, nameof(DimensionValue.Label));
      var values = root.GetProperty(nameof(DimensionValue.Value)).Deserialize<Dictionary<string, object>>(options)
        ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

      return new CompositeDimensionValue(key, label, values);
    }

    public override void Write(Utf8JsonWriter writer, CompositeDimensionValue value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();
      writer.WriteString("$type", TypeResolutionCache.GetTypeName(value.GetType()));
      writer.WriteString(nameof(DimensionValue.Key), value.Key);

      if (!string.IsNullOrEmpty(value.Label))
        writer.WriteString(nameof(DimensionValue.Label), value.Label);

      writer.WritePropertyName(nameof(DimensionValue.Value));
      ObjectValueJsonConverter.WriteValue(writer, value.Value, options);
      writer.WriteEndObject();
    }
  }

  private sealed class TypedCompositeDimensionValueJsonConverter<TEntity> : JsonConverter<CompositeDimensionValue<TEntity>>
    where TEntity : new()
  {
    public override CompositeDimensionValue<TEntity> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      var root = document.RootElement;

      var key = GetRequiredString(root, nameof(DimensionValue.Key));
      var label = GetOptionalString(root, nameof(DimensionValue.Label));
      var values = root.GetProperty(nameof(DimensionValue.Value)).Deserialize<Dictionary<string, object>>(options)
        ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

      return new CompositeDimensionValue<TEntity>(key, label, values, default);
    }

    public override void Write(Utf8JsonWriter writer, CompositeDimensionValue<TEntity> value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();
      writer.WriteString("$type", TypeResolutionCache.GetTypeName(value.GetType()));
      writer.WriteString(nameof(DimensionValue.Key), value.Key);

      if (!string.IsNullOrEmpty(value.Label))
        writer.WriteString(nameof(DimensionValue.Label), value.Label);

      writer.WritePropertyName(nameof(DimensionValue.Value));
      ObjectValueJsonConverter.WriteValue(writer, value.Value, options);
      writer.WriteEndObject();
    }
  }

  private static string GetRequiredString(JsonElement root, string propertyName)
  {
    if (!root.TryGetProperty(propertyName, out var property) || string.IsNullOrEmpty(property.GetString()))
      throw new JsonException($"DimensionValue must have a {propertyName}.");

    return property.GetString()!;
  }

  private static string GetOptionalString(JsonElement root, string propertyName)
  {
    return root.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
  }
}
