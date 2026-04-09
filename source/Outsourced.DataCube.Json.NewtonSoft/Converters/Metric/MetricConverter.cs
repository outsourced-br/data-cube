namespace Outsourced.DataCube.Json.NewtonSoft.Converters;

using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using DataCube;
using Internal;
using Metrics;

public class MetricConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return typeof(Metric).IsAssignableFrom(objectType);
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
  {
    var jsonObject = JObject.Load(reader);

    var typeName = jsonObject["$type"]?.ToString() ?? string.Empty;
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

    Metric result = null;

    // Handle specific metric types
    if (!string.IsNullOrEmpty(typeName))
    {
      if (typeName.Contains(nameof(DistinctCountMetric))) result = new DistinctCountMetric(key, businessKeyDimensionKey, label);
      else if (typeName.Contains(nameof(CountMetric))) result = new CountMetric(key, label);
      else if (typeName.Contains(nameof(PercentageMetric))) result = new PercentageMetric(key, label);
      else if (typeName.Contains(nameof(AverageMetric))) result = new AverageMetric(key, label);
      else if (typeName.Contains(nameof(CurrencyMetric))) result = new CurrencyMetric(key, label, unit);
      else if (typeName.Contains(nameof(UniqueMetric))) result = new UniqueMetric(key, label);
      else if (typeName.Contains(nameof(DuplicateMetric))) result = new DuplicateMetric(key, label);
      else if (typeName.Contains(nameof(MissingMetric))) result = new MissingMetric(key, label);
      else if (typeName.Contains("CustomMetric`1"))
      {
        try
        {
          var runtimeType = TypeResolutionCache.Resolve(typeName, typeof(Metric));
          if (runtimeType != null)
          {
            result = (Metric)Activator.CreateInstance(runtimeType, key, metricType, aggregationType, label, format, unit);

            // Set default AggregateFunc for CustomMetric<T>
            var aggregateFuncProp = runtimeType.GetProperty("AggregateFunc");
            if (aggregateFuncProp != null)
            {
              var genericType = runtimeType.GetGenericArguments()[0];
              var defaultFunc = GetDefaultAggregateFunc(genericType, aggregationType);
              if (defaultFunc != null)
              {
                aggregateFuncProp.SetValue(result, defaultFunc);
              }
            }
          }
        }
        catch { }
      }
    }

    if (result == null)
    {
      result = aggregationType == AggregationType.DistinctCount
        ? new DistinctCountMetric(key, businessKeyDimensionKey, label)
        : new Metric(key, metricType, aggregationType, label, format, unit);
    }
    else
    {
      if (format != null) result.Format = format;
      if (unit != null) result.Unit = unit;
    }

    result.Semantics = ReadSemantics(jsonObject, aggregationType);
    return result;
  }

  private object GetDefaultAggregateFunc(Type type, AggregationType aggregationType)
  {
    if (type == typeof(decimal))
    {
      if (aggregationType == AggregationType.Currency || aggregationType == AggregationType.Count || aggregationType == AggregationType.Sum)
        return (Func<IEnumerable<decimal>, decimal>)(values => values.Sum());
      if (aggregationType == AggregationType.Average)
        return (Func<IEnumerable<decimal>, decimal>)(values => values.Any() ? values.Average() : 0m);
    }
    else if (type == typeof(double))
    {
      if (aggregationType == AggregationType.Average || aggregationType == AggregationType.Percentage)
        return (Func<IEnumerable<double>, double>)(values => values.Any() ? values.Average() : 0.0);
      if (aggregationType == AggregationType.Sum || aggregationType == AggregationType.Count)
        return (Func<IEnumerable<double>, double>)(values => values.Sum());
    }
    else if (type == typeof(int))
    {
      return (Func<IEnumerable<int>, int>)(values => values.Sum());
    }

    return null;
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    var metric = (Metric)value;
    writer.WriteStartObject();
    writer.WritePropertyName("$type");
    writer.WriteValue(TypeResolutionCache.GetTypeName(metric.GetType()));
    writer.WritePropertyName(nameof(Metric.Key));
    writer.WriteValue(metric.Key);

    if (!string.IsNullOrEmpty(metric.Label))
    {
      writer.WritePropertyName(nameof(Metric.Label));
      writer.WriteValue(metric.Label);
    }

    writer.WritePropertyName(nameof(Metric.MetricType));
    writer.WriteValue(metric.MetricType.ToString());

    writer.WritePropertyName(nameof(Metric.AggregationType));
    writer.WriteValue(metric.AggregationType.ToString());

    if (!string.IsNullOrEmpty(metric.Format))
    {
      writer.WritePropertyName(nameof(Metric.Format));
      writer.WriteValue(metric.Format);
    }

    if (!string.IsNullOrEmpty(metric.Unit))
    {
      writer.WritePropertyName(nameof(Metric.Unit));
      writer.WriteValue(metric.Unit);
    }

    if (metric is DistinctCountMetric distinctCountMetric)
    {
      writer.WritePropertyName(nameof(DistinctCountMetric.BusinessKeyDimensionKey));
      writer.WriteValue(distinctCountMetric.BusinessKeyDimensionKey);
    }

    WriteSemantics(writer, metric.Semantics ?? MetricSemantics.CreateDefault(metric.AggregationType));

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
}
