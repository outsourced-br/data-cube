namespace Outsourced.DataCube.Json.NewtonSoft.TypeBinders;

using System;
using DataCube;

internal static class DimensionTypeBinder
{
  internal static bool BindToName(Type serializedType, out string assemblyName, out string typeName)
  {
    assemblyName = null;
    typeName = serializedType.Name;

    if (!serializedType.IsAssignableFrom(typeof(Dimension)))
    {
      return false;
    }

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

    return true;
  }
}

