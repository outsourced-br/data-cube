namespace Outsourced.DataCube;

/// <summary>
/// Represents a comparison between a current time period and a prior period.
/// </summary>
public sealed class TimePeriodComparisonResult<T>
  where T : struct
{
  /// <summary>
  /// Gets the current time period result.
  /// </summary>
  public TimeWindowMetricResult<T> CurrentPeriod { get; }

  /// <summary>
  /// Gets the previous or comparable time period result, when available.
  /// </summary>
  public TimeWindowMetricResult<T> PreviousPeriod { get; }

  /// <summary>
  /// Gets the arithmetic difference between the current and previous period, when both are available.
  /// </summary>
  public T? Delta { get; }

  /// <summary>
  /// Gets the percent change expressed as a ratio, where 0.10 means 10%.
  /// </summary>
  public decimal? PercentChange { get; }

  /// <summary>
  /// Initializes a new period-comparison result.
  /// </summary>
  public TimePeriodComparisonResult(
    TimeWindowMetricResult<T> currentPeriod,
    TimeWindowMetricResult<T> previousPeriod,
    T? delta,
    decimal? percentChange)
  {
    ArgumentNullException.ThrowIfNull(currentPeriod);

    CurrentPeriod = currentPeriod;
    PreviousPeriod = previousPeriod;
    Delta = delta;
    PercentChange = percentChange;
  }
}
