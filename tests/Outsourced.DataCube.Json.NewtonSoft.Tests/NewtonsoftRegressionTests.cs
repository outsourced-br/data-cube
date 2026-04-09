namespace Outsourced.DataCube.Json.NewtonSoft.Tests;

using NUnit.Framework;
using DataCube;
using global::Outsourced.DataCube.Tests.Shared;
using global::Newtonsoft.Json;
using global::Newtonsoft.Json.Linq;
using SerializerSettings;

[TestFixture]
public sealed class NewtonsoftRegressionTests
{
  [Test]
  public void Serializer_settings_do_not_enable_json_net_type_name_handling()
  {
    var settings = CubeSerializerSettings.Initialize();

    Assert.That(settings.TypeNameHandling, Is.EqualTo(TypeNameHandling.None));
    Assert.That(settings.MetadataPropertyHandling, Is.EqualTo(MetadataPropertyHandling.Ignore));
  }

  [Test]
  public void Round_trip_preserves_typed_dimensions_with_converter_managed_type_markers()
  {
    var cube = SerializationFixtureFactory.CreateAnalyticsCube();

    var result = CubeSerializer.Deserialize(CubeSerializer.Serialize(cube));

    Assert.That(result.GetDimension("entityType"), Is.InstanceOf<Dimension<string>>());
    Assert.That(result.GetDimension("year"), Is.InstanceOf<Dimension<int>>());
    Assert.That(result.GetDimension("Composite"), Is.InstanceOf<CompositeDimension>());
    Assert.That(result.FactGroups[0].GetDimensionValue("year"), Is.InstanceOf<DimensionValue<int>>());
    Assert.That(result.FactGroups[0].GetDimensionValue("Composite"), Is.InstanceOf<CompositeDimensionValue>());
  }

  [Test]
  public void Serialize_writes_string_type_markers_for_dimensions_and_dimension_values()
  {
    var cube = SerializationFixtureFactory.CreateAnalyticsCube();

    var json = CubeSerializer.Serialize(cube);
    var root = JObject.Parse(json);
    var yearValues = root["Dimensions"]?["year"]?["Values"] as JArray;

    Assert.That(root["$type"], Is.Null);
    Assert.That(root["Dimensions"]?["year"]?["$type"]?.Type, Is.EqualTo(JTokenType.String));
    Assert.That(root["Dimensions"]?["year"]?["$type"]?.Value<string>(), Is.EqualTo(typeof(Dimension<int>).AssemblyQualifiedName));
    Assert.That(yearValues, Is.Not.Null.And.Not.Empty);
    Assert.That(yearValues!.All(static value => value["$type"]?.Type == JTokenType.String), Is.True);
    Assert.That(root["FactGroups"]?[0]?["D"]?["year"]?["$type"]?.Type, Is.EqualTo(JTokenType.String));
  }
}
