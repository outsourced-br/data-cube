namespace Outsourced.DataCube.Tests;

using NUnit.Framework;

[TestFixture]
public sealed class HierarchyOlapTests
{
  [Test]
  public void Aggregate_at_level_and_roll_up_support_time_hierarchies()
  {
    var (cube, time, calendar, revenue) = CreateCalendarCube();

    var revenueByQuarter = cube.AggregateAtLevel(time, calendar, "quarter", revenue);
    var revenueByYear = cube.AggregateAtLevel("time", "calendar", "year", revenue);
    var januaryRollUp = cube.RollUp(time, calendar, time.GetValue("2026-01"), revenue);
    var firstQuarterRollUp = cube.RollUp("time", "calendar", "2026-Q1", revenue);

    Assert.That(
      revenueByQuarter.Select(static result => (Level: result.Level.Key, Member: result.Member.Key, Revenue: result.Value)),
      Is.EqualTo(new[]
      {
        ("quarter", "2026-Q1", 300m),
        ("quarter", "2026-Q2", 425m),
      }));
    Assert.That(revenueByYear.Select(static result => (result.Member.Key, result.Value)), Is.EqualTo(new[] { ("2026", 725m) }));
    Assert.That(januaryRollUp, Is.Not.Null);
    Assert.That(januaryRollUp.Level.Key, Is.EqualTo("quarter"));
    Assert.That(januaryRollUp.Member.Key, Is.EqualTo("2026-Q1"));
    Assert.That(januaryRollUp.Value, Is.EqualTo(300m));
    Assert.That(firstQuarterRollUp, Is.Not.Null);
    Assert.That(firstQuarterRollUp.Level.Key, Is.EqualTo("year"));
    Assert.That(firstQuarterRollUp.Member.Key, Is.EqualTo("2026"));
    Assert.That(firstQuarterRollUp.Value, Is.EqualTo(725m));
  }

  [Test]
  public void Drill_down_supports_country_to_region_navigation()
  {
    var (cube, location, geography, revenue) = CreateGeographyCube();

    var revenueByRegion = cube.AggregateAtLevel("location", "geography", "region", revenue);
    var netherlandsRegions = cube.DrillDown(location, geography, location.GetValue("country:nl"), revenue);

    Assert.That(
      revenueByRegion.Select(static result => (result.Member.Key, result.Value)),
      Is.EqualTo(new[]
      {
        ("region:randstad", 180m),
        ("region:brabant", 120m),
        ("region:berlin", 200m),
      }));
    Assert.That(
      netherlandsRegions.Select(static result => (result.Level.Key, result.Member.Key, result.Value)),
      Is.EqualTo(new[]
      {
        ("region", "region:randstad", 180m),
        ("region", "region:brabant", 120m),
      }));
  }

  [Test]
  public void Missing_levels_throw_while_root_and_leaf_navigation_stop_cleanly()
  {
    var (cube, _, calendar, revenue) = CreateCalendarCube();

    Assert.That(calendar.RollUp("month")?.Key, Is.EqualTo("quarter"));
    Assert.That(calendar.DrillDown("quarter")?.Key, Is.EqualTo("month"));
    Assert.That(calendar.RollUp("year"), Is.Null);
    Assert.That(calendar.DrillDown("month"), Is.Null);
    Assert.That(
      () => cube.AggregateAtLevel("time", "calendar", "week", revenue),
      Throws.TypeOf<KeyNotFoundException>().With.Message.Contains("week"));
    Assert.That(
      () => calendar.DrillDown("week"),
      Throws.TypeOf<KeyNotFoundException>().With.Message.Contains("week"));
  }

  private static (AnalyticsCube Cube, Dimension<string> Time, Hierarchy.Hierarchy Calendar, Metrics.Metric<decimal> Revenue) CreateCalendarCube()
  {
    var cube = new AnalyticsCube { Key = "sales", Label = "Sales" };
    var time = cube.AddTypedDimension<string>("time", "Time");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    var year2026 = time.CreateValue("2026", "2026", "2026");
    var quarterOne = time.CreateValue("2026-Q1", "Q1 2026", "Q1 2026");
    var quarterTwo = time.CreateValue("2026-Q2", "Q2 2026", "Q2 2026");
    var january = time.CreateValue("2026-01", "January 2026", "2026-01");
    var february = time.CreateValue("2026-02", "February 2026", "2026-02");
    var april = time.CreateValue("2026-04", "April 2026", "2026-04");
    var may = time.CreateValue("2026-05", "May 2026", "2026-05");

    var calendar = time.CreateHierarchy("calendar", "Calendar");
    calendar.AddLevel("year", "Year");
    calendar.AddLevel("quarter", "Quarter");
    calendar.AddLevel("month", "Month");
    calendar.MapValue("year", year2026);
    calendar.MapValue("quarter", quarterOne, year2026);
    calendar.MapValue("quarter", quarterTwo, year2026);
    calendar.MapValue("month", january, quarterOne);
    calendar.MapValue("month", february, quarterOne);
    calendar.MapValue("month", april, quarterTwo);
    calendar.MapValue("month", may, quarterTwo);

    cube.CreateFactGroup()
      .WithDimensionValue(time, january)
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, february)
      .WithMetricValue(revenue, 200m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, april)
      .WithMetricValue(revenue, 125m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, may)
      .WithMetricValue(revenue, 300m)
      .Build();

    return (cube, time, calendar, revenue);
  }

  private static (AnalyticsCube Cube, Dimension<string> Location, Hierarchy.Hierarchy Geography, Metrics.Metric<decimal> Revenue) CreateGeographyCube()
  {
    var cube = new AnalyticsCube { Key = "sales", Label = "Sales" };
    var location = cube.AddTypedDimension<string>("location", "Location");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    var netherlands = location.CreateValue("country:nl", "Netherlands", "Netherlands");
    var germany = location.CreateValue("country:de", "Germany", "Germany");
    var randstad = location.CreateValue("region:randstad", "Randstad", "Randstad");
    var brabant = location.CreateValue("region:brabant", "Brabant", "Brabant");
    var berlinRegion = location.CreateValue("region:berlin", "Berlin", "Berlin");
    var amsterdam = location.CreateValue("city:ams", "Amsterdam", "Amsterdam");
    var utrecht = location.CreateValue("city:utr", "Utrecht", "Utrecht");
    var eindhoven = location.CreateValue("city:eind", "Eindhoven", "Eindhoven");
    var berlin = location.CreateValue("city:ber", "Berlin", "Berlin");

    var geography = location.CreateHierarchy("geography", "Geography");
    geography.AddLevel("country", "Country");
    geography.AddLevel("region", "Region");
    geography.AddLevel("city", "City");
    geography.MapValue("country", netherlands);
    geography.MapValue("country", germany);
    geography.MapValue("region", randstad, netherlands);
    geography.MapValue("region", brabant, netherlands);
    geography.MapValue("region", berlinRegion, germany);
    geography.MapValue("city", amsterdam, randstad);
    geography.MapValue("city", utrecht, randstad);
    geography.MapValue("city", eindhoven, brabant);
    geography.MapValue("city", berlin, berlinRegion);

    cube.CreateFactGroup()
      .WithDimensionValue(location, amsterdam)
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(location, utrecht)
      .WithMetricValue(revenue, 80m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(location, eindhoven)
      .WithMetricValue(revenue, 120m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(location, berlin)
      .WithMetricValue(revenue, 200m)
      .Build();

    return (cube, location, geography, revenue);
  }
}
