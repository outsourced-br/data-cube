namespace Outsourced.DataCube.Json.SystemText.Converters;

using DataCube;
using Internal;
using Metrics;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;

/// <summary>
/// Creates converters for <see cref="Metric"/> model types.
/// </summary>
public sealed class MetricJsonConverterFactory : JsonConverterFactory
{
  /// <inheritdoc />
  public override bool CanConvert(Type typeToConvert)
  {
    return typeof(Metric).IsAssignableFrom(typeToConvert);
  }

  /// <inheritdoc />
  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    var converterType = typeof(MetricJsonConverter<>).MakeGenericType(typeToConvert);
    return (JsonConverter)Activator.CreateInstance(converterType)!;
  }

  private sealed class MetricJsonConverter<TMetric> : JsonConverter<TMetric>
    where TMetric : Metric
  {
    public override TMetric Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      return (TMetric)ReadMetric(document.RootElement, typeToConvert);
    }

    public override void Write(Utf8JsonWriter writer, TMetric value, JsonSerializerOptions options)
    {
      WriteMetric(writer, value);
    }
  }

  private static Metric ReadMetric(JsonElement root, Type declaredType)
  {
    var runtimeType = declaredType;
    if (root.TryGetProperty("$type", out var typeElement))
      runtimeType = TypeResolutionCache.Resolve(typeElement.GetString()) ?? declaredType;

    var key = GetRequiredString(root, nameof(Metric.Key));
    var label = GetOptionalString(root, nameof(Metric.Label));
    var format = GetOptionalString(root, nameof(Metric.Format));
    var unit = GetOptionalString(root, nameof(Metric.Unit));
    var businessKeyDimensionKey = GetOptionalString(root, nameof(DistinctCountMetric.BusinessKeyDimensionKey));

    var metricType = MetricType.Unknown;
    if (root.TryGetProperty(nameof(Metric.MetricType), out var metricTypeElement) &&
        Enum.TryParse(metricTypeElement.GetString(), ignoreCase: true, out MetricType parsedMetricType))
    {
      metricType = parsedMetricType;
    }

    var aggregationType = AggregationType.Count;
    if (root.TryGetProperty(nameof(Metric.AggregationType), out var aggregationTypeElement) &&
        Enum.TryParse(aggregationTypeElement.GetString(), ignoreCase: true, out AggregationType parsedAggregationType))
    {
      aggregationType = parsedAggregationType;
    }

    var result = MetricFactory.CreateMetric(runtimeType, key, label, metricType, aggregationType, format, unit, businessKeyDimensionKey);
    result.Semantics = ReadSemantics(root, aggregationType);
    return result;
  }

  private static void WriteMetric(Utf8JsonWriter writer, Metric metric)
  {
    writer.WriteStartObject();
    writer.WriteString("$type", TypeResolutionCache.GetTypeName(metric.GetType()));
    writer.WriteString(nameof(Metric.Key), metric.Key);

    if (!string.IsNullOrEmpty(metric.Label))
      writer.WriteString(nameof(Metric.Label), metric.Label);

    writer.WriteString(nameof(Metric.MetricType), metric.MetricType.ToString());
    writer.WriteString(nameof(Metric.AggregationType), metric.AggregationType.ToString());

    if (!string.IsNullOrEmpty(metric.Format))
      writer.WriteString(nameof(Metric.Format), metric.Format);

    if (!string.IsNullOrEmpty(metric.Unit))
      writer.WriteString(nameof(Metric.Unit), metric.Unit);

    if (metric is DistinctCountMetric distinctCountMetric)
      writer.WriteString(nameof(DistinctCountMetric.BusinessKeyDimensionKey), distinctCountMetric.BusinessKeyDimensionKey);

    WriteSemantics(writer, metric.Semantics ?? MetricSemantics.CreateDefault(metric.AggregationType));

    writer.WriteEndObject();
  }

  private static MetricSemantics ReadSemantics(JsonElement root, AggregationType aggregationType)
  {
    var semantics = MetricSemantics.CreateDefault(aggregationType);

    if (!root.TryGetProperty(nameof(Metric.Semantics), out var semanticsElement) || semanticsElement.ValueKind == JsonValueKind.Null)
      return semantics;

    if (semanticsElement.TryGetProperty(nameof(MetricSemantics.Additivity), out var additivityElement) &&
        Enum.TryParse(additivityElement.GetString(), ignoreCase: true, out MetricAdditivity additivity))
    {
      semantics.Additivity = additivity;
    }

    if (semanticsElement.TryGetProperty(nameof(MetricSemantics.SemiAdditive), out var semiAdditiveElement) &&
        semiAdditiveElement.ValueKind != JsonValueKind.Null)
    {
      var policy = new SemiAdditiveMetricPolicy();

      if (semiAdditiveElement.TryGetProperty(nameof(SemiAdditiveMetricPolicy.TimeDimensionKey), out var timeDimensionKeyElement))
        policy.TimeDimensionKey = timeDimensionKeyElement.GetString();

      if (semiAdditiveElement.TryGetProperty(nameof(SemiAdditiveMetricPolicy.Aggregation), out var semiAdditiveAggregationElement) &&
          Enum.TryParse(semiAdditiveAggregationElement.GetString(), ignoreCase: true, out SemiAdditiveAggregationType semiAdditiveAggregation))
      {
        policy.Aggregation = semiAdditiveAggregation;
      }

      semantics.SemiAdditive = policy;
    }
    else
    {
      semantics.SemiAdditive = null;
    }

    return semantics;
  }

  private static void WriteSemantics(Utf8JsonWriter writer, MetricSemantics semantics)
  {
    writer.WritePropertyName(nameof(Metric.Semantics));
    writer.WriteStartObject();
    writer.WriteString(nameof(MetricSemantics.Additivity), semantics.Additivity.ToString());

    if (semantics.SemiAdditive != null)
    {
      writer.WritePropertyName(nameof(MetricSemantics.SemiAdditive));
      writer.WriteStartObject();
      writer.WriteString(nameof(SemiAdditiveMetricPolicy.TimeDimensionKey), semantics.SemiAdditive.TimeDimensionKey);
      writer.WriteString(nameof(SemiAdditiveMetricPolicy.Aggregation), semantics.SemiAdditive.Aggregation.ToString());
      writer.WriteEndObject();
    }

    writer.WriteEndObject();
  }

  private static string GetRequiredString(JsonElement root, string propertyName)
  {
    if (!root.TryGetProperty(propertyName, out var property) || string.IsNullOrEmpty(property.GetString()))
      throw new JsonException($"Metric must have a {propertyName}.");

    return property.GetString()!;
  }

  private static string GetOptionalString(JsonElement root, string propertyName)
  {
    return root.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
  }
}
