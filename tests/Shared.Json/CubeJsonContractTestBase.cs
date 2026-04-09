namespace Outsourced.DataCube.Serialization.Tests.Shared;

using DataCube;

public abstract class CubeJsonContractTestBase
{
  protected abstract ICubeJsonSerializerAdapter Serializer { get; }

  protected (string Json, AnalyticsCube Cube) RoundTripThroughString(AnalyticsCube cube)
  {
    var json = Serializer.Serialize(cube);
    return (json, Serializer.Deserialize(json));
  }
}

public interface ICubeJsonSerializerAdapter
{
  string Serialize(AnalyticsCube cube);
  void Serialize(AnalyticsCube cube, Stream stream);
  AnalyticsCube Deserialize(string json);
  AnalyticsCube Deserialize(Stream stream);
  Stream WriteAsJsonStream(AnalyticsCube cube, Stream stream = null);
  T ReadJsonStreamAs<T>(Stream jsonStream);
  void SaveToFile(AnalyticsCube cube, string filePath);
  AnalyticsCube LoadFromFile(string filePath);
}
