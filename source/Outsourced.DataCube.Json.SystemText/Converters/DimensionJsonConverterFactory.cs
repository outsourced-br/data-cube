namespace Outsourced.DataCube.Json.SystemText.Converters;

using DataCube;
using Internal;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;

/// <summary>
/// Creates converters for <see cref="Dimension"/> and <see cref="CompositeDimension"/> model types.
/// </summary>
public sealed class DimensionJsonConverterFactory : JsonConverterFactory
{
  /// <inheritdoc />
  public override bool CanConvert(Type typeToConvert)
  {
    return typeof(Dimension).IsAssignableFrom(typeToConvert);
  }

  /// <inheritdoc />
  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    if (typeToConvert == typeof(Dimension))
      return new DimensionJsonConverter();

    if (typeToConvert == typeof(CompositeDimension))
      return new CompositeDimensionJsonConverter();

    if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Dimension<>))
    {
      var valueType = typeToConvert.GetGenericArguments()[0];
      var converterType = typeof(TypedDimensionJsonConverter<>).MakeGenericType(valueType);
      return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(CompositeDimension<>))
    {
      var entityType = typeToConvert.GetGenericArguments()[0];
      var converterType = typeof(TypedCompositeDimensionJsonConverter<>).MakeGenericType(entityType);
      return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    throw new NotSupportedException($"Unsupported dimension type {typeToConvert.FullName}.");
  }

  private sealed class DimensionJsonConverter : JsonConverter<Dimension>
  {
    public override Dimension Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      var root = document.RootElement;

      if (root.TryGetProperty("$type", out var typeElement))
      {
        var runtimeType = TypeResolutionCache.Resolve(typeElement.GetString());
        if (runtimeType != null && runtimeType != typeof(Dimension))
          return (Dimension)JsonSerializer.Deserialize(root.GetRawText(), runtimeType, options)!;
      }

      var key = GetRequiredString(root, nameof(Dimension.Key));
      var label = GetOptionalString(root, nameof(Dimension.Label));
      var dimension = new Dimension(key, label ?? key);

      AddValues(root, dimension, options);
      AddHierarchies(root, dimension, options);
      return dimension;
    }

    public override void Write(Utf8JsonWriter writer, Dimension value, JsonSerializerOptions options)
    {
      if (value.GetType() != typeof(Dimension))
      {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
        return;
      }

      WriteDimensionStart(writer, value);
      WriteValues(writer, value, options);
      WriteHierarchies(writer, value, options);
      writer.WriteEndObject();
    }
  }

  private sealed class TypedDimensionJsonConverter<T> : JsonConverter<Dimension<T>>
  {
    public override Dimension<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      var root = document.RootElement;

      var key = GetRequiredString(root, nameof(Dimension.Key));
      var label = GetOptionalString(root, nameof(Dimension.Label));
      var dimension = new Dimension<T>(key, label ?? key);

      if (root.TryGetProperty(nameof(Dimension.Values), out var valuesElement) && valuesElement.ValueKind == JsonValueKind.Array)
      {
        foreach (var item in valuesElement.EnumerateArray())
        {
          try
          {
            var typedValue = item.Deserialize<DimensionValue<T>>(options);
            dimension.CreateValue(typedValue.Key, typedValue.Label, typedValue.Value);
          }
          catch
          {
          }
        }
      }

      AddHierarchies(root, dimension, options);
      return dimension;
    }

    public override void Write(Utf8JsonWriter writer, Dimension<T> value, JsonSerializerOptions options)
    {
      WriteDimensionStart(writer, value);
      WriteValues(writer, value, options);
      WriteHierarchies(writer, value, options);
      writer.WriteEndObject();
    }
  }

  private sealed class CompositeDimensionJsonConverter : JsonConverter<CompositeDimension>
  {
    public override CompositeDimension Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      var root = document.RootElement;

      var key = GetRequiredString(root, nameof(Dimension.Key));
      var label = GetOptionalString(root, nameof(Dimension.Label));
      var keyCriteria = root.GetProperty(nameof(CompositeDimension.KeyCriteria)).Deserialize<List<string>>(options);

      if (keyCriteria == null || keyCriteria.Count == 0)
        throw new JsonException("CompositeDimension must have at least one key criterion.");

      var dimension = new CompositeDimension(keyCriteria.ToArray())
      {
        Key = key,
        Label = label ?? key
      };

      AddValues(root, dimension, options);
      AddHierarchies(root, dimension, options);
      return dimension;
    }

    public override void Write(Utf8JsonWriter writer, CompositeDimension value, JsonSerializerOptions options)
    {
      WriteDimensionStart(writer, value);
      writer.WritePropertyName(nameof(CompositeDimension.KeyCriteria));
      JsonSerializer.Serialize(writer, value.KeyCriteria, options);
      WriteValues(writer, value, options);
      WriteHierarchies(writer, value, options);
      writer.WriteEndObject();
    }
  }

  private sealed class TypedCompositeDimensionJsonConverter<TEntity> : JsonConverter<CompositeDimension<TEntity>>
    where TEntity : new()
  {
    public override CompositeDimension<TEntity> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      var root = document.RootElement;

      var key = GetRequiredString(root, nameof(Dimension.Key));
      var label = GetOptionalString(root, nameof(Dimension.Label));
      var keyCriteria = root.GetProperty(nameof(CompositeDimension.KeyCriteria)).Deserialize<List<string>>(options);

      if (keyCriteria == null || keyCriteria.Count == 0)
        throw new JsonException("CompositeDimension must have at least one key criterion.");

      var dimension = new SerializationCompositeDimension<TEntity>(keyCriteria.ToArray())
      {
        Key = key,
        Label = label ?? key
      };

      AddValues(root, dimension, options);
      AddHierarchies(root, dimension, options);
      return dimension;
    }

    public override void Write(Utf8JsonWriter writer, CompositeDimension<TEntity> value, JsonSerializerOptions options)
    {
      WriteDimensionStart(writer, value);
      writer.WritePropertyName(nameof(CompositeDimension.KeyCriteria));
      JsonSerializer.Serialize(writer, value.KeyCriteria, options);
      WriteValues(writer, value, options);
      WriteHierarchies(writer, value, options);
      writer.WriteEndObject();
    }
  }

  private sealed class SerializationCompositeDimension<TEntity> : CompositeDimension<TEntity>
    where TEntity : new()
  {
    public SerializationCompositeDimension(params string[] criteria)
      : base(criteria)
    {
    }

    public override CompositeDimensionValue<TEntity> CreateCompositeValue(TEntity entity)
    {
      throw new NotSupportedException("This dimension was created from serialized data and cannot create new values.");
    }
  }

  private static void AddValues(JsonElement root, Dimension dimension, JsonSerializerOptions options)
  {
    if (!root.TryGetProperty(nameof(Dimension.Values), out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Array)
      return;

    foreach (var item in valuesElement.EnumerateArray())
    {
      try
      {
        var value = item.Deserialize<DimensionValue>(options);
        dimension.AddValue(value);
      }
      catch
      {
      }
    }
  }

  private static void AddHierarchies(JsonElement root, Dimension dimension, JsonSerializerOptions options)
  {
    if (!root.TryGetProperty(nameof(Dimension.Hierarchies), out var hierarchiesElement) || hierarchiesElement.ValueKind != JsonValueKind.Array)
      return;

    var hierarchies = hierarchiesElement.Deserialize<List<Outsourced.DataCube.Hierarchy.Hierarchy>>(options);
    if (hierarchies == null)
      return;

    foreach (var hierarchy in hierarchies)
      dimension.AddHierarchy(hierarchy);
  }

  private static void WriteDimensionStart(Utf8JsonWriter writer, Dimension dimension)
  {
    writer.WriteStartObject();
    writer.WriteString("$type", TypeResolutionCache.GetTypeName(dimension.GetType()));
    writer.WriteString(nameof(Dimension.Key), dimension.Key);

    if (!string.IsNullOrEmpty(dimension.Label))
      writer.WriteString(nameof(Dimension.Label), dimension.Label);
  }

  private static void WriteValues(Utf8JsonWriter writer, Dimension dimension, JsonSerializerOptions options)
  {
    if (dimension.Values.Count == 0)
      return;

    writer.WritePropertyName(nameof(Dimension.Values));
    JsonSerializer.Serialize(writer, dimension.Values, options);
  }

  private static void WriteHierarchies(Utf8JsonWriter writer, Dimension dimension, JsonSerializerOptions options)
  {
    if (dimension.Hierarchies.Count == 0)
      return;

    writer.WritePropertyName(nameof(Dimension.Hierarchies));
    JsonSerializer.Serialize(writer, dimension.Hierarchies, options);
  }

  private static string GetRequiredString(JsonElement root, string propertyName)
  {
    if (!root.TryGetProperty(propertyName, out var property) || string.IsNullOrEmpty(property.GetString()))
      throw new JsonException($"Dimension must have a {propertyName}.");

    return property.GetString()!;
  }

  private static string GetOptionalString(JsonElement root, string propertyName)
  {
    return root.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
  }
}
