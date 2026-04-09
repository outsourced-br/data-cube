#nullable enable

namespace Outsourced.DataCube.Json.SystemText;

using Microsoft.IO;
using DataCube;
using global::System.Text.Json;

/// <summary>
/// Serializes and deserializes <see cref="AnalyticsCube"/> instances using <see cref="JsonSerializer"/>.
/// </summary>
public static class CubeSerializer
{
  private static readonly RecyclableMemoryStreamManager StreamManager = new();
  private static readonly JsonSerializerOptions Options = SerializerSettings.CubeSerializerSettings.CreateOptions();

  /// <summary>
  /// Writes a cube to a JSON stream and returns the target stream.
  /// </summary>
  public static Stream WriteAsJsonStream(AnalyticsCube input, Stream? stream = null)
  {
    ArgumentNullException.ThrowIfNull(input);

    stream ??= StreamManager.GetStream();
    if (!stream.CanWrite)
      throw new IOException("Cannot write to stream");

    if (stream.CanSeek)
      stream.Position = 0;

    Serialize(input, stream);
    return stream;
  }

  /// <summary>
  /// Serializes a cube to the supplied stream.
  /// </summary>
  public static void Serialize(AnalyticsCube cube, Stream stream)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(stream);

    JsonSerializer.Serialize(stream, cube, Options);
    stream.Flush();
  }

  /// <summary>
  /// Serializes a cube to a JSON string.
  /// </summary>
  public static string Serialize(AnalyticsCube cube)
  {
    ArgumentNullException.ThrowIfNull(cube);
    return JsonSerializer.Serialize(cube, Options);
  }

  /// <summary>
  /// Serializes a cube to a file.
  /// </summary>
  public static void SaveToFile(AnalyticsCube cube, string filePath)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentException.ThrowIfNullOrEmpty(filePath);

    using var fileStream = File.Create(filePath);
    Serialize(cube, fileStream);
  }

  /// <summary>
  /// Deserializes an <see cref="AnalyticsCube"/> from a JSON string.
  /// </summary>
  public static AnalyticsCube Deserialize(string json)
  {
    ArgumentNullException.ThrowIfNull(json);
    return JsonSerializer.Deserialize<AnalyticsCube>(json, Options)
      ?? throw new JsonException("Unable to deserialize AnalyticsCube.");
  }

  /// <summary>
  /// Deserializes an <see cref="AnalyticsCube"/> from a stream.
  /// </summary>
  public static AnalyticsCube Deserialize(Stream stream)
  {
    ArgumentNullException.ThrowIfNull(stream);
    return JsonSerializer.Deserialize<AnalyticsCube>(stream, Options)
      ?? throw new JsonException("Unable to deserialize AnalyticsCube.");
  }

  /// <summary>
  /// Deserializes a JSON stream into the requested type.
  /// </summary>
  public static T ReadJsonStreamAs<T>(Stream jsonStream)
  {
    ArgumentNullException.ThrowIfNull(jsonStream);

    if (!jsonStream.CanRead)
      throw new IOException("Cannot read from stream");

    if (jsonStream.CanSeek)
      jsonStream.Position = 0;

    var result = JsonSerializer.Deserialize<T>(jsonStream, Options);
    if (result is null)
      throw new JsonException($"Unable to deserialize {typeof(T).FullName ?? typeof(T).Name}.");

    return result;
  }

  /// <summary>
  /// Loads a cube from a JSON file.
  /// </summary>
  public static AnalyticsCube LoadFromFile(string filePath)
  {
    ArgumentException.ThrowIfNullOrEmpty(filePath);

    using var fileStream = File.OpenRead(filePath);
    return Deserialize(fileStream);
  }
}
