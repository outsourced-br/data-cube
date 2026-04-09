namespace Outsourced.DataCube;

/// <summary>
/// Provides reusable predicates for dimension-value based filtering.
/// </summary>
public static class DimensionFilters
{
  /// <summary>
  /// Returns the supplied predicate after null validation.
  /// </summary>
  public static Func<DimensionValue, bool> Where(Func<DimensionValue, bool> predicate)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    return predicate;
  }

  /// <summary>
  /// Creates a predicate that evaluates the raw typed value of a dimension member.
  /// </summary>
  public static Func<DimensionValue, bool> Where<TValue>(Func<TValue, bool> predicate)
  {
    ArgumentNullException.ThrowIfNull(predicate);

    return dimensionValue =>
      TryGetTypedValue(dimensionValue, out TValue value) &&
      predicate(value);
  }

  /// <summary>
  /// Creates a predicate that matches exact raw values.
  /// </summary>
  public static Func<DimensionValue, bool> EqualTo<TValue>(TValue value)
  {
    var comparer = EqualityComparer<TValue>.Default;
    return Where<TValue>(candidate => comparer.Equals(candidate, value));
  }

  /// <summary>
  /// Creates a predicate that matches when the raw value is contained in the supplied set.
  /// </summary>
  public static Func<DimensionValue, bool> In<TValue>(params TValue[] values)
  {
    return In((IEnumerable<TValue>)values);
  }

  /// <summary>
  /// Creates a predicate that matches when the raw value is contained in the supplied set.
  /// </summary>
  public static Func<DimensionValue, bool> In<TValue>(IEnumerable<TValue> values)
  {
    ArgumentNullException.ThrowIfNull(values);

    var set = new HashSet<TValue>(values);
    return Where<TValue>(set.Contains);
  }

  /// <summary>
  /// Creates a predicate that matches when the raw value is not contained in the supplied set.
  /// </summary>
  public static Func<DimensionValue, bool> NotIn<TValue>(params TValue[] values)
  {
    return NotIn((IEnumerable<TValue>)values);
  }

  /// <summary>
  /// Creates a predicate that matches when the raw value is not contained in the supplied set.
  /// </summary>
  public static Func<DimensionValue, bool> NotIn<TValue>(IEnumerable<TValue> values)
  {
    ArgumentNullException.ThrowIfNull(values);

    var set = new HashSet<TValue>(values);
    return Where<TValue>(value => !set.Contains(value));
  }

  /// <summary>
  /// Creates a predicate that matches when the raw value is greater than the supplied threshold.
  /// </summary>
  public static Func<DimensionValue, bool> GreaterThan<TValue>(TValue threshold)
    where TValue : IComparable<TValue>
  {
    var comparer = Comparer<TValue>.Default;
    return Where<TValue>(value => comparer.Compare(value, threshold) > 0);
  }

  /// <summary>
  /// Creates a predicate that matches when the raw value is less than the supplied threshold.
  /// </summary>
  public static Func<DimensionValue, bool> LessThan<TValue>(TValue threshold)
    where TValue : IComparable<TValue>
  {
    var comparer = Comparer<TValue>.Default;
    return Where<TValue>(value => comparer.Compare(value, threshold) < 0);
  }

  /// <summary>
  /// Creates a predicate that matches when the raw value falls within the supplied range.
  /// </summary>
  public static Func<DimensionValue, bool> Between<TValue>(TValue minimum, TValue maximum, bool inclusive = true)
    where TValue : IComparable<TValue>
  {
    var comparer = Comparer<TValue>.Default;

    if (comparer.Compare(minimum, maximum) > 0)
      throw new ArgumentException("The minimum value must be less than or equal to the maximum value.", nameof(minimum));

    return inclusive
      ? Where<TValue>(value => comparer.Compare(value, minimum) >= 0 && comparer.Compare(value, maximum) <= 0)
      : Where<TValue>(value => comparer.Compare(value, minimum) > 0 && comparer.Compare(value, maximum) < 0);
  }

  private static bool TryGetTypedValue<TValue>(DimensionValue dimensionValue, out TValue value)
  {
    if (dimensionValue is DimensionValue<TValue> typedDimensionValue)
    {
      value = typedDimensionValue.Value;
      return true;
    }

    if (dimensionValue?.Value is TValue rawValue)
    {
      value = rawValue;
      return true;
    }

    value = default;
    return false;
  }
}
