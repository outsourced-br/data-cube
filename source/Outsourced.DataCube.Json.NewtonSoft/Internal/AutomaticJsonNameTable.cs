namespace Outsourced.DataCube.Json.NewtonSoft.Internal;

using global::Newtonsoft.Json;

internal class AutomaticJsonNameTable(int max = 500) : DefaultJsonNameTable
{
  private int _added;
  private readonly int _max = max;

  public override string Get(char[] key, int start, int length)
  {
    var value = base.Get(key, start, length);

    if (value != null || _added >= _max)
      return value;

    value = new string(key, start, length);
    Add(value);
    _added++;

    return value;
  }
}
