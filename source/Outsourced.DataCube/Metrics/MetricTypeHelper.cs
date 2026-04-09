namespace Outsourced.DataCube.Metrics;

/// <summary>
/// Maps between CLR value types and <see cref="MetricType"/> values.
/// </summary>
public static class MetricTypeHelper
{
  /// <summary>
  /// Gets the <see cref="MetricType"/> corresponding to <typeparamref name="T"/>.
  /// </summary>
  public static MetricType GetMetricType<T>() where T : struct
  {
    return typeof(T) switch
    {
      { } t when t == typeof(int) => MetricType.Int,
      { } t when t == typeof(long) => MetricType.Long,
      { } t when t == typeof(float) => MetricType.Float,
      { } t when t == typeof(double) => MetricType.Double,
      { } t when t == typeof(decimal) => MetricType.Decimal,
      _ => throw new NotSupportedException($"Unsupported metric type: {typeof(T).Name}"),
    };
  }

  /// <summary>
  /// Gets the CLR type represented by a <see cref="MetricType"/>.
  /// </summary>
  public static Type GetTypeFromMetricType(MetricType metricType)
  {
    return metricType switch
    {
      MetricType.Int => typeof(int),
      MetricType.Long => typeof(long),
      MetricType.Float => typeof(float),
      MetricType.Double => typeof(double),
      MetricType.Decimal => typeof(decimal),
      _ => throw new ArgumentException($"Unsupported MetricType: {metricType}", nameof(metricType))
    };
  }
}

