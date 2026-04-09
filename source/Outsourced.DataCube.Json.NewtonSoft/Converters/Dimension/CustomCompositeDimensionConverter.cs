namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using System;
using System.Collections.Generic;
using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using DataCube;
using Internal;

/// <summary>
/// Handles serialization for the private CustomCompositeDimension class from CompositeDimensionBuilder
/// </summary>
public class CustomCompositeDimensionConverter : JsonConverter
{
  // Identifies if a type is a CustomCompositeDimension from CompositeDimensionBuilder
  public static bool IsCustomCompositeDimension(Type type)
  {
    return type != null &&
           type.IsNested &&
           type.FullName.Contains("CustomCompositeDimension") &&
           type.FullName.Contains("Outsourced.DataCube.Builders.CompositeDimensionBuilder");
  }

  public override bool CanConvert(Type objectType)
  {
    return IsCustomCompositeDimension(objectType);
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
  {
    if (reader.TokenType != JsonToken.StartObject)
      throw new JsonException("Expected StartObject token.");

    var jsonObject = JObject.Load(reader);

    string key = jsonObject[nameof(CompositeDimension.Key)]?.ToString();
    string label = jsonObject[nameof(CompositeDimension.Label)]?.ToString();
    List<string> keyCriteria = null;

    if (jsonObject.TryGetValue(nameof(CompositeDimension.KeyCriteria), out var criteriaToken))
      keyCriteria = criteriaToken.ToObject<List<string>>(serializer);

    if (string.IsNullOrEmpty(key))
      throw new JsonException("Dimension must have a Key.");

    if (keyCriteria == null || keyCriteria.Count == 0)
      throw new JsonException("CompositeDimension must have at least one key criterion.");

    // Create a standard CompositeDimension as a fallback
    var dimension = new CompositeDimension(keyCriteria.ToArray())
    {
      Key = key,
      Label = label ?? key
    };

    // Add values if present
    if (jsonObject.TryGetValue(nameof(CompositeDimension.Values), out var valuesToken) && valuesToken is JArray valuesArray)
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
    // Use reflection to get properties since we can't cast to the private type
    var type = value.GetType();
    writer.WriteStartObject();

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(typeof(CompositeDimension)));

    // Key property
    var keyProperty = type.GetProperty(nameof(CompositeDimension.Key));
    writer.WritePropertyName(nameof(CompositeDimension.Key));
    writer.WriteValue(keyProperty?.GetValue(value)?.ToString());

    // Label property
    var labelProperty = type.GetProperty(nameof(CompositeDimension.Label));
    writer.WritePropertyName(nameof(CompositeDimension.Label));
    writer.WriteValue(labelProperty?.GetValue(value)?.ToString());

    // KeyCriteria property
    var keyCriteriaProperty = type.GetProperty(nameof(CompositeDimension.KeyCriteria));
    writer.WritePropertyName(nameof(CompositeDimension.KeyCriteria));
    serializer.Serialize(writer, keyCriteriaProperty?.GetValue(value));

    var valuesProperty = type.GetProperty(nameof(CompositeDimension.Values));
    var dimensionValues = valuesProperty?.GetValue(value);
    if (dimensionValues != null)
    {
      writer.WritePropertyName(nameof(CompositeDimension.Values));
      serializer.Serialize(writer, dimensionValues);
    }

    if (type.GetProperty(nameof(Dimension.Hierarchies))?.GetValue(value) is IList<Outsourced.DataCube.Hierarchy.Hierarchy> hierarchies && hierarchies.Count > 0)
    {
      writer.WritePropertyName(nameof(Dimension.Hierarchies));
      serializer.Serialize(writer, hierarchies);
    }

    writer.WriteEndObject();
  }
}
