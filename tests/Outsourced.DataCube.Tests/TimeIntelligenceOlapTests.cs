namespace Outsourced.DataCube.Tests;

using NUnit.Framework;

[TestFixture]
public sealed class TimeIntelligenceOlapTests
{
  [Test]
  public void Year_to_date_quarter_to_date_and_month_to_date_aggregate_ordered_windows()
  {
    var (cube, time, calendar, revenue) = CreateDailyCalendarCube();

    var yearToDate = cube.YearToDate(time, calendar, time.GetValue("2026-03-20"), revenue);
    var quarterToDate = cube.QuarterToDate("time", "calendar", "2026-03-20", revenue);
    var monthToDate = cube.MonthToDate("time", "calendar", "2026-03-20", revenue);

    Assert.That(yearToDate, Is.Not.Null);
    Assert.That(yearToDate.Value, Is.EqualTo(370m));
    Assert.That(yearToDate.IsPartial, Is.True);
    Assert.That(yearToDate.Members.Select(static member => member.Key), Is.EqualTo(new[] { "2026-01-05", "2026-02-10", "2026-03-02", "2026-03-20" }));

    Assert.That(quarterToDate, Is.Not.Null);
    Assert.That(quarterToDate.Value, Is.EqualTo(370m));
    Assert.That(quarterToDate.IsPartial, Is.True);
    Assert.That(quarterToDate.StartMember.Key, Is.EqualTo("2026-01-05"));
    Assert.That(quarterToDate.EndMember.Key, Is.EqualTo("2026-03-20"));

    Assert.That(monthToDate, Is.Not.Null);
    Assert.That(monthToDate.Value, Is.EqualTo(120m));
    Assert.That(monthToDate.IsPartial, Is.True);
    Assert.That(monthToDate.Members.Select(static member => member.Key), Is.EqualTo(new[] { "2026-03-02", "2026-03-20" }));
  }

  [Test]
  public void Previous_period_and_year_over_year_helpers_cross_year_boundaries()
  {
    var (cube, time, calendar, revenue) = CreateMonthlyBoundaryCube();

    var previousPeriod = cube.PreviousPeriod("time", "calendar", "2026-01", revenue);
    var periodOverPeriod = cube.PeriodOverPeriodChange(time, calendar, time.GetValue("2026-01"), revenue);
    var yearOverYear = cube.YearOverYearChange("time", "calendar", "2026-01", revenue);

    Assert.That(previousPeriod, Is.Not.Null);
    Assert.That(previousPeriod.AnchorMember.Key, Is.EqualTo("2025-12"));
    Assert.That(previousPeriod.Value, Is.EqualTo(90m));
    Assert.That(previousPeriod.IsPartial, Is.False);

    Assert.That(periodOverPeriod, Is.Not.Null);
    Assert.That(periodOverPeriod.CurrentPeriod.Value, Is.EqualTo(120m));
    Assert.That(periodOverPeriod.PreviousPeriod, Is.Not.Null);
    Assert.That(periodOverPeriod.PreviousPeriod.Value, Is.EqualTo(90m));
    Assert.That(periodOverPeriod.Delta, Is.EqualTo(30m));
    Assert.That(periodOverPeriod.PercentChange, Is.EqualTo(30m / 90m));

    Assert.That(yearOverYear, Is.Not.Null);
    Assert.That(yearOverYear.CurrentPeriod.AnchorMember.Key, Is.EqualTo("2026-01"));
    Assert.That(yearOverYear.PreviousPeriod, Is.Not.Null);
    Assert.That(yearOverYear.PreviousPeriod.AnchorMember.Key, Is.EqualTo("2025-01"));
    Assert.That(yearOverYear.PreviousPeriod.Value, Is.EqualTo(80m));
    Assert.That(yearOverYear.Delta, Is.EqualTo(40m));
    Assert.That(yearOverYear.PercentChange, Is.EqualTo(0.5m));
  }

  [Test]
  public void Missing_periods_are_not_zero_filled_for_to_date_or_comparison_helpers()
  {
    var (cube, time, calendar, revenue) = CreateMonthlyMissingDataCube();

    var yearToDate = cube.YearToDate("time", "calendar", "2026-05", revenue);
    var previousPeriod = cube.PreviousPeriod("time", "calendar", "2026-03", revenue);
    var periodOverPeriod = cube.PeriodOverPeriodChange("time", "calendar", "2026-03", revenue);

    Assert.That(yearToDate, Is.Not.Null);
    Assert.That(yearToDate.Value, Is.EqualTo(575m));
    Assert.That(yearToDate.Members.Select(static member => member.Key), Is.EqualTo(new[] { "2026-01", "2026-02", "2026-03", "2026-04", "2026-05" }));

    Assert.That(previousPeriod, Is.Null);

    Assert.That(periodOverPeriod, Is.Not.Null);
    Assert.That(periodOverPeriod.CurrentPeriod.Value, Is.EqualTo(50m));
    Assert.That(periodOverPeriod.PreviousPeriod, Is.Null);
    Assert.That(periodOverPeriod.Delta, Is.Null);
    Assert.That(periodOverPeriod.PercentChange, Is.Null);
  }

