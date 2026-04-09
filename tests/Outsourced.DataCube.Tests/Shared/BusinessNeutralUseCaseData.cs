namespace Outsourced.DataCube.Tests.Shared;

internal static class BusinessNeutralUseCaseData
{
  public static RegionalPerformanceRow[] GetRegionalPerformanceRows() =>
  [
    new() { Region = "North", Period = "2026-04", Channel = "Direct", Revenue = 1200m },
    new() { Region = "North", Period = "2026-04", Channel = "Partner", Revenue = 800m },
    new() { Region = "North", Period = "2026-05", Channel = "Direct", Revenue = 1500m },
    new() { Region = "South", Period = "2026-04", Channel = "Direct", Revenue = 600m },
    new() { Region = "South", Period = "2026-05", Channel = "Partner", Revenue = 900m },
    new() { Region = "South", Period = "2026-05", Channel = "Direct", Revenue = 700m },
  ];

  public static CohortIdentifierRow[] GetCohortIdentifierRows() =>
  [
    new() { Segment = "Starter", AccountReference = "ACC-100" },
    new() { Segment = "Starter", AccountReference = "ACC-101" },
    new() { Segment = "Starter", AccountReference = "ACC-100" },
    new() { Segment = "Growth", AccountReference = "ACC-200" },
    new() { Segment = "Growth", AccountReference = "ACC-201" },
    new() { Segment = "Growth", AccountReference = "ACC-202" },
    new() { Segment = "Enterprise", AccountReference = "ACC-300" },
    new() { Segment = "Enterprise", AccountReference = "ACC-300" },
    new() { Segment = "Enterprise", AccountReference = "ACC-301" },
    new() { Segment = "Enterprise", AccountReference = "ACC-302" },
  ];

  public static CategoryAmountRow[] GetCategoryAmountRows() =>
  [
    new() { Category = "Standard", Amount = 120m },
    new() { Category = "Standard", Amount = 180m },
    new() { Category = "Standard", Amount = 90m },
    new() { Category = "Priority", Amount = 300m },
    new() { Category = "Priority", Amount = 450m },
    new() { Category = "Flexible", Amount = 75m },
    new() { Category = "Flexible", Amount = 100m },
    new() { Category = "Flexible", Amount = 125m },
    new() { Category = "Flexible", Amount = 80m },
  ];

  public static ReconciliationMismatchRow[] GetReconciliationMismatchRows() =>
  [
    new() { RecordId = "REC-001", AmountType = "ApprovedAmount", ParentAmount = 1000m, ChildSum = 900m, Difference = 100m, ChildItemCount = 2 },
    new() { RecordId = "REC-002", AmountType = "ReservedAmount", ParentAmount = 850m, ChildSum = 1000m, Difference = -150m, ChildItemCount = 3 },
    new() { RecordId = "REC-003", AmountType = "RecoveredAmount", ParentAmount = 400m, ChildSum = 0m, Difference = 400m, ChildItemCount = 1 },
  ];

  public static DuplicateClusterRow[] GetDuplicateClusterRows() =>
  [
    new() { PartyName = "Alex Stone", ActivityDate = "2026-03-14", Location = "North Hub", DuplicateCount = 8, DuplicatePercentage = 40d },
    new() { PartyName = "Jamie Lane", ActivityDate = "2026-03-16", Location = "South Hub", DuplicateCount = 5, DuplicatePercentage = 25d },
    new() { PartyName = "Taylor Reed", ActivityDate = "2026-03-20", Location = "West Hub", DuplicateCount = 3, DuplicatePercentage = 15d },
  ];

  public static FuzzyReviewPairRow[] GetFuzzyReviewPairRows() =>
  [
    new() { LeftRecordId = "REC-041", LeftPartyName = "Morgan Fleet", RightRecordId = "REC-872", RightPartyName = "Morgan Fleat", SharedLocation = "North Hub", SimilarityBand = "High" },
    new() { LeftRecordId = "REC-052", LeftPartyName = "Riley Brooks", RightRecordId = "REC-901", RightPartyName = "Rylie Brooks", SharedLocation = "East Hub", SimilarityBand = "Medium" },
    new() { LeftRecordId = "REC-063", LeftPartyName = "Jordan Hale", RightRecordId = "REC-914", RightPartyName = "Jordon Hale", SharedLocation = "South Hub", SimilarityBand = "Medium" },
  ];

