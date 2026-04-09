namespace Outsourced.DataCube.WellKnown;

using Outsourced.DataCube.Metrics;

/// <summary>
/// Provides reusable, preconfigured metric definitions.
/// </summary>
public static class Metrics
{
  /// <summary>
  /// Common currency metrics.
  /// </summary>
  public static class CurrencyMetrics
  {
    public static CurrencyMetric AmountEuro => new(nameof(AmountEuro), "Total transaction amount", "EUR");
    public static CurrencyMetric AmountDollar => new(nameof(AmountDollar), "Total transaction amount", "USD");
  }

  /// <summary>
  /// Common count-based metrics.
  /// </summary>
  public static class CountMetrics
  {
    public static CountMetric Count => new(nameof(Count), "Number of entries");
    public static PercentageMetric Percentage => new(nameof(Percentage), "Percentage of total records");
  }

  /// <summary>
  /// Common missing-value metrics.
  /// </summary>
  public static class MissingMetrics
  {
    public static MissingMetric MissingCount => new(nameof(MissingCount), "Number of missing entries");
    public static PercentageMetric MissingPercentage => new(nameof(MissingPercentage), "Percentage of total records that are missing");
  }

  /// <summary>
  /// Common duplicate-analysis metrics.
  /// </summary>
  public static class DuplicateMetrics
  {
    public static DuplicateMetric DuplicateCount => new(nameof(DuplicateCount), "Number of duplicate entries");
    public static PercentageMetric DuplicatePercentage => new(nameof(DuplicatePercentage), "Percentage of total records that are duplicates");
  }

  /// <summary>
  /// Common uniqueness metrics.
  /// </summary>
  public static class UniquenessMetrics
  {
    public static UniqueMetric UniqueCount => new(nameof(UniqueCount), "Number of unique entries");
    public static PercentageMetric Uniqueness => new(nameof(Uniqueness), "Percentage of unique entries");
  }

  /// <summary>
  /// Example vehicle-related metrics.
  /// </summary>
  public static class VehicleMetrics
  {
    public static CurrencyMetric ListPrice => new(nameof(ListPrice));
    public static CurrencyMetric Value => new(nameof(Value));
  }

  /// <summary>
  /// Example policy-related metrics.
  /// </summary>
  public static class PolicyMetrics
  {
    public static CurrencyMetric AnnualPremium => new(nameof(AnnualPremium));
    public static CurrencyMetric InsuredAmount => new(nameof(InsuredAmount));
  }
}


