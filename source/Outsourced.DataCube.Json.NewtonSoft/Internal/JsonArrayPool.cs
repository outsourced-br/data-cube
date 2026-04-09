namespace Outsourced.DataCube.Json.NewtonSoft.Internal;

using System.Buffers;
using System.Runtime.CompilerServices;
using global::Newtonsoft.Json;

internal sealed class JsonArrayPool<T> : IArrayPool<T>
{
  private readonly ArrayPool<T> _inner;

  public JsonArrayPool(ArrayPool<T> inner)
  {
    ArgumentNullException.ThrowIfNull(inner);
    _inner = inner;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T[] Rent(int minimumLength)
  {
    return _inner.Rent(minimumLength);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Return(T[] array)
  {
    if (array == null)
      return;

    _inner.Return(array);
  }
}
