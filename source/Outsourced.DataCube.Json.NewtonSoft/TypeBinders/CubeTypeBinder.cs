namespace Outsourced.DataCube.Json.NewtonSoft.TypeBinders;

using System;
using global::Newtonsoft.Json.Serialization;
using global::Newtonsoft.Json;
using DataCube;

/// <summary>
/// Maps common DataCube dimension type names to and from compact Json.NET type metadata.
/// </summary>
public class CubeTypeBinder : ISerializationBinder
{
  /// <inheritdoc />
  public void BindToName(Type serializedType, out string assemblyName, out string typeName)
  {
    // if (DimensionTypeBinder.BindToName(serializedType, out assemblyName, out typeName)) return;

    // We don't include the assembly name.
    assemblyName = null;

    if (serializedType.IsGenericType)
    {
      // Create a string like "Dimension<String>" or "CompositeDimension<String>"
      var genericDef = serializedType.GetGenericTypeDefinition();
      var genericArg = serializedType.GetGenericArguments()[0];
      typeName = genericDef.Name + "<" + genericArg.Name + ">";
    }
    else
    {
      // For non-generic types like Dimension or CompositeDimension
      typeName = serializedType.Name;
    }
  }

  /// <inheritdoc />
  public Type BindToType(string assemblyName, string typeName)
  {
    if (typeName.StartsWith("Dimension", StringComparison.OrdinalIgnoreCase))
    {
      if (typeName.Contains('<'))
      {
        // Parse out the generic type argument, e.g. "String" from "Dimension<String>"
        // and create the appropriate generic type. This is pseudocode.
        var genericArgName = ParseGenericArgument(typeName);
        var genericArgType = Type.GetType("System." + genericArgName);
        return typeof(Dimension<>).MakeGenericType(genericArgType);
      }
      else
      {
        return typeof(Dimension);
      }
    }
    else if (typeName.StartsWith("CompositeDimension", StringComparison.OrdinalIgnoreCase))
    {
      if (typeName.Contains('<'))
      {
        var genericArgName = ParseGenericArgument(typeName);
        var genericArgType = Type.GetType("System." + genericArgName);
        return typeof(CompositeDimension<>).MakeGenericType(genericArgType);
      }
      else
      {
        return typeof(CompositeDimension);
      }
    }
    throw new JsonSerializationException($"Unknown type: {typeName}");
  }

  private static string ParseGenericArgument(string typeName)
  {
    // Implement logic to extract the generic argument from the string.
    // For example, from "Dimension<String>" extract "String".
    var start = typeName.IndexOf('<') + 1;
    var end = typeName.IndexOf('>');
    return typeName.Substring(start, end - start);
  }
}

