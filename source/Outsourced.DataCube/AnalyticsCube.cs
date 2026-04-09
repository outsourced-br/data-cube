namespace Outsourced.DataCube;

using System.Buffers;
using System.Globalization;
using Builders;
using Collections;
using Metrics;

/// <summary>
/// Represents an in-memory analytics cube containing dimensions, metrics, and grouped facts.
/// </summary>
public class AnalyticsCube
{
  private readonly Dictionary<string, FactGroup> _coordinateIndex = new(StringComparer.Ordinal);
  private readonly Dictionary<string, List<FactGroup>> _coordinateBuckets = new(StringComparer.Ordinal);
  private readonly Dictionary<FactGroup, IndexedCoordinate> _factGroupCoordinates = new();
  private readonly Dictionary<int, int> _coordinateSizeCounts = new();

  private IDictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>(StringComparer.OrdinalIgnoreCase);
  private IList<FactGroup> _factGroups;
  private CubeGrain _grain;
  private IDictionary<string, Metric> _metrics = new Dictionary<string, Metric>(StringComparer.OrdinalIgnoreCase);

  public AnalyticsCube()
  {
    _factGroups = CreateTrackedFactGroupList(null);
  }

  /// <summary>
  /// Gets or sets the stable identifier for the cube.
  /// </summary>
  public string Key { get; set; }

  /// <summary>
  /// Gets or sets the display label for the cube.
  /// </summary>
  public string Label { get; set; }

  /// <summary>
  /// Gets the total population represented by the cube, when known.
  /// </summary>
  public int PopulationCount { get; init; }

  /// <summary>
  /// Gets the number of fact groups currently stored in the cube.
  /// </summary>
  public int FactCount => FactGroups.Count;

