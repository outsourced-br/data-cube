namespace Outsourced.DataCube.Metrics;

using System.Globalization;

/// <summary>
/// Provides access to aggregated dependency values while evaluating a calculated metric.
/// </summary>
public sealed class CalculatedMetricContext
{
  private readonly IReadOnlyDictionary<string, AggregatedMetricValue> _values;

  internal CalculatedMetricContext(IReadOnlyDictionary<string, AggregatedMetricValue> values)
  {
    _values = values ?? throw new ArgumentNullException(nameof(values));
  }

  /// <summary>
  /// Returns <see langword="true"/> when the dependency produced an aggregated value.
  /// </summary>
  public bool HasValue(Metric metric)
  {
    ArgumentNullException.ThrowIfNull(metric);
    return _values.TryGetValue(metric.Key, out var entry) && entry.HasValue;
  }

  /// <summary>
  /// Tries to get a typed aggregated dependency value.
  /// </summary>
  public bool TryGetValue<T>(Metric<T> metric, out T value)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(metric);

    if (_values.TryGetValue(metric.Key, out var entry) && entry.HasValue && entry.Value is T typedValue)
    {
      value = typedValue;
      return true;
    }

    value = default;
    return false;
  }

  /// <summary>
  /// Returns the aggregated dependency value or the supplied default when it is missing.
  /// </summary>
  public T GetValueOrDefault<T>(Metric<T> metric, T defaultValue = default)
    where T : struct
  {
    return TryGetValue(metric, out var value) ? value : defaultValue;
  }

  /// <summary>
  /// Tries to read a dependency as a decimal value.
  /// </summary>
  public bool TryGetDecimal(Metric metric, out decimal value)
  {
    ArgumentNullException.ThrowIfNull(metric);

    if (_values.TryGetValue(metric.Key, out var entry) && entry.HasValue)
    {
      value = ConvertToDecimal(entry.Value, metric);
      return true;
    }

    value = default;
    return false;
  }

  /// <summary>
  /// Tries to read a dependency as a double value.
  /// </summary>
  public bool TryGetDouble(Metric metric, out double value)
  {
    ArgumentNullException.ThrowIfNull(metric);

    if (_values.TryGetValue(metric.Key, out var entry) && entry.HasValue)
    {
      value = ConvertToDouble(entry.Value, metric);
      return true;
    }

    value = default;
    return false;
  }

  /// <summary>
  /// Divides one aggregated dependency by another using explicit missing-value and divide-by-zero policies.
  /// </summary>
  public decimal Divide(
    Metric numerator,
    Metric denominator,
    CalculatedMetricMissingValueBehavior missingValueBehavior = CalculatedMetricMissingValueBehavior.ReturnDefault,
    CalculatedMetricDivideByZeroBehavior divideByZeroBehavior = CalculatedMetricDivideByZeroBehavior.ReturnDefault,
    decimal defaultValue = default)
  {
    ArgumentNullException.ThrowIfNull(numerator);
    ArgumentNullException.ThrowIfNull(denominator);

    var hasNumerator = TryGetDecimal(numerator, out var numeratorValue);
    var hasDenominator = TryGetDecimal(denominator, out var denominatorValue);

    if (!hasNumerator || !hasDenominator)
    {
      if (missingValueBehavior == CalculatedMetricMissingValueBehavior.Throw)
      {
        throw new InvalidOperationException(
          $"Calculated metric dependency values are missing for '{numerator.Key}' or '{denominator.Key}'.");
      }

      return defaultValue;
    }

    if (denominatorValue == 0m)
    {
      if (divideByZeroBehavior == CalculatedMetricDivideByZeroBehavior.Throw)
        throw new DivideByZeroException($"Calculated metric denominator '{denominator.Key}' resolved to zero.");

      return defaultValue;
    }

    return numeratorValue / denominatorValue;
  }

  /// <summary>
  /// Divides one aggregated dependency by another and returns the result as a double.
  /// </summary>
  public double DivideAsDouble(
    Metric numerator,
    Metric denominator,
    CalculatedMetricMissingValueBehavior missingValueBehavior = CalculatedMetricMissingValueBehavior.ReturnDefault,
    CalculatedMetricDivideByZeroBehavior divideByZeroBehavior = CalculatedMetricDivideByZeroBehavior.ReturnDefault,
    double defaultValue = default)
  {
    var decimalDefault = Convert.ToDecimal(defaultValue, CultureInfo.InvariantCulture);
    var result = Divide(numerator, denominator, missingValueBehavior, divideByZeroBehavior, decimalDefault);
    return Convert.ToDouble(result, CultureInfo.InvariantCulture);
  }

  private static decimal ConvertToDecimal(object value, Metric metric)
  {
    return metric.MetricType switch
    {
      MetricType.Int => Convert.ToDecimal((int)value, CultureInfo.InvariantCulture),
      MetricType.Long => Convert.ToDecimal((long)value, CultureInfo.InvariantCulture),
      MetricType.Float => Convert.ToDecimal((float)value, CultureInfo.InvariantCulture),
      MetricType.Double => Convert.ToDecimal((double)value, CultureInfo.InvariantCulture),
      MetricType.Decimal => (decimal)value,
      _ => throw new InvalidOperationException($"Metric '{metric.Key}' cannot be converted to decimal.")
    };
  }

  private static double ConvertToDouble(object value, Metric metric)
  {
    return metric.MetricType switch
    {
      MetricType.Int => Convert.ToDouble((int)value, CultureInfo.InvariantCulture),
      MetricType.Long => Convert.ToDouble((long)value, CultureInfo.InvariantCulture),
      MetricType.Float => Convert.ToDouble((float)value, CultureInfo.InvariantCulture),
      MetricType.Double => (double)value,
      MetricType.Decimal => Convert.ToDouble((decimal)value, CultureInfo.InvariantCulture),
      _ => throw new InvalidOperationException($"Metric '{metric.Key}' cannot be converted to double.")
    };
  }
}

internal readonly struct AggregatedMetricValue
{
  public AggregatedMetricValue(Metric metric, object value, bool hasValue)
  {
    Metric = metric ?? throw new ArgumentNullException(nameof(metric));
    Value = value;
    HasValue = hasValue;
  }

  public Metric Metric { get; }

  public object Value { get; }

  public bool HasValue { get; }
}
