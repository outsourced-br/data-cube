namespace Outsourced.DataCube.Json.SystemText.SerializerSettings;

using System.Linq;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;
using Converters;

/// <summary>
/// Configures <see cref="JsonSerializerOptions"/> for DataCube serialization.
/// </summary>
public static class CubeSerializerSettings
{
  /// <summary>
  /// Applies the library's recommended converters and options to an existing <see cref="JsonSerializerOptions"/> instance.
  /// </summary>
  public static JsonSerializerOptions Initialize(JsonSerializerOptions options = null)
  {
    options ??= new JsonSerializerOptions();

    options.PropertyNameCaseInsensitive = true;
    options.WriteIndented = true;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;

    AddConverter<ObjectValueJsonConverter>(options, static () => new ObjectValueJsonConverter());
    AddConverter<DimensionJsonConverterFactory>(options, static () => new DimensionJsonConverterFactory());
    AddConverter<DimensionValueJsonConverterFactory>(options, static () => new DimensionValueJsonConverterFactory());
    AddConverter<MetricJsonConverterFactory>(options, static () => new MetricJsonConverterFactory());
    AddConverter<MetricCollectionJsonConverterFactory>(options, static () => new MetricCollectionJsonConverterFactory());
    AddConverter<FactGroupJsonConverter>(options, static () => new FactGroupJsonConverter());

    return options;
  }

  /// <summary>
  /// Creates a new <see cref="JsonSerializerOptions"/> instance configured for DataCube serialization.
  /// </summary>
  public static JsonSerializerOptions CreateOptions()
  {
    return Initialize();
  }

  private static void AddConverter<TConverter>(JsonSerializerOptions options, Func<TConverter> factory)
    where TConverter : JsonConverter
  {
    if (options.Converters.OfType<TConverter>().Any())
      return;

    options.Converters.Add(factory());
  }
}
