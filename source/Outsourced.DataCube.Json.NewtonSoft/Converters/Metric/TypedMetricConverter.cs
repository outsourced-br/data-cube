namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using Internal;
using Metrics;

public class TypedMetricConverter<T> : JsonConverter<Metric<T>> where T : struct
{
  public override Metric<T> ReadJson(JsonReader reader, Type objectType, Metric<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var jsonObject = JObject.Load(reader);

    var key = jsonObject[nameof(Metric.Key)]?.ToString();
    var label = jsonObject[nameof(Metric.Label)]?.ToString();
    var format = jsonObject[nameof(Metric.Format)]?.ToString();
    var unit = jsonObject[nameof(Metric.Unit)]?.ToString();
    var businessKeyDimensionKey = jsonObject[nameof(DistinctCountMetric.BusinessKeyDimensionKey)]?.ToString();

    var metricType = MetricType.Unknown;
    if (jsonObject.TryGetValue(nameof(Metric.MetricType), out var metricTypeToken))
      Enum.TryParse(metricTypeToken.ToString(), true, out metricType);

    var aggregationType = AggregationType.Count;
    if (jsonObject.TryGetValue(nameof(Metric.AggregationType), out var aggTypeToken))
      Enum.TryParse(aggTypeToken.ToString(), true, out aggregationType);

    if (string.IsNullOrEmpty(key))
      throw new JsonException("Metric must have a Key.");

    Metric<T> result;

    // Create appropriate metric based on type and aggregation
    if (typeof(T) == typeof(int) && aggregationType == AggregationType.Count)
      result = new CountMetric(key, label) as Metric<T>;
    else if (typeof(T) == typeof(int) && aggregationType == AggregationType.DistinctCount)
      result = new DistinctCountMetric(key, businessKeyDimensionKey, label) as Metric<T>;
    else if (typeof(T) == typeof(double) && aggregationType == AggregationType.Percentage)
      result = new PercentageMetric(key, label) as Metric<T>;
    else if (typeof(T) == typeof(double) && aggregationType == AggregationType.Average)
      result = new AverageMetric(key, label) as Metric<T>;
    else if (typeof(T) == typeof(decimal) && aggregationType == AggregationType.Currency)
      result = new CurrencyMetric(key, label, unit) as Metric<T>;
    else
      result = new SerializationMetric<T>(key, metricType, aggregationType, label, format, unit);

    result.Semantics = ReadSemantics(jsonObject, aggregationType);
    return result;
  }

  public override void WriteJson(JsonWriter writer, Metric<T> value, JsonSerializer serializer)
  {
    writer.WriteStartObject();

    // Add type discriminator for polymorphic deserialization
    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(value.GetType()));

    writer.WritePropertyName(nameof(Metric.Key));
    writer.WriteValue(value.Key);

    if (!string.IsNullOrEmpty(value.Label))
    {
      writer.WritePropertyName(nameof(Metric.Label));
      writer.WriteValue(value.Label);
    }

    writer.WritePropertyName(nameof(Metric.MetricType));
    writer.WriteValue(value.MetricType.ToString());

    writer.WritePropertyName(nameof(Metric.AggregationType));
    writer.WriteValue(value.AggregationType.ToString());

    if (!string.IsNullOrEmpty(value.Format))
    {
      writer.WritePropertyName(nameof(Metric.Format));
      writer.WriteValue(value.Format);
    }

    if (!string.IsNullOrEmpty(value.Unit))
    {
      writer.WritePropertyName(nameof(Metric.Unit));
      writer.WriteValue(value.Unit);
    }

    if (value is DistinctCountMetric distinctCountMetric)
    {
      writer.WritePropertyName(nameof(DistinctCountMetric.BusinessKeyDimensionKey));
      writer.WriteValue(distinctCountMetric.BusinessKeyDimensionKey);
    }

    WriteSemantics(writer, value.Semantics ?? MetricSemantics.CreateDefault(value.AggregationType));

    writer.WriteEndObject();
  }

  private static MetricSemantics ReadSemantics(JObject jsonObject, AggregationType aggregationType)
  {
    var semantics = MetricSemantics.CreateDefault(aggregationType);

    if (!jsonObject.TryGetValue(nameof(Metric.Semantics), out var semanticsToken) || semanticsToken.Type == JTokenType.Null)
      return semantics;

    if (semanticsToken[nameof(MetricSemantics.Additivity)] != null &&
        Enum.TryParse(semanticsToken[nameof(MetricSemantics.Additivity)]!.ToString(), ignoreCase: true, out MetricAdditivity additivity))
    {
      semantics.Additivity = additivity;
    }

    var semiAdditiveToken = semanticsToken[nameof(MetricSemantics.SemiAdditive)];
    if (semiAdditiveToken != null && semiAdditiveToken.Type != JTokenType.Null)
    {
      var policy = new SemiAdditiveMetricPolicy
      {
        TimeDimensionKey = semiAdditiveToken[nameof(SemiAdditiveMetricPolicy.TimeDimensionKey)]?.ToString(),
      };

      if (semiAdditiveToken[nameof(SemiAdditiveMetricPolicy.Aggregation)] != null &&
          Enum.TryParse(semiAdditiveToken[nameof(SemiAdditiveMetricPolicy.Aggregation)]!.ToString(), ignoreCase: true, out SemiAdditiveAggregationType semiAdditiveAggregation))
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

  private static void WriteSemantics(JsonWriter writer, MetricSemantics semantics)
  {
    writer.WritePropertyName(nameof(Metric.Semantics));
    writer.WriteStartObject();
    writer.WritePropertyName(nameof(MetricSemantics.Additivity));
    writer.WriteValue(semantics.Additivity.ToString());

    if (semantics.SemiAdditive != null)
    {
      writer.WritePropertyName(nameof(MetricSemantics.SemiAdditive));
      writer.WriteStartObject();
      writer.WritePropertyName(nameof(SemiAdditiveMetricPolicy.TimeDimensionKey));
      writer.WriteValue(semantics.SemiAdditive.TimeDimensionKey);
      writer.WritePropertyName(nameof(SemiAdditiveMetricPolicy.Aggregation));
      writer.WriteValue(semantics.SemiAdditive.Aggregation.ToString());
      writer.WriteEndObject();
    }

    writer.WriteEndObject();
  }

  // A minimal TypedMetric implementation for deserialization
  private sealed class SerializationMetric<TValue> : Metric<TValue> where TValue : struct
  {
    public SerializationMetric(string key, MetricType type, AggregationType aggregation, string label = null, string format = null, string unit = null)
      : base(key, type, aggregation, label, format, unit)
    {
    }

    public override TValue Aggregate(IEnumerable<TValue> values)
    {
      return values?.Any() == true ? values.First() : default;
    }

    public override string FormatValue(TValue value)
    {
      return value.ToString();
    }
  }
}
