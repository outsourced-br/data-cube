namespace Outsourced.DataCube.Metrics;

using DataCube;

/// <summary>
/// Represents a metric definition stored in an <see cref="AnalyticsCube"/>.
/// </summary>
public class Metric
{
  /// <summary>
  /// Gets the serialized/runtime type name for the metric.
  /// </summary>
  public virtual string Type => GetType().Name;

  /// <summary>
  /// Gets or sets the metric key.
  /// </summary>
  public string Key { get; set; }

  /// <summary>
  /// Gets or sets the display label for the metric.
  /// </summary>
  public string Label { get; set; }

  /// <summary>
  /// Gets or sets the metric value type.
  /// </summary>
  public MetricType MetricType { get; set; }

  /// <summary>
  /// Gets or sets the aggregation strategy associated with the metric.
  /// </summary>
  public AggregationType AggregationType { get; set; }

  /// <summary>
  /// Gets or sets the display format string for metric values.
  /// </summary>
  public string Format { get; set; }

  /// <summary>
  /// Gets or sets aggregation semantics that go beyond <see cref="AggregationType"/>.
  /// </summary>
  public MetricSemantics Semantics { get; set; }

  /// <summary>
  /// Gets or sets the unit associated with the metric.
  /// </summary>
  public string Unit { get; set; }

  /// <summary>
  /// Initializes a new empty metric definition.
  /// </summary>
  public Metric()
  {
    Semantics = MetricSemantics.CreateDefault(AggregationType);
  }

  /// <summary>
  /// Initializes a new metric definition.
  /// </summary>
  public Metric(
    string key,
    MetricType type,
    AggregationType aggregation,
    string label = null,
    string format = null,
    string unit = null)
  {
    Key = key;
    Label = label;
    MetricType = type;
    AggregationType = aggregation;
    Format = format;
    Unit = unit;
    Semantics = MetricSemantics.CreateDefault(aggregation);
  }
}

/// <summary>
/// Represents a strongly typed metric definition.
/// </summary>
/// <typeparam name="T">The CLR type used for stored metric values.</typeparam>
public abstract class Metric<T> : Metric
{
  /// <summary>
  /// Initializes a new typed metric definition.
  /// </summary>
  protected Metric(
    string key,
    MetricType type,
    AggregationType aggregation,
    string label = null,
    string format = null,
    string unit = null)
    : base(key, type, aggregation, label, format) { }

  /// <summary>
  /// Aggregates a sequence of metric values into a single result.
  /// </summary>
  public abstract T Aggregate(IEnumerable<T> values);

  /// <summary>
  /// Formats a metric value for display.
  /// </summary>
  public abstract string FormatValue(T value);
}

