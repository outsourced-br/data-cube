using Outsourced.DataCube.Tests.Shared;
using NUnit.Framework;

namespace Outsourced.DataCube.Tests;

[TestFixture]
public sealed class CubeGuideWorkflowTests
{
  [Test]
  public void A_cube_can_capture_assignments_that_exist_only_at_the_parent_level()
  {
    // Scenario: assignments exist on the parent record but were never propagated to child work items.
    // Why a cube helps: the action list stays compact, structured, and easy to transfer without inventing a separate export shape.
    var rows = BusinessNeutralUseCaseData.GetAssignmentsPresentOnlyAtParentLevelRows();

    var cube = new AnalyticsCube
    {
      Key = "parent-only-assignments",
      Label = "Parent Only Assignments",
      PopulationCount = rows.Length,
    };

    var assignment = cube.CreateCompositeDimension<BusinessNeutralUseCaseData.AssignmentGapRow>("assignment", "Assignment")
      .AddProperty(x => x.RecordId)
      .AddProperty(x => x.ActorId)
      .AddProperty(x => x.ActorName)
      .Build();

    foreach (var row in rows)
    {
      cube.CreateFactGroup()
        .WithDimensionValue(assignment, row)
        .Build();
    }

    var firstAssignment = (CompositeDimensionValue)cube.FactGroups[0].GetDimensionValue("assignment");

    Assert.That(cube.FactGroups.Count, Is.EqualTo(rows.Length));
    Assert.That(firstAssignment.GetFieldValue<string>("RecordId"), Is.EqualTo("REC-201"));
    Assert.That(firstAssignment.GetFieldValue<string>("ActorName"), Is.EqualTo("Harper Cole"));
  }

  [Test]
  public void A_cube_can_capture_assignments_that_exist_only_at_the_child_level()
  {
    // Scenario: assignments appear on child work items but are missing from the parent coordination layer.
    // Why a cube helps: the same cube pattern works for the inverse workflow gap, so teams can compare both discrepancy lists consistently.
    var rows = BusinessNeutralUseCaseData.GetAssignmentsPresentOnlyAtChildLevelRows();

    var cube = new AnalyticsCube
    {
      Key = "child-only-assignments",
      Label = "Child Only Assignments",
      PopulationCount = rows.Length,
    };

    var assignment = cube.CreateCompositeDimension<BusinessNeutralUseCaseData.AssignmentGapRow>("assignment", "Assignment")
      .AddProperty(x => x.RecordId)
      .AddProperty(x => x.ActorId)
      .AddProperty(x => x.ActorName)
      .Build();

    foreach (var row in rows)
    {
      cube.CreateFactGroup()
        .WithDimensionValue(assignment, row)
        .Build();
    }

    var secondAssignment = (CompositeDimensionValue)cube.FactGroups[1].GetDimensionValue("assignment");

    Assert.That(cube.FactGroups.Count, Is.EqualTo(rows.Length));
    Assert.That(secondAssignment.GetFieldValue<string>("RecordId"), Is.EqualTo("REC-302"));
    Assert.That(secondAssignment.GetFieldValue<string>("ActorId"), Is.EqualTo("ACT-05"));
  }

  [Test]
  public void A_cube_can_compare_role_distribution_across_two_contexts_side_by_side()
  {
    // Scenario: the same role can appear in both a primary workflow and a secondary workflow, and teams want those counts side by side.
    // Why a cube helps: one role dimension can carry both context-specific metrics, making comparison natural and queryable.
    var rows = BusinessNeutralUseCaseData.GetRoleDistributionRows();

    var cube = new AnalyticsCube
    {
      Key = "role-distribution",
      Label = "Role Distribution",
      PopulationCount = rows.Length,
    };

    var role = cube.AddTypedDimension<string>("role", "Role");
    var primaryContextCount = cube.AddCountMetric("primaryContextCount", "Primary Context Count");
    var secondaryContextCount = cube.AddCountMetric("secondaryContextCount", "Secondary Context Count");

    foreach (var row in rows)
    {
      cube.CreateFactGroup()
        .WithDimensionValue(role, row.Role)
        .WithMetricValue(primaryContextCount, row.PrimaryContextCount)
        .WithMetricValue(secondaryContextCount, row.SecondaryContextCount)
        .Build();
    }

    var primaryByRole = cube.Pivot("role", primaryContextCount);
    var secondaryByRole = cube.Pivot("role", secondaryContextCount);

    Assert.That(cube.Aggregate(primaryContextCount), Is.EqualTo(32));
    Assert.That(cube.Aggregate(secondaryContextCount), Is.EqualTo(32));
    Assert.That(primaryByRole["Owner"], Is.EqualTo(14));
    Assert.That(secondaryByRole["Reviewer"], Is.EqualTo(12));
    Assert.That(cube.GetDimension("role").Values.Count, Is.EqualTo(rows.Length));
  }
}
