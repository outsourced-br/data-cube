namespace Outsourced.DataCube.Json.NewtonSoft.Tests;

using DataCube;
using global::Outsourced.DataCube.Serialization.Tests.Shared;
using CubeJsonSerializer = CubeSerializer;

internal sealed class NewtonSoftCubeJsonSerializerAdapter : ICubeJsonSerializerAdapter
{
  public static readonly ICubeJsonSerializerAdapter Instance = new NewtonSoftCubeJsonSerializerAdapter();

  private NewtonSoftCubeJsonSerializerAdapter()
  {
  }

  public string Serialize(AnalyticsCube cube) => CubeJsonSerializer.Serialize(cube);

  public void Serialize(AnalyticsCube cube, Stream stream) => CubeJsonSerializer.Serialize(cube, stream);

  public AnalyticsCube Deserialize(string json) => CubeJsonSerializer.Deserialize(json);

  public AnalyticsCube Deserialize(Stream stream) => CubeJsonSerializer.Deserialize(stream);

  public Stream WriteAsJsonStream(AnalyticsCube cube, Stream stream = null) => CubeJsonSerializer.WriteAsJsonStream(cube, stream);

  public T ReadJsonStreamAs<T>(Stream jsonStream) => CubeJsonSerializer.ReadJsonStreamAs<T>(jsonStream);

  public void SaveToFile(AnalyticsCube cube, string filePath) => CubeJsonSerializer.SaveToFile(cube, filePath);

  public AnalyticsCube LoadFromFile(string filePath) => CubeJsonSerializer.LoadFromFile(filePath);
}
