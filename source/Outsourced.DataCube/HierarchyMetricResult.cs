using Outsourced.DataCube.Hierarchy;

namespace Outsourced.DataCube;

/// <summary>
/// Represents an aggregated metric value for a specific hierarchy member.
/// </summary>
public sealed class HierarchyMetricResult<T>
  where T : struct
{
  /// <summary>
  /// Gets the dimension that owns the hierarchy.
  /// </summary>
  public Dimension Dimension { get; }

  /// <summary>
  /// Gets the hierarchy used to produce the result.
  /// </summary>
  public Hierarchy.Hierarchy Hierarchy { get; }

  /// <summary>
  /// Gets the hierarchy level represented by the result.
  /// </summary>
  public HierarchyLevel Level { get; }

  /// <summary>
  /// Gets the hierarchy member represented by the result.
  /// </summary>
  public DimensionValue Member { get; }

  /// <summary>
  /// Gets the aggregated metric value for the member.
  /// </summary>
  public T Value { get; }

  /// <summary>
  /// Initializes a new hierarchy aggregation result.
  /// </summary>
  public HierarchyMetricResult(Dimension dimension, Hierarchy.Hierarchy hierarchy, HierarchyLevel level, DimensionValue member, T value)
  {
    ArgumentNullException.ThrowIfNull(dimension);
    ArgumentNullException.ThrowIfNull(hierarchy);
    ArgumentNullException.ThrowIfNull(level);
    ArgumentNullException.ThrowIfNull(member);

    Dimension = dimension;
    Hierarchy = hierarchy;
    Level = level;
    Member = member;
    Value = value;
  }
}
