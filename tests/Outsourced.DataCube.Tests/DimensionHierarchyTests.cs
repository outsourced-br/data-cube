namespace Outsourced.DataCube.Tests;

using NUnit.Framework;

[TestFixture]
public sealed class DimensionHierarchyTests
{
  [Test]
  public void Time_hierarchy_registration_exposes_balanced_navigation_metadata()
  {
    var time = new Dimension("time", "Time");
    var year2026 = time.CreateValue("2026", "2026", 2026);
    var quarterOne = time.CreateValue("2026-Q1", "Q1 2026", "Q1 2026");
    var january = time.CreateValue("2026-01", "January 2026", "2026-01");
    var february = time.CreateValue("2026-02", "February 2026", "2026-02");

    var hierarchy = time.CreateHierarchy("calendar", "Calendar");
    hierarchy.AddLevel("year", "Year");
    hierarchy.AddLevel("quarter", "Quarter");
    hierarchy.AddLevel("month", "Month");
    hierarchy.MapValue("year", year2026);
    hierarchy.MapValue("quarter", quarterOne, year2026);
    hierarchy.MapValue("month", january, quarterOne);
    hierarchy.MapValue("month", february, quarterOne);

    Assert.That(time.GetHierarchy("calendar"), Is.SameAs(hierarchy));
    Assert.That(
      hierarchy.Levels.Select(static level => (level.Key, level.Ordinal)),
      Is.EqualTo(new[]
      {
        ("year", 0),
        ("quarter", 1),
        ("month", 2),
      }));
    Assert.That(hierarchy.GetLevelForValue(january)?.Key, Is.EqualTo("month"));
    Assert.That(hierarchy.GetParent(january), Is.SameAs(quarterOne));
    Assert.That(hierarchy.GetAncestor(january, "year"), Is.SameAs(year2026));
    Assert.That(
      hierarchy.GetValues("month").Select(static value => value.Key),
      Is.EqualTo(new[] { "2026-01", "2026-02" }));
    Assert.That(
      hierarchy.GetChildren(quarterOne).Select(static value => value.Key).OrderBy(static key => key),
      Is.EqualTo(new[] { "2026-01", "2026-02" }));
    Assert.That(
      hierarchy.GetPath(january).Select(static value => value.Key),
      Is.EqualTo(new[] { "2026", "2026-Q1", "2026-01" }));
  }

  [Test]
  public void Geography_hierarchy_can_be_attached_to_a_typed_dimension()
  {
    var geography = new Dimension<string>("location", "Location");
    var netherlands = geography.CreateValue("country:nl", "Netherlands", "Netherlands");
    var randstad = geography.CreateValue("region:randstad", "Randstad", "Randstad");
    var amsterdam = geography.CreateValue("city:ams", "Amsterdam", "Amsterdam");
    var utrecht = geography.CreateValue("city:utr", "Utrecht", "Utrecht");

    var hierarchy = new Hierarchy.Hierarchy("geography", "Geography");
    hierarchy.AddLevel("country", "Country");
    hierarchy.AddLevel("region", "Region");
    hierarchy.AddLevel("city", "City");
    geography.AddHierarchy(hierarchy);

    hierarchy.MapValue("country", netherlands);
    hierarchy.MapValue("region", randstad, netherlands);
    hierarchy.MapValue("city", amsterdam, randstad);
    hierarchy.MapValue("city", utrecht, randstad);

    Assert.That(geography.GetHierarchy("GEOGRAPHY"), Is.SameAs(hierarchy));
    Assert.That(hierarchy.GetChildren(netherlands).Single(), Is.SameAs(randstad));
    Assert.That(hierarchy.GetAncestor(utrecht, "country"), Is.SameAs(netherlands));
    Assert.That(
      hierarchy.GetLevel("city")?.ValueKeys,
      Is.EquivalentTo(new[] { "city:ams", "city:utr" }));
  }

  [Test]
  public void Balanced_hierarchies_require_parents_from_the_previous_level()
  {
    var geography = new Dimension<string>("location", "Location");
    var netherlands = geography.CreateValue("country:nl", "Netherlands", "Netherlands");
    var amsterdam = geography.CreateValue("city:ams", "Amsterdam", "Amsterdam");

    var hierarchy = geography.CreateHierarchy("geography", "Geography");
    hierarchy.AddLevel("country", "Country");
    hierarchy.AddLevel("region", "Region");
    hierarchy.AddLevel("city", "City");
    hierarchy.MapValue("country", netherlands);

    Assert.That(
      () => hierarchy.MapValue("city", amsterdam, netherlands),
      Throws.InvalidOperationException.With.Message.Contains("region"));
  }
}
