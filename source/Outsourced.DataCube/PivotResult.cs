namespace Outsourced.DataCube;

/// <summary>
/// Represents a two-axis pivot result with explicit rows, columns, and populated cells.
/// </summary>
public sealed class PivotResult<T>
  where T : struct
{
  private readonly Dictionary<string, T> _cellValues = new(StringComparer.Ordinal);

  /// <summary>
  /// Gets the ordered row coordinates.
  /// </summary>
  public IReadOnlyList<DimensionCoordinate> Rows { get; }

  /// <summary>
  /// Gets the ordered column coordinates.
  /// </summary>
  public IReadOnlyList<DimensionCoordinate> Columns { get; }

  /// <summary>
  /// Gets the populated cells in row-major order.
  /// </summary>
  public IReadOnlyList<PivotCell<T>> Cells { get; }

  /// <summary>
  /// Initializes a new pivot result.
  /// </summary>
  public PivotResult(IEnumerable<DimensionCoordinate> rows, IEnumerable<DimensionCoordinate> columns, IEnumerable<PivotCell<T>> cells)
  {
    ArgumentNullException.ThrowIfNull(rows);
    ArgumentNullException.ThrowIfNull(columns);
    ArgumentNullException.ThrowIfNull(cells);

    Rows = rows.ToArray();
    Columns = columns.ToArray();

    var cellList = cells.ToArray();
    Cells = cellList;

    foreach (var cell in cellList)
    {
      ArgumentNullException.ThrowIfNull(cell);

      if (!_cellValues.TryAdd(CreateCellKey(cell.RowKey, cell.ColumnKey), cell.Value))
        throw new ArgumentException("Duplicate pivot cells are not allowed", nameof(cells));
    }
  }

  /// <summary>
  /// Tries to get the aggregated value for a row and column coordinate.
  /// </summary>
  public bool TryGetValue(DimensionCoordinate rowKey, DimensionCoordinate columnKey, out T value)
  {
    ArgumentNullException.ThrowIfNull(rowKey);
    ArgumentNullException.ThrowIfNull(columnKey);

    return _cellValues.TryGetValue(CreateCellKey(rowKey, columnKey), out value);
  }

  /// <summary>
  /// Gets the aggregated value for a row and column coordinate, or the metric default when no cell exists.
  /// </summary>
  public T GetValueOrDefault(DimensionCoordinate rowKey, DimensionCoordinate columnKey)
  {
    return TryGetValue(rowKey, columnKey, out var value) ? value : default;
  }

  private static string CreateCellKey(DimensionCoordinate rowKey, DimensionCoordinate columnKey)
  {
    return $"{rowKey.CoordinateKey}=>{columnKey.CoordinateKey}";
  }
}
