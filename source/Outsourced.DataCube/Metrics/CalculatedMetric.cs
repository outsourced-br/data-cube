namespace Outsourced.DataCube.Metrics;

using System.Globalization;

/// <summary>
/// Represents a metric whose value is derived from already-aggregated dependency metrics.
/// </summary>
public sealed class CalculatedMetric<T> : Metric<T>
  where T : struct
{
  private readonly IReadOnlyList<Metric> _dependencies;

  /// <summary>
  /// Gets the dependent metrics that must be aggregated before this metric is evaluated.
  /// </summary>
  public IReadOnlyList<Metric> Dependencies => _dependencies;

  /// <summary>
  /// Gets the calculation that derives the final metric value from aggregated dependency values.
  /// </summary>
  public Func<CalculatedMetricContext, T> Calculation { get; }

  /// <summary>
  /// Initializes a new calculated metric.
  /// </summary>
  public CalculatedMetric(
    string key,
    IEnumerable<Metric> dependencies,
    Func<CalculatedMetricContext, T> calculation,
    string label = null,
    string format = null,
    string unit = null)
    : base(key, MetricTypeHelper.GetMetricType<T>(), AggregationType.Calculated, label, format, unit)
  {
    ArgumentException.ThrowIfNullOrEmpty(key);
    ArgumentNullException.ThrowIfNull(dependencies);
    ArgumentNullException.ThrowIfNull(calculation);

    var dependencyList = dependencies.ToArray();
    if (dependencyList.Length == 0)
      throw new ArgumentException("At least one dependency metric is required.", nameof(dependencies));

    var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var dependency in dependencyList)
    {
      ArgumentNullException.ThrowIfNull(dependency, nameof(dependencies));

      if (string.Equals(dependency.Key, key, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("A calculated metric cannot depend on itself.", nameof(dependencies));

      if (!seenKeys.Add(dependency.Key))
        throw new ArgumentException($"Dependency '{dependency.Key}' can only be used once.", nameof(dependencies));
    }

    _dependencies = dependencyList;
    Calculation = calculation;
  }

  /// <inheritdoc />
  public override T Aggregate(IEnumerable<T> values)
  {
    throw new NotSupportedException(
      $"Calculated metric '{Key}' must be evaluated from aggregated dependency values instead of raw fact values.");
  }

  /// <inheritdoc />
  public override string FormatValue(T value)
  {
    return value is IFormattable formattable
      ? formattable.ToString(Format, CultureInfo.InvariantCulture)
      : string.Create(CultureInfo.InvariantCulture, $"{value}");
  }

  internal T Evaluate(CalculatedMetricContext context)
  {
    ArgumentNullException.ThrowIfNull(context);
    return Calculation(context);
  }
}
