namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using DataCube;
using Internal;

public class CompositeDimensionConverterT<T> : JsonConverter<CompositeDimension<T>>
  where T : new()
{
  public override CompositeDimension<T> ReadJson(JsonReader reader, Type objectType, CompositeDimension<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var jsonObject = JObject.Load(reader);

    var key = jsonObject[nameof(Dimension.Key)]?.ToString();
    var label = jsonObject[nameof(Dimension.Label)]?.ToString();
    List<string> keyCriteria = null;

    if (jsonObject.TryGetValue(nameof(CompositeDimension.KeyCriteria), out var criteriaToken))
      keyCriteria = criteriaToken.ToObject<List<string>>(serializer);

    if (string.IsNullOrEmpty(key))
      throw new JsonException("Dimension must have a Key.");

    if (keyCriteria == null || keyCriteria.Count == 0)
      throw new JsonException("CompositeDimension must have at least one key criterion.");

    // Since we can't reconstruct the property expressions during deserialization,
    // create a minimal implementation that stores the criteria
    var dimension = new SerializationCompositeDimension<T>(keyCriteria.ToArray())
    {
      Key = key,
      Label = label ?? key
    };

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

  public override void WriteJson(JsonWriter writer, CompositeDimension<T> value, JsonSerializer serializer)
  {
    //serializer.SerializationBinder.BindToName(value.GetType(), out _, out var typeName);

    writer.WriteStartObject();

    //writer.WritePropertyName("$type");
    //writer.WriteValue($"CompositeDimension<{typeof(T).Name}>");

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(value.GetType()));

    writer.WritePropertyName(nameof(Dimension.Key));
    writer.WriteValue(value.Key);

    writer.WritePropertyName(nameof(Dimension.Label));
    writer.WriteValue(value.Label);

    writer.WritePropertyName(nameof(CompositeDimension.KeyCriteria));
    serializer.Serialize(writer, value.KeyCriteria);

    if (value.Values.Count > 0)
    {
      writer.WritePropertyName(nameof(Dimension.Values));
      serializer.Serialize(writer, value.Values);
    }

    HierarchySerializationHelpers.WriteHierarchies(writer, value, serializer);

    writer.WriteEndObject();
  }

  // Special implementation that only supports deserialization
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
}
