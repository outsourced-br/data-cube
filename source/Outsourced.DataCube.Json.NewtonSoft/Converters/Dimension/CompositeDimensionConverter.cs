namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using DataCube;
using Internal;

public class CompositeDimensionConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof(CompositeDimension);
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
    List<string> keyCriteria = null;

    if (jsonObject.TryGetValue(nameof(CompositeDimension.KeyCriteria), out var criteriaToken))
      keyCriteria = criteriaToken.ToObject<List<string>>(serializer);

    if (string.IsNullOrEmpty(key))
      throw new JsonException("Dimension must have a Key.");

    if (keyCriteria == null || keyCriteria.Count == 0)
      throw new JsonException("CompositeDimension must have at least one key criterion.");

    var dimension = new CompositeDimension(keyCriteria.ToArray())
    {
      Key = key,
      Label = label ?? key
    };

    // Add values if present
    if (jsonObject.TryGetValue(nameof(Dimension.Values), out var valuesToken) && valuesToken is JArray valuesArray)
    {
      foreach (var valueToken in valuesArray)
      {
        var dimValue = valueToken.ToObject<DimensionValue>(serializer);
        dimension.AddValue(dimValue);
      }
    }

    HierarchySerializationHelpers.AddHierarchies(jsonObject, dimension, serializer);

    return dimension;
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    var dimension = (CompositeDimension)value;
    //serializer.SerializationBinder.BindToName(value.GetType(), out _, out var typeName);

    writer.WriteStartObject();

    //writer.WritePropertyName("$type");
    //writer.WriteValue($"CompositeDimension");

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(value.GetType()));

    writer.WritePropertyName(nameof(Dimension.Key));
    writer.WriteValue(dimension.Key);

    writer.WritePropertyName(nameof(Dimension.Label));
    writer.WriteValue(dimension.Label);

    writer.WritePropertyName(nameof(CompositeDimension.KeyCriteria));
    serializer.Serialize(writer, dimension.KeyCriteria);

    if (dimension.Values.Count > 0)
    {
      writer.WritePropertyName(nameof(Dimension.Values));
      serializer.Serialize(writer, dimension.Values);
    }

    HierarchySerializationHelpers.WriteHierarchies(writer, dimension, serializer);

    writer.WriteEndObject();
  }
}
