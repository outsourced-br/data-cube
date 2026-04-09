namespace Outsourced.DataCube.Tests;

using Collections;
using Metrics;
using NUnit.Framework;

[TestFixture]
public sealed class AllocationFreeRegressionTests
{
  [Test]
  public void Flat_string_dictionary_rejects_duplicate_keys_ignoring_case()
  {
    var dictionary = new FlatStringDictionary<int>();

    dictionary.Add("region", 1);

    Assert.That(() => dictionary.Add("REGION", 2), Throws.ArgumentException);
  }

  [Test]
  public void Flat_string_dictionary_copy_to_validates_target_array()
  {
    var dictionary = new FlatStringDictionary<int>();
    dictionary.Add("region", 1);

    Assert.That(() => dictionary.CopyTo(null, 0), Throws.ArgumentNullException);
    Assert.That(() => dictionary.CopyTo(Array.Empty<KeyValuePair<string, int>>(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => dictionary.CopyTo(Array.Empty<KeyValuePair<string, int>>(), 0), Throws.ArgumentException);
  }

  [Test]
  public void Flat_string_dictionary_value_collection_reflects_entries_and_validates_copy()
  {
    var dictionary = new FlatStringDictionary<string>();
    dictionary.Add("region", "EU");
    dictionary.Add("product", "Widget");

    Assert.That(dictionary.Values.Contains("EU"), Is.True);
    var enumeratedValues = new List<string>();
    foreach (var value in dictionary.Values)
    {
      enumeratedValues.Add(value);
    }

    Assert.That(enumeratedValues, Is.EqualTo(new[] { "EU", "Widget" }));

    var values = new string[2];
    dictionary.Values.CopyTo(values, 0);

    Assert.That(values, Is.EqualTo(new[] { "EU", "Widget" }));
    Assert.That(() => dictionary.Values.CopyTo(null, 0), Throws.ArgumentNullException);
    Assert.That(() => dictionary.Values.CopyTo(Array.Empty<string>(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => dictionary.Values.CopyTo(Array.Empty<string>(), 0), Throws.ArgumentException);
  }

  [Test]
  public void Metric_type_dictionary_rejects_invalid_keys_and_null_values()
  {
    var dictionary = new MetricTypeDictionary();

    Assert.That(() => dictionary.Add((MetricType)999, new MetricCollection<int>()), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => dictionary.Add(MetricType.Int, null), Throws.ArgumentNullException);
    Assert.That(() => { _ = dictionary[(MetricType)999]; }, Throws.TypeOf<ArgumentOutOfRangeException>());

    Assert.That(dictionary.TryGetValue((MetricType)999, out var collection), Is.False);
    Assert.That(collection, Is.Null);
  }

  [Test]
  public void Metric_type_dictionary_key_and_value_collections_reflect_entries()
  {
    var dictionary = new MetricTypeDictionary();
    var intCollection = new MetricCollection<int>();
    var doubleCollection = new MetricCollection<double>();

    dictionary.Add(MetricType.Int, intCollection);
    dictionary.Add(MetricType.Double, doubleCollection);

    var enumeratedPairs = new List<MetricType>();
    foreach (var pair in dictionary)
    {
      enumeratedPairs.Add(pair.Key);
    }

    var enumeratedKeys = new List<MetricType>();
    foreach (var key in dictionary.Keys)
    {
      enumeratedKeys.Add(key);
    }

    var enumeratedValues = new List<IMetricCollection>();
    foreach (var value in dictionary.Values)
    {
      enumeratedValues.Add(value);
    }

    Assert.That(enumeratedPairs, Is.EqualTo(new[] { MetricType.Int, MetricType.Double }));
    Assert.That(enumeratedKeys, Is.EqualTo(new[] { MetricType.Int, MetricType.Double }));
    Assert.That(dictionary.Values.Contains(intCollection), Is.True);
    Assert.That(enumeratedValues, Is.EqualTo(new IMetricCollection[] { intCollection, doubleCollection }));

    var keys = new MetricType[2];
    dictionary.Keys.CopyTo(keys, 0);
    Assert.That(keys, Is.EqualTo(new[] { MetricType.Int, MetricType.Double }));

    var values = new IMetricCollection[2];
    dictionary.Values.CopyTo(values, 0);
    Assert.That(values, Is.EqualTo(new IMetricCollection[] { intCollection, doubleCollection }));
  }

  [Test]
  public void Average_for_integer_metrics_preserves_fractional_result()
  {
    var cube = new AnalyticsCube();
    var orders = cube.AddMetric(new CountMetric("orders", "Orders"));

    var firstFactGroup = cube.CreateAddFactGroup();
    firstFactGroup.SetMetricValue(orders, 1);

    var secondFactGroup = cube.CreateAddFactGroup();
    secondFactGroup.SetMetricValue(orders, 2);

    Assert.That(cube.Average(orders), Is.EqualTo(1.5d));
  }

  [Test]
  public void Calculation_extensions_cover_metric_key_dimension_and_extrema_paths()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var orders = cube.AddMetric(new CountMetric("orders", "Orders"));

    var firstFactGroup = cube.CreateAddFactGroup();
    firstFactGroup.SetDimensionValue(region, "EU");
    firstFactGroup.SetMetricValue(orders, 10);

    var secondFactGroup = cube.CreateAddFactGroup();
    secondFactGroup.SetDimensionValue(region, "NA");
    secondFactGroup.SetMetricValue(orders, 5);

    var metricOnlyFactGroup = cube.CreateAddFactGroup();
    metricOnlyFactGroup.SetMetricValue(orders, 50);

    Assert.That(cube.Calculate(orders, static values => values.Sum()), Is.EqualTo(65));
    Assert.That(cube.Calculate<int>("orders", static values => values.Count()), Is.EqualTo(3));
    Assert.That(cube.Calculate(region, orders, static values => values.Sum()), Is.EqualTo(15));
    Assert.That(cube.Sum(orders), Is.EqualTo(65));
    Assert.That(cube.Min(orders), Is.EqualTo(5));
    Assert.That(cube.Max(orders), Is.EqualTo(50));
  }

  [Test]
  public void Double_and_decimal_calculation_extensions_sum_and_average_values()
  {
    var cube = new AnalyticsCube();
    var score = cube.AddMetric(new AverageMetric("score", "Score"));
    var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

    var firstFactGroup = cube.CreateAddFactGroup();
    firstFactGroup.SetMetricValue(score, 1.5d);
    firstFactGroup.SetMetricValue(revenue, 10m);

    var secondFactGroup = cube.CreateAddFactGroup();
    secondFactGroup.SetMetricValue(score, 3.0d);
    secondFactGroup.SetMetricValue(revenue, 6.5m);

    Assert.That(cube.Sum(score), Is.EqualTo(4.5d));
    Assert.That(cube.Average(score), Is.EqualTo(2.25d));
    Assert.That(cube.Sum(revenue), Is.EqualTo(16.5m));
    Assert.That(cube.Average(revenue), Is.EqualTo(8.25m));
  }

  [Test]
  public void Fact_group_dimension_value_extensions_return_expected_values()
  {
    var cube = new AnalyticsCube();
    var region = cube.AddTypedDimension<string>("region", "Region");
    var orders = cube.AddMetric(new CountMetric("orders", "Orders"));

    var factGroup = cube.CreateAddFactGroup();
    factGroup.SetDimensionValue(region, "EU");
    factGroup.SetMetricValue(orders, 5);

    Assert.That(cube.FactGroups.GetDimensionValue((Dimension)region, (Metric)orders)?.Key, Is.EqualTo("EU"));
    Assert.That(cube.FactGroups.GetDimensionValue(region, orders)?.Value, Is.EqualTo("EU"));
    Assert.That(factGroup.GetFirstDimensionValue(), Is.EqualTo("EU"));
    Assert.That(factGroup.GetFirstDimensionValue<string>(), Is.EqualTo("EU"));
    Assert.That(factGroup.GetFirstDimensionValue("region"), Is.EqualTo("EU"));
    Assert.That(factGroup.GetFirstDimensionValue<string>("region"), Is.EqualTo("EU"));
    Assert.That(factGroup.GetFirstDimensionValue<int>("region"), Is.EqualTo(default(int)));
  }

  [Test]
  public void Dice_matches_full_grain_filters_regardless_of_filter_key_order_and_case()
  {
    var cube = new AnalyticsCube { Key = "sales", Label = "Sales" };
    var region = cube.AddTypedDimension<string>("region", "Region");
    var product = cube.AddTypedDimension<string>("product", "Product");
    var orders = cube.AddMetric(new CountMetric("orders", "Orders"));

    var euWidget = cube.CreateAddFactGroup();
    euWidget.SetDimensionValue(region, "EU");
    euWidget.SetDimensionValue(product, "Widget");
    euWidget.SetMetricValue(orders, 10);

    var naWidget = cube.CreateAddFactGroup();
    naWidget.SetDimensionValue(region, "NA");
    naWidget.SetDimensionValue(product, "Widget");
    naWidget.SetMetricValue(orders, 8);

    var filter = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      ["PRODUCT"] = "Widget",
      ["REGION"] = "EU"
    };

    var sliced = cube.Dice(filter);

    Assert.That(sliced.FactGroups, Has.Count.EqualTo(1));
    Assert.That(sliced.FactGroups.Single(), Is.SameAs(euWidget));
  }
}
