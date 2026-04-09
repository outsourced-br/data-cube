namespace Outsourced.DataCube;

/// <summary>
/// Controls which subtotal and total coordinates are returned from a two-axis pivot.
/// </summary>
public sealed class PivotTotalsOptions
{
  /// <summary>
  /// Gets or sets a value indicating whether subtotal rows should be returned for multi-dimensional row axes.
  /// </summary>
  public bool IncludeRowSubtotals { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether subtotal columns should be returned for multi-dimensional column axes.
  /// </summary>
  public bool IncludeColumnSubtotals { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether each row should include an all-columns total.
  /// </summary>
  public bool IncludeRowTotals { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether each column should include an all-rows total.
  /// </summary>
  public bool IncludeColumnTotals { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether the all-rows/all-columns grand total should be returned.
  /// </summary>
  public bool IncludeGrandTotal { get; set; }
}
