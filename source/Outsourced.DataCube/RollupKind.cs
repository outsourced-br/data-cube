namespace Outsourced.DataCube;

/// <summary>
/// Describes whether a grouped coordinate or pivot cell is a leaf, subtotal, or grand total.
/// </summary>
public enum RollupKind
{
  /// <summary>
  /// A leaf-level value with no total members.
  /// </summary>
  Leaf,

  /// <summary>
  /// A subtotal value that contains one or more total members but is not the all-members grand total.
  /// </summary>
  Subtotal,

  /// <summary>
  /// The all-members grand total.
  /// </summary>
  GrandTotal
}
