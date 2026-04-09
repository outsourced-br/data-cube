namespace Outsourced.DataCube.Builders;

/// <summary>
/// Builds a <see cref="CubeGrain"/> definition for an <see cref="AnalyticsCube"/>.
/// </summary>
public sealed class CubeGrainBuilder
{
  private readonly CubeGrain _grain;

  internal CubeGrainBuilder(CubeGrain grain = null)
  {
    _grain = grain?.Clone() ?? new CubeGrain();
  }

  /// <summary>
  /// Adds required dimensions to the grain definition.
  /// </summary>
  public CubeGrainBuilder Require(params string[] dimensionKeys)
  {
    _grain.Require(dimensionKeys);
    return this;
  }

  /// <summary>
  /// Adds required dimensions to the grain definition.
  /// </summary>
  public CubeGrainBuilder Require(params Dimension[] dimensions)
  {
    _grain.Require(dimensions);
    return this;
  }

  /// <summary>
  /// Adds optional dimensions to the grain definition.
  /// </summary>
  public CubeGrainBuilder AllowOptional(params string[] dimensionKeys)
  {
    _grain.AllowOptional(dimensionKeys);
    return this;
  }

  /// <summary>
  /// Adds optional dimensions to the grain definition.
  /// </summary>
  public CubeGrainBuilder AllowOptional(params Dimension[] dimensions)
  {
    _grain.AllowOptional(dimensions);
    return this;
  }

  /// <summary>
  /// Builds the configured grain definition.
  /// </summary>
  public CubeGrain Build() => _grain.Clone();
}