  private static (AnalyticsCube Cube, Dimension<string> Time, Hierarchy.Hierarchy Calendar, Metrics.Metric<decimal> Revenue) CreateDailyCalendarCube()
  {
    var cube = new AnalyticsCube { Key = "sales", Label = "Sales" };
    var time = cube.AddTypedDimension<string>("time", "Time");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    var year2025 = time.CreateValue("2025", "2025", "2025");
    var year2026 = time.CreateValue("2026", "2026", "2026");
    var quarter2025Q4 = time.CreateValue("2025-Q4", "Q4 2025", "Q4 2025");
    var quarter2026Q1 = time.CreateValue("2026-Q1", "Q1 2026", "Q1 2026");
    var quarter2026Q2 = time.CreateValue("2026-Q2", "Q2 2026", "Q2 2026");
    var month202512 = time.CreateValue("2025-12", "December 2025", "2025-12");
    var month202601 = time.CreateValue("2026-01", "January 2026", "2026-01");
    var month202602 = time.CreateValue("2026-02", "February 2026", "2026-02");
    var month202603 = time.CreateValue("2026-03", "March 2026", "2026-03");
    var month202604 = time.CreateValue("2026-04", "April 2026", "2026-04");
    var month202605 = time.CreateValue("2026-05", "May 2026", "2026-05");
    var day20251231 = time.CreateValue("2025-12-31", "31 Dec 2025", "2025-12-31");
    var day20260105 = time.CreateValue("2026-01-05", "5 Jan 2026", "2026-01-05");
    var day20260210 = time.CreateValue("2026-02-10", "10 Feb 2026", "2026-02-10");
    var day20260302 = time.CreateValue("2026-03-02", "2 Mar 2026", "2026-03-02");
    var day20260320 = time.CreateValue("2026-03-20", "20 Mar 2026", "2026-03-20");
    var day20260325 = time.CreateValue("2026-03-25", "25 Mar 2026", "2026-03-25");
    var day20260415 = time.CreateValue("2026-04-15", "15 Apr 2026", "2026-04-15");
    var day20260510 = time.CreateValue("2026-05-10", "10 May 2026", "2026-05-10");

    var calendar = time.CreateHierarchy("calendar", "Calendar");
    calendar.AddLevel("year", "Year");
    calendar.AddLevel("quarter", "Quarter");
    calendar.AddLevel("month", "Month");
    calendar.AddLevel("day", "Day");
    calendar.MapValue("year", year2025);
    calendar.MapValue("year", year2026);
    calendar.MapValue("quarter", quarter2025Q4, year2025);
    calendar.MapValue("quarter", quarter2026Q1, year2026);
    calendar.MapValue("quarter", quarter2026Q2, year2026);
    calendar.MapValue("month", month202512, quarter2025Q4);
    calendar.MapValue("month", month202601, quarter2026Q1);
    calendar.MapValue("month", month202602, quarter2026Q1);
    calendar.MapValue("month", month202603, quarter2026Q1);
    calendar.MapValue("month", month202604, quarter2026Q2);
    calendar.MapValue("month", month202605, quarter2026Q2);
    calendar.MapValue("day", day20251231, month202512);
    calendar.MapValue("day", day20260105, month202601);
    calendar.MapValue("day", day20260210, month202602);
    calendar.MapValue("day", day20260302, month202603);
    calendar.MapValue("day", day20260320, month202603);
    calendar.MapValue("day", day20260325, month202603);
    calendar.MapValue("day", day20260415, month202604);
    calendar.MapValue("day", day20260510, month202605);

    cube.CreateFactGroup()
      .WithDimensionValue(time, day20251231)
      .WithMetricValue(revenue, 90m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, day20260105)
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, day20260210)
      .WithMetricValue(revenue, 150m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, day20260302)
      .WithMetricValue(revenue, 50m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, day20260320)
      .WithMetricValue(revenue, 70m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, day20260415)
      .WithMetricValue(revenue, 125m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, day20260510)
      .WithMetricValue(revenue, 300m)
      .Build();

