namespace Outsourced.DataCube;

/// <summary>
/// Represents a populated cell in a two-axis pivot result.
/// </summary>
public sealed class PivotCell<T>
  where T : struct
{
  /// <summary>
  /// Gets the row coordinate for the cell.
  /// </summary>
  public DimensionCoordinate RowKey { get; }

  /// <summary>
  /// Gets the column coordinate for the cell.
  /// </summary>
  public DimensionCoordinate ColumnKey { get; }

  /// <summary>
  /// Gets the aggregated metric value stored in the cell.
  /// </summary>
  public T Value { get; }

  /// <summary>
  /// Gets the roll-up kind represented by this pivot cell.
  /// </summary>
  public RollupKind Kind =>
    RowKey.IsGrandTotal && ColumnKey.IsGrandTotal
      ? RollupKind.GrandTotal
      : RowKey.IsTotal || ColumnKey.IsTotal
        ? RollupKind.Subtotal
        : RollupKind.Leaf;

  /// <summary>
  /// Gets a value indicating whether this cell represents a subtotal or grand total.
  /// </summary>
  public bool IsTotal => Kind is not RollupKind.Leaf;

  /// <summary>
  /// Initializes a new pivot cell.
  /// </summary>
  public PivotCell(DimensionCoordinate rowKey, DimensionCoordinate columnKey, T value)
  {
    ArgumentNullException.ThrowIfNull(rowKey);
    ArgumentNullException.ThrowIfNull(columnKey);

    RowKey = rowKey;
    ColumnKey = columnKey;
    Value = value;
  }
}
