namespace Outsourced.DataCube.Metrics;

/// <summary>
/// Describes how a calculated metric should behave when a denominator resolves to zero.
/// </summary>
public enum CalculatedMetricDivideByZeroBehavior
{
  /// <summary>
  /// Returns the caller-supplied default value.
  /// </summary>
  ReturnDefault,

  /// <summary>
  /// Throws a <see cref="DivideByZeroException"/>.
  /// </summary>
  Throw
}
