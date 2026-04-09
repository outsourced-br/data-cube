namespace Outsourced.DataCube.Json.SystemText.Tests;

using NUnit.Framework;
using Metrics;

[TestFixture]
public sealed class ObjectValueJsonRegressionTests
{
  [Test]
  public void Composite_dimension_values_deserialize_as_clr_primitives_instead_of_json_elements()
  {
    var cube = new AnalyticsCube
    {
      Key = "typed-composite-values",
      Label = "Typed Composite Values",
      PopulationCount = 1,
    };

    var payload = new CompositeDimension("year", "isActive", "amount")
    {
      Key = "payload",
      Label = "Payload",
    };

    cube.AddDimension(payload);
    cube.AddMetric(new CountMetric("count", "Count"));

    cube.CreateFactGroup()
      .WithDimensionValue(payload, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
      {
        ["year"] = 2026,
        ["isActive"] = true,
        ["amount"] = 12.5m,
      })
      .Build();

    var result = CubeSerializer.Deserialize(CubeSerializer.Serialize(cube));
    var resultPayload = (CompositeDimensionValue)result.FactGroups.Single().GetDimensionValue("payload");

    Assert.That(resultPayload.GetFieldValue<int>("year"), Is.EqualTo(2026));
    Assert.That(resultPayload.GetFieldValue<bool>("isActive"), Is.True);
    Assert.That(resultPayload.GetFieldValue<decimal>("amount"), Is.EqualTo(12.5m));
    Assert.That(resultPayload.Value["year"], Is.TypeOf<int>());
    Assert.That(resultPayload.Value["isActive"], Is.TypeOf<bool>());
    Assert.That(resultPayload.Value["amount"], Is.TypeOf<decimal>());
  }
}
