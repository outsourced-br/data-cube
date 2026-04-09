namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using DataCube;
using Internal;

public class CompositeDimensionValueConverter : JsonConverter<CompositeDimensionValue>
{
  public override CompositeDimensionValue ReadJson(JsonReader reader, Type objectType, CompositeDimensionValue existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var jsonObject = JObject.Load(reader);

    var key = jsonObject[nameof(DimensionValue.Key)]?.ToString();
    var label = jsonObject[nameof(DimensionValue.Label)]?.ToString();
    var values = jsonObject[nameof(DimensionValue.Value)].ToObject<Dictionary<string, object>>(serializer);

    if (string.IsNullOrEmpty(key))
      throw new JsonException("DimensionValue must have a Key.");

    return new CompositeDimensionValue(key, label, values);
  }

  public override void WriteJson(JsonWriter writer, CompositeDimensionValue value, JsonSerializer serializer)
  {
    //serializer.SerializationBinder.BindToName(value.GetType(), out _, out var typeName);

    writer.WriteStartObject();

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(value.GetType()));

    writer.WritePropertyName(nameof(DimensionValue.Key));
    writer.WriteValue(value.Key);

    if (!string.IsNullOrEmpty(value.Label))
    {
      writer.WritePropertyName(nameof(DimensionValue.Label));
      writer.WriteValue(value.Label);
    }

    writer.WritePropertyName(nameof(DimensionValue.Value));
    serializer.Serialize(writer, value.Value);

    writer.WriteEndObject();
  }
}

public class CompositeDimensionValueConverter<T> : JsonConverter<CompositeDimensionValue<T>>
  where T : new()
{
  public override CompositeDimensionValue<T> ReadJson(JsonReader reader, Type objectType, CompositeDimensionValue<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var jsonObject = JObject.Load(reader);

    var key = jsonObject[nameof(DimensionValue.Key)]?.ToString();
    var label = jsonObject[nameof(DimensionValue.Label)]?.ToString();
    var values = jsonObject[nameof(DimensionValue.Value)].ToObject<Dictionary<string, object>>(serializer);

    if (string.IsNullOrEmpty(key))
      throw new JsonException("DimensionValue must have a Key.");

    // Note: We can't deserialize the Entity property,
    // so we create a value with the default entity
    return new CompositeDimensionValue<T>(key, label, values, default);
  }

  public override void WriteJson(JsonWriter writer, CompositeDimensionValue<T> value, JsonSerializer serializer)
  {
    //serializer.SerializationBinder.BindToName(value.GetType(), out _, out var typeName);

    writer.WriteStartObject();

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(value.GetType()));

    writer.WritePropertyName(nameof(DimensionValue.Key));
    writer.WriteValue(value.Key);

    if (!string.IsNullOrEmpty(value.Label))
    {
      writer.WritePropertyName(nameof(DimensionValue.Label));
      writer.WriteValue(value.Label);
    }

    writer.WritePropertyName(nameof(DimensionValue.Value));
    serializer.Serialize(writer, value.Value);

    writer.WriteEndObject();
  }
}
