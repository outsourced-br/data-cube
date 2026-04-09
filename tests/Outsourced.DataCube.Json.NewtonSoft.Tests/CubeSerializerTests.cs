namespace Outsourced.DataCube.Json.NewtonSoft.Tests;

using NUnit.Framework;
using global::Outsourced.DataCube.Serialization.Tests.Shared;

[TestFixture]
public sealed class CubeSerializerTests : CubeSerializerContractTests
{
  protected override ICubeJsonSerializerAdapter Serializer => NewtonSoftCubeJsonSerializerAdapter.Instance;
}
