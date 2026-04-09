using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Outsourced.DataCube;
using Outsourced.DataCube.Tests.Shared;
using SystemTextSerializer = Outsourced.DataCube.Json.SystemText.CubeSerializer;
using NewtonsoftSerializer = Outsourced.DataCube.Json.NewtonSoft.CubeSerializer;

namespace Outsourced.DataCube.Json.Data.Tests;

[TestFixture]
public class DataParityTests
{
  [Test]
  public void SystemText_And_Newtonsoft_Produce_Identical_Json()
  {
    var cube = SerializationFixtureFactory.CreateAnalyticsCube();

    var newtonsoftJson = NewtonsoftSerializer.Serialize(cube);
    var systemTextJson = SystemTextSerializer.Serialize(cube);

    var newtonsoftJToken = JToken.Parse(newtonsoftJson);
    var systemTextJToken = JToken.Parse(systemTextJson);

    bool areEqual = JToken.DeepEquals(newtonsoftJToken, systemTextJToken);

    if (!areEqual)
    {
      Console.WriteLine("Newtonsoft:");
      Console.WriteLine(newtonsoftJson);
      Console.WriteLine();
      Console.WriteLine("SystemText:");
      Console.WriteLine(systemTextJson);
    }

    Assert.That(areEqual, Is.True, "Newtonsoft and SystemText serialization results should be structurally identical.");
  }

  [Test]
  public void SystemText_And_Newtonsoft_Write_Outsourced_Type_Markers_For_Polymorphic_Members()
  {
    var cube = SerializationFixtureFactory.CreateAnalyticsCube();
    var expectedDimensionType = typeof(Dimension<int>).AssemblyQualifiedName;
    var expectedTypedValueType = typeof(DimensionValue<int>).AssemblyQualifiedName;
    var expectedCompositeValueType = typeof(CompositeDimensionValue).AssemblyQualifiedName;

    AssertOutsourcedTypeMarkers(NewtonsoftSerializer.Serialize(cube), expectedDimensionType, expectedTypedValueType, expectedCompositeValueType);
    AssertOutsourcedTypeMarkers(SystemTextSerializer.Serialize(cube), expectedDimensionType, expectedTypedValueType, expectedCompositeValueType);
  }

  private static void AssertOutsourcedTypeMarkers(
    string json,
    string expectedDimensionType,
    string expectedTypedValueType,
    string expectedCompositeValueType)
  {
    var root = JToken.Parse(json);

    Assert.That(root["Dimensions"]?["year"]?["$type"]?.Value<string>(), Is.EqualTo(expectedDimensionType));
    Assert.That(root["FactGroups"]?[0]?["D"]?["year"]?["$type"]?.Value<string>(), Is.EqualTo(expectedTypedValueType));
    Assert.That(root["FactGroups"]?[0]?["D"]?["Composite"]?["$type"]?.Value<string>(), Is.EqualTo(expectedCompositeValueType));
  }
}
