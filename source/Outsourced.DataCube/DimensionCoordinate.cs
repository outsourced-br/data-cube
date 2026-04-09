#nullable enable

namespace Outsourced.DataCube;

using System.Text;

/// <summary>
/// Represents an ordered set of dimension values that identify a grouped result, row, or column.
/// </summary>
public sealed class DimensionCoordinate : IEquatable<DimensionCoordinate>
{
  private const string TotalCoordinateComponentPrefix = "[Total]";
  private readonly Dictionary<string, DimensionValue> _values;

  internal string CoordinateKey { get; }
  internal int NonTotalPartCount { get; }

  /// <summary>
  /// Gets the ordered dimension parts that make up this coordinate.
  /// </summary>
  public IReadOnlyList<DimensionCoordinatePart> Parts { get; }

  /// <summary>
  /// Gets the roll-up kind represented by this coordinate.
  /// </summary>
  public RollupKind Kind { get; }

  /// <summary>
  /// Gets a value indicating whether this coordinate represents a subtotal or grand total.
  /// </summary>
  public bool IsTotal => Kind is not RollupKind.Leaf;

  /// <summary>
  /// Gets a value indicating whether this coordinate represents the all-members grand total.
  /// </summary>
  public bool IsGrandTotal => Kind == RollupKind.GrandTotal;

  /// <summary>
  /// Gets the value for a specific dimension key.
  /// </summary>
  public DimensionValue this[string dimensionKey] => _values[dimensionKey];

  /// <summary>
  /// Initializes a new coordinate from ordered dimension parts.
  /// </summary>
  public DimensionCoordinate(IEnumerable<DimensionCoordinatePart> parts)
  {
    ArgumentNullException.ThrowIfNull(parts);

    var orderedParts = parts.ToArray();
    if (orderedParts.Length == 0)
      throw new ArgumentException("At least one dimension coordinate part is required", nameof(parts));

    _values = new Dictionary<string, DimensionValue>(StringComparer.OrdinalIgnoreCase);

    foreach (var part in orderedParts)
    {
      ArgumentNullException.ThrowIfNull(part);

      if (!_values.TryAdd(part.DimensionKey, part.Value))
        throw new ArgumentException($"Duplicate dimension key '{part.DimensionKey}' is not allowed in a coordinate", nameof(parts));
    }

    Parts = orderedParts;
    NonTotalPartCount = orderedParts.Count(static part => !part.IsTotal);
    Kind = GetKind(orderedParts.Length, NonTotalPartCount);
    CoordinateKey = CreateCoordinateKey(orderedParts);
  }

  /// <summary>
  /// Tries to get the value for a specific dimension key.
  /// </summary>
  public bool TryGetValue(string dimensionKey, out DimensionValue? value)
  {
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    return _values.TryGetValue(dimensionKey, out value);
  }

  public override string ToString()
  {
    return string.Join(", ", Parts.Select(static part => $"{part.DimensionKey}={part.Value.Key}"));
  }

  public bool Equals(DimensionCoordinate? other)
  {
    return
      other is not null &&
      string.Equals(CoordinateKey, other.CoordinateKey, StringComparison.Ordinal);
  }

  public override bool Equals(object? obj) => obj is DimensionCoordinate other && Equals(other);

  public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CoordinateKey);

  private static string CreateCoordinateKey(IReadOnlyList<DimensionCoordinatePart> parts)
  {
    var builder = new StringBuilder();

    foreach (var part in parts)
    {
      AppendCoordinateComponent(builder, part.DimensionKey);
      AppendCoordinateComponent(builder, part.IsTotal ? $"{TotalCoordinateComponentPrefix}{part.Value.Key}" : part.Value.Key);
    }

    return builder.ToString();
  }

  private static RollupKind GetKind(int partCount, int nonTotalPartCount)
  {
    if (nonTotalPartCount == partCount)
      return RollupKind.Leaf;

    return nonTotalPartCount == 0 ? RollupKind.GrandTotal : RollupKind.Subtotal;
  }

  private static void AppendCoordinateComponent(StringBuilder builder, string value)
  {
    var normalizedValue = NormalizeCoordinateComponent(value);
    builder.Append(normalizedValue.Length);
    builder.Append(':');
    builder.Append(normalizedValue);
    builder.Append('|');
  }

  private static string NormalizeCoordinateComponent(string? value)
  {
    if (value == null)
      return Constants.NULL_LABEL;

    if (value.Length == 0)
      return Constants.EMPTY_LABEL;

    return value.ToUpperInvariant();
  }
}

/// <summary>
/// Represents one dimension/value pair within a coordinate.
/// </summary>
public sealed class DimensionCoordinatePart
{
  /// <summary>
  /// Gets the dimension key for this part.
  /// </summary>
  public string DimensionKey { get; }

  /// <summary>
  /// Gets the dimension value for this part.
  /// </summary>
  public DimensionValue Value { get; }

  /// <summary>
  /// Gets a value indicating whether this part represents an all-members total.
  /// </summary>
  public bool IsTotal => Value.IsTotal;

  /// <summary>
  /// Initializes a new coordinate part.
  /// </summary>
  public DimensionCoordinatePart(string dimensionKey, DimensionValue value)
  {
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentNullException.ThrowIfNull(value);

    DimensionKey = dimensionKey;
    Value = value;
  }
}
