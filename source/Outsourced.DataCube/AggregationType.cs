namespace Outsourced.DataCube;

/// <summary>
/// Describes how a metric should be aggregated across multiple fact groups.
/// </summary>
public enum AggregationType
{
  /// <summary>
  /// Counts non-null and non-empty values.
  /// </summary>
  Count,              // Count of the number of values (non-null and non-empty values)

  /// <summary>
  /// Counts distinct values.
  /// </summary>
  Unique,             // Count of the distinct number of values

  /// <summary>
  /// Counts distinct business keys at query time.
  /// </summary>
  DistinctCount,      // Query-time distinct count over contributing fact groups

  /// <summary>
  /// Counts duplicate values.
  /// </summary>
  Duplicate,          // Count of the duplicate number of values

  /// <summary>
  /// Counts missing values.
  /// </summary>
  Missing,            // Count of the missing (negative) number of items (non-null and non-empty values)

  /// <summary>
  /// Sums numeric values.
  /// </summary>
  Sum,                // Sum of the numeric value

  /// <summary>
  /// Computes the arithmetic mean.
  /// </summary>
  Average,            // Values that should be averaged, not summed (arimethic mean)

  /// <summary>
  /// Selects the minimum value.
  /// </summary>
  Min,                // The minimum of all values

  /// <summary>
  /// Selects the maximum value.
  /// </summary>
  Max,                // The maximum of all values

  /// <summary>
  /// Multiplies all values together.
  /// </summary>
  Product,            // Multiplication of all values

  /// <summary>
  /// Represents a percentage value.
  /// </summary>
  Percentage,         // Values from 0-100

  /// <summary>
  /// Computes a specific percentile.
  /// </summary>
  Percentile,         // Specific percentile

  /// <summary>
  /// Aggregates ratio values.
  /// </summary>
  Ratio,              // Ratios that need special aggregation

  /// <summary>
  /// Computes the median value.
  /// </summary>
  Median,             // Median of all values

  /// <summary>
  /// Returns the most frequently occurring value.
  /// </summary>
  Mode,               // Most frequently occuring value

  /// <summary>
  /// Computes population variance.
  /// </summary>
  VariancePopulation, // Variance for all values

  /// <summary>
  /// Computes sample variance.
  /// </summary>
  VarianceSample,     // Variance for a subset of all values

  /// <summary>
  /// Computes population standard deviation.
  /// </summary>
  StdDevPopulation,   // Standard deviation for all values

  /// <summary>
  /// Computes sample standard deviation.
  /// </summary>
  StdDevSample,       // Standard deviation for a subset of all values

  /// <summary>
  /// Selects the first ordered value.
  /// </summary>
  First,              // First value of all values in an ordered fashion

  /// <summary>
  /// Selects the last ordered value.
  /// </summary>
  Last,               // Last value of all values in an ordered fashion

  /// <summary>
  /// Computes a cumulative sum.
  /// </summary>
  CumSum,             // Cumulative or running sum over all values

  /// <summary>
  /// Computes a running average.
  /// </summary>
  RunAvg,             // Running or moving average over all values

  /// <summary>
  /// Computes the difference between maximum and minimum values.
  /// </summary>
  Range,              // Range of all values (diff max to min of all values)

  /// <summary>
  /// Concatenates values.
  /// </summary>
  Concat,             // Concatenation of all values

  /// <summary>
  /// Represents monetary values.
  /// </summary>
  Currency,           // Monetary values

  /// <summary>
  /// Computes a value from already-aggregated dependent metrics.
  /// </summary>
  Calculated,         // Post-aggregation derived metric

  /// <summary>
  /// Uses caller-supplied custom aggregation logic.
  /// </summary>
  Custom,             // Custom aggregation logic required
}
