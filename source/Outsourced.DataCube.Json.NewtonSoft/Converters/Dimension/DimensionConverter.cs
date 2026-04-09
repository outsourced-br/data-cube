namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using DataCube;
using Internal;

public class DimensionConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof(Dimension);
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
  {
    var jsonObject = JObject.Load(reader);

    if (jsonObject.TryGetValue("$type", out var typeToken))
    {
      var actualType = TypeResolutionCache.Resolve(typeToken.ToString(), typeof(Dimension));
      if (actualType != null && actualType != objectType)
        return jsonObject.ToObject(actualType, serializer);
    }

    var key = jsonObject[nameof(Dimension.Key)]?.ToString();
    var label = jsonObject[nameof(Dimension.Label)]?.ToString();

    if (string.IsNullOrEmpty(key))
      throw new JsonException("Dimension must have a Key.");

    var dimension = new Dimension(key, label ?? key);

    // Add values if present
    if (jsonObject.TryGetValue(nameof(Dimension.Values), out var valuesToken) && valuesToken is JArray valuesArray)
    {
      foreach (var valueToken in valuesArray)
      {
        try
        {
          var dimValue = valueToken.ToObject<DimensionValue>(serializer);
          dimension.AddValue(dimValue);
        }
        catch
        {
          // Skip values that can't be properly deserialized
        }
      }
    }

    HierarchySerializationHelpers.AddHierarchies(jsonObject, dimension, serializer);

    return dimension;
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    if (value.GetType() != typeof(Dimension))
    {
      serializer.Serialize(writer, value, value.GetType());
      return;
    }

    var dimension = (Dimension)value;

    writer.WriteStartObject();

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(value.GetType()));

    writer.WritePropertyName(nameof(Dimension.Key));
    writer.WriteValue(dimension.Key);

    writer.WritePropertyName(nameof(Dimension.Label));
    writer.WriteValue(dimension.Label);

    if (dimension.Values.Count > 0)
    {
      writer.WritePropertyName(nameof(Dimension.Values));
      serializer.Serialize(writer, dimension.Values);
    }

    HierarchySerializationHelpers.WriteHierarchies(writer, dimension, serializer);

    writer.WriteEndObject();
  }
}

public class DimensionConverter<T> : JsonConverter<Dimension<T>>
{
  public override Dimension<T> ReadJson(JsonReader reader, Type objectType, Dimension<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var jsonObject = JObject.Load(reader);

    var key = jsonObject[nameof(Dimension.Key)]?.ToString();
    var label = jsonObject[nameof(Dimension.Label)]?.ToString();

    if (string.IsNullOrEmpty(key))
      throw new JsonException("Dimension must have a Key.");

    var dimension = new Dimension<T>(key, label ?? key);

    // Add values if present
    if (jsonObject.TryGetValue(nameof(Dimension.Values), out var valuesToken) && valuesToken is JArray valuesArray)
    {
      foreach (var valueToken in valuesArray)
      {
        try
        {
          if (valueToken is JObject valueObj)
          {
            var valueKey = valueObj[nameof(DimensionValue.Key)]?.ToString();
            var valueLabel = valueObj[nameof(DimensionValue.Label)]?.ToString();
            var typedValue = valueObj[nameof(DimensionValue.Value)].ToObject<T>(serializer);

            dimension.CreateValue(valueKey, valueLabel, typedValue);
          }
        }
        catch
        {
          // Skip values that can't be properly deserialized
        }
      }
    }

    HierarchySerializationHelpers.AddHierarchies(jsonObject, dimension, serializer);

    return dimension;
  }

  public override void WriteJson(JsonWriter writer, Dimension<T> value, JsonSerializer serializer)
  {
    //serializer.SerializationBinder.BindToName(value.GetType(), out _, out var typeName);

    writer.WriteStartObject();

    //writer.WritePropertyName("$type");
    //writer.WriteValue($"Dimension<{typeof(T).Name}>");

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(value.GetType()));

    writer.WritePropertyName(nameof(Dimension.Key));
    writer.WriteValue(value.Key);

    if (value.Label != null)
    {
      writer.WritePropertyName(nameof(Dimension.Label));
      writer.WriteValue(value.Label);
    }

    if (value.Values.Count > 0)
    {
      writer.WritePropertyName(nameof(Dimension.Values));
      serializer.Serialize(writer, value.Values);
    }

    HierarchySerializationHelpers.WriteHierarchies(writer, value, serializer);

    writer.WriteEndObject();
  }
}

internal static class HierarchySerializationHelpers
{
  internal static void AddHierarchies(JObject jsonObject, Dimension dimension, JsonSerializer serializer)
  {
    if (!jsonObject.TryGetValue(nameof(Dimension.Hierarchies), out var hierarchiesToken) || hierarchiesToken is not JArray hierarchiesArray)
      return;

    var hierarchies = hierarchiesArray.ToObject<List<Outsourced.DataCube.Hierarchy.Hierarchy>>(serializer);
    if (hierarchies == null)
      return;

    foreach (var hierarchy in hierarchies)
      dimension.AddHierarchy(hierarchy);
  }

  internal static void WriteHierarchies(JsonWriter writer, Dimension dimension, JsonSerializer serializer)
  {
    if (dimension.Hierarchies.Count == 0)
      return;

    writer.WritePropertyName(nameof(Dimension.Hierarchies));
    serializer.Serialize(writer, dimension.Hierarchies);
  }
}
