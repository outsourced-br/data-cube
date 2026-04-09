#nullable enable

namespace Outsourced.DataCube.Json.NewtonSoft;

using System.Diagnostics;
using System.Text;
using Microsoft.IO;
using global::Newtonsoft.Json;
using DataCube;
using Internal;

/// <summary>
/// Serializes and deserializes <see cref="AnalyticsCube"/> instances using Json.NET.
/// </summary>
public static class CubeSerializer
{
  private static readonly RecyclableMemoryStreamManager _streamManager;
  private static readonly JsonSerializer _serializer;

  private static readonly Encoding _encoding = new UTF8Encoding(false, true);
  private static readonly int _bufferSize = 16384;

  internal static readonly JsonArrayPool<char> ArrayPool = new(System.Buffers.ArrayPool<char>.Shared);
  internal static readonly DefaultJsonNameTable PropertyNameTable = new AutomaticJsonNameTable(5000);

  static CubeSerializer()
  {
    var settings = SerializerSettings.CubeSerializerSettings.Initialize();
    _serializer = JsonSerializer.Create(settings);
    _streamManager = new RecyclableMemoryStreamManager();

#if DEBUG
    _serializer.Error += (s, e) =>
    {
      Debugger.Break();
    };
#endif
  }

  #region Serialization

  /// <summary>
  /// Writes a cube to a JSON stream and returns the target stream.
  /// </summary>
  /// <param name="input">The cube to serialize.</param>
  /// <param name="stream">The destination stream. When <see langword="null"/>, a recyclable memory stream is created.</param>
  /// <returns>The stream containing the serialized cube.</returns>
  public static Stream WriteAsJsonStream(AnalyticsCube input, Stream? stream = null)
  {
    ArgumentNullException.ThrowIfNull(input);

    if (stream == null)
    {
      stream = _streamManager.GetStream();
      stream.Position = 0;
    }

    if (!stream.CanWrite) throw new IOException("Cannot write to stream");
    if (stream.CanSeek) stream.Position = 0;

    using var streamWriter = new StreamWriter(stream, _encoding, bufferSize: _bufferSize, leaveOpen: true);
    using var jsonWriter = new JsonTextWriter(streamWriter);

    _serializer.Serialize(jsonWriter, input);

    return stream;
  }

  /// <summary>
  /// Serializes a cube to the supplied stream using indented JSON.
  /// </summary>
  public static void Serialize(AnalyticsCube cube, Stream stream)
  {
    ArgumentNullException.ThrowIfNull(cube);
    ArgumentNullException.ThrowIfNull(stream);

    using var writer = new StreamWriter(stream, _encoding, _bufferSize, leaveOpen: true);
    using var jsonWriter = new JsonTextWriter(writer)
    {
      Formatting = Formatting.Indented,
      Indentation = 2,
      IndentChar = ' '
    };

    _serializer.Serialize(jsonWriter, cube);
    jsonWriter.Flush();
  }

  /// <summary>
  /// Serializes a cube to a JSON string.
  /// </summary>
  public static string Serialize(AnalyticsCube cube)
  {
    ArgumentNullException.ThrowIfNull(cube);

    using var stream = new MemoryStream();
    Serialize(cube, stream);

    stream.Position = 0;
    using var reader = new StreamReader(stream, _encoding);
    return reader.ReadToEnd();
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

  #endregion

  #region Deserialization

  /// <summary>
  /// Deserializes an <see cref="AnalyticsCube"/> from a JSON string.
  /// </summary>
  public static AnalyticsCube Deserialize(string json)
  {
    ArgumentNullException.ThrowIfNull(json);

    using var reader = new StringReader(json);
    using var jsonReader = new JsonTextReader(reader);

    jsonReader.ArrayPool = ArrayPool;
    jsonReader.PropertyNameTable = PropertyNameTable;

    var cube = _serializer.Deserialize<AnalyticsCube>(jsonReader);
    return cube ?? throw new JsonSerializationException("Unable to deserialize AnalyticsCube.");
  }

  /// <summary>
  /// Deserializes an <see cref="AnalyticsCube"/> from a stream.
  /// </summary>
  public static AnalyticsCube Deserialize(Stream stream)
  {
    ArgumentNullException.ThrowIfNull(stream);

    using var reader = new StreamReader(stream, _encoding, detectEncodingFromByteOrderMarks: true, bufferSize: _bufferSize, leaveOpen: true);
    using var jsonReader = new JsonTextReader(reader);

    jsonReader.ArrayPool = ArrayPool;
    jsonReader.PropertyNameTable = PropertyNameTable;

    var cube = _serializer.Deserialize<AnalyticsCube>(jsonReader);
    return cube ?? throw new JsonSerializationException("Unable to deserialize AnalyticsCube.");
  }

  /// <summary>
  /// Deserializes a JSON stream into the requested type.
  /// </summary>
  public static T ReadJsonStreamAs<T>(Stream jsonStream)
  {
    ArgumentNullException.ThrowIfNull(jsonStream);

    if (!jsonStream.CanRead) throw new IOException("Cannot read from stream");

    if (jsonStream.CanSeek)
      jsonStream.Position = 0;

    using var streamReader = new StreamReader(jsonStream, _encoding, bufferSize: _bufferSize, leaveOpen: true);
    using var jsonReader = new JsonTextReader(streamReader);

    jsonReader.ArrayPool = ArrayPool;
    jsonReader.PropertyNameTable = PropertyNameTable;

    var result = _serializer.Deserialize<T>(jsonReader);
    if (result is null)
      throw new JsonSerializationException($"Unable to deserialize {typeof(T).FullName ?? typeof(T).Name}.");

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

  #endregion
}
