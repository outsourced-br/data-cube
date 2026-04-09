namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using DataCube;
using Internal;

public class DimensionValueConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof(DimensionValue);
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
  {
    JObject jsonObject = JObject.Load(reader);

    if (jsonObject.TryGetValue("$type", out JToken typeToken))
    {
      var typeName = typeToken.ToString();
      var actualType = TypeResolutionCache.Resolve(typeName, typeof(DimensionValue));
      if (actualType != null && actualType != objectType)
      {
        return jsonObject.ToObject(actualType, serializer);
      }
    }

    // Fallback: deserialize as a regular DimensionValue
    var key = jsonObject[nameof(DimensionValue.Key)]?.ToString();

    if (string.IsNullOrEmpty(key))
      throw new JsonException("DimensionValue must have a Key.");

    // Check if this is a composite value, if needed
    if (string.Equals(jsonObject["$type"]?.ToString(), nameof(CompositeDimensionValue), StringComparison.OrdinalIgnoreCase))
    {
      var valueObj = jsonObject[nameof(DimensionValue.Value)].ToObject<Dictionary<string, object>>(serializer);
      return new CompositeDimensionValue(key, null, valueObj);
    }

    var value = jsonObject[nameof(DimensionValue.Value)]?.ToObject<object>(serializer);
    return new DimensionValue(key, null, value);
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    if (value.GetType() != typeof(DimensionValue))
    {
      serializer.Serialize(writer, value, value.GetType());
      return;
    }

    var dimensionValue = (DimensionValue)value;

    writer.WriteStartObject();

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(value.GetType()));

    writer.WritePropertyName(nameof(DimensionValue.Key));
    writer.WriteValue(dimensionValue.Key);

    writer.WritePropertyName(nameof(DimensionValue.Value));
    serializer.Serialize(writer, dimensionValue.Value);

    writer.WriteEndObject();
  }
}

public class DimensionValueConverter<T> : JsonConverter<DimensionValue<T>>
{
  public override DimensionValue<T> ReadJson(JsonReader reader, Type objectType, DimensionValue<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var jsonObject = JObject.Load(reader);

    var key = jsonObject[nameof(DimensionValue.Key)]?.ToString();
    var label = jsonObject[nameof(DimensionValue.Label)]?.ToString();

    if (string.IsNullOrEmpty(key))
      throw new JsonException("DimensionValue must have a Key.");

    var value = jsonObject[nameof(DimensionValue.Value)].ToObject<T>(serializer);

    return new DimensionValue<T>(key, label, value);
  }

  public override void WriteJson(JsonWriter writer, DimensionValue<T> value, JsonSerializer serializer)
  {
    //serializer.SerializationBinder.BindToName(value.GetType(), out _, out var typeName);

    writer.WriteStartObject();

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(value.GetType()));

    writer.WritePropertyName(nameof(DimensionValue.Key));
    writer.WriteValue(value.Key);

    if (value.Label != null)
    {
      writer.WritePropertyName(nameof(DimensionValue.Label));
      writer.WriteValue(value.Label);
    }

    writer.WritePropertyName(nameof(DimensionValue.Value));
    serializer.Serialize(writer, value.Value);

    writer.WriteEndObject();
  }
}
