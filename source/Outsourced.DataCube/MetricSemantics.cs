namespace Outsourced.DataCube;

/// <summary>
/// Describes whether a metric can be aggregated across all dimensions, only across non-time dimensions, or not at all.
/// </summary>
public enum MetricAdditivity
{
  /// <summary>
  /// The metric can be aggregated across every dimension using its configured <see cref="AggregationType"/>.
  /// </summary>
  Additive,

  /// <summary>
  /// The metric can be aggregated across non-time dimensions, but time rollups require a dedicated policy.
  /// </summary>
  SemiAdditive,

  /// <summary>
  /// The metric should not be rolled up across multiple fact groups.
  /// </summary>
  NonAdditive,
}

/// <summary>
/// Describes how a semi-additive metric should choose a value across time.
/// </summary>
public enum SemiAdditiveAggregationType
{
  /// <summary>
  /// Uses the value from the latest available time member.
  /// </summary>
  LastValue,

  /// <summary>
  /// Uses the latest time member that still contains a metric value.
  /// </summary>
  LastNonEmpty,
}

/// <summary>
/// Configures how a semi-additive metric behaves across a time dimension.
/// </summary>
public sealed class SemiAdditiveMetricPolicy
{
  /// <summary>
  /// Gets or sets the dimension key used as the time axis.
  /// </summary>
  public string TimeDimensionKey { get; set; }

  /// <summary>
  /// Gets or sets the policy used to select the time-based snapshot value.
  /// </summary>
  public SemiAdditiveAggregationType Aggregation { get; set; } = SemiAdditiveAggregationType.LastValue;

  /// <summary>
  /// Initializes an empty policy.
  /// </summary>
  public SemiAdditiveMetricPolicy() { }

  /// <summary>
  /// Initializes a policy for the supplied time dimension key.
  /// </summary>
  public SemiAdditiveMetricPolicy(
    string timeDimensionKey,
    SemiAdditiveAggregationType aggregation = SemiAdditiveAggregationType.LastValue)
  {
    ArgumentException.ThrowIfNullOrEmpty(timeDimensionKey);

    TimeDimensionKey = timeDimensionKey;
    Aggregation = aggregation;
  }

  internal SemiAdditiveMetricPolicy Clone()
  {
    return new SemiAdditiveMetricPolicy(TimeDimensionKey, Aggregation);
  }
}

/// <summary>
/// Captures aggregation semantics that go beyond <see cref="AggregationType"/>.
/// </summary>
public sealed class MetricSemantics
{
  /// <summary>
  /// Gets or sets the metric additivity classification.
  /// </summary>
  public MetricAdditivity Additivity { get; set; } = MetricAdditivity.Additive;

  /// <summary>
  /// Gets or sets the semi-additive policy when <see cref="Additivity"/> is <see cref="MetricAdditivity.SemiAdditive"/>.
  /// </summary>
  public SemiAdditiveMetricPolicy SemiAdditive { get; set; }

  /// <summary>
  /// Creates semantics for a fully aggregatable metric.
  /// </summary>
  public static MetricSemantics CreateAdditive()
  {
    return new MetricSemantics
    {
      Additivity = MetricAdditivity.Additive,
    };
  }

  /// <summary>
  /// Creates semantics for a metric that should not be rolled up across multiple fact groups.
  /// </summary>
  public static MetricSemantics CreateNonAdditive()
  {
    return new MetricSemantics
    {
      Additivity = MetricAdditivity.NonAdditive,
    };
  }

  /// <summary>
  /// Creates semantics for a metric that rolls up across time by selecting a snapshot value.
  /// </summary>
  public static MetricSemantics CreateSemiAdditive(
    string timeDimensionKey,
    SemiAdditiveAggregationType aggregation = SemiAdditiveAggregationType.LastValue)
  {
    return new MetricSemantics
    {
      Additivity = MetricAdditivity.SemiAdditive,
      SemiAdditive = new SemiAdditiveMetricPolicy(timeDimensionKey, aggregation),
    };
  }

  public static MetricSemantics CreateDefault(AggregationType aggregationType)
  {
    return aggregationType switch
    {
      AggregationType.Percentage => CreateNonAdditive(),
      AggregationType.Ratio => CreateNonAdditive(),
      AggregationType.Unique => CreateNonAdditive(),
      AggregationType.DistinctCount => CreateAdditive(),
      _ => CreateAdditive(),
    };
  }

  internal MetricSemantics Clone()
  {
    return new MetricSemantics
    {
      Additivity = Additivity,
      SemiAdditive = SemiAdditive?.Clone(),
    };
  }

  internal void Validate(string metricKey)
  {
    if (Additivity == MetricAdditivity.SemiAdditive)
    {
      if (SemiAdditive == null || string.IsNullOrWhiteSpace(SemiAdditive.TimeDimensionKey))
      {
        throw new InvalidOperationException(
          $"Metric '{metricKey}' is semi-additive and must declare a time dimension key.");
      }

      return;
    }

    if (SemiAdditive != null)
    {
      throw new InvalidOperationException(
        $"Metric '{metricKey}' cannot define a semi-additive policy when additivity is '{Additivity}'.");
    }
  }
}