  /// <summary>
  /// Gets or sets arbitrary metadata associated with the cube.
  /// </summary>
  public IDictionary<string, string> Bag { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Gets or sets the registered dimensions keyed by dimension key.
  /// </summary>
  public IDictionary<string, Dimension> Dimensions
  {
    get => _dimensions;
    set
    {
      var dimensions = NormalizeCaseInsensitiveDictionary(value);
      ValidateGrain(dimensions, _grain);
      ValidateFactGroups(_grain, dimensions, _factGroups);
      _dimensions = dimensions;
    }
  }

  /// <summary>
  /// Gets or sets the registered metrics keyed by metric key.
  /// </summary>
  public IDictionary<string, Metric> Metrics
  {
    get => _metrics;
    set => _metrics = NormalizeCaseInsensitiveDictionary(value);
  }

  /// <summary>
  /// Gets or sets the declared analytical grain for fact groups stored in the cube.
  /// </summary>
  public CubeGrain Grain
  {
    get => _grain?.Clone();
    set
    {
      var grain = value?.Clone();
      ValidateGrain(_dimensions, grain);
      ValidateFactGroups(grain, _dimensions, _factGroups);
      _grain = grain;
    }
  }

  /// <summary>
  /// Gets or sets the fact groups that make up the cube body.
  /// </summary>
  public IList<FactGroup> FactGroups
  {
    get => _factGroups;
    set
    {
      var factGroups = value ?? Array.Empty<FactGroup>();
      ValidateFactGroups(_grain, _dimensions, factGroups);
      DetachIndexedFactGroups();
      _factGroups = CreateTrackedFactGroupList(factGroups);
      RebuildCoordinateIndex();
    }
  }

  /// <summary>
  /// Returns all registered dimensions.
  /// </summary>
  public IEnumerable<Dimension> GetDimensions() => Dimensions.Values;

  /// <summary>
  /// Creates a new empty <see cref="FactGroup"/>, adds it to the cube, and returns it.
  /// </summary>
  public FactGroup CreateAddFactGroup()
  {
    var group = new FactGroup();
    FactGroups.Add(group);

    return group;
  }

  /// <summary>
  /// Sets the declared grain for the cube.
  /// </summary>
  public AnalyticsCube SetGrain(CubeGrain grain)
  {
    Grain = grain;
    return this;
  }

  /// <summary>
  /// Builds and applies a grain definition for the cube.
  /// </summary>
  public AnalyticsCube DefineGrain(Action<CubeGrainBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(configure);

    var builder = new CubeGrainBuilder(_grain);
    configure(builder);
    Grain = builder.Build();

    return this;
  }

  /// <summary>
  /// Adds a dimension to the cube.
  /// </summary>
  /// <param name="dimension">The dimension to register.</param>
  /// <exception cref="ArgumentException">Thrown when a dimension with the same key already exists.</exception>
  public void AddDimension(Dimension dimension)
  {
    if (Dimensions.ContainsKey(dimension.Key))
      throw new ArgumentException($"Dimension {dimension.Key} already exists", nameof(dimension));

    Dimensions[dimension.Key] = dimension;
  }

  /// <summary>
  /// Retrieves a registered dimension by key.
  /// </summary>
  /// <param name="name">The dimension key.</param>
  /// <returns>The matching dimension.</returns>
  /// <exception cref="KeyNotFoundException">Thrown when no dimension exists for the supplied key.</exception>
  public Dimension GetDimension(string name)
  {
    if (!Dimensions.TryGetValue(name, out var dimension))
      throw new KeyNotFoundException($"Dimension {name} not found");

    return dimension;
  }

  /// <summary>
  /// Adds multiple metrics of the same value type to the cube.
  /// </summary>
  /// <typeparam name="T">The metric value type.</typeparam>
  /// <param name="metrics">The metrics to register.</param>
  /// <exception cref="ArgumentException">Thrown when any metric key already exists.</exception>
  public void AddMetrics<T>(params Metric<T>[] metrics)
  {
    foreach (var metric in metrics)
    {
      (metric.Semantics ?? MetricSemantics.CreateDefault(metric.AggregationType)).Validate(metric.Key);

      if (!Metrics.TryAdd(metric.Key, metric))
        throw new ArgumentException($"Metric {metric.Key} already exists", nameof(metrics));
    }
  }

  /// <summary>
  /// Adds a metric to the cube.
  /// </summary>
  /// <typeparam name="T">The metric value type.</typeparam>
  /// <param name="metric">The metric to register.</param>
  /// <returns>The registered metric.</returns>
  /// <exception cref="ArgumentException">Thrown when the metric key already exists.</exception>
  public Metric<T> AddMetric<T>(Metric<T> metric)
  {
    (metric.Semantics ?? MetricSemantics.CreateDefault(metric.AggregationType)).Validate(metric.Key);

    if (!Metrics.TryAdd(metric.Key, metric))
      throw new ArgumentException($"Metric {metric.Key} already exists", nameof(metric));

    return metric;
  }

  /// <summary>
  /// Retrieves a registered metric by key.
  /// </summary>
  /// <param name="name">The metric key.</param>
  /// <returns>The matching metric.</returns>
  /// <exception cref="KeyNotFoundException">Thrown when no metric exists for the supplied key.</exception>
  public Metric GetMetric(string name)
  {
    if (!Metrics.TryGetValue(name, out var metric))
      throw new KeyNotFoundException($"Metric {name} not found");

    return metric;
  }

  /// <summary>
  /// Adds or updates a metric value for the fact group identified by the supplied dimension values.
  /// </summary>
  /// <typeparam name="T">The metric value type.</typeparam>
  /// <param name="dimensions">The dimension values that identify the fact group.</param>
  /// <param name="metric">The metric definition.</param>
  /// <param name="value">The metric value to store.</param>
  public void AddMetricValue<T>(IDictionary<string, DimensionValue> dimensions, Metric<T> metric, T value) where T : struct
  {
    // Find or create a fact group with matching dimensions
    var factGroup = FindOrCreateFactGroup(dimensions);

    // Set the metric value in the fact group
    factGroup.SetMetricValue(metric, value);
  }

  /// <summary>
  /// Finds a fact group with matching dimensions or creates a new one
  /// </summary>
  private FactGroup FindOrCreateFactGroup(IDictionary<string, DimensionValue> dimensions)
  {
    // Validate that all dimensions exist
    foreach (var dimKey in dimensions.Keys)
    {
      if (!Dimensions.ContainsKey(dimKey))
        throw new ArgumentException($"Unknown dimension: {dimKey}", nameof(dimensions));
    }

    if (TryGetIndexedFactGroup(dimensions, out var factGroup))
      return factGroup;

    factGroup = new FactGroup();

    foreach (var dim in dimensions)
      factGroup.SetDimensionValue(dim.Key, dim.Value);

    FactGroups.Add(factGroup);
    return factGroup;
  }

  /// <summary>
  /// Returns fact groups that contain metric collections for the supplied metric value type.
  /// </summary>
  /// <typeparam name="T">The metric value type.</typeparam>
  public IEnumerable<FactGroup> GetFactGroupsWithMetricType<T>()
    where T : struct
  {
    var metricType = MetricTypeHelper.GetMetricType<T>();
    return FactGroups.Where(fg => fg.MetricCollections.ContainsKey(metricType));
  }

  /// <summary>
  /// Calculates the percentage contribution of a fact group's metric value relative to <see cref="PopulationCount"/>.
  /// </summary>
  /// <typeparam name="T">The metric value type.</typeparam>
  /// <param name="factGroup">The fact group to evaluate.</param>
  /// <param name="metric">The metric whose value should be converted into a percentage.</param>
  public decimal CalculatePercentage<T>(FactGroup factGroup, Metric metric)
    where T : struct
  {
    if (PopulationCount == 0) return 0;

    var value = factGroup.GetMetricValue<T>(metric);
    return Convert.ToDecimal(value, CultureInfo.InvariantCulture) / PopulationCount * 100m;
  }

  /// <summary>
  /// Returns duplicate-count analysis for each fact group containing metrics of the supplied value type.
  /// </summary>
  /// <typeparam name="T">The metric value type used by the duplicate metric collection.</typeparam>
  public IEnumerable<(FactGroup FactGroup, int Count, decimal Percentage)> GetDuplicateAnalysis<T>()
    where T : struct
  {
    var factGroups = GetFactGroupsWithMetricType<T>();

    return factGroups.Select(factGroup =>
    {
      var count = factGroup.GetMetricValue<T>(WellKnown.Metrics.DuplicateMetrics.DuplicateCount);
      var percentage = CalculatePercentage<T>(factGroup, WellKnown.Metrics.DuplicateMetrics.DuplicateCount);

      return (factGroup, Convert.ToInt32(count, CultureInfo.InvariantCulture), percentage);
    });
  }

  internal bool TryGetIndexedFactGroup(IDictionary<string, DimensionValue> dimensions, out FactGroup factGroup)
  {
    ArgumentNullException.ThrowIfNull(dimensions);

    var coordinateKey = CreateCoordinateKey(dimensions);
    return _coordinateIndex.TryGetValue(coordinateKey, out factGroup);
  }

  internal bool TryGetExactFactGroups(IDictionary<string, object> filter, out IReadOnlyList<FactGroup> factGroups)
  {
    ArgumentNullException.ThrowIfNull(filter);

    factGroups = Array.Empty<FactGroup>();

    if (!CanUseExactCoordinateLookup(filter.Count))
      return false;

    if (!TryCreateCoordinateKey(filter, out var coordinateKey))
      return false;

    if (_coordinateBuckets.TryGetValue(coordinateKey, out var bucket))
      factGroups = bucket;

    return true;
  }

  private void RebuildCoordinateIndex()
  {
    _coordinateIndex.Clear();
    _coordinateBuckets.Clear();
    _factGroupCoordinates.Clear();
    _coordinateSizeCounts.Clear();

    foreach (var factGroup in _factGroups)
    {
      TrackFactGroup(factGroup);
    }
  }

  private void DetachIndexedFactGroups()
  {
    foreach (var factGroup in _factGroupCoordinates.Keys.ToArray())
    {
      factGroup.DimensionValuesChanged -= HandleFactGroupDimensionsChanged;
    }

    _coordinateIndex.Clear();
    _coordinateBuckets.Clear();
    _factGroupCoordinates.Clear();
    _coordinateSizeCounts.Clear();
  }

  private IList<FactGroup> CreateTrackedFactGroupList(IEnumerable<FactGroup> factGroups)
  {
    return new ObservableFactGroupList(
      factGroups ?? Array.Empty<FactGroup>(),
      TrackFactGroup,
      UntrackFactGroup);
  }

  private void TrackFactGroup(FactGroup factGroup)
  {
    ArgumentNullException.ThrowIfNull(factGroup);
    ValidateFactGroup(factGroup);

    factGroup.DimensionValuesChanged -= HandleFactGroupDimensionsChanged;
    factGroup.DimensionValuesChanged += HandleFactGroupDimensionsChanged;

    AddIndexedFactGroup(factGroup);
  }

  private void UntrackFactGroup(FactGroup factGroup)
  {
    if (factGroup == null)
      return;

    factGroup.DimensionValuesChanged -= HandleFactGroupDimensionsChanged;
    RemoveIndexedFactGroup(factGroup);
  }

  private void HandleFactGroupDimensionsChanged(FactGroup factGroup)
  {
    ValidateFactGroup(factGroup);
    RemoveIndexedFactGroup(factGroup);
    AddIndexedFactGroup(factGroup);
  }

  private void AddIndexedFactGroup(FactGroup factGroup)
  {
    var coordinateKey = CreateCoordinateKey(factGroup.DimensionValues);
    _factGroupCoordinates[factGroup] = new IndexedCoordinate(coordinateKey, factGroup.DimensionValues.Count);

    if (!_coordinateBuckets.TryGetValue(coordinateKey, out var bucket))
    {
      bucket = new List<FactGroup>();
      _coordinateBuckets[coordinateKey] = bucket;
    }

    bucket.Add(factGroup);

    if (!_coordinateIndex.ContainsKey(coordinateKey))
      _coordinateIndex[coordinateKey] = factGroup;

    AdjustCoordinateSizeCount(factGroup.DimensionValues.Count, 1);
  }

  private void RemoveIndexedFactGroup(FactGroup factGroup)
  {
    if (!_factGroupCoordinates.Remove(factGroup, out var indexedCoordinate))
      return;

    if (_coordinateBuckets.TryGetValue(indexedCoordinate.Key, out var bucket))
    {
      bucket.Remove(factGroup);

      if (bucket.Count == 0)
      {
        _coordinateBuckets.Remove(indexedCoordinate.Key);
        _coordinateIndex.Remove(indexedCoordinate.Key);
      }
      else if (_coordinateIndex.TryGetValue(indexedCoordinate.Key, out var indexedFactGroup) && ReferenceEquals(indexedFactGroup, factGroup))
      {
        _coordinateIndex[indexedCoordinate.Key] = bucket[0];
      }
    }

    AdjustCoordinateSizeCount(indexedCoordinate.DimensionCount, -1);
  }

  private void AdjustCoordinateSizeCount(int dimensionCount, int delta)
  {
    if (!_coordinateSizeCounts.TryGetValue(dimensionCount, out var currentCount))
    {
      if (delta > 0)
        _coordinateSizeCounts[dimensionCount] = delta;

      return;
    }

    var updatedCount = currentCount + delta;
    if (updatedCount <= 0)
      _coordinateSizeCounts.Remove(dimensionCount);
    else
      _coordinateSizeCounts[dimensionCount] = updatedCount;
  }

  private bool CanUseExactCoordinateLookup(int filterCount)
  {
    return
      filterCount > 0 &&
      _coordinateSizeCounts.Count == 1 &&
      _coordinateSizeCounts.ContainsKey(filterCount);
  }

  private bool TryCreateCoordinateKey(IDictionary<string, object> filter, out string coordinateKey)
  {
    var resolvedValues = new Dictionary<string, DimensionValue>(StringComparer.OrdinalIgnoreCase);

    foreach (var entry in filter)
    {
      if (!Dimensions.TryGetValue(entry.Key, out var dimension))
      {
        coordinateKey = null;
        return false;
      }

      var dimensionValue = dimension.GetValueByRawValue(entry.Value);
      if (dimensionValue == null)
      {
        coordinateKey = null;
        return false;
      }

      resolvedValues[dimension.Key] = dimensionValue;
    }

    coordinateKey = CreateCoordinateKey(resolvedValues);
    return true;
  }

  private static string CreateCoordinateKey(IDictionary<string, DimensionValue> dimensions)
  {
    int count = dimensions.Count;
    if (count == 0)
      return string.Empty;

    string[] keys = ArrayPool<string>.Shared.Rent(count);
    char[] buffer = ArrayPool<char>.Shared.Rent(256);

    try
    {
      int i = 0;
      foreach (var key in dimensions.Keys) keys[i++] = key;

      Array.Sort(keys, 0, count, StringComparer.OrdinalIgnoreCase);
      int pos = 0;
      for (int j = 0; j < count; j++)
      {
        string key = keys[j];
        string valueKey = dimensions[key]?.Key;

        pos = AppendCoordinateComponent(ref buffer, pos, key);
        pos = AppendCoordinateComponent(ref buffer, pos, valueKey);
      }

      return new string(buffer, 0, pos);
    }
    finally
    {
      ArrayPool<char>.Shared.Return(buffer);
      ArrayPool<string>.Shared.Return(keys, clearArray: true);
    }
  }

  private static int AppendCoordinateComponent(ref char[] buffer, int pos, string value)
  {
    if (value == null)
      return AppendRaw(ref buffer, pos, Constants.NULL_LABEL);

    if (value.Length == 0)
      return AppendRaw(ref buffer, pos, Constants.EMPTY_LABEL);

    pos = AppendLengthPrefix(ref buffer, pos, value.Length);
    buffer = EnsureCapacity(buffer, pos, value.Length + 1);
    pos += MemoryExtensions.ToUpperInvariant(value.AsSpan(), buffer.AsSpan(pos));
    buffer[pos++] = '|';
    return pos;
  }

  private static int AppendRaw(ref char[] buffer, int pos, string value)
  {
    pos = AppendLengthPrefix(ref buffer, pos, value.Length);
    buffer = EnsureCapacity(buffer, pos, value.Length + 1);
    value.AsSpan().CopyTo(buffer.AsSpan(pos));
    pos += value.Length;
    buffer[pos++] = '|';
    return pos;
  }

  private static int AppendLengthPrefix(ref char[] buffer, int pos, int valueLength)
  {
    Span<char> lengthBuffer = stackalloc char[11];
    if (!valueLength.TryFormat(lengthBuffer, out var charsWritten, provider: CultureInfo.InvariantCulture))
      throw new InvalidOperationException("Unable to format a coordinate component length.");

    buffer = EnsureCapacity(buffer, pos, charsWritten + 1);
    lengthBuffer[..charsWritten].CopyTo(buffer.AsSpan(pos));
    pos += charsWritten;
    buffer[pos++] = ':';
    return pos;
  }

  private static char[] EnsureCapacity(char[] buffer, int pos, int additionalCapacity)
  {
    if (buffer.Length - pos >= additionalCapacity)
      return buffer;

    int requiredCapacity = pos + additionalCapacity;
    int newCapacity = buffer.Length;
    while (newCapacity < requiredCapacity)
    {
      newCapacity *= 2;
    }

    char[] newBuffer = ArrayPool<char>.Shared.Rent(newCapacity);
    Array.Copy(buffer, newBuffer, pos);
    ArrayPool<char>.Shared.Return(buffer);
    return newBuffer;
  }

  private static void ValidateGrain(IDictionary<string, Dimension> dimensions, CubeGrain grain)
  {
    if (grain == null)
      return;

    grain.ValidateStructure();

    if (dimensions.Count > 0)
      grain.ValidateRegisteredDimensions(dimensions);
  }

  private static void ValidateFactGroups(CubeGrain grain, IDictionary<string, Dimension> dimensions, IEnumerable<FactGroup> factGroups)
  {
    if (grain == null)
      return;

    ValidateGrain(dimensions, grain);

    foreach (var factGroup in factGroups ?? Array.Empty<FactGroup>())
    {
      grain.ValidateFactGroup(factGroup);
    }
  }

  private void ValidateFactGroup(FactGroup factGroup)
  {
    if (_grain == null)
      return;

    ValidateGrain(_dimensions, _grain);
    _grain.ValidateFactGroup(factGroup);
  }

  private readonly record struct IndexedCoordinate(string Key, int DimensionCount);

  private static IDictionary<string, TValue> NormalizeCaseInsensitiveDictionary<TValue>(IDictionary<string, TValue> values)
  {
    if (values is Dictionary<string, TValue> dictionary &&
        dictionary.Comparer.Equals(StringComparer.OrdinalIgnoreCase))
    {
      return dictionary;
    }

    return values == null
      ? new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase)
      : new Dictionary<string, TValue>(values, StringComparer.OrdinalIgnoreCase);
  }
}
