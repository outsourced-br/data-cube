namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using System.Diagnostics;
using System.Reflection;
using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using DataCube;
using Internal;

/// <summary>
/// Handles serialization for DimensionCompositeValue when associated with a CustomCompositeDimension
/// </summary>
public class CustomCompositeDimensionValueConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof(CompositeDimensionValue) ||
           (objectType.IsGenericType &&
            objectType.GetGenericTypeDefinition() == typeof(CompositeDimensionValue<>));
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
  {
    var jsonObject = JObject.Load(reader);

    string key = jsonObject[nameof(DimensionValue.Key)]?.ToString();
    string label = jsonObject[nameof(DimensionValue.Label)]?.ToString();

    if (string.IsNullOrEmpty(key))
      throw new JsonException("DimensionValue must have a Key.");

    // Handle generic DimensionCompositeValue<T>
    if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(CompositeDimensionValue<>))
    {
      var valueObj = jsonObject[nameof(DimensionValue.Value)]?.ToObject<Dictionary<string, object>>(serializer);

      // Create an instance using reflection since we can't directly construct generic types
      var entityType = objectType.GetGenericArguments()[0];
      var constructor = objectType.GetConstructor(new[] {
        typeof(string), typeof(string), typeof(Dictionary<string, object>), entityType
      });

      // We can't deserialize the original entity, so use default
      return constructor?.Invoke(new object[] { key, label, valueObj, GetDefault(entityType) });
    }

    // Regular DimensionCompositeValue
    var values = jsonObject[nameof(DimensionValue.Value)]?.ToObject<Dictionary<string, object>>(serializer);
    return new CompositeDimensionValue(key, label, values);
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    writer.WriteStartObject();

    // Use reflection to get common properties
    var type = value.GetType();

#if DEBUG
    Debug.WriteLine($"{nameof(CustomCompositeDimensionValueConverter)} for {type}");
#endif

    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(type));

    // Key property
    writer.WritePropertyName(nameof(DimensionValue.Key));
    var keyProperty = type.GetProperty(nameof(DimensionValue.Key));
    writer.WriteValue(keyProperty?.GetValue(value)?.ToString());

    // Label property
    var labelProperty = type.GetProperty(nameof(DimensionValue.Label));
    var label = labelProperty?.GetValue(value)?.ToString();
    if (!string.IsNullOrEmpty(label))
    {
      writer.WritePropertyName(nameof(DimensionValue.Label));
      writer.WriteValue(label);
    }

    // Value property (Dictionary<string, object>)
    writer.WritePropertyName(nameof(DimensionValue.Value));

    // Need to handle the Value property carefully to avoid ambiguity
    Dictionary<string, object> fieldValues;

    if (type == typeof(CompositeDimensionValue))
    {
      // For standard DimensionCompositeValue
      var valueProp = typeof(CompositeDimensionValue).GetProperty(nameof(DimensionValue.Value),
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
      fieldValues = valueProp?.GetValue(value) as Dictionary<string, object>;
    }
    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CompositeDimensionValue<>))
    {
      // For generic DimensionCompositeValue<T>
      // First get the base DimensionCompositeValue type
      var baseType = type.BaseType;
      var baseProp = baseType?.GetProperty(nameof(DimensionValue.Value),
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
      fieldValues = baseProp?.GetValue(value) as Dictionary<string, object>;
    }
    else
    {
      // Fallback - try using standard property access
      var valueProp = type.GetProperty(nameof(DimensionValue.Value));
      fieldValues = valueProp?.GetValue(value) as Dictionary<string, object>;
    }

    // Serialize the field values
    serializer.Serialize(writer, fieldValues);

    writer.WriteEndObject();
  }

  private static object GetDefault(Type type)
  {
    return type.IsValueType ? Activator.CreateInstance(type) : null;
  }
}
