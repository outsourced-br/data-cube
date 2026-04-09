namespace Outsourced.DataCube.Hierarchy;

/// <summary>
/// Represents a single balanced hierarchy level within a dimension hierarchy.
/// </summary>
public class HierarchyLevel
{
  /// <summary>
  /// Gets or sets the stable key for the hierarchy level.
  /// </summary>
  public string Key { get; set; }

  /// <summary>
  /// Gets or sets the display label for the hierarchy level.
  /// </summary>
  public string Label { get; set; }

  /// <summary>
  /// Gets or sets the zero-based position of the level within the hierarchy.
  /// </summary>
  public int Ordinal { get; set; }

  /// <summary>
  /// Gets or sets the dimension value keys mapped to this level.
  /// </summary>
  public IList<string> ValueKeys { get; set; } = new List<string>();

  /// <summary>
  /// Initializes an empty hierarchy level for serialization.
  /// </summary>
  public HierarchyLevel()
  {
  }

  /// <summary>
  /// Initializes a new hierarchy level.
  /// </summary>
  public HierarchyLevel(string key, string label = null)
  {
    Key = key;
    Label = label;
  }
}
