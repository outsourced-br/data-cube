namespace Outsourced.DataCube.Metrics;

using System.Collections.Generic;

/// <summary>
/// Represents a collection of metric values for a single metric value type.
/// </summary>
public interface IMetricCollection
{
  /// <summary>
  /// Gets the metric value type stored by this collection.
  /// </summary>
  MetricType Type { get; }

  /// <summary>
  /// Returns the metric keys currently stored in the collection.
  /// </summary>
  IEnumerable<string> GetMetricKeys();
}

