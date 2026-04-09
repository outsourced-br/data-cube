using BenchmarkDotNet.Attributes;
using Outsourced.DataCube.Builders;
using Outsourced.DataCube.Metrics;

namespace Outsourced.DataCube.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class CubeBenchmarks
{
  private AnalyticsCube _cube = null!;
  private Metric<int> _countMetric = null!;
  private Metric<decimal> _revenueMetric = null!;
  private Dimension<string> _region = null!;
  private Dimension<string> _product = null!;

  [Params(100, 10000)]
  public int N;

  [GlobalSetup]
  public void Setup()
  {
    _cube = new AnalyticsCube { Key = "benchmark", Label = "Benchmark Cube" };
    _region = _cube.AddTypedDimension<string>("region", "Region");
    _product = _cube.AddTypedDimension<string>("product", "Product");
    _countMetric = MetricBuilder<int>.Count(_cube, "count").Build();
    _revenueMetric = _cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    var regions = new[] { "NA", "EU", "APAC" };
    var products = new[] { "A", "B", "C" };
    var random = new Random(42);

    for (int i = 0; i < N; i++)
    {
      _cube.CreateFactGroup()
          .WithDimensionValue(_region, regions[random.Next(regions.Length)])
          .WithDimensionValue(_product, products[random.Next(products.Length)])
          .WithMetricValue(_countMetric, 1)
          .WithMetricValue(_revenueMetric, (decimal)(random.NextDouble() * 100))
          .Build();
    }
  }

  [Benchmark]
  public void BuildFactGroup()
  {
    var cube = new AnalyticsCube { Key = "benchmark", Label = "Benchmark Cube" };
    var region = cube.AddTypedDimension<string>("region", "Region");
    var revenueMetric = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    for (int i = 0; i < N; i++)
    {
      cube.CreateFactGroup()
          .WithDimensionValue(region, "EU")
          .WithMetricValue(revenueMetric, 10m)
          .Build();
    }
  }

  [Benchmark]
  public decimal Aggregate()
  {
    return _cube.Aggregate(_revenueMetric);
  }

  [Benchmark]
  public AnalyticsCube Slice()
  {
    return _cube.Slice("region", "EU");
  }
}

