namespace Outsourced.DataCube.Metrics;

/// <summary>
/// Describes how a calculated metric should behave when one or more dependencies have no aggregated value.
/// </summary>
public enum CalculatedMetricMissingValueBehavior
{
  /// <summary>
  /// Returns the caller-supplied default value.
  /// </summary>
  ReturnDefault,

  /// <summary>
  /// Throws an <see cref="InvalidOperationException"/>.
  /// </summary>
  Throw
}
