# Outsourced.DataCube

`Outsourced.DataCube` is an in-memory analytics cube for .NET.

It gives your application one reusable analytical engine for working with business data through **dimensions**, **metrics**, and **fact groups**. Instead of rebuilding the same grouping and KPI logic in SQL, LINQ, exports, dashboards, and ad hoc scripts, you model the analytical structure once and reuse it.

In plain language, it helps you answer questions like:

- What did we sell by product, category, week, and hour?
- Which SKUs are growing, declining, or stagnant?
- Where is margin improving or eroding?
- Which products hold stock without demand?
- How do the same numbers look when sliced by different business angles?

## Why use a Data Cube?

Most teams do not suffer from a lack of data. They suffer from a lack of **consistent, reusable analysis**.

The same business question gets implemented over and over again:

- one SQL query for sales by week
- another report for margin by product
- another service method for inventory by category
- another export for repricing candidates

Over time, definitions drift. Revenue gets calculated slightly differently in different places. Percentages get averaged incorrectly. Time windows are handled one way in one module and another way elsewhere.

`Outsourced.DataCube` helps solve that by giving you:

- **one analytical model** instead of many scattered calculations
- **one place to define metrics** and their meaning
- **one reusable structure** for slicing, grouping, aggregating, and pivoting data
- **one testable foundation** for business analysis inside your application

## What problems it solves

### Business problems

**1. Inconsistent numbers across reports**
A cube gives you one place to define dimensions and metrics, so the same KPI is not reimplemented five different ways.

**2. Slow delivery of new analyses**
Once the model exists, a new analysis is often just a new view over existing dimensions and metrics instead of a new pipeline from scratch.

**3. Harder root-cause analysis**
Business questions are rarely one-dimensional. People want to move from total sales to sales by category, then by week, then by product, then by tag. A cube is built for that style of questioning.

**4. Weak trust in analytics**
When numbers are produced by many unrelated code paths, trust drops. A cube improves trust because analytical logic becomes more centralized and explicit.

### Technical problems

**1. Repeated grouping logic**
Instead of repeating `GroupBy`, filter, aggregate, and pivot logic across modules, you reuse one analytical model.

**2. Metric drift**
The same metric often gets defined slightly differently in different services or reports. A cube creates a shared contract for business meaning.

**3. Fragile report code**
Ad hoc analytical code tends to become large, repetitive, and hard to test. A cube gives you a clearer abstraction for multidimensional analysis.

**4. Harder evolution of analytics**
As dimensions like category, week, channel, store, or taxonomy tags get added, scattered code becomes costly to maintain. A cube makes those extra analytical axes much easier to absorb.

## Who it is for

### Developers

Use the cube when your application needs the same data analyzed from many angles and you want to avoid rebuilding analytical logic in multiple places.

### Product owners, founders, and solution consultants

Use the cube when you need a shared analytical language between business and implementation. It turns vague requests like “show me margin by category and week” into reusable model concepts instead of bespoke report work every time.

### Technically invested business users

Use the cube when you want faster iteration on analysis without turning every new question into a new reporting project.

## What it is not

`Outsourced.DataCube` is **not** a BI server, query language, or hosted analytics platform.

It is an **in-memory analytical engine** for your application or library.

That is its strength.

It is small enough to embed, explicit enough to test, and flexible enough to support rich analytical features without forcing you into a heavyweight platform.

## Core ideas

### Dimensions

Dimensions are the business angles you want to analyze by.

Examples:

- product
- category
- date
- week
- hour
- store
- region
- channel
- taxonomy tag

### Metrics

Metrics are the values you want to measure.

Examples:

- revenue
- units sold
- receipt count
- inventory value
- gross profit
- margin percentage

### Fact groups

A fact group is one grouped observation in the cube: a combination of dimension values plus one or more metric values.

## Repository structure

This repository ships three related packages:

- `Outsourced.DataCube`
- `Outsourced.DataCube.Json.NewtonSoft`
- `Outsourced.DataCube.Json.SystemText`

The core package focuses on the in-memory cube model and query behavior. The JSON packages are optional adapters so consumers can opt into serialization support without pulling serializer dependencies into the core package.