    return (cube, time, calendar, revenue);
  }

  private static (AnalyticsCube Cube, Dimension<string> Time, Hierarchy.Hierarchy Calendar, Metrics.Metric<decimal> Revenue) CreateMonthlyBoundaryCube()
  {
    var cube = new AnalyticsCube { Key = "sales", Label = "Sales" };
    var time = cube.AddTypedDimension<string>("time", "Time");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    var year2025 = time.CreateValue("2025", "2025", "2025");
    var year2026 = time.CreateValue("2026", "2026", "2026");
    var quarter2025Q1 = time.CreateValue("2025-Q1", "Q1 2025", "Q1 2025");
    var quarter2025Q4 = time.CreateValue("2025-Q4", "Q4 2025", "Q4 2025");
    var quarter2026Q1 = time.CreateValue("2026-Q1", "Q1 2026", "Q1 2026");
    var month202501 = time.CreateValue("2025-01", "January 2025", "2025-01");
    var month202512 = time.CreateValue("2025-12", "December 2025", "2025-12");
    var month202601 = time.CreateValue("2026-01", "January 2026", "2026-01");
    var month202602 = time.CreateValue("2026-02", "February 2026", "2026-02");

    var calendar = time.CreateHierarchy("calendar", "Calendar");
    calendar.AddLevel("year", "Year");
    calendar.AddLevel("quarter", "Quarter");
    calendar.AddLevel("month", "Month");
    calendar.MapValue("year", year2025);
    calendar.MapValue("year", year2026);
    calendar.MapValue("quarter", quarter2025Q1, year2025);
    calendar.MapValue("quarter", quarter2025Q4, year2025);
    calendar.MapValue("quarter", quarter2026Q1, year2026);
    calendar.MapValue("month", month202501, quarter2025Q1);
    calendar.MapValue("month", month202512, quarter2025Q4);
    calendar.MapValue("month", month202601, quarter2026Q1);
    calendar.MapValue("month", month202602, quarter2026Q1);

    cube.CreateFactGroup()
      .WithDimensionValue(time, month202501)
      .WithMetricValue(revenue, 80m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, month202512)
      .WithMetricValue(revenue, 90m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, month202601)
      .WithMetricValue(revenue, 120m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, month202602)
      .WithMetricValue(revenue, 150m)
      .Build();

    return (cube, time, calendar, revenue);
  }

  private static (AnalyticsCube Cube, Dimension<string> Time, Hierarchy.Hierarchy Calendar, Metrics.Metric<decimal> Revenue) CreateMonthlyMissingDataCube()
  {
    var cube = new AnalyticsCube { Key = "sales", Label = "Sales" };
    var time = cube.AddTypedDimension<string>("time", "Time");
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    var year2026 = time.CreateValue("2026", "2026", "2026");
    var quarter2026Q1 = time.CreateValue("2026-Q1", "Q1 2026", "Q1 2026");
    var quarter2026Q2 = time.CreateValue("2026-Q2", "Q2 2026", "Q2 2026");
    var month202601 = time.CreateValue("2026-01", "January 2026", "2026-01");
    var month202602 = time.CreateValue("2026-02", "February 2026", "2026-02");
    var month202603 = time.CreateValue("2026-03", "March 2026", "2026-03");
    var month202604 = time.CreateValue("2026-04", "April 2026", "2026-04");
    var month202605 = time.CreateValue("2026-05", "May 2026", "2026-05");

    var calendar = time.CreateHierarchy("calendar", "Calendar");
    calendar.AddLevel("year", "Year");
    calendar.AddLevel("quarter", "Quarter");
    calendar.AddLevel("month", "Month");
    calendar.MapValue("year", year2026);
    calendar.MapValue("quarter", quarter2026Q1, year2026);
    calendar.MapValue("quarter", quarter2026Q2, year2026);
    calendar.MapValue("month", month202601, quarter2026Q1);
    calendar.MapValue("month", month202602, quarter2026Q1);
    calendar.MapValue("month", month202603, quarter2026Q1);
    calendar.MapValue("month", month202604, quarter2026Q2);
    calendar.MapValue("month", month202605, quarter2026Q2);

    cube.CreateFactGroup()
      .WithDimensionValue(time, month202601)
      .WithMetricValue(revenue, 100m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, month202603)
      .WithMetricValue(revenue, 50m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, month202604)
      .WithMetricValue(revenue, 125m)
      .Build();

    cube.CreateFactGroup()
      .WithDimensionValue(time, month202605)
      .WithMetricValue(revenue, 300m)
      .Build();

    return (cube, time, calendar, revenue);
  }
}
