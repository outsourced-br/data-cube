namespace Outsourced.DataCube.Json.NewtonSoft.SerializerSettings;

using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Converters;

/// <summary>
/// Configures Json.NET serializer settings for <see cref="Outsourced.DataCube.AnalyticsCube"/> serialization.
/// </summary>
public static class CubeSerializerSettings
{
  /// <summary>
  /// Applies the library's recommended Json.NET configuration to an existing settings instance.
  /// </summary>
  public static JsonSerializerSettings Initialize(JsonSerializerSettings settings = null)
  {
    settings ??= new JsonSerializerSettings();

    // Let the custom converters own polymorphic metadata instead of relying on Json.NET type-name handling.
    settings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
    settings.TypeNameHandling = TypeNameHandling.None;

    // Handle object creation and references
    settings.ObjectCreationHandling = ObjectCreationHandling.Auto;
    settings.PreserveReferencesHandling = PreserveReferencesHandling.None;
    settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

    // Handle null values and defaults
    settings.NullValueHandling = NullValueHandling.Ignore;
    settings.DefaultValueHandling = DefaultValueHandling.Ignore;
    settings.MissingMemberHandling = MissingMemberHandling.Ignore;

    // Handle date times
    settings.DateParseHandling = DateParseHandling.DateTime;

    // Formatting
    settings.Formatting = Formatting.Indented;

    // Add default converters and resolver
    settings.Converters.Add(new StringEnumConverter());
    settings.ContractResolver = new CubeContractResolver();

    return settings;
  }

  /// <summary>
  /// Creates a Json.NET serializer configured for DataCube serialization.
  /// </summary>
  public static JsonSerializer CreateSerializer()
  {
    return JsonSerializer.Create(Initialize());
  }
}
