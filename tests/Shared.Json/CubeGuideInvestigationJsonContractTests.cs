namespace Outsourced.DataCube.Serialization.Tests.Shared;

using Metrics;
using global::Outsourced.DataCube.Tests.Shared;
using NUnit.Framework;

public abstract class CubeGuideInvestigationJsonContractTests : CubeJsonContractTestBase
{
  [Test]
  public void A_cube_can_capture_parent_child_reconciliation_mismatches_as_drillable_rows_and_roundtrip_them_as_json()
  {
    // Scenario: analysts need to review where parent totals do not reconcile with their child line items.
    // Why a cube helps: the mismatch record stays drillable through dimensions while the important numeric gaps stay attached as metrics.
    var rows = BusinessNeutralUseCaseData.GetReconciliationMismatchRows();

    var cube = new AnalyticsCube
    {
      Key = "reconciliation-mismatches",
      Label = "Reconciliation Mismatches",
      PopulationCount = rows.Length,
    };

    var mismatch = cube.CreateCompositeDimension<BusinessNeutralUseCaseData.ReconciliationMismatchRow>("mismatch", "Mismatch")
      .AddProperty(x => x.RecordId)
      .AddProperty(x => x.AmountType)
      .Build();

    var parentAmount = cube.AddCurrencyMetric("parentAmount", "EUR", "Parent Amount");
    var childSum = cube.AddCurrencyMetric("childSum", "EUR", "Child Sum");
    var difference = cube.AddCurrencyMetric("difference", "EUR", "Difference");
    var childItemCount = cube.AddCountMetric("childItemCount", "Child Item Count");

    foreach (var row in rows)
    {
      cube.CreateFactGroup()
        .WithDimensionValue(mismatch, row)
        .WithMetricValue(parentAmount, row.ParentAmount)
        .WithMetricValue(childSum, row.ChildSum)
        .WithMetricValue(difference, row.Difference)
        .WithMetricValue(childItemCount, row.ChildItemCount)
        .Build();
    }

    var (json, roundTripped) = RoundTripThroughString(cube);
    var roundTrippedDifference = (Metric<decimal>)roundTripped.GetMetric("difference");
    var largestGap = roundTripped.FactGroups
      .Select(factGroup => new
      {
        Gap = factGroup.GetMetricValue<decimal>(roundTrippedDifference),
        DimensionValue = (CompositeDimensionValue)factGroup.GetDimensionValue("mismatch"),
      })
      .OrderByDescending(x => Math.Abs(x.Gap))
      .First();

    Assert.That(cube.Aggregate(difference), Is.EqualTo(350m));
    Assert.That(largestGap.Gap, Is.EqualTo(400m));
    Assert.That(largestGap.DimensionValue.GetFieldValue<string>("RecordId"), Is.EqualTo("REC-003"));
    Assert.That(roundTripped.GetDimension("mismatch").Values.Count, Is.EqualTo(rows.Length));
    Assert.That(json, Does.Contain("\"difference\""));
  }

  [Test]
  public void A_cube_can_rank_exact_duplicate_clusters_with_counts_and_percentages_and_roundtrip_them_as_json()
  {
    // Scenario: duplicate fingerprints need to be ranked so reviewers can tackle the largest clusters first.
    // Why a cube helps: each fingerprint becomes one fact group with the identifying fields and ranking metrics bound together.
    var rows = BusinessNeutralUseCaseData.GetDuplicateClusterRows();

    var cube = new AnalyticsCube
    {
      Key = "duplicate-clusters",
      Label = "Duplicate Clusters",
      PopulationCount = 20,
    };

    var duplicateFingerprint = cube.CreateCompositeDimension<BusinessNeutralUseCaseData.DuplicateClusterRow>("duplicateFingerprint", "Duplicate Fingerprint")
      .AddProperty(x => x.PartyName)
      .AddProperty(x => x.ActivityDate)
      .AddProperty(x => x.Location)
      .Build();

    var duplicateCount = cube.AddMetric(new CountMetric("duplicateCount", "Duplicate Count"));
    var duplicatePercentage = cube.AddMetric(new PercentageMetric("duplicatePercentage", "Duplicate Percentage"));

    foreach (var row in rows)
    {
      cube.CreateFactGroup()
        .WithDimensionValue(duplicateFingerprint, row)
        .WithMetricValue(duplicateCount, row.DuplicateCount)
        .WithMetricValue(duplicatePercentage, row.DuplicatePercentage)
        .Build();
    }

    var (json, roundTripped) = RoundTripThroughString(cube);
    var roundTrippedDuplicateCount = (Metric<int>)roundTripped.GetMetric("duplicateCount");
    var topCluster = roundTripped.FactGroups
      .Select(factGroup => new
      {
        Count = factGroup.GetMetricValue<int>(roundTrippedDuplicateCount),
        Fingerprint = (CompositeDimensionValue)factGroup.GetDimensionValue("duplicateFingerprint"),
      })
      .OrderByDescending(x => x.Count)
      .First();

    Assert.That(cube.Aggregate(duplicateCount), Is.EqualTo(16));
    Assert.That(topCluster.Count, Is.EqualTo(8));
    Assert.That(topCluster.Fingerprint.GetFieldValue<string>("PartyName"), Is.EqualTo("Alex Stone"));
    Assert.That(topCluster.Fingerprint.GetFieldValue<string>("Location"), Is.EqualTo("North Hub"));
    Assert.That(roundTripped.GetDimension("duplicateFingerprint").Values.Count, Is.EqualTo(rows.Length));
    Assert.That(json, Does.Contain("\"duplicatePercentage\""));
  }
}
