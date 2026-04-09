using Outsourced.DataCube.Tests.Shared;
using NUnit.Framework;

namespace Outsourced.DataCube.Tests;

[TestFixture]
public sealed class CubeGuideInvestigationTests
{
  [Test]
  public void A_cube_can_hold_dimension_only_fuzzy_review_pairs_when_the_dimension_is_the_investigation_payload()
  {
    // Scenario: reviewers need candidate pairs for manual comparison, not an aggregated scorecard.
    // Why a cube helps: the cube can still transfer and index those review rows cleanly even when the dimensions carry all the meaning.
    var rows = BusinessNeutralUseCaseData.GetFuzzyReviewPairRows();

    var cube = new AnalyticsCube
    {
      Key = "fuzzy-review-pairs",
      Label = "Fuzzy Review Pairs",
      PopulationCount = rows.Length,
    };

    var reviewPair = cube.CreateCompositeDimension<BusinessNeutralUseCaseData.FuzzyReviewPairRow>("reviewPair", "Review Pair")
      .AddProperty(x => x.LeftRecordId)
      .AddProperty(x => x.LeftPartyName)
      .AddProperty(x => x.RightRecordId)
      .AddProperty(x => x.RightPartyName)
      .AddProperty(x => x.SharedLocation)
      .AddProperty(x => x.SimilarityBand)
      .Build();

    foreach (var row in rows)
    {
      cube.CreateFactGroup()
        .WithDimensionValue(reviewPair, row)
        .Build();
    }

    var firstPair = (CompositeDimensionValue)cube.FactGroups[0].GetDimensionValue("reviewPair");

    Assert.That(cube.FactGroups.Count, Is.EqualTo(rows.Length));
    Assert.That(cube.GetDimension("reviewPair").Values.Count, Is.EqualTo(rows.Length));
    Assert.That(firstPair.GetFieldValue<string>("LeftPartyName"), Is.EqualTo("Morgan Fleet"));
    Assert.That(firstPair.GetFieldValue<string>("SimilarityBand"), Is.EqualTo("High"));
  }

  [Test]
  public void A_cube_can_surface_hierarchy_violations_as_structured_anomaly_rows()
  {
    // Scenario: data links across layers disagree and each mismatch needs to stay traceable.
    // Why a cube helps: the anomaly row is represented once, with the key relationship fields preserved for drill-down and transfer.
    var rows = BusinessNeutralUseCaseData.GetHierarchyViolationRows();

    var cube = new AnalyticsCube
    {
      Key = "hierarchy-violations",
      Label = "Hierarchy Violations",
      PopulationCount = rows.Length,
    };

    var violation = cube.CreateCompositeDimension<BusinessNeutralUseCaseData.HierarchyViolationRow>("violation", "Hierarchy Violation")
      .AddProperty(x => x.RecordId)
      .AddProperty(x => x.AssetReference)
      .AddProperty(x => x.ExpectedParentReference)
      .AddProperty(x => x.ActualParentReference)
      .AddProperty(x => x.ViolationType)
      .Build();

    foreach (var row in rows)
    {
      cube.CreateFactGroup()
        .WithDimensionValue(violation, row)
        .Build();
    }

    var specificViolation = (CompositeDimensionValue)cube.FactGroups[1].GetDimensionValue("violation");

    Assert.That(cube.FactGroups.Count, Is.EqualTo(rows.Length));
    Assert.That(cube.GetDimension("violation").Values.Count, Is.EqualTo(rows.Length));
    Assert.That(specificViolation.GetFieldValue<string>("RecordId"), Is.EqualTo("REC-111"));
    Assert.That(specificViolation.GetFieldValue<string>("ViolationType"), Is.EqualTo("CrossLinkedParent"));
  }
}
