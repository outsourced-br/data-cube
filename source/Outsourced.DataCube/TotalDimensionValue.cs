namespace Outsourced.DataCube;

/// <summary>
/// Represents an explicit all-members coordinate value used for subtotals and grand totals.
/// </summary>
public sealed class TotalDimensionValue : DimensionValue
{
  private TotalDimensionValue()
    : base(Constants.ALL_LABEL, Constants.ALL_LABEL, Constants.ALL_LABEL)
  {
  }

  /// <summary>
  /// Gets the shared all-members total value instance.
  /// </summary>
  public static TotalDimensionValue All { get; } = new();

  /// <inheritdoc />
  public override bool IsTotal => true;

  public override bool Equals(object obj) => obj is TotalDimensionValue;

  public override int GetHashCode() => typeof(TotalDimensionValue).GetHashCode();
}
