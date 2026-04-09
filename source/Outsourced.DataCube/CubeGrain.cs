namespace Outsourced.DataCube;

using System.Text;

/// <summary>
/// Describes the intended dimensional grain for fact groups stored in a cube.
/// </summary>
public sealed class CubeGrain
{
  private string[] _optionalDimensions = [];
  private string[] _requiredDimensions = [];

  /// <summary>
  /// Gets or sets the dimensions that every fact group must contain.
  /// </summary>
  public string[] RequiredDimensions
  {
    get => [.. _requiredDimensions];
    set => _requiredDimensions = NormalizeDimensionKeys(value, nameof(RequiredDimensions));
  }

  /// <summary>
  /// Gets or sets the dimensions that fact groups may contain in addition to the required grain.
  /// </summary>
  public string[] OptionalDimensions
  {
    get => [.. _optionalDimensions];
    set => _optionalDimensions = NormalizeDimensionKeys(value, nameof(OptionalDimensions));
  }

  /// <summary>
  /// Adds required dimensions to the grain definition.
  /// </summary>
  public CubeGrain Require(params string[] dimensionKeys)
  {
    ArgumentNullException.ThrowIfNull(dimensionKeys);

    _requiredDimensions = MergeDimensionKeys(_requiredDimensions, dimensionKeys, nameof(dimensionKeys));
    return this;
  }

  /// <summary>
  /// Adds required dimensions to the grain definition.
  /// </summary>
  public CubeGrain Require(params Dimension[] dimensions)
  {
    ArgumentNullException.ThrowIfNull(dimensions);

    return Require(GetDimensionKeys(dimensions, nameof(dimensions)));
  }

  /// <summary>
  /// Adds optional dimensions to the grain definition.
  /// </summary>
  public CubeGrain AllowOptional(params string[] dimensionKeys)
  {
    ArgumentNullException.ThrowIfNull(dimensionKeys);

    _optionalDimensions = MergeDimensionKeys(_optionalDimensions, dimensionKeys, nameof(dimensionKeys));
    return this;
  }

  /// <summary>
  /// Adds optional dimensions to the grain definition.
  /// </summary>
  public CubeGrain AllowOptional(params Dimension[] dimensions)
  {
    ArgumentNullException.ThrowIfNull(dimensions);

    return AllowOptional(GetDimensionKeys(dimensions, nameof(dimensions)));
  }

  internal CubeGrain Clone()
  {
    return new CubeGrain
    {
      RequiredDimensions = _requiredDimensions,
      OptionalDimensions = _optionalDimensions,
    };
  }

  internal void ValidateStructure()
  {
    var overlappingDimensions = _requiredDimensions
      .Intersect(_optionalDimensions, StringComparer.OrdinalIgnoreCase)
      .OrderBy(static dimensionKey => dimensionKey, StringComparer.OrdinalIgnoreCase)
      .ToArray();

    if (overlappingDimensions.Length > 0)
    {
      throw new InvalidOperationException(
        $"Cube grain dimensions cannot be both required and optional: {FormatDimensionList(overlappingDimensions)}.");
    }
  }

  internal void ValidateRegisteredDimensions(IDictionary<string, Dimension> dimensions)
  {
    ArgumentNullException.ThrowIfNull(dimensions);

    var unknownDimensions = GetAllDimensions()
      .Where(dimensionKey => !dimensions.ContainsKey(dimensionKey))
      .OrderBy(static dimensionKey => dimensionKey, StringComparer.OrdinalIgnoreCase)
      .ToArray();

    if (unknownDimensions.Length > 0)
    {
      throw new InvalidOperationException(
        $"Cube grain references unknown dimensions: {FormatDimensionList(unknownDimensions)}. Register those dimensions before declaring the grain.");
    }
  }

  internal void ValidateFactGroup(FactGroup factGroup)
  {
    ArgumentNullException.ThrowIfNull(factGroup);

    var dimensionKeys = factGroup.DimensionValues.Keys.ToArray();
    var allowedDimensions = GetAllDimensions();
    var missingDimensions = _requiredDimensions
      .Except(dimensionKeys, StringComparer.OrdinalIgnoreCase)
      .OrderBy(static dimensionKey => dimensionKey, StringComparer.OrdinalIgnoreCase)
      .ToArray();
    var unexpectedDimensions = dimensionKeys
      .Except(allowedDimensions, StringComparer.OrdinalIgnoreCase)
      .OrderBy(static dimensionKey => dimensionKey, StringComparer.OrdinalIgnoreCase)
      .ToArray();

    if (missingDimensions.Length == 0 && unexpectedDimensions.Length == 0)
      return;

    var message = new StringBuilder("Fact group does not match the cube grain.");

    if (missingDimensions.Length > 0)
      message.Append($" Missing required dimensions: {FormatDimensionList(missingDimensions)}.");

    if (unexpectedDimensions.Length > 0)
      message.Append($" Unexpected dimensions: {FormatDimensionList(unexpectedDimensions)}.");

    message.Append($" Expected grain: required [{FormatDimensionList(_requiredDimensions)}]");

    if (_optionalDimensions.Length > 0)
      message.Append($", optional [{FormatDimensionList(_optionalDimensions)}]");

    message.Append($". Fact group dimensions: [{FormatDimensionList(dimensionKeys)}].");

    throw new InvalidOperationException(message.ToString());
  }

  private string[] GetAllDimensions()
  {
    return [.. _requiredDimensions, .. _optionalDimensions];
  }

  private static string[] GetDimensionKeys(IEnumerable<Dimension> dimensions, string paramName)
  {
    return dimensions.Select(dimension =>
    {
      ArgumentNullException.ThrowIfNull(dimension, paramName);
      return dimension.Key;
    }).ToArray();
  }

  private static string[] MergeDimensionKeys(IEnumerable<string> existingDimensionKeys, IEnumerable<string> dimensionKeys, string paramName)
  {
    return NormalizeDimensionKeys(existingDimensionKeys.Concat(dimensionKeys), paramName);
  }

  private static string[] NormalizeDimensionKeys(IEnumerable<string> dimensionKeys, string paramName)
  {
    if (dimensionKeys == null)
      return [];

    var normalizedDimensionKeys = new List<string>();
    var seenDimensionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var dimensionKey in dimensionKeys)
    {
      if (dimensionKey == null)
        throw new ArgumentException("Dimension key cannot be null.", paramName);

      if (string.IsNullOrWhiteSpace(dimensionKey))
        throw new ArgumentException("Dimension key cannot be empty or whitespace.", paramName);

      if (seenDimensionKeys.Add(dimensionKey))
        normalizedDimensionKeys.Add(dimensionKey);
    }

    return [.. normalizedDimensionKeys];
  }

  private static string FormatDimensionList(IEnumerable<string> dimensionKeys)
  {
    var values = dimensionKeys
      ?.Where(static dimensionKey => !string.IsNullOrWhiteSpace(dimensionKey))
      .ToArray()
      ?? [];

    return values.Length == 0 ? "none" : string.Join(", ", values);
  }
}
