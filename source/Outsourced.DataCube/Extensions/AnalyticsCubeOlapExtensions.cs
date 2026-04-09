using Outsourced.DataCube.Hierarchy;

namespace Outsourced.DataCube;

using Metrics;

/// <summary>
/// Provides basic OLAP-style operations over an <see cref="AnalyticsCube"/>.
/// </summary>
public static class AnalyticsCubeOlapExtensions
{
  private const string DefaultYearLevelKey = "year";
  private const string DefaultQuarterLevelKey = "quarter";
  private const string DefaultMonthLevelKey = "month";

  /// <summary>
  /// Slices the cube to fact groups whose dimension matches the supplied value.
  /// </summary>
  public static AnalyticsCube Slice(this AnalyticsCube cube, string dimensionKey, object value)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);

    var result = CreateFilteredCube(cube, $"{cube.Key}_slice_{dimensionKey}", $"{cube.Label} (Slice: {dimensionKey}={value})");

    var filter = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      [dimensionKey] = value
    };

    if (cube.TryGetExactFactGroups(filter, out var exactMatches))
    {
      result.FactGroups = exactMatches.ToList();
      return result;
    }

    AddMatchingFactGroups(
      cube,
      result,
      factGroup =>
        factGroup.DimensionValues.TryGetValue(dimensionKey, out var dimValue) &&
        Equals(dimValue.Value, value));

    return result;
  }

  /// <summary>
  /// Slices the cube to fact groups whose dimension matches the supplied predicate.
  /// </summary>
  public static AnalyticsCube Slice(this AnalyticsCube cube, string dimensionKey, Func<DimensionValue, bool> predicate)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentNullException.ThrowIfNull(predicate);

    var result = CreateFilteredCube(cube, $"{cube.Key}_slice_{dimensionKey}", $"{cube.Label} (Slice: {dimensionKey} predicate)");

    AddMatchingFactGroups(
      cube,
      result,
      factGroup =>
        factGroup.DimensionValues.TryGetValue(dimensionKey, out var dimValue) &&
        predicate(dimValue));

    return result;
  }

  /// <summary>
  /// Slices the cube to fact groups whose typed raw dimension value matches the supplied predicate.
  /// </summary>
  public static AnalyticsCube Slice<TValue>(this AnalyticsCube cube, string dimensionKey, Func<TValue, bool> predicate)
  {
    ArgumentNullException.ThrowIfNull(predicate);

    return cube.Slice(dimensionKey, DimensionFilters.Where(predicate));
  }

  /// <summary>
  /// Dices the cube using multiple dimension filters.
  /// </summary>
  public static AnalyticsCube Dice(this AnalyticsCube cube, IDictionary<string, object> filter)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(filter);

    var result = CreateFilteredCube(cube, $"{cube.Key}_dice", $"{cube.Label} (Dice)");

    if (cube.TryGetExactFactGroups(filter, out var exactMatches))
    {
      result.FactGroups = exactMatches.ToList();
      return result;
    }

    AddMatchingFactGroups(cube, result, factGroup => MatchesExactFilter(factGroup, filter));

    return result;
  }

  /// <summary>
  /// Dices the cube using per-dimension predicates.
  /// </summary>
  public static AnalyticsCube Dice(this AnalyticsCube cube, IDictionary<string, Func<DimensionValue, bool>> filter)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(filter);

    ValidatePredicateFilter(filter);

    var result = CreateFilteredCube(cube, $"{cube.Key}_dice", $"{cube.Label} (Dice)");

    AddMatchingFactGroups(cube, result, factGroup => MatchesPredicateFilter(factGroup, filter));

    return result;
  }

  /// <summary>
  /// Aggregates a metric across all fact groups in the cube.
  /// </summary>
  public static T Aggregate<T>(this AnalyticsCube cube, Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    return GetValueOrDefault<T>(EvaluateMetric(cube.FactGroups, metric));
  }

  /// <summary>
  /// Aggregates a metric by resolving each fact group's hierarchy member to the requested level.
  /// Fact groups already stored above the requested level are skipped because they cannot be distributed downward.
  /// </summary>
  public static IReadOnlyList<HierarchyMetricResult<T>> AggregateAtLevel<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string levelKey,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentException.ThrowIfNullOrEmpty(hierarchyKey);

    var (dimension, hierarchy) = ResolveHierarchyContext(cube, dimensionKey, hierarchyKey);
    return cube.AggregateAtLevel(dimension, hierarchy, levelKey, metric);
  }

  /// <summary>
  /// Aggregates a metric by resolving each fact group's hierarchy member to the requested level.
  /// Fact groups already stored above the requested level are skipped because they cannot be distributed downward.
  /// </summary>
  public static IReadOnlyList<HierarchyMetricResult<T>> AggregateAtLevel<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    string levelKey,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(metric);

    ValidateHierarchyRegistration(cube, dimension, hierarchy);

    var level = GetRequiredHierarchyLevel(hierarchy, levelKey);
    var orderedMembers = hierarchy.GetValues(level.Key).ToArray();

    return AggregateAtResolvedLevel(cube, dimension, hierarchy, level, metric, orderedMembers);
  }

  /// <summary>
  /// Rolls a hierarchy member up to its immediate parent level and aggregates that parent member.
  /// Returns <see langword="null"/> when the supplied member already belongs to the root level.
  /// </summary>
  public static HierarchyMetricResult<T> RollUp<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentException.ThrowIfNullOrEmpty(hierarchyKey);

    var (dimension, hierarchy) = ResolveHierarchyContext(cube, dimensionKey, hierarchyKey);
    var member = GetRequiredHierarchyMember(dimension, memberKey, nameof(memberKey));

    return cube.RollUp(dimension, hierarchy, member, metric);
  }

  /// <summary>
  /// Rolls a hierarchy member up to its immediate parent level and aggregates that parent member.
  /// Returns <see langword="null"/> when the supplied member already belongs to the root level.
  /// </summary>
  public static HierarchyMetricResult<T> RollUp<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(member);
    ArgumentNullException.ThrowIfNull(metric);

    ValidateHierarchyRegistration(cube, dimension, hierarchy);

    member = GetRequiredHierarchyMember(dimension, member.Key, nameof(member));
    var currentLevel = GetRequiredMappedLevel(hierarchy, member);
    var parentLevel = hierarchy.RollUp(currentLevel);

    if (parentLevel == null)
      return null;

    var parentMember = hierarchy.GetAncestor(member, parentLevel.Key);
    if (parentMember == null)
      return null;

    return AggregateAtResolvedLevel(cube, dimension, hierarchy, parentLevel, metric, [parentMember]).SingleOrDefault();
  }

  /// <summary>
  /// Drills a hierarchy member down to its immediate child level and aggregates each direct child member.
  /// Returns an empty result when the supplied member already belongs to the leaf level.
  /// </summary>
  public static IReadOnlyList<HierarchyMetricResult<T>> DrillDown<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentException.ThrowIfNullOrEmpty(hierarchyKey);

    var (dimension, hierarchy) = ResolveHierarchyContext(cube, dimensionKey, hierarchyKey);
    var member = GetRequiredHierarchyMember(dimension, memberKey, nameof(memberKey));

    return cube.DrillDown(dimension, hierarchy, member, metric);
  }

  /// <summary>
  /// Drills a hierarchy member down to its immediate child level and aggregates each direct child member.
  /// Returns an empty result when the supplied member already belongs to the leaf level.
  /// </summary>
  public static IReadOnlyList<HierarchyMetricResult<T>> DrillDown<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(member);
    ArgumentNullException.ThrowIfNull(metric);

    ValidateHierarchyRegistration(cube, dimension, hierarchy);

    member = GetRequiredHierarchyMember(dimension, member.Key, nameof(member));
    var currentLevel = GetRequiredMappedLevel(hierarchy, member);
    var childLevel = hierarchy.DrillDown(currentLevel);

    if (childLevel == null)
      return Array.Empty<HierarchyMetricResult<T>>();

    var orderedChildren = hierarchy
      .GetValues(childLevel.Key)
      .Where(child => string.Equals(hierarchy.GetParent(child)?.Key, member.Key, StringComparison.OrdinalIgnoreCase))
      .ToArray();

    return orderedChildren.Length == 0
      ? Array.Empty<HierarchyMetricResult<T>>()
      : AggregateAtResolvedLevel(cube, dimension, hierarchy, childLevel, metric, orderedChildren);
  }

  /// <summary>
  /// Aggregates a metric from the start of the requested containing period up to the supplied member.
  /// Returns <see langword="null"/> when the window has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> ToDate<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    string periodLevelKey,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentException.ThrowIfNullOrEmpty(hierarchyKey);

    var (dimension, hierarchy) = ResolveHierarchyContext(cube, dimensionKey, hierarchyKey);
    var member = GetRequiredHierarchyMember(dimension, memberKey, nameof(memberKey));

    return cube.ToDate(dimension, hierarchy, member, periodLevelKey, metric);
  }

  /// <summary>
  /// Aggregates a metric from the start of the requested containing period up to the supplied member.
  /// Returns <see langword="null"/> when the window has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> ToDate<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    string periodLevelKey,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(member);
    ArgumentNullException.ThrowIfNull(metric);

    ValidateHierarchyRegistration(cube, dimension, hierarchy);

    member = GetRequiredHierarchyMember(dimension, member.Key, nameof(member));
    var currentLevel = GetRequiredMappedLevel(hierarchy, member);
    var periodLevel = GetRequiredHierarchyLevel(hierarchy, periodLevelKey);

    if (currentLevel.Ordinal < periodLevel.Ordinal)
    {
      throw new InvalidOperationException(
        $"Dimension value '{member.Key}' belongs to level '{currentLevel.Key}' and cannot be aggregated to-date for lower level '{periodLevel.Key}'.");
    }

    var orderedMembers = GetOrderedToDateMembers(hierarchy, member, currentLevel, periodLevel, out var isPartial);
    return CreateTimeWindowResult(cube, dimension, hierarchy, currentLevel, member, orderedMembers, metric, isPartial);
  }

  /// <summary>
  /// Aggregates a metric from the start of the containing year up to the supplied member.
  /// Returns <see langword="null"/> when the window has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> YearToDate<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    Metric<T> metric)
    where T : struct
  {
    return cube.ToDate(dimensionKey, hierarchyKey, memberKey, DefaultYearLevelKey, metric);
  }

  /// <summary>
  /// Aggregates a metric from the start of the containing year up to the supplied member.
  /// Returns <see langword="null"/> when the window has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> YearToDate<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    Metric<T> metric)
    where T : struct
  {
    return cube.ToDate(dimension, hierarchy, member, DefaultYearLevelKey, metric);
  }

  /// <summary>
  /// Aggregates a metric from the start of the containing quarter up to the supplied member.
  /// Returns <see langword="null"/> when the window has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> QuarterToDate<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    Metric<T> metric)
    where T : struct
  {
    return cube.ToDate(dimensionKey, hierarchyKey, memberKey, DefaultQuarterLevelKey, metric);
  }

  /// <summary>
  /// Aggregates a metric from the start of the containing quarter up to the supplied member.
  /// Returns <see langword="null"/> when the window has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> QuarterToDate<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    Metric<T> metric)
    where T : struct
  {
    return cube.ToDate(dimension, hierarchy, member, DefaultQuarterLevelKey, metric);
  }

  /// <summary>
  /// Aggregates a metric from the start of the containing month up to the supplied member.
  /// Returns <see langword="null"/> when the window has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> MonthToDate<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    Metric<T> metric)
    where T : struct
  {
    return cube.ToDate(dimensionKey, hierarchyKey, memberKey, DefaultMonthLevelKey, metric);
  }

  /// <summary>
  /// Aggregates a metric from the start of the containing month up to the supplied member.
  /// Returns <see langword="null"/> when the window has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> MonthToDate<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    Metric<T> metric)
    where T : struct
  {
    return cube.ToDate(dimension, hierarchy, member, DefaultMonthLevelKey, metric);
  }

  /// <summary>
  /// Aggregates the immediately preceding mapped period at the supplied member's hierarchy level.
  /// Returns <see langword="null"/> when no prior period exists or when the prior period has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> PreviousPeriod<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentException.ThrowIfNullOrEmpty(hierarchyKey);

    var (dimension, hierarchy) = ResolveHierarchyContext(cube, dimensionKey, hierarchyKey);
    var member = GetRequiredHierarchyMember(dimension, memberKey, nameof(memberKey));

    return cube.PreviousPeriod(dimension, hierarchy, member, metric);
  }

  /// <summary>
  /// Aggregates the immediately preceding mapped period at the supplied member's hierarchy level.
  /// Returns <see langword="null"/> when no prior period exists or when the prior period has no contributing metric values.
  /// </summary>
  public static TimeWindowMetricResult<T> PreviousPeriod<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(member);
    ArgumentNullException.ThrowIfNull(metric);

    ValidateHierarchyRegistration(cube, dimension, hierarchy);

    member = GetRequiredHierarchyMember(dimension, member.Key, nameof(member));
    var previousMember = GetPreviousLevelMember(hierarchy, member);
    if (previousMember == null)
      return null;

    var previousLevel = GetRequiredMappedLevel(hierarchy, previousMember);
    return CreateTimeWindowResult(cube, dimension, hierarchy, previousLevel, previousMember, [previousMember], metric, isPartial: false);
  }

  /// <summary>
  /// Compares the supplied member's period to the immediately preceding mapped period at the same level.
  /// Returns <see langword="null"/> when the current period has no contributing metric values.
  /// </summary>
  public static TimePeriodComparisonResult<T> PeriodOverPeriodChange<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    Metric<T> metric)
    where T : struct, System.Numerics.INumber<T>
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentException.ThrowIfNullOrEmpty(hierarchyKey);

    var (dimension, hierarchy) = ResolveHierarchyContext(cube, dimensionKey, hierarchyKey);
    var member = GetRequiredHierarchyMember(dimension, memberKey, nameof(memberKey));

    return cube.PeriodOverPeriodChange(dimension, hierarchy, member, metric);
  }

  /// <summary>
  /// Compares the supplied member's period to the immediately preceding mapped period at the same level.
  /// Returns <see langword="null"/> when the current period has no contributing metric values.
  /// </summary>
  public static TimePeriodComparisonResult<T> PeriodOverPeriodChange<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    Metric<T> metric)
    where T : struct, System.Numerics.INumber<T>
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(member);
    ArgumentNullException.ThrowIfNull(metric);

    ValidateHierarchyRegistration(cube, dimension, hierarchy);

    member = GetRequiredHierarchyMember(dimension, member.Key, nameof(member));
    var currentLevel = GetRequiredMappedLevel(hierarchy, member);
    var currentPeriod = CreateTimeWindowResult(cube, dimension, hierarchy, currentLevel, member, [member], metric, isPartial: false);
    if (currentPeriod == null)
      return null;

    var previousPeriod = cube.PreviousPeriod(dimension, hierarchy, member, metric);
    return CreatePeriodComparisonResult(currentPeriod, previousPeriod);
  }

  /// <summary>
  /// Compares the supplied member's period to the equivalent member in the previous year.
  /// Returns <see langword="null"/> when the current period has no contributing metric values.
  /// </summary>
  public static TimePeriodComparisonResult<T> YearOverYearChange<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    Metric<T> metric)
    where T : struct, System.Numerics.INumber<T>
  {
    return cube.YearOverYearChange(dimensionKey, hierarchyKey, memberKey, DefaultYearLevelKey, metric);
  }

  /// <summary>
  /// Compares the supplied member's period to the equivalent member in the previous year.
  /// Returns <see langword="null"/> when the current period has no contributing metric values.
  /// </summary>
  public static TimePeriodComparisonResult<T> YearOverYearChange<T>(
    this AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey,
    string memberKey,
    string yearLevelKey,
    Metric<T> metric)
    where T : struct, System.Numerics.INumber<T>
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentException.ThrowIfNullOrEmpty(hierarchyKey);

    var (dimension, hierarchy) = ResolveHierarchyContext(cube, dimensionKey, hierarchyKey);
    var member = GetRequiredHierarchyMember(dimension, memberKey, nameof(memberKey));

    return cube.YearOverYearChange(dimension, hierarchy, member, yearLevelKey, metric);
  }

  /// <summary>
  /// Compares the supplied member's period to the equivalent member in the previous year.
  /// Returns <see langword="null"/> when the current period has no contributing metric values.
  /// </summary>
  public static TimePeriodComparisonResult<T> YearOverYearChange<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    Metric<T> metric)
    where T : struct, System.Numerics.INumber<T>
  {
    return cube.YearOverYearChange(dimension, hierarchy, member, DefaultYearLevelKey, metric);
  }

  /// <summary>
  /// Compares the supplied member's period to the equivalent member in the previous year.
  /// Returns <see langword="null"/> when the current period has no contributing metric values.
  /// </summary>
  public static TimePeriodComparisonResult<T> YearOverYearChange<T>(
    this AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    DimensionValue member,
    string yearLevelKey,
    Metric<T> metric)
    where T : struct, System.Numerics.INumber<T>
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(member);
    ArgumentNullException.ThrowIfNull(metric);

    ValidateHierarchyRegistration(cube, dimension, hierarchy);

    member = GetRequiredHierarchyMember(dimension, member.Key, nameof(member));
    var currentLevel = GetRequiredMappedLevel(hierarchy, member);
    var currentPeriod = CreateTimeWindowResult(cube, dimension, hierarchy, currentLevel, member, [member], metric, isPartial: false);
    if (currentPeriod == null)
      return null;

    var previousYearMember = GetEquivalentMemberInPreviousYear(hierarchy, member, yearLevelKey);
    var previousPeriod = previousYearMember == null
      ? null
      : CreateTimeWindowResult(cube, dimension, hierarchy, currentLevel, previousYearMember, [previousYearMember], metric, isPartial: false);

    return CreatePeriodComparisonResult(currentPeriod, previousPeriod);
  }

  /// <summary>
  /// Pivots the cube by a dimension key and aggregates each bucket using the supplied metric definition.
  /// </summary>
  public static IDictionary<object, T> Pivot<T>(this AnalyticsCube cube, string dimensionKey, Metric<T> metric)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(dimensionKey);
    ArgumentNullException.ThrowIfNull(metric);

    var groupedFactGroups = new Dictionary<object, List<FactGroup>>();

    var factGroups = cube.FactGroups as List<FactGroup> ?? cube.FactGroups.ToList();

    foreach (var factGroup in factGroups)
    {
      if (!TryGetDimensionValue(factGroup, dimensionKey, out var dimValue))
        continue;

      var value = dimValue.Value;
      if (!groupedFactGroups.TryGetValue(value, out var groupedBucket))
      {
        groupedBucket = new List<FactGroup>();
        groupedFactGroups[value] = groupedBucket;
      }

      groupedBucket.Add(factGroup);
    }

    return groupedFactGroups
      .Select(kvp => new
      {
        kvp.Key,
        Evaluation = EvaluateMetric(kvp.Value, metric)
      })
      .Where(result => result.Evaluation.HasValue)
      .ToDictionary(
        result => result.Key,
        result => GetValueOrDefault<T>(result.Evaluation));
  }

  /// <summary>
  /// Groups the cube by one or more dimension keys and aggregates the supplied metric for each grouped coordinate.
  /// </summary>
  public static IReadOnlyList<GroupedMetricResult<T>> GroupBy<T>(
    this AnalyticsCube cube,
    IEnumerable<string> dimensionKeys,
    Metric<T> metric,
    GroupTotalsOptions totalsOptions = null)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(dimensionKeys);
    ArgumentNullException.ThrowIfNull(metric);

    totalsOptions ??= new GroupTotalsOptions();

    var normalizedDimensionKeys = NormalizeDimensionKeys(cube, dimensionKeys, nameof(dimensionKeys));

    var orderedLeafCoordinates = new List<DimensionCoordinate>();
    var orderedSubtotalCoordinates = CreateRollupBuckets(normalizedDimensionKeys.Length);
    DimensionCoordinate grandTotalCoordinate = null;
    var coordinatesByKey = new Dictionary<string, DimensionCoordinate>(StringComparer.Ordinal);
    var factGroupsByKey = new Dictionary<string, List<FactGroup>>(StringComparer.Ordinal);

    foreach (var factGroup in cube.FactGroups)
    {
      if (!TryCreateCoordinate(factGroup, normalizedDimensionKeys, out var leafCoordinate))
        continue;

      foreach (var coordinate in ExpandRollupCoordinates(
                 leafCoordinate,
                 totalsOptions.IncludeSubtotals,
                 totalsOptions.IncludeGrandTotal))
      {
        if (coordinatesByKey.TryAdd(coordinate.CoordinateKey, coordinate))
        {
          RegisterOrderedCoordinate(
            coordinate,
            orderedLeafCoordinates,
            orderedSubtotalCoordinates,
            ref grandTotalCoordinate);

          factGroupsByKey[coordinate.CoordinateKey] = new List<FactGroup>();
        }

        factGroupsByKey[coordinate.CoordinateKey].Add(factGroup);
      }
    }

    return ComposeOrderedCoordinates(orderedLeafCoordinates, orderedSubtotalCoordinates, grandTotalCoordinate)
      .Select(coordinate => new
      {
        Coordinate = coordinate,
        Evaluation = EvaluateMetric(factGroupsByKey[coordinate.CoordinateKey], metric)
      })
      .Where(result => result.Evaluation.HasValue)
      .Select(result => new GroupedMetricResult<T>(result.Coordinate, GetValueOrDefault<T>(result.Evaluation)))
      .ToArray();
  }

  /// <summary>
  /// Groups the cube by one or more dimension keys and aggregates the supplied metric for each grouped coordinate.
  /// </summary>
  public static IReadOnlyList<GroupedMetricResult<T>> GroupBy<T>(this AnalyticsCube cube, Metric<T> metric, params string[] dimensionKeys)
    where T : struct
  {
    return cube.GroupBy((IEnumerable<string>)dimensionKeys, metric);
  }

  /// <summary>
  /// Groups the cube by one or more dimension keys and aggregates the supplied metric for each grouped coordinate.
  /// </summary>
  public static IReadOnlyList<GroupedMetricResult<T>> GroupBy<T>(
    this AnalyticsCube cube,
    Metric<T> metric,
    GroupTotalsOptions totalsOptions,
    params string[] dimensionKeys)
    where T : struct
  {
    return cube.GroupBy((IEnumerable<string>)dimensionKeys, metric, totalsOptions);
  }

  /// <summary>
  /// Pivots the cube across a single axis using one or more dimensions and returns structured grouped results.
  /// </summary>
  public static IReadOnlyList<GroupedMetricResult<T>> Pivot<T>(
    this AnalyticsCube cube,
    IEnumerable<string> dimensionKeys,
    Metric<T> metric,
    GroupTotalsOptions totalsOptions = null)
    where T : struct
  {
    return cube.GroupBy(dimensionKeys, metric, totalsOptions);
  }

  /// <summary>
  /// Pivots the cube into a two-axis cross-tab using ordered row and column dimensions.
  /// </summary>
  public static PivotResult<T> Pivot<T>(
    this AnalyticsCube cube,
    IEnumerable<string> rowDimensionKeys,
    IEnumerable<string> columnDimensionKeys,
    Metric<T> metric,
    PivotTotalsOptions totalsOptions = null)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(rowDimensionKeys);
    ArgumentNullException.ThrowIfNull(columnDimensionKeys);
    ArgumentNullException.ThrowIfNull(metric);

    totalsOptions ??= new PivotTotalsOptions();

    var normalizedRowKeys = NormalizeDimensionKeys(cube, rowDimensionKeys, nameof(rowDimensionKeys));
    var normalizedColumnKeys = NormalizeDimensionKeys(cube, columnDimensionKeys, nameof(columnDimensionKeys));
    ValidateDistinctAxes(normalizedRowKeys, normalizedColumnKeys);

    var orderedLeafRows = new List<DimensionCoordinate>();
    var orderedLeafColumns = new List<DimensionCoordinate>();
    var orderedSubtotalRows = CreateRollupBuckets(normalizedRowKeys.Length);
    var orderedSubtotalColumns = CreateRollupBuckets(normalizedColumnKeys.Length);
    DimensionCoordinate grandTotalRow = null;
    DimensionCoordinate grandTotalColumn = null;
    var rowsByKey = new Dictionary<string, DimensionCoordinate>(StringComparer.Ordinal);
    var columnsByKey = new Dictionary<string, DimensionCoordinate>(StringComparer.Ordinal);
    var cellFactGroups = new Dictionary<string, List<FactGroup>>(StringComparer.Ordinal);

    var includeRowGrandTotal = totalsOptions.IncludeColumnTotals || totalsOptions.IncludeGrandTotal;
    var includeColumnGrandTotal = totalsOptions.IncludeRowTotals || totalsOptions.IncludeGrandTotal;

    foreach (var factGroup in cube.FactGroups)
    {
      if (!CanContributeToMetric(factGroup, metric))
        continue;

      if (!TryCreateCoordinate(factGroup, normalizedRowKeys, out var leafRowCoordinate) ||
          !TryCreateCoordinate(factGroup, normalizedColumnKeys, out var leafColumnCoordinate))
      {
        continue;
      }

      var rowCoordinates = ExpandRollupCoordinates(
        leafRowCoordinate,
        totalsOptions.IncludeRowSubtotals,
        includeRowGrandTotal).ToArray();
      var columnCoordinates = ExpandRollupCoordinates(
        leafColumnCoordinate,
        totalsOptions.IncludeColumnSubtotals,
        includeColumnGrandTotal).ToArray();

      foreach (var rowCoordinate in rowCoordinates)
      {
        if (rowsByKey.TryAdd(rowCoordinate.CoordinateKey, rowCoordinate))
        {
          RegisterOrderedCoordinate(
            rowCoordinate,
            orderedLeafRows,
            orderedSubtotalRows,
            ref grandTotalRow);
        }

        foreach (var columnCoordinate in columnCoordinates)
        {
          if (columnsByKey.TryAdd(columnCoordinate.CoordinateKey, columnCoordinate))
          {
            RegisterOrderedCoordinate(
              columnCoordinate,
              orderedLeafColumns,
              orderedSubtotalColumns,
              ref grandTotalColumn);
          }

          var cellKey = CreateCellKey(rowCoordinate.CoordinateKey, columnCoordinate.CoordinateKey);
          if (!cellFactGroups.TryGetValue(cellKey, out var factGroups))
          {
            factGroups = new List<FactGroup>();
            cellFactGroups[cellKey] = factGroups;
          }

          factGroups.Add(factGroup);
        }
      }
    }

    var orderedRows = ComposeOrderedCoordinates(orderedLeafRows, orderedSubtotalRows, grandTotalRow);
    var orderedColumns = ComposeOrderedCoordinates(orderedLeafColumns, orderedSubtotalColumns, grandTotalColumn);
    var cells = new List<PivotCell<T>>();

    foreach (var row in orderedRows)
    {
      foreach (var column in orderedColumns)
      {
        var cellKey = CreateCellKey(row.CoordinateKey, column.CoordinateKey);
        if (!cellFactGroups.TryGetValue(cellKey, out var factGroups))
          continue;

        var evaluation = EvaluateMetric(factGroups, metric);
        if (!evaluation.HasValue)
          continue;

        cells.Add(new PivotCell<T>(row, column, GetValueOrDefault<T>(evaluation)));
      }
    }

    if (cells.Count > 0)
    {
      var populatedRowKeys = new HashSet<string>(cells.Select(static cell => cell.RowKey.CoordinateKey), StringComparer.Ordinal);
      var populatedColumnKeys = new HashSet<string>(cells.Select(static cell => cell.ColumnKey.CoordinateKey), StringComparer.Ordinal);

      orderedRows = orderedRows.Where(row => populatedRowKeys.Contains(row.CoordinateKey)).ToArray();
      orderedColumns = orderedColumns.Where(column => populatedColumnKeys.Contains(column.CoordinateKey)).ToArray();
    }
    else
    {
      orderedRows = Array.Empty<DimensionCoordinate>();
      orderedColumns = Array.Empty<DimensionCoordinate>();
    }

    return new PivotResult<T>(orderedRows, orderedColumns, cells);
  }

  /// <summary>
  /// Pivots the cube into a two-axis cross-tab using one row dimension and one column dimension.
  /// </summary>
  public static PivotResult<T> Pivot<T>(
    this AnalyticsCube cube,
    string rowDimensionKey,
    string columnDimensionKey,
    Metric<T> metric,
    PivotTotalsOptions totalsOptions = null)
    where T : struct
  {
    return cube.Pivot(new[] { rowDimensionKey }, new[] { columnDimensionKey }, metric, totalsOptions);
  }

  private static AnalyticsCube CreateFilteredCube(AnalyticsCube cube, string key, string label)
  {
    return new AnalyticsCube
    {
      Key = key,
      Label = label,
      Dimensions = cube.Dimensions,
      Metrics = cube.Metrics,
      Grain = cube.Grain,
    };
  }

  private static IReadOnlyList<HierarchyMetricResult<T>> AggregateAtResolvedLevel<T>(
    AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    HierarchyLevel level,
    Metric<T> metric,
    IEnumerable<DimensionValue> orderedMembers)
    where T : struct
  {
    var members = orderedMembers
      .Where(static member => member != null)
      .ToArray();

    if (members.Length == 0)
      return Array.Empty<HierarchyMetricResult<T>>();

    var allowedMemberKeys = new HashSet<string>(
      members.Select(static member => member.Key),
      StringComparer.OrdinalIgnoreCase);
    var factGroupsByMemberKey = new Dictionary<string, List<FactGroup>>(StringComparer.OrdinalIgnoreCase);

    foreach (var factGroup in cube.FactGroups)
    {
      if (!TryGetDimensionValue(factGroup, dimension.Key, out var dimensionValue) ||
          !TryResolveHierarchyMemberAtLevel(hierarchy, dimensionValue, level, out var memberAtLevel) ||
          !allowedMemberKeys.Contains(memberAtLevel.Key))
      {
        continue;
      }

      if (!factGroupsByMemberKey.TryGetValue(memberAtLevel.Key, out var factGroups))
      {
        factGroups = new List<FactGroup>();
        factGroupsByMemberKey[memberAtLevel.Key] = factGroups;
      }

      factGroups.Add(factGroup);
    }

    var results = new List<HierarchyMetricResult<T>>(factGroupsByMemberKey.Count);

    foreach (var member in members)
    {
      if (!factGroupsByMemberKey.TryGetValue(member.Key, out var factGroups))
        continue;

      var evaluation = EvaluateMetric(factGroups, metric);
      if (!evaluation.HasValue)
        continue;

      results.Add(new HierarchyMetricResult<T>(dimension, hierarchy, level, member, GetValueOrDefault<T>(evaluation)));
    }

    return results;
  }

  private static TimeWindowMetricResult<T> CreateTimeWindowResult<T>(
    AnalyticsCube cube,
    Dimension dimension,
    Hierarchy.Hierarchy hierarchy,
    HierarchyLevel level,
    DimensionValue anchorMember,
    IEnumerable<DimensionValue> orderedMembers,
    Metric<T> metric,
    bool isPartial)
    where T : struct
  {
    var members = orderedMembers
      .Where(static member => member != null)
      .ToArray();

    if (members.Length == 0)
      return null;

    var allowedMemberKeys = new HashSet<string>(
      members.Select(static member => member.Key),
      StringComparer.OrdinalIgnoreCase);
    var contributingFactGroups = new List<FactGroup>();

    foreach (var factGroup in cube.FactGroups)
    {
      if (!TryGetDimensionValue(factGroup, dimension.Key, out var dimensionValue) ||
          !TryResolveHierarchyMemberAtLevel(hierarchy, dimensionValue, level, out var memberAtLevel) ||
          !allowedMemberKeys.Contains(memberAtLevel.Key))
      {
        continue;
      }

      contributingFactGroups.Add(factGroup);
    }

    var evaluation = EvaluateMetric(contributingFactGroups, metric);
    if (!evaluation.HasValue)
      return null;

    return new TimeWindowMetricResult<T>(
      dimension,
      hierarchy,
      level,
      anchorMember,
      members,
      GetValueOrDefault<T>(evaluation),
      isPartial);
  }

  private static IReadOnlyList<DimensionValue> GetOrderedToDateMembers(
    Hierarchy.Hierarchy hierarchy,
    DimensionValue anchorMember,
    HierarchyLevel currentLevel,
    HierarchyLevel periodLevel,
    out bool isPartial)
  {
    var scopeMember = currentLevel.Ordinal == periodLevel.Ordinal
      ? anchorMember
      : hierarchy.GetAncestor(anchorMember, periodLevel.Key)
        ?? throw new InvalidOperationException(
          $"Dimension value '{anchorMember.Key}' does not have an ancestor at level '{periodLevel.Key}' in hierarchy '{hierarchy.Key}'.");

    var orderedLevelMembers = hierarchy.GetValues(currentLevel.Key).ToArray();
    var members = new List<DimensionValue>();
    var foundAnchor = false;
    var hasLaterMembersInScope = false;

    foreach (var candidate in orderedLevelMembers)
    {
      if (!BelongsToScope(hierarchy, candidate, currentLevel, periodLevel, scopeMember))
        continue;

      if (!foundAnchor)
      {
        members.Add(candidate);
        if (string.Equals(candidate.Key, anchorMember.Key, StringComparison.OrdinalIgnoreCase))
          foundAnchor = true;

        continue;
      }

      hasLaterMembersInScope = true;
    }

    if (!foundAnchor)
    {
      throw new InvalidOperationException(
        $"Dimension value '{anchorMember.Key}' was not found in the ordered members for level '{currentLevel.Key}'.");
    }

    isPartial = hasLaterMembersInScope;
    return members;
  }

  private static bool BelongsToScope(
    Hierarchy.Hierarchy hierarchy,
    DimensionValue candidate,
    HierarchyLevel currentLevel,
    HierarchyLevel periodLevel,
    DimensionValue scopeMember)
  {
    if (currentLevel.Ordinal == periodLevel.Ordinal)
      return string.Equals(candidate.Key, scopeMember.Key, StringComparison.OrdinalIgnoreCase);

    return string.Equals(
      hierarchy.GetAncestor(candidate, periodLevel.Key)?.Key,
      scopeMember.Key,
      StringComparison.OrdinalIgnoreCase);
  }

  private static DimensionValue GetPreviousLevelMember(Hierarchy.Hierarchy hierarchy, DimensionValue member)
  {
    var currentLevel = GetRequiredMappedLevel(hierarchy, member);
    var orderedMembers = hierarchy.GetValues(currentLevel.Key).ToArray();
    var memberIndex = FindMemberIndex(orderedMembers, member);

    if (memberIndex <= 0)
      return null;

    return orderedMembers[memberIndex - 1];
  }

  private static DimensionValue GetEquivalentMemberInPreviousYear(Hierarchy.Hierarchy hierarchy, DimensionValue member, string yearLevelKey)
  {
    var currentLevel = GetRequiredMappedLevel(hierarchy, member);
    var yearLevel = GetRequiredHierarchyLevel(hierarchy, yearLevelKey);

    if (currentLevel.Ordinal < yearLevel.Ordinal)
    {
      throw new InvalidOperationException(
        $"Dimension value '{member.Key}' belongs to level '{currentLevel.Key}' and cannot be compared year-over-year using lower level '{yearLevel.Key}'.");
    }

    var currentYearMember = currentLevel.Ordinal == yearLevel.Ordinal
      ? member
      : hierarchy.GetAncestor(member, yearLevel.Key)
        ?? throw new InvalidOperationException(
          $"Dimension value '{member.Key}' does not have an ancestor at year level '{yearLevel.Key}' in hierarchy '{hierarchy.Key}'.");

    var previousYearMember = GetPreviousLevelMember(hierarchy, currentYearMember);
    if (previousYearMember == null)
      return null;

    if (currentLevel.Ordinal == yearLevel.Ordinal)
      return previousYearMember;

    var path = hierarchy.GetPath(member);
    var equivalentMember = previousYearMember;

    for (var ordinal = yearLevel.Ordinal + 1; ordinal <= currentLevel.Ordinal; ordinal++)
    {
      var currentParent = path[ordinal - 1];
      var currentMember = path[ordinal];
      var childLevel = hierarchy.Levels[ordinal];

      var currentSiblings = GetOrderedChildren(hierarchy, currentParent, childLevel);
      var siblingIndex = FindMemberIndex(currentSiblings, currentMember);
      if (siblingIndex < 0)
        return null;

      var previousSiblings = GetOrderedChildren(hierarchy, equivalentMember, childLevel);
      if (siblingIndex >= previousSiblings.Count)
        return null;

      equivalentMember = previousSiblings[siblingIndex];
    }

    return equivalentMember;
  }

  private static IReadOnlyList<DimensionValue> GetOrderedChildren(Hierarchy.Hierarchy hierarchy, DimensionValue parentMember, HierarchyLevel childLevel)
  {
    return hierarchy
      .GetValues(childLevel.Key)
      .Where(child => string.Equals(hierarchy.GetParent(child)?.Key, parentMember.Key, StringComparison.OrdinalIgnoreCase))
      .ToArray();
  }

  private static int FindMemberIndex(IReadOnlyList<DimensionValue> members, DimensionValue member)
  {
    for (var index = 0; index < members.Count; index++)
    {
      if (string.Equals(members[index].Key, member.Key, StringComparison.OrdinalIgnoreCase))
        return index;
    }

    return -1;
  }

  private static TimePeriodComparisonResult<T> CreatePeriodComparisonResult<T>(
    TimeWindowMetricResult<T> currentPeriod,
    TimeWindowMetricResult<T> previousPeriod)
    where T : struct, System.Numerics.INumber<T>
  {
    if (previousPeriod == null)
      return new TimePeriodComparisonResult<T>(currentPeriod, previousPeriod, delta: null, percentChange: null);

    var delta = currentPeriod.Value - previousPeriod.Value;
    var percentChange = previousPeriod.Value == T.Zero
      ? (decimal?)null
      : decimal.CreateChecked(delta) / decimal.CreateChecked(previousPeriod.Value);

    return new TimePeriodComparisonResult<T>(currentPeriod, previousPeriod, delta, percentChange);
  }

  private static bool TryResolveHierarchyMemberAtLevel(
    Hierarchy.Hierarchy hierarchy,
    DimensionValue value,
    HierarchyLevel targetLevel,
    out DimensionValue memberAtLevel)
  {
    var currentLevel = hierarchy.GetLevelForValue(value);

    if (currentLevel == null)
    {
      memberAtLevel = null;
      return false;
    }

    if (currentLevel.Ordinal == targetLevel.Ordinal)
    {
      memberAtLevel = value;
      return true;
    }

    if (currentLevel.Ordinal > targetLevel.Ordinal)
    {
      memberAtLevel = hierarchy.GetAncestor(value, targetLevel.Key);
      return memberAtLevel != null;
    }

    memberAtLevel = null;
    return false;
  }

  private static void AddMatchingFactGroups(AnalyticsCube cube, AnalyticsCube result, Func<FactGroup, bool> predicate)
  {
    foreach (var factGroup in cube.FactGroups)
    {
      if (predicate(factGroup))
        result.FactGroups.Add(factGroup);
    }
  }

  private static bool MatchesExactFilter(FactGroup factGroup, IDictionary<string, object> filter)
  {
    foreach (var kvp in filter)
    {
      if (!factGroup.DimensionValues.TryGetValue(kvp.Key, out var dimValue) || !Equals(dimValue.Value, kvp.Value))
        return false;
    }

    return true;
  }

  private static bool MatchesPredicateFilter(FactGroup factGroup, IDictionary<string, Func<DimensionValue, bool>> filter)
  {
    foreach (var kvp in filter)
    {
      if (!factGroup.DimensionValues.TryGetValue(kvp.Key, out var dimValue) || !kvp.Value(dimValue))
        return false;
    }

    return true;
  }

  private static (Dimension Dimension, Hierarchy.Hierarchy Hierarchy) ResolveHierarchyContext(
    AnalyticsCube cube,
    string dimensionKey,
    string hierarchyKey)
  {
    if (!cube.Dimensions.TryGetValue(dimensionKey, out var dimension))
      throw new KeyNotFoundException($"Dimension {dimensionKey} not found");

    var hierarchy = dimension.GetHierarchy(hierarchyKey);
    if (hierarchy == null)
      throw new KeyNotFoundException($"Hierarchy '{hierarchyKey}' was not found on dimension '{dimension.Key}'.");

    return (dimension, hierarchy);
  }

  private static void ValidateHierarchyRegistration(AnalyticsCube cube, Dimension dimension, Hierarchy.Hierarchy hierarchy)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(dimension);
    ArgumentNullException.ThrowIfNull(hierarchy);

    if (!cube.Dimensions.TryGetValue(dimension.Key, out var registeredDimension) || !ReferenceEquals(registeredDimension, dimension))
      throw new InvalidOperationException($"Dimension '{dimension.Key}' is not registered on cube '{cube.Key}'.");

    if (!ReferenceEquals(dimension.GetHierarchy(hierarchy.Key), hierarchy))
      throw new InvalidOperationException($"Hierarchy '{hierarchy.Key}' is not registered on dimension '{dimension.Key}'.");
  }

  private static HierarchyLevel GetRequiredHierarchyLevel(Hierarchy.Hierarchy hierarchy, string levelKey)
  {
    ArgumentException.ThrowIfNullOrEmpty(levelKey);

    return hierarchy.GetLevel(levelKey)
           ?? throw new KeyNotFoundException($"Hierarchy level '{levelKey}' was not found in hierarchy '{hierarchy.Key}'.");
  }

  private static DimensionValue GetRequiredHierarchyMember(Dimension dimension, string memberKey, string parameterName)
  {
    ArgumentException.ThrowIfNullOrEmpty(memberKey, parameterName);

    return dimension.GetValue(memberKey)
           ?? throw new KeyNotFoundException($"Dimension value '{memberKey}' was not found on dimension '{dimension.Key}'.");
  }

  private static HierarchyLevel GetRequiredMappedLevel(Hierarchy.Hierarchy hierarchy, DimensionValue member)
  {
    return hierarchy.GetLevelForValue(member)
           ?? throw new InvalidOperationException($"Dimension value '{member.Key}' is not mapped in hierarchy '{hierarchy.Key}'.");
  }

  private static void ValidatePredicateFilter(IDictionary<string, Func<DimensionValue, bool>> filter)
  {
    foreach (var kvp in filter)
    {
      ArgumentException.ThrowIfNullOrEmpty(kvp.Key);
      ArgumentNullException.ThrowIfNull(kvp.Value);
    }
  }

  private static List<DimensionCoordinate>[] CreateRollupBuckets(int dimensionCount)
  {
    if (dimensionCount <= 1)
      return [];

    var buckets = new List<DimensionCoordinate>[dimensionCount - 1];
    for (var index = 0; index < buckets.Length; index++)
      buckets[index] = [];

    return buckets;
  }

  private static IEnumerable<DimensionCoordinate> ExpandRollupCoordinates(
    DimensionCoordinate leafCoordinate,
    bool includeSubtotals,
    bool includeGrandTotal)
  {
    yield return leafCoordinate;

    if (includeSubtotals)
    {
      for (var prefixLength = leafCoordinate.Parts.Count - 1; prefixLength >= 1; prefixLength--)
        yield return CreateRollupCoordinate(leafCoordinate, prefixLength);
    }

    if (includeGrandTotal)
      yield return CreateRollupCoordinate(leafCoordinate, 0);
  }

  private static DimensionCoordinate CreateRollupCoordinate(DimensionCoordinate leafCoordinate, int prefixLength)
  {
    var parts = new DimensionCoordinatePart[leafCoordinate.Parts.Count];

    for (var index = 0; index < leafCoordinate.Parts.Count; index++)
    {
      var sourcePart = leafCoordinate.Parts[index];
      parts[index] = new DimensionCoordinatePart(
        sourcePart.DimensionKey,
        index < prefixLength ? sourcePart.Value : TotalDimensionValue.All);
    }

    return new DimensionCoordinate(parts);
  }

  private static void RegisterOrderedCoordinate(
    DimensionCoordinate coordinate,
    ICollection<DimensionCoordinate> orderedLeafCoordinates,
    IReadOnlyList<List<DimensionCoordinate>> orderedSubtotalCoordinates,
    ref DimensionCoordinate grandTotalCoordinate)
  {
    switch (coordinate.Kind)
    {
      case RollupKind.Leaf:
        orderedLeafCoordinates.Add(coordinate);
        break;
      case RollupKind.Subtotal:
        orderedSubtotalCoordinates[coordinate.NonTotalPartCount - 1].Add(coordinate);
        break;
      case RollupKind.GrandTotal:
        grandTotalCoordinate ??= coordinate;
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate.Kind, "Unsupported roll-up kind.");
    }
  }

  private static IReadOnlyList<DimensionCoordinate> ComposeOrderedCoordinates(
    IReadOnlyList<DimensionCoordinate> orderedLeafCoordinates,
    IReadOnlyList<List<DimensionCoordinate>> orderedSubtotalCoordinates,
    DimensionCoordinate grandTotalCoordinate)
  {
    var coordinates = new List<DimensionCoordinate>(orderedLeafCoordinates.Count + orderedSubtotalCoordinates.Sum(static bucket => bucket.Count) + (grandTotalCoordinate is null ? 0 : 1));
    coordinates.AddRange(orderedLeafCoordinates);

    for (var index = orderedSubtotalCoordinates.Count - 1; index >= 0; index--)
      coordinates.AddRange(orderedSubtotalCoordinates[index]);

    if (grandTotalCoordinate is not null)
      coordinates.Add(grandTotalCoordinate);

    return coordinates;
  }

  private static string[] NormalizeDimensionKeys(AnalyticsCube cube, IEnumerable<string> dimensionKeys, string parameterName)
  {
    var normalizedKeys = new List<string>();
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var dimensionKey in dimensionKeys)
    {
      ArgumentException.ThrowIfNullOrEmpty(dimensionKey);

      if (!cube.Dimensions.TryGetValue(dimensionKey, out var dimension))
        throw new KeyNotFoundException($"Dimension {dimensionKey} not found");

      if (!seen.Add(dimension.Key))
        throw new ArgumentException($"Dimension '{dimension.Key}' can only be used once", parameterName);

      normalizedKeys.Add(dimension.Key);
    }

    if (normalizedKeys.Count == 0)
      throw new ArgumentException("At least one dimension key is required", parameterName);

    return normalizedKeys.ToArray();
  }

  private static void ValidateDistinctAxes(IReadOnlyList<string> rowDimensionKeys, IReadOnlyList<string> columnDimensionKeys)
  {
    var rowKeys = new HashSet<string>(rowDimensionKeys, StringComparer.OrdinalIgnoreCase);

    foreach (var columnKey in columnDimensionKeys)
    {
      if (rowKeys.Contains(columnKey))
        throw new ArgumentException($"Dimension '{columnKey}' cannot be used on both row and column axes", nameof(columnDimensionKeys));
    }
  }

  private static bool TryCreateCoordinate(FactGroup factGroup, IReadOnlyList<string> dimensionKeys, out DimensionCoordinate coordinate)
  {
    var parts = new DimensionCoordinatePart[dimensionKeys.Count];

    for (var index = 0; index < dimensionKeys.Count; index++)
    {
      var dimensionKey = dimensionKeys[index];
      if (!TryGetDimensionValue(factGroup, dimensionKey, out var dimensionValue))
      {
        coordinate = null;
        return false;
      }

      parts[index] = new DimensionCoordinatePart(dimensionKey, dimensionValue);
    }

    coordinate = new DimensionCoordinate(parts);
    return true;
  }

  private static bool TryGetDimensionValue(FactGroup factGroup, string dimensionKey, out DimensionValue dimensionValue)
  {
    if (factGroup.DimensionValues is Collections.FlatStringDictionary<DimensionValue> typedDimensions)
      return typedDimensions.TryGetValue(dimensionKey, out dimensionValue);

    return factGroup.DimensionValues.TryGetValue(dimensionKey, out dimensionValue);
  }

  private static bool TryGetMetricValue<T>(FactGroup factGroup, string metricKey, MetricType metricType, out T value)
    where T : struct
  {
    if (factGroup.MetricCollections is Collections.MetricTypeDictionary typedCollections)
    {
      if (!typedCollections.TryGetValue(metricType, out var metricCollection) || metricCollection is not MetricCollection<T> typedCollection)
      {
        value = default;
        return false;
      }

      return typedCollection.TryGetValue(metricKey, out value);
    }

    if (!factGroup.MetricCollections.TryGetValue(metricType, out var collection) || collection is not MetricCollection<T> untypedCollection)
    {
      value = default;
      return false;
    }

    return untypedCollection.TryGetValue(metricKey, out value);
  }

  private static string CreateCellKey(string rowCoordinateKey, string columnCoordinateKey)
  {
    return $"{rowCoordinateKey}=>{columnCoordinateKey}";
  }

  private static AggregatedMetricValue EvaluateMetric<T>(IEnumerable<FactGroup> factGroups, Metric<T> metric)
    where T : struct
  {
    var factGroupList = factGroups as IReadOnlyList<FactGroup> ?? factGroups.ToArray();
    return EvaluateMetric(factGroupList, metric, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
  }

  private static AggregatedMetricValue EvaluateMetric<T>(
    IReadOnlyList<FactGroup> factGroups,
    Metric<T> metric,
    ISet<string> dependencyChain)
    where T : struct
  {
    ArgumentNullException.ThrowIfNull(factGroups);
    ArgumentNullException.ThrowIfNull(metric);
    ArgumentNullException.ThrowIfNull(dependencyChain);

    if (metric is CalculatedMetric<T> calculatedMetric)
      return EvaluateCalculatedMetric(factGroups, calculatedMetric, dependencyChain);

    if (metric is Metric metricDefinition && metricDefinition is DistinctCountMetric distinctCountMetric)
      return EvaluateDistinctCountMetric(factGroups, distinctCountMetric);

    var semantics = ResolveMetricSemantics(metric);

    return semantics.Additivity switch
    {
      MetricAdditivity.Additive => EvaluateFullyAggregatableMetric(factGroups, metric),
      MetricAdditivity.SemiAdditive => EvaluateSemiAdditiveMetric(factGroups, metric, semantics),
      MetricAdditivity.NonAdditive => EvaluateNonAdditiveMetric(factGroups, metric),
      _ => throw new ArgumentOutOfRangeException(nameof(semantics.Additivity), semantics.Additivity, "Unsupported metric additivity.")
    };
  }

  private static AggregatedMetricValue EvaluateCalculatedMetric<T>(
    IReadOnlyList<FactGroup> factGroups,
    CalculatedMetric<T> metric,
    ISet<string> dependencyChain)
    where T : struct
  {
    if (!dependencyChain.Add(metric.Key))
      throw new InvalidOperationException($"Calculated metric dependency cycle detected for '{metric.Key}'.");

    try
    {
      var dependencyValues = new Dictionary<string, AggregatedMetricValue>(StringComparer.OrdinalIgnoreCase);
      var hasAnyDependencyValue = false;

      foreach (var dependency in metric.Dependencies)
      {
        var aggregatedDependency = EvaluateMetricUntyped(factGroups, dependency, dependencyChain);
        dependencyValues[dependency.Key] = aggregatedDependency;
        hasAnyDependencyValue |= aggregatedDependency.HasValue;
      }

      if (!hasAnyDependencyValue)
        return new AggregatedMetricValue(metric, default(T), hasValue: false);

      var context = new CalculatedMetricContext(dependencyValues);
      return new AggregatedMetricValue(metric, metric.Evaluate(context), hasValue: true);
    }
    finally
    {
      dependencyChain.Remove(metric.Key);
    }
  }

  private static AggregatedMetricValue EvaluateMetricUntyped(
    IReadOnlyList<FactGroup> factGroups,
    Metric metric,
    ISet<string> dependencyChain)
  {
    ArgumentNullException.ThrowIfNull(metric);

    return metric switch
    {
      Metric<int> intMetric => EvaluateMetric(factGroups, intMetric, dependencyChain),
      Metric<long> longMetric => EvaluateMetric(factGroups, longMetric, dependencyChain),
      Metric<float> floatMetric => EvaluateMetric(factGroups, floatMetric, dependencyChain),
      Metric<double> doubleMetric => EvaluateMetric(factGroups, doubleMetric, dependencyChain),
      Metric<decimal> decimalMetric => EvaluateMetric(factGroups, decimalMetric, dependencyChain),
      _ => throw new InvalidOperationException($"Metric '{metric.Key}' must be a typed metric to be evaluated.")
    };
  }

  private static IEnumerable<T> EnumerateMetricValues<T>(IEnumerable<FactGroup> factGroups, Metric<T> metric)
    where T : struct
  {
    foreach (var factGroup in factGroups)
    {
      if (TryGetMetricValue(factGroup, metric.Key, metric.MetricType, out T value))
        yield return value;
    }
  }

  private static AggregatedMetricValue EvaluateDistinctCountMetric(
    IReadOnlyList<FactGroup> factGroups,
    DistinctCountMetric metric)
  {
    ArgumentNullException.ThrowIfNull(factGroups);
    ArgumentNullException.ThrowIfNull(metric);

    var businessKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var factGroup in factGroups)
    {
      if (metric.TryGetBusinessKey(factGroup, out var businessKey))
        businessKeys.Add(businessKey);
    }

    return businessKeys.Count == 0
      ? new AggregatedMetricValue(metric, default(int), hasValue: false)
      : new AggregatedMetricValue(metric, businessKeys.Count, hasValue: true);
  }

  private static T GetValueOrDefault<T>(AggregatedMetricValue aggregatedValue)
    where T : struct
  {
    return aggregatedValue.HasValue && aggregatedValue.Value is T typedValue ? typedValue : default;
  }

  private static MetricSemantics ResolveMetricSemantics(Metric metric)
  {
    ArgumentNullException.ThrowIfNull(metric);

    var semantics = metric.Semantics?.Clone() ?? MetricSemantics.CreateDefault(metric.AggregationType);
    semantics.Validate(metric.Key);
    return semantics;
  }

  private static AggregatedMetricValue EvaluateFullyAggregatableMetric<T>(
    IReadOnlyList<FactGroup> factGroups,
    Metric<T> metric)
    where T : struct
  {
    var values = EnumerateMetricValues(factGroups, metric).ToArray();
    if (values.Length == 0)
      return new AggregatedMetricValue(metric, default(T), hasValue: false);

    return new AggregatedMetricValue(metric, metric.Aggregate(values), hasValue: true);
  }

  private static AggregatedMetricValue EvaluateNonAdditiveMetric<T>(
    IReadOnlyList<FactGroup> factGroups,
    Metric<T> metric)
    where T : struct
  {
    var values = EnumerateMetricValues(factGroups, metric).ToArray();
    if (values.Length == 0)
      return new AggregatedMetricValue(metric, default(T), hasValue: false);

    if (values.Length > 1)
    {
      throw new InvalidOperationException(
        $"Metric '{metric.Key}' is non-additive and cannot be rolled up across multiple fact groups. Store additive components and expose ratios through a calculated metric instead.");
    }

    return new AggregatedMetricValue(metric, values[0], hasValue: true);
  }

  private static AggregatedMetricValue EvaluateSemiAdditiveMetric<T>(
    IReadOnlyList<FactGroup> factGroups,
    Metric<T> metric,
    MetricSemantics semantics)
    where T : struct
  {
    var policy = semantics.SemiAdditive
                 ?? throw new InvalidOperationException($"Metric '{metric.Key}' is semi-additive but has no semi-additive policy.");
    var factGroupsBySeriesKey = new Dictionary<string, List<(FactGroup FactGroup, DimensionValue TimeValue)>>(StringComparer.Ordinal);

    foreach (var factGroup in factGroups)
    {
      var hasMetricValue = HasRawMetricValue(factGroup, metric);
      if (!TryGetDimensionValue(factGroup, policy.TimeDimensionKey, out var timeValue))
      {
        if (hasMetricValue)
        {
          throw new InvalidOperationException(
            $"Metric '{metric.Key}' is semi-additive and requires the time dimension '{policy.TimeDimensionKey}' on every contributing fact group.");
        }

        continue;
      }

      var seriesKey = CreateSemiAdditiveSeriesKey(factGroup, policy.TimeDimensionKey);
      if (!factGroupsBySeriesKey.TryGetValue(seriesKey, out var series))
      {
        series = new List<(FactGroup FactGroup, DimensionValue TimeValue)>();
        factGroupsBySeriesKey[seriesKey] = series;
      }

      series.Add((factGroup, timeValue));
    }

    if (factGroupsBySeriesKey.Count == 0)
      return new AggregatedMetricValue(metric, default(T), hasValue: false);

    var selectedValues = new List<T>(factGroupsBySeriesKey.Count);
    foreach (var series in factGroupsBySeriesKey.Values)
    {
      if (TrySelectSemiAdditiveValue(series, metric, policy.Aggregation, out var value))
        selectedValues.Add(value);
    }

    if (selectedValues.Count == 0)
      return new AggregatedMetricValue(metric, default(T), hasValue: false);

    return new AggregatedMetricValue(metric, metric.Aggregate(selectedValues), hasValue: true);
  }

  private static bool TrySelectSemiAdditiveValue<T>(
    IReadOnlyCollection<(FactGroup FactGroup, DimensionValue TimeValue)> series,
    Metric<T> metric,
    SemiAdditiveAggregationType aggregation,
    out T value)
    where T : struct
  {
    var orderedTimeBuckets = series
      .GroupBy(static entry => entry.TimeValue.Key, StringComparer.OrdinalIgnoreCase)
      .Select(grouping => new
      {
        TimeValue = grouping.First().TimeValue,
        FactGroups = grouping.Select(static entry => entry.FactGroup).ToArray(),
      })
      .OrderByDescending(bucket => bucket.TimeValue, Comparer<DimensionValue>.Create(CompareSemiAdditiveTimeValues))
      .ToArray();

    if (orderedTimeBuckets.Length == 0)
    {
      value = default;
      return false;
    }

    if (aggregation == SemiAdditiveAggregationType.LastValue)
      return TryAggregateSemiAdditiveBucket(orderedTimeBuckets[0].FactGroups, metric, out value);

    foreach (var bucket in orderedTimeBuckets)
    {
      if (TryAggregateSemiAdditiveBucket(bucket.FactGroups, metric, out value))
        return true;
    }

    value = default;
    return false;
  }

  private static bool TryAggregateSemiAdditiveBucket<T>(
    IReadOnlyCollection<FactGroup> factGroups,
    Metric<T> metric,
    out T value)
    where T : struct
  {
    var values = EnumerateMetricValues(factGroups, metric).ToArray();
    if (values.Length == 0)
    {
      value = default;
      return false;
    }

    value = metric.Aggregate(values);
    return true;
  }

  private static string CreateSemiAdditiveSeriesKey(FactGroup factGroup, string excludedDimensionKey)
  {
    return string.Join(
      "|",
      factGroup.DimensionValues
        .Where(entry => !string.Equals(entry.Key, excludedDimensionKey, StringComparison.OrdinalIgnoreCase))
        .OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
        .Select(entry => CreateSemiAdditiveSeriesKeyPart(entry.Key, entry.Value?.Key)));
  }

  private static string CreateSemiAdditiveSeriesKeyPart(string dimensionKey, string valueKey)
  {
    var normalizedDimensionKey = NormalizeSemiAdditiveKeyPart(dimensionKey);
    var normalizedValueKey = NormalizeSemiAdditiveKeyPart(valueKey);

    return $"{normalizedDimensionKey.Length}:{normalizedDimensionKey}={normalizedValueKey.Length}:{normalizedValueKey}";
  }

  private static string NormalizeSemiAdditiveKeyPart(string value)
  {
    if (value == null)
      return Constants.NULL_LABEL;

    if (value.Length == 0)
      return Constants.EMPTY_LABEL;

    return value.ToUpperInvariant();
  }

  private static int CompareSemiAdditiveTimeValues(DimensionValue left, DimensionValue right)
  {
    if (ReferenceEquals(left, right))
      return 0;

    if (left == null)
      return -1;

    if (right == null)
      return 1;

    if (left.Value != null &&
        right.Value != null &&
        left.Value.GetType() == right.Value.GetType() &&
        left.Value is IComparable comparable)
    {
      return comparable.CompareTo(right.Value);
    }

    return StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key);
  }

  private static bool CanContributeToMetric<T>(FactGroup factGroup, Metric<T> metric)
    where T : struct
  {
    return CanContributeToMetric(factGroup, metric, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
  }

  private static bool CanContributeToMetric(FactGroup factGroup, Metric metric, ISet<string> dependencyChain)
  {
    ArgumentNullException.ThrowIfNull(factGroup);
    ArgumentNullException.ThrowIfNull(metric);
    ArgumentNullException.ThrowIfNull(dependencyChain);

    if (!dependencyChain.Add(metric.Key))
      throw new InvalidOperationException($"Calculated metric dependency cycle detected for '{metric.Key}'.");

    try
    {
      return metric switch
      {
        CalculatedMetric<int> calculatedIntMetric => calculatedIntMetric.Dependencies.Any(dependency => CanContributeToMetric(factGroup, dependency, dependencyChain)),
        CalculatedMetric<long> calculatedLongMetric => calculatedLongMetric.Dependencies.Any(dependency => CanContributeToMetric(factGroup, dependency, dependencyChain)),
        CalculatedMetric<float> calculatedFloatMetric => calculatedFloatMetric.Dependencies.Any(dependency => CanContributeToMetric(factGroup, dependency, dependencyChain)),
        CalculatedMetric<double> calculatedDoubleMetric => calculatedDoubleMetric.Dependencies.Any(dependency => CanContributeToMetric(factGroup, dependency, dependencyChain)),
        CalculatedMetric<decimal> calculatedDecimalMetric => calculatedDecimalMetric.Dependencies.Any(dependency => CanContributeToMetric(factGroup, dependency, dependencyChain)),
        _ => CanContributeRawMetric(factGroup, metric)
      };
    }
    finally
    {
      dependencyChain.Remove(metric.Key);
    }
  }

  private static bool HasRawMetricValue(FactGroup factGroup, Metric metric)
  {
    return metric.MetricType switch
    {
      MetricType.Int => TryGetMetricValue<int>(factGroup, metric.Key, metric.MetricType, out _),
      MetricType.Long => TryGetMetricValue<long>(factGroup, metric.Key, metric.MetricType, out _),
      MetricType.Float => TryGetMetricValue<float>(factGroup, metric.Key, metric.MetricType, out _),
      MetricType.Double => TryGetMetricValue<double>(factGroup, metric.Key, metric.MetricType, out _),
      MetricType.Decimal => TryGetMetricValue<decimal>(factGroup, metric.Key, metric.MetricType, out _),
      _ => false
    };
  }

  private static bool CanContributeRawMetric(FactGroup factGroup, Metric metric)
  {
    if (metric is DistinctCountMetric distinctCountMetric)
      return distinctCountMetric.TryGetBusinessKey(factGroup, out _);

    if (HasRawMetricValue(factGroup, metric))
      return true;

    var semantics = ResolveMetricSemantics(metric);
    return semantics.Additivity == MetricAdditivity.SemiAdditive &&
           TryGetDimensionValue(factGroup, semantics.SemiAdditive!.TimeDimensionKey, out _);
  }
}