## Installation

Install the core package first:

```bash
dotnet add package Outsourced.DataCube
```

Install one serializer package only if your application needs JSON persistence or transport:

```bash
dotnet add package Outsourced.DataCube.Json.NewtonSoft
```

```bash
dotnet add package Outsourced.DataCube.Json.SystemText
```

## Usage flow

The normal usage flow is:

1. Create an `AnalyticsCube`.
2. Register dimensions and metrics.
3. Add fact groups.
4. Query the cube using slice, dice, aggregate, and pivot helpers.
5. Optionally serialize the cube through one of the JSON packages.

## Quick start

```csharp
using System;
using System.Collections.Generic;
using Outsourced.DataCube;

var rows = new[]
{
  new { Region = "North", Period = "2026-04", Channel = "Direct", Revenue = 1200m },
  new { Region = "North", Period = "2026-04", Channel = "Partner", Revenue = 2300m },
  new { Region = "South", Period = "2026-04", Channel = "Direct", Revenue = 2200m },
};

var cube = new AnalyticsCube
{
  Key = "regional-performance",
  Label = "Regional Performance",
  PopulationCount = rows.Length,
};

var region = cube.AddTypedDimension<string>("region", "Region");
var period = cube.AddTypedDimension<string>("period", "Period");
var channel = cube.AddTypedDimension<string>("channel", "Channel");
var revenue = cube.AddCurrencyMetric("revenue", "EUR", "Revenue");

foreach (var row in rows)
{
  cube.CreateFactGroup()
    .WithDimensionValue(region, row.Region)
    .WithDimensionValue(period, row.Period)
    .WithDimensionValue(channel, row.Channel)
    .WithMetricValue(revenue, row.Revenue)
    .Build();
}

var northSlice = cube.Slice("region", "North");
var aprilNorthDirect = cube.Dice(new Dictionary<string, object>
{
  ["region"] = "North",
  ["period"] = "2026-04",
  ["channel"] = "Direct",
});
var revenueByRegion = cube.Pivot("region", revenue);

Console.WriteLine(northSlice.Aggregate(revenue));
Console.WriteLine(aprilNorthDirect.Aggregate(revenue));
Console.WriteLine(revenueByRegion["South"]);
```

## Real-world fit

The cube is especially useful when the same business data needs to support many kinds of analysis.

Examples:

- retail and product analytics
- pricing and margin analysis
- sales trends and time-window comparisons
- inventory and stock-risk analysis
- category and taxonomy-based slicing
- operational dashboards inside line-of-business applications

## Serialization

Serialization is optional and lives in separate packages.

### `System.Text.Json`

```csharp
using Outsourced.DataCube.Json.SystemText;

var json = CubeSerializer.Serialize(cube);
var clone = CubeSerializer.Deserialize(json);
```

### `Newtonsoft.Json`

```csharp
using Outsourced.DataCube.Json.NewtonSoft;

var json = CubeSerializer.Serialize(cube);
var clone = CubeSerializer.Deserialize(json);
```

## Development

Build, test, and pack from the repository root:

```bash
dotnet build ./Outsourced.DataCube.sln -c Release
dotnet test ./Outsourced.DataCube.sln -c Release
dotnet pack ./Outsourced.DataCube.sln -c Release
```

## Documentation and AI context

For repository navigation and AI-assisted analysis, the main context files are:

- `README.md`
- `.ai/architecture.md`
- `.ai/tech-stack.md`
- `.ai/coding-style.md`
- `llms.txt`

The `docs` folder also contains repomix-generated packed repository snapshots:

- `docs/index.html`
- `docs/Outsourced.DataCube.xml`
- `docs/Outsourced.DataCube.Json.NewtonSoft.xml`
- `docs/Outsourced.DataCube.Json.SystemText.xml`

Those files are useful for AI/repository analysis workflows and should be treated as generated reference artifacts, not hand-maintained source documentation.

## Summary

Use `Outsourced.DataCube` when you have the same business data being analyzed from many angles and you want one reusable, testable, and consistent way to turn that data into decisions.

## License

This repository is licensed under the [GNU General Public License v3.0](LICENSE).
