namespace Outsourced.DataCube;

/// <summary>
/// Represents an aggregated metric value for a grouped coordinate.
/// </summary>
public sealed class GroupedMetricResult<T>
  where T : struct
{
  /// <summary>
  /// Gets the grouped coordinate.
  /// </summary>
  public DimensionCoordinate Key { get; }

  /// <summary>
  /// Gets the aggregated metric value for the coordinate.
  /// </summary>
  public T Value { get; }

  /// <summary>
  /// Gets the roll-up kind represented by this grouped result.
  /// </summary>
  public RollupKind Kind => Key.Kind;

  /// <summary>
  /// Gets a value indicating whether this result represents a subtotal or grand total.
  /// </summary>
  public bool IsTotal => Kind is not RollupKind.Leaf;

  /// <summary>
  /// Initializes a new grouped metric result.
  /// </summary>
  public GroupedMetricResult(DimensionCoordinate key, T value)
  {
    ArgumentNullException.ThrowIfNull(key);

    Key = key;
    Value = value;
  }
}
