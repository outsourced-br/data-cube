namespace Outsourced.DataCube;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Represents a dimension whose key is composed from multiple named fields.
/// </summary>
public class CompositeDimension : Dimension
{
  /// <summary>
  /// Gets the serialized/runtime type name for the dimension.
  /// </summary>
  public override string Type => GetType().Name;

  /// <summary>
  /// Gets or sets the ordered list of fields that participate in the composite key.
  /// </summary>
  public IList<string> KeyCriteria { get; set; }

  /// <summary>
  /// Initializes a new composite dimension.
  /// </summary>
  /// <param name="keyCriteria">The field names used to build the composite key.</param>
  public CompositeDimension(params string[] keyCriteria)
    : base("Composite", "Composite dimension")
  {
    KeyCriteria = keyCriteria ?? throw new ArgumentNullException(nameof(keyCriteria));

    if (keyCriteria.Length == 0)
      throw new ArgumentException("At least one key criterion must be provided", nameof(keyCriteria));
  }

  /// <summary>
  /// Creates and registers a composite value from a set of field values.
  /// </summary>
  public virtual CompositeDimensionValue CreateCompositeValue(IDictionary<string, object> values)
  {
    foreach (var criterion in KeyCriteria)
    {
      if (!values.ContainsKey(criterion))
        throw new ArgumentException($"Missing required key criterion: {criterion}", nameof(values));
    }

    var key = CreateKey(values);
    var label = CreateLabel(values);
    var value = new CompositeDimensionValue(key, label, values);

    AddValue(value);
    return value;
  }

  protected virtual string CreateKey(IDictionary<string, object> values)
  {
    var orderedValues = KeyCriteria
      .Select(criterion => values[criterion]?.ToString() ?? "null")
      .ToList();

    var composite = string.Join("|", orderedValues);

    var bytes = Encoding.UTF8.GetBytes(composite);
    var hash = SHA256.HashData(bytes);

    return Convert.ToBase64String(hash);
  }

  protected virtual string CreateLabel(IDictionary<string, object> values)
  {
    var parts = KeyCriteria
      .Select(criterion =>
      {
        var value = values[criterion];
        return $"{criterion}: {FormatValue(value)}";
      });

    return string.Join(", ", parts);
  }

  protected virtual string FormatValue(object value)
  {
    return value switch
    {
      null => "null",
      DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
      _ => string.Create(CultureInfo.InvariantCulture, $"{value}")
    };
  }
}

/// <summary>
/// Represents a composite dimension backed by a specific entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type used to project field values.</typeparam>
public class CompositeDimension<TEntity> : CompositeDimension
  where TEntity : new()
{
  private readonly Func<TEntity, IDictionary<string, object>>[] _propertyAccessors;

  /// <summary>
  /// Initializes a new entity-backed composite dimension.
  /// </summary>
  /// <param name="properties">The entity properties that participate in the composite key.</param>
  public CompositeDimension(params string[] properties)
    : base(properties)
  {
    _propertyAccessors = properties.Select(CreatePropertyAccessor).ToArray();
  }

  /// <summary>
  /// Creates and registers a composite value for the supplied entity instance.
  /// </summary>
  public virtual CompositeDimensionValue<TEntity> CreateCompositeValue(TEntity entity)
  {
    var entityValues = ExtractValues(entity);

    foreach (var criterion in KeyCriteria)
    {
      if (!entityValues.ContainsKey(criterion))
        throw new ArgumentException($"Missing required key criterion: {criterion}", nameof(entity));
    }

    var key = CreateKey(entityValues);
    var displayName = CreateLabel(entityValues);
    var value = new CompositeDimensionValue<TEntity>(key, displayName, entityValues, entity);

    AddValue(value);
    return value;
  }

  private Dictionary<string, object> ExtractValues(TEntity entity)
  {
    var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < KeyCriteria.Count; i++)
    {
      var propertyValues = _propertyAccessors[i](entity);
      foreach (var kvp in propertyValues)
      {
        values[kvp.Key] = kvp.Value switch
        {
          null => "",
          string s when string.IsNullOrEmpty(s) => Constants.EMPTY_LABEL,
          _ => kvp.Value,
        };
      }
    }

    return values;
  }

  private Func<TEntity, IDictionary<string, object>> CreatePropertyAccessor(string propertyName)
  {
    var entityType = typeof(TEntity);
    var property = entityType.GetProperty(propertyName) ?? throw new ArgumentException($"Property {propertyName} not found on type {entityType.Name}", nameof(propertyName));

    return entity =>
    {
      var value = property.GetValue(entity);
      return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
      {
        [propertyName] = value
      };
    };
  }
}

