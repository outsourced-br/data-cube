namespace Outsourced.DataCube.Json.NewtonSoft.TypeBinders;

using System;
using global::Newtonsoft.Json.Serialization;
using global::Newtonsoft.Json;
using DataCube;

/// <summary>
/// Maps friendly DataCube dimension names to runtime types for Json.NET polymorphic deserialization.
/// </summary>
public class CustomTypeBinder : ISerializationBinder
{
  /// <inheritdoc />
  public void BindToName(Type serializedType, out string assemblyName, out string typeName)
  {
    // Omit the assembly name.
    assemblyName = null;

    // Check if the type is generic.
    if (serializedType.IsGenericType)
    {
      // Get the generic type definition.
      var genericDef = serializedType.GetGenericTypeDefinition();
      var genericArg = serializedType.GetGenericArguments()[0];

      // Compare against your known generic definitions.
      if (genericDef == typeof(Dimension<>))
      {
        typeName = $"Dimension<{genericArg.Name}>";
      }
      else if (genericDef == typeof(CompositeDimension<>))
      {
        typeName = $"CompositeDimension<{genericArg.Name}>";
      }
      else
      {
        // Fallback for any other generic types.
        typeName = serializedType.Name;
      }
    }
    else
    {
      // For non-generic types, simply use the type name.
      typeName = serializedType.Name;
    }
  }

  /// <inheritdoc />
  public Type BindToType(string assemblyName, string typeName)
  {
    // Handle generic types first.
    if (typeName.StartsWith("CompositeDimension<", StringComparison.OrdinalIgnoreCase))
    {
      string genericArgName = ExtractGenericArgument(typeName);
      var genericArgType = GetTypeFromName(genericArgName);
      return typeof(CompositeDimension<>).MakeGenericType(genericArgType);
    }
    else if (typeName.StartsWith("Dimension<", StringComparison.OrdinalIgnoreCase))
    {
      string genericArgName = ExtractGenericArgument(typeName);
      var genericArgType = GetTypeFromName(genericArgName);
      return typeof(Dimension<>).MakeGenericType(genericArgType);
    }
    // Handle non-generic cases.
    else if (string.Equals(typeName, "CompositeDimension", StringComparison.OrdinalIgnoreCase))
    {
      return typeof(CompositeDimension);
    }
    else if (string.Equals(typeName, "Dimension", StringComparison.OrdinalIgnoreCase))
    {
      return typeof(Dimension);
    }
    throw new JsonSerializationException($"Unknown type: {typeName}");
  }

  private static string ExtractGenericArgument(string typeName)
  {
    var start = typeName.IndexOf('<') + 1;
    var end = typeName.IndexOf('>');
    return typeName.Substring(start, end - start);
  }

  private static Type GetTypeFromName(string typeName)
  {
    // Here you might map the simple name to a .NET type.
    // For example, if you expect "String", "Int32", etc.
    switch (typeName)
    {
      case "String":
        return typeof(string);
      case "Int32":
        return typeof(int);
      // Add additional mappings as needed.
      default:
        throw new JsonSerializationException($"Unknown generic argument type: {typeName}");
    }
  }
}