  public static HierarchyViolationRow[] GetHierarchyViolationRows() =>
  [
    new() { RecordId = "REC-110", AssetReference = "AST-12", ExpectedParentReference = "PRN-10", ActualParentReference = "PRN-00", ViolationType = "DetachedChild" },
    new() { RecordId = "REC-111", AssetReference = "AST-13", ExpectedParentReference = "PRN-11", ActualParentReference = "PRN-99", ViolationType = "CrossLinkedParent" },
    new() { RecordId = "REC-112", AssetReference = "AST-14", ExpectedParentReference = "PRN-12", ActualParentReference = "PRN-01", ViolationType = "OrphanedBranch" },
  ];

  public static AssignmentGapRow[] GetAssignmentsPresentOnlyAtParentLevelRows() =>
  [
    new() { RecordId = "REC-201", ActorId = "ACT-01", ActorName = "Harper Cole" },
    new() { RecordId = "REC-202", ActorId = "ACT-03", ActorName = "Rowan Bell" },
    new() { RecordId = "REC-203", ActorId = "ACT-07", ActorName = "Avery Moss" },
  ];

  public static AssignmentGapRow[] GetAssignmentsPresentOnlyAtChildLevelRows() =>
  [
    new() { RecordId = "REC-301", ActorId = "ACT-02", ActorName = "Finley Park" },
    new() { RecordId = "REC-302", ActorId = "ACT-05", ActorName = "Skyler Dunn" },
    new() { RecordId = "REC-303", ActorId = "ACT-09", ActorName = "Parker West" },
  ];

  public static RoleDistributionRow[] GetRoleDistributionRows() =>
  [
    new() { Role = "Owner", PrimaryContextCount = 14, SecondaryContextCount = 9 },
    new() { Role = "Reviewer", PrimaryContextCount = 9, SecondaryContextCount = 12 },
    new() { Role = "Beneficiary", PrimaryContextCount = 6, SecondaryContextCount = 4 },
    new() { Role = "Observer", PrimaryContextCount = 3, SecondaryContextCount = 7 },
  ];

  internal sealed class RegionalPerformanceRow
  {
    public string Region { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
  }

  internal sealed class CohortIdentifierRow
  {
    public string Segment { get; set; } = string.Empty;
    public string AccountReference { get; set; } = string.Empty;
  }

  internal sealed class CategoryAmountRow
  {
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
  }

  internal sealed class ReconciliationMismatchRow
  {
    public string RecordId { get; set; } = string.Empty;
    public string AmountType { get; set; } = string.Empty;
    public decimal ParentAmount { get; set; }
    public decimal ChildSum { get; set; }
    public decimal Difference { get; set; }
    public int ChildItemCount { get; set; }
  }

  internal sealed class DuplicateClusterRow
  {
    public string PartyName { get; set; } = string.Empty;
    public string ActivityDate { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int DuplicateCount { get; set; }
    public double DuplicatePercentage { get; set; }
  }

  internal sealed class FuzzyReviewPairRow
  {
    public string LeftRecordId { get; set; } = string.Empty;
    public string LeftPartyName { get; set; } = string.Empty;
    public string RightRecordId { get; set; } = string.Empty;
    public string RightPartyName { get; set; } = string.Empty;
    public string SharedLocation { get; set; } = string.Empty;
    public string SimilarityBand { get; set; } = string.Empty;
  }

  internal sealed class HierarchyViolationRow
  {
    public string RecordId { get; set; } = string.Empty;
    public string AssetReference { get; set; } = string.Empty;
    public string ExpectedParentReference { get; set; } = string.Empty;
    public string ActualParentReference { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
  }

  internal sealed class AssignmentGapRow
  {
    public string RecordId { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
  }

  internal sealed class RoleDistributionRow
  {
    public string Role { get; set; } = string.Empty;
    public int PrimaryContextCount { get; set; }
    public int SecondaryContextCount { get; set; }
  }
}
