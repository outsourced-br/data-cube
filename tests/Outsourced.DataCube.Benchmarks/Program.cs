using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Outsourced.DataCube.Benchmarks;

public class Program
{
  public static void Main(string[] args)
  {
    var rootDir = GetProjectRoot();
    var artifactsPath = Path.Combine(rootDir, "artifacts", "benchmarks");

    var config = DefaultConfig.Instance.WithArtifactsPath(artifactsPath);

    BenchmarkRunner.Run<CubeBenchmarks>(config);
  }

  private static string GetProjectRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && dir.GetFiles("Outsourced.DataCube.sln").Length == 0)
    {
      dir = dir.Parent;
    }
    return dir?.FullName ?? AppContext.BaseDirectory;
  }
}
