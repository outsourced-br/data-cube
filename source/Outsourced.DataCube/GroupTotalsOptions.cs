namespace Outsourced.DataCube;

/// <summary>
/// Controls whether grouped results include subtotal and grand-total roll-up coordinates.
/// </summary>
public sealed class GroupTotalsOptions
{
  /// <summary>
  /// Gets or sets a value indicating whether trailing-dimension subtotals should be returned.
  /// </summary>
  public bool IncludeSubtotals { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether the all-members grand total should be returned.
  /// </summary>
  public bool IncludeGrandTotal { get; set; }
}
