using Outsourced.DataCube.Hierarchy;

namespace Outsourced.DataCube;

/// <summary>
/// Represents an aggregated metric over an ordered time window anchored to a hierarchy member.
/// </summary>
public sealed class TimeWindowMetricResult<T>
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
  /// Gets the hierarchy level represented by the ordered window members.
  /// </summary>
  public HierarchyLevel Level { get; }

  /// <summary>
  /// Gets the member used to anchor the time window.
  /// </summary>
  public DimensionValue AnchorMember { get; }

  /// <summary>
  /// Gets the ordered members that make up the time window.
  /// </summary>
  public IReadOnlyList<DimensionValue> Members { get; }

  /// <summary>
  /// Gets the first member in the ordered window.
  /// </summary>
  public DimensionValue StartMember => Members[0];

  /// <summary>
  /// Gets the last member in the ordered window.
  /// </summary>
  public DimensionValue EndMember => Members[^1];

  /// <summary>
  /// Gets the aggregated metric value across the window.
  /// </summary>
  public T Value { get; }

  /// <summary>
  /// Gets a value indicating whether the anchored window stops before the end of its containing period.
  /// </summary>
  public bool IsPartial { get; }

  /// <summary>
  /// Initializes a new time-window result.
  /// </summary>
  public TimeWindowMetricResult(
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    HierarchyLevel level,
    DimensionValue anchorMember,
    IEnumerable<DimensionValue> members,
    T value,
    bool isPartial)
  {
    ArgumentNullException.ThrowIfNull(dimension);
    ArgumentNullException.ThrowIfNull(hierarchy);
    ArgumentNullException.ThrowIfNull(level);
    ArgumentNullException.ThrowIfNull(anchorMember);
    ArgumentNullException.ThrowIfNull(members);

    var orderedMembers = members
      .Where(static member => member != null)
      .ToArray();

    if (orderedMembers.Length == 0)
      throw new ArgumentException("Time windows must include at least one member.", nameof(members));

    Dimension = dimension;
    Hierarchy = hierarchy;
    Level = level;
    AnchorMember = anchorMember;
    Members = orderedMembers;
    Value = value;
    IsPartial = isPartial;
  }
}
