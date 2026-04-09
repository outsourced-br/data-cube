namespace Outsourced.DataCube.WellKnown;

using System.Globalization;

/// <summary>
/// Provides reusable, preconfigured dimensions.
/// </summary>
public static class Dimensions
{
  /// <summary>
  /// Creates a time dimension spanning an inclusive date range.
  /// </summary>
  public static Dimension CreateTimeDimension(DateTime start, DateTime end, TimeSpan interval)
  {
    var dimension = new Dimension("Time", "Time period for analysis");
    for (var date = start; date <= end; date += interval)
    {
      var key = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
      dimension.AddValue(new DimensionValue(key, key, date));
    }
    return dimension;
  }

  /// <summary>
  /// Creates a simple region dimension with common region codes.
  /// </summary>
  public static Dimension CreateRegionDimension()
  {
    var dimension = new Dimension("Region", "Geographical region");
    dimension.AddValue(new DimensionValue("EU", "Europe", "EU"));
    dimension.AddValue(new DimensionValue("NA", "North America", "NA"));
    dimension.AddValue(new DimensionValue("APAC", "Asia Pacific", "APAC"));
    dimension.AddValue(new DimensionValue("LATAM", "Latin America", "LATAM"));
    dimension.AddValue(new DimensionValue("MEA", "Middle East & Africa", "MEA"));
    return dimension;
  }
}
