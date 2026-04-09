namespace Outsourced.DataCube.Json.NewtonSoft.TypeBinders;

using System;
using System.Collections.Generic;
using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Serialization;

/// <summary>
/// Serializes and resolves generic type names using a friendly full-name representation.
/// </summary>
public class GenericTypeNameSerializationBinder : ISerializationBinder
{
  /// <inheritdoc />
  public Type BindToType(string assemblyName, string typeName)
  {
    return ParseFriendlyTypeName(typeName);
  }

  /// <inheritdoc />
  public void BindToName(Type serializedType, out string assemblyName, out string typeName)
  {
    assemblyName = null;
    typeName = GetFriendlyTypeName(serializedType);
  }

  private static string GetFriendlyTypeName(Type type)
  {
    if (!type.IsGenericType)
      return type.FullName; // use the full name instead of just Name

    string genericTypeName = type.GetGenericTypeDefinition().FullName;
    int backtickIndex = genericTypeName.IndexOf('`');
    if (backtickIndex > 0)
      genericTypeName = genericTypeName.Substring(0, backtickIndex);

    string genericArgs = string.Join(",", type.GetGenericArguments().Select(t => GetFriendlyTypeName(t)));
    return $"{genericTypeName}<{genericArgs}>";
  }

  private static Type ParseFriendlyTypeName(string friendlyName)
  {
    int genericStart = friendlyName.IndexOf('<', StringComparison.OrdinalIgnoreCase);
    if (genericStart < 0)
      return ResolveType(friendlyName);

    string mainTypeName = friendlyName[..genericStart];
    string genericArgsSection = friendlyName.Substring(genericStart + 1, friendlyName.Length - genericStart - 2);

    List<string> argStrings = SplitGenericArguments(genericArgsSection);
    Type[] genericArgTypes = new Type[argStrings.Count];
    for (int i = 0; i < argStrings.Count; i++)
      genericArgTypes[i] = ParseFriendlyTypeName(argStrings[i].Trim());

    // Look for the generic type definition by appending the arity (e.g. "Cube`1")
    string genericTypeName = $"{mainTypeName}`{genericArgTypes.Length}";
    Type genericTypeDef = ResolveType(genericTypeName);
    if (genericTypeDef == null)
      throw new JsonSerializationException($"Cannot find generic type definition for {genericTypeName}");

    return genericTypeDef.MakeGenericType(genericArgTypes);
  }

  // Splits a comma-separated list of generic arguments, handling nested generics.
  private static List<string> SplitGenericArguments(string args)
  {
    var result = new List<string>();
    int bracketLevel = 0;
    int lastPos = 0;
    for (int i = 0; i < args.Length; i++)
    {
      char c = args[i];
      switch (c)
      {
        case '<':
          bracketLevel++;
          break;
        case '>':
          bracketLevel--;
          break;
        case ',' when bracketLevel == 0:
          result.Add(args.Substring(lastPos, i - lastPos));
          lastPos = i + 1;
          break;
      }
    }

    // Add the final argument.
    result.Add(args[lastPos..]);
    return result;
  }

  // Attempts to resolve a type by its simple name (or with a generic arity).
  private static Type ResolveType(string typeName)
  {
    // Try Type.GetType first.
    Type type = Type.GetType(typeName);
    if (type != null) return type;

    // Search all loaded assemblies.
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      // First try a direct lookup.
      type = assembly.GetType(typeName);
      if (type != null) return type;

      // If that fails, try comparing simple names.
      foreach (var t in assembly.GetTypes())
      {
        if (string.Equals(t.FullName, typeName, StringComparison.OrdinalIgnoreCase))
          return t;
      }
    }

    throw new JsonSerializationException($"Type '{typeName}' not found.");
  }
}
