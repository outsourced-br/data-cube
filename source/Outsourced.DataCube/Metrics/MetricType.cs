namespace Outsourced.DataCube.Metrics;

/// <summary>
/// Identifies the CLR value type used by a metric collection.
/// </summary>
public enum MetricType
{
  /// <summary>
  /// No supported metric type has been assigned.
  /// </summary>
  Unknown = 0,

  /// <summary>
  /// 32-bit signed integer values.
  /// </summary>
  Int,

  /// <summary>
  /// 64-bit signed integer values.
  /// </summary>
  Long,

  /// <summary>
  /// Single-precision floating-point values.
  /// </summary>
  Float,

  /// <summary>
  /// Double-precision floating-point values.
  /// </summary>
  Double,

  /// <summary>
  /// Decimal values.
  /// </summary>
  Decimal,

  /// <summary>
  /// String values.
  /// </summary>
  String
}
