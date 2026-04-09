namespace Outsourced.DataCube.Hierarchy;

/// <summary>
/// Represents a balanced hierarchy attached to a dimension.
/// </summary>
public class Hierarchy
{
  private Dimension _dimension;

  /// <summary>
  /// Gets or sets the stable key for the hierarchy.
  /// </summary>
  public string Key { get; set; }

  /// <summary>
  /// Gets or sets the display label for the hierarchy.
  /// </summary>
  public string Label { get; set; }

  /// <summary>
  /// Gets or sets the ordered levels that make up the balanced hierarchy.
  /// </summary>
  public IList<HierarchyLevel> Levels { get; set; } = new List<HierarchyLevel>();

  /// <summary>
  /// Gets or sets the direct parent mapping for non-root hierarchy members.
  /// Keys are child value keys and values are parent value keys.
  /// </summary>
  public IDictionary<string, string> ParentKeys { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Initializes an empty hierarchy for serialization.
  /// </summary>
  public Hierarchy()
  {
  }

  /// <summary>
  /// Initializes a new hierarchy.
  /// </summary>
  public Hierarchy(string key, string label = null)
  {
    Key = key;
    Label = label;
  }

  /// <summary>
  /// Adds a new level to the hierarchy.
  /// </summary>
  public HierarchyLevel AddLevel(string key, string label = null)
  {
    var level = new HierarchyLevel(key, label);
    AddLevel(level);
    return level;
  }

  /// <summary>
  /// Adds an existing level to the hierarchy.
  /// </summary>
  public void AddLevel(HierarchyLevel level)
  {
    ArgumentNullException.ThrowIfNull(level);

    Levels ??= new List<HierarchyLevel>();
    Levels.Add(level);
    Normalize();
    Validate();
  }

  /// <summary>
  /// Finds a level by its key.
  /// </summary>
  public HierarchyLevel GetLevel(string key)
  {
    foreach (var level in Levels ?? Array.Empty<HierarchyLevel>())
    {
      if (level != null && string.Equals(level.Key, key, StringComparison.OrdinalIgnoreCase))
        return level;
    }

    return null;
  }

  /// <summary>
  /// Rolls up one level from the supplied hierarchy level.
  /// Returns <see langword="null"/> when the supplied level is already the root.
  /// </summary>
  public HierarchyLevel RollUp(string levelKey)
  {
    var level = GetRequiredLevel(levelKey);
    return level.Ordinal == 0 ? null : Levels[level.Ordinal - 1];
  }

  /// <summary>
  /// Rolls up one level from the supplied hierarchy level.
  /// Returns <see langword="null"/> when the supplied level is already the root.
  /// </summary>
  public HierarchyLevel RollUp(HierarchyLevel level)
  {
    ArgumentNullException.ThrowIfNull(level);
    return RollUp(level.Key);
  }

  /// <summary>
  /// Drills down one level from the supplied hierarchy level.
  /// Returns <see langword="null"/> when the supplied level is already the leaf.
  /// </summary>
  public HierarchyLevel DrillDown(string levelKey)
  {
    var level = GetRequiredLevel(levelKey);
    return level.Ordinal >= Levels.Count - 1 ? null : Levels[level.Ordinal + 1];
  }

  /// <summary>
  /// Drills down one level from the supplied hierarchy level.
  /// Returns <see langword="null"/> when the supplied level is already the leaf.
  /// </summary>
  public HierarchyLevel DrillDown(HierarchyLevel level)
  {
    ArgumentNullException.ThrowIfNull(level);
    return DrillDown(level.Key);
  }

  /// <summary>
  /// Finds the level currently associated with a dimension value.
  /// </summary>
  public HierarchyLevel GetLevelForValue(DimensionValue value)
  {
    return value == null ? null : GetLevelForValue(value.Key);
  }

  /// <summary>
  /// Finds the level currently associated with a dimension value key.
  /// </summary>
  public HierarchyLevel GetLevelForValue(string valueKey)
  {
    if (string.IsNullOrEmpty(valueKey))
      valueKey = Constants.EMPTY_LABEL;

    foreach (var level in Levels ?? Array.Empty<HierarchyLevel>())
    {
      if (level == null)
        continue;

      foreach (var mappedValueKey in level.ValueKeys ?? Array.Empty<string>())
      {
        if (string.Equals(mappedValueKey, valueKey, StringComparison.OrdinalIgnoreCase))
          return level;
      }
    }

    return null;
  }

  /// <summary>
  /// Returns the dimension values mapped to the requested level.
  /// </summary>
  public IEnumerable<DimensionValue> GetValues(string levelKey)
  {
    var level = GetLevel(levelKey);
    if (level == null || _dimension == null)
      return Array.Empty<DimensionValue>();

    return level.ValueKeys
      .Select(ResolveValue)
      .Where(static value => value != null);
  }

  /// <summary>
  /// Maps an existing dimension value to a hierarchy level.
  /// Non-root values must specify a parent from the immediately preceding level.
  /// </summary>
  public void MapValue(string levelKey, DimensionValue value, DimensionValue parent = null)
  {
    ArgumentNullException.ThrowIfNull(value);
    MapValue(levelKey, value.Key, parent?.Key);
  }

  /// <summary>
  /// Maps an existing dimension value key to a hierarchy level.
  /// Non-root values must specify a parent from the immediately preceding level.
  /// </summary>
  public void MapValue(string levelKey, string valueKey, string parentValueKey = null)
  {
    EnsureAttached();
    ArgumentException.ThrowIfNullOrEmpty(levelKey);

    var level = GetLevel(levelKey) ?? throw new ArgumentException($"Hierarchy level '{levelKey}' was not found.", nameof(levelKey));
    var normalizedValueKey = EnsureDimensionContainsValueKey(valueKey, nameof(valueKey));
    var existingLevel = GetLevelForValue(normalizedValueKey);

    if (existingLevel != null && !string.Equals(existingLevel.Key, level.Key, StringComparison.OrdinalIgnoreCase))
      throw new InvalidOperationException($"Dimension value '{normalizedValueKey}' is already mapped to level '{existingLevel.Key}'.");

    if (level.Ordinal == 0)
    {
      if (!string.IsNullOrEmpty(parentValueKey))
        throw new InvalidOperationException($"Root level '{level.Key}' values cannot declare a parent.");

      RemoveParentKey(normalizedValueKey);
    }
    else
    {
      if (string.IsNullOrEmpty(parentValueKey))
        throw new InvalidOperationException($"Level '{level.Key}' values must map to a parent in level '{Levels[level.Ordinal - 1].Key}'.");

      var normalizedParentKey = EnsureDimensionContainsValueKey(parentValueKey, nameof(parentValueKey));
      var parentLevel = GetLevelForValue(normalizedParentKey);

      if (parentLevel == null || parentLevel.Ordinal != level.Ordinal - 1)
        throw new InvalidOperationException($"Parent value '{normalizedParentKey}' must be mapped to level '{Levels[level.Ordinal - 1].Key}' before assigning children to level '{level.Key}'.");

      ParentKeys[normalizedValueKey] = normalizedParentKey;
    }

    AddValueKey(level, normalizedValueKey);
  }

  /// <summary>
  /// Gets the direct parent for a mapped hierarchy value.
  /// </summary>
  public DimensionValue GetParent(DimensionValue value)
  {
    return value == null ? null : GetParent(value.Key);
  }

  /// <summary>
  /// Gets the direct parent for a mapped hierarchy value key.
  /// </summary>
  public DimensionValue GetParent(string valueKey)
  {
    if (_dimension == null || valueKey == null)
      return null;

    valueKey = valueKey.Length == 0 ? Constants.EMPTY_LABEL : valueKey;

    return TryGetParentKey(valueKey, out var parentKey)
      ? ResolveValue(parentKey)
      : null;
  }

  /// <summary>
  /// Gets the direct child values for a mapped hierarchy member.
  /// </summary>
  public IEnumerable<DimensionValue> GetChildren(DimensionValue value)
  {
    return value == null || _dimension == null
      ? Array.Empty<DimensionValue>()
      : GetChildren(value.Key);
  }

  /// <summary>
  /// Gets the direct child values for a mapped hierarchy member key.
  /// </summary>
  public IEnumerable<DimensionValue> GetChildren(string valueKey)
  {
    if (_dimension == null || valueKey == null)
      return Array.Empty<DimensionValue>();

    valueKey = valueKey.Length == 0 ? Constants.EMPTY_LABEL : valueKey;

    var children = new List<DimensionValue>();

    foreach (var mapping in ParentKeys)
    {
      if (!string.Equals(mapping.Value, valueKey, StringComparison.OrdinalIgnoreCase))
        continue;

      var child = ResolveValue(mapping.Key);
      if (child != null)
        children.Add(child);
    }

    return children;
  }

  /// <summary>
  /// Gets the root-to-leaf path for a mapped hierarchy value.
  /// </summary>
  public IReadOnlyList<DimensionValue> GetPath(DimensionValue value)
  {
    return value == null ? Array.Empty<DimensionValue>() : GetPath(value.Key);
  }

  /// <summary>
  /// Gets the root-to-leaf path for a mapped hierarchy value key.
  /// </summary>
  public IReadOnlyList<DimensionValue> GetPath(string valueKey)
  {
    if (valueKey != null && valueKey.Length == 0)
      valueKey = Constants.EMPTY_LABEL;

    if (_dimension == null || GetLevelForValue(valueKey) == null)
      return Array.Empty<DimensionValue>();

    var path = new List<DimensionValue>();
    var current = ResolveValue(valueKey);

    while (current != null)
    {
      path.Add(current);
      current = GetParent(current);
    }

    path.Reverse();
    return path;
  }

  /// <summary>
  /// Gets the mapped ancestor at the requested level, if present.
  /// </summary>
  public DimensionValue GetAncestor(DimensionValue value, string levelKey)
  {
    if (value == null)
      return null;

    return GetAncestor(value.Key, levelKey);
  }

  /// <summary>
  /// Gets the mapped ancestor at the requested level, if present.
  /// </summary>
  public DimensionValue GetAncestor(string valueKey, string levelKey)
  {
    ArgumentException.ThrowIfNullOrEmpty(levelKey);

    foreach (var value in GetPath(valueKey))
    {
      var level = GetLevelForValue(value);
      if (level != null && string.Equals(level.Key, levelKey, StringComparison.OrdinalIgnoreCase))
        return value;
    }

    return null;
  }

  internal void Attach(Dimension dimension)
  {
    ArgumentNullException.ThrowIfNull(dimension);

    if (_dimension != null && !ReferenceEquals(_dimension, dimension))
      throw new InvalidOperationException("Hierarchy is already attached to a different dimension.");

    _dimension = dimension;
    Normalize();
    Validate();
  }

  private void EnsureAttached()
  {
    if (_dimension == null)
      throw new InvalidOperationException("Hierarchy must be attached to a dimension before values can be mapped.");
  }

  private HierarchyLevel GetRequiredLevel(string levelKey)
  {
    ArgumentException.ThrowIfNullOrEmpty(levelKey);

    return GetLevel(levelKey)
           ?? throw new KeyNotFoundException($"Hierarchy level '{levelKey}' was not found in hierarchy '{Key}'.");
  }

  private void Normalize()
  {
    ArgumentException.ThrowIfNullOrEmpty(Key);

    Levels ??= new List<HierarchyLevel>();
    ParentKeys ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    var seenLevelKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    for (var index = 0; index < Levels.Count; index++)
    {
      var level = Levels[index] ?? throw new InvalidOperationException("Hierarchy levels cannot contain null entries.");

      ArgumentException.ThrowIfNullOrEmpty(level.Key);

      if (!seenLevelKeys.Add(level.Key))
        throw new InvalidOperationException($"Hierarchy level '{level.Key}' is registered more than once.");

      var normalizedValueKeys = new List<string>();
      foreach (var valueKey in level.ValueKeys ?? Array.Empty<string>())
      {
        var normalizedValueKey = NormalizeValueKey(valueKey, nameof(level.ValueKeys));
        if (!normalizedValueKeys.Contains(normalizedValueKey, StringComparer.OrdinalIgnoreCase))
          normalizedValueKeys.Add(normalizedValueKey);
      }

      level.ValueKeys = normalizedValueKeys;
      level.Ordinal = index;
    }

    var normalizedParentKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var mapping in ParentKeys)
    {
      var childKey = NormalizeValueKey(mapping.Key, nameof(ParentKeys));
      var parentKey = NormalizeValueKey(mapping.Value, nameof(ParentKeys));

      if (normalizedParentKeys.TryGetValue(childKey, out var existingParentKey) &&
          !string.Equals(existingParentKey, parentKey, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException($"Dimension value '{childKey}' cannot be mapped to multiple parents in hierarchy '{Key}'.");
      }

      normalizedParentKeys[childKey] = parentKey;
    }

    ParentKeys = normalizedParentKeys;
  }

  private void Validate()
  {
    if (Levels.Count == 0)
      return;

    var levelsByValueKey = new Dictionary<string, HierarchyLevel>(StringComparer.OrdinalIgnoreCase);

    foreach (var level in Levels)
    {
      foreach (var valueKey in level.ValueKeys)
      {
        EnsureDimensionContainsValueKey(valueKey, nameof(level.ValueKeys));

        if (!levelsByValueKey.TryAdd(valueKey, level))
          throw new InvalidOperationException($"Dimension value '{valueKey}' cannot be mapped to multiple levels in hierarchy '{Key}'.");
      }
    }

    foreach (var level in Levels)
    {
      foreach (var valueKey in level.ValueKeys)
      {
        if (level.Ordinal == 0)
        {
          if (TryGetParentKey(valueKey, out _))
            throw new InvalidOperationException($"Root level value '{valueKey}' cannot declare a parent in hierarchy '{Key}'.");

          continue;
        }

        if (!TryGetParentKey(valueKey, out var parentKey))
          throw new InvalidOperationException($"Level '{level.Key}' value '{valueKey}' must declare a parent in hierarchy '{Key}'.");

        if (!levelsByValueKey.TryGetValue(parentKey, out var parentLevel) || parentLevel.Ordinal != level.Ordinal - 1)
          throw new InvalidOperationException($"Parent value '{parentKey}' must belong to level '{Levels[level.Ordinal - 1].Key}' in hierarchy '{Key}'.");
      }
    }

    foreach (var mapping in ParentKeys)
    {
      if (!levelsByValueKey.ContainsKey(mapping.Key))
        throw new InvalidOperationException($"Hierarchy '{Key}' contains a parent mapping for unmapped value '{mapping.Key}'.");
    }
  }

  private void AddValueKey(HierarchyLevel level, string valueKey)
  {
    if (!level.ValueKeys.Contains(valueKey, StringComparer.OrdinalIgnoreCase))
      level.ValueKeys.Add(valueKey);
  }

  private void RemoveParentKey(string childValueKey)
  {
    var existingKey = ParentKeys.Keys.FirstOrDefault(key => string.Equals(key, childValueKey, StringComparison.OrdinalIgnoreCase));
    if (existingKey != null)
      ParentKeys.Remove(existingKey);
  }

  private bool TryGetParentKey(string childValueKey, out string parentValueKey)
  {
    foreach (var mapping in ParentKeys)
    {
      if (string.Equals(mapping.Key, childValueKey, StringComparison.OrdinalIgnoreCase))
      {
        parentValueKey = mapping.Value;
        return true;
      }
    }

    parentValueKey = null;
    return false;
  }

  private string EnsureDimensionContainsValueKey(string valueKey, string parameterName)
  {
    var normalizedValueKey = NormalizeValueKey(valueKey, parameterName);
    if (_dimension?.GetValue(normalizedValueKey) == null)
      throw new InvalidOperationException($"Dimension '{_dimension?.Key}' does not contain a registered value with key '{normalizedValueKey}'.");

    return normalizedValueKey;
  }

  private DimensionValue ResolveValue(string valueKey)
  {
    return _dimension?.GetValue(valueKey);
  }

  private static string NormalizeValueKey(string valueKey, string parameterName)
  {
    if (valueKey == null)
      throw new ArgumentNullException(parameterName);

    return valueKey.Length == 0 ? Constants.EMPTY_LABEL : valueKey;
  }
}
