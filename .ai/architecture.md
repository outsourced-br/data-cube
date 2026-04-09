# Architecture

## Overview

This repository is a package-oriented .NET library solution, not a typical web application with API, Application, Domain, and Infrastructure projects. The architecture is centered around one core assembly and two thin serialization adapter assemblies:

- `source/Outsourced.DataCube`: core cube model, builders, OLAP-style query helpers, hierarchies, and metrics
- `source/Outsourced.DataCube.Json.NewtonSoft`: Json.NET serialization adapter for the core model
- `source/Outsourced.DataCube.Json.SystemText`: `System.Text.Json` serialization adapter for the core model
- `tests/*`: NUnit verification projects for core behavior and serializer parity
- `tests/Outsourced.DataCube.Benchmarks`: BenchmarkDotNet harness for manual performance work

## Solution Shape

The repository follows a library-first architecture with clear package boundaries:

1. Core domain and query engine
   - The core package contains the in-memory data model and the behavior that operates on it.
   - Important types include `AnalyticsCube`, `Dimension`, `DimensionValue`, `FactGroup`, `Metric`, `Hierarchy`, and related result models.

2. Construction layer
   - Builders and extension methods provide the fluent authoring experience.
   - This layer lives mostly under `Builders/` and `Extensions/`.
   - It helps callers register dimensions, metrics, grain, and fact groups without exposing a separate application service layer.

3. Query and analysis layer
   - OLAP-style operations are implemented as extensions over the core model.
   - This includes slice, dice, aggregate, pivot, roll-up, drill-down, totals, and time-window helpers.
   - The core behavior is in-memory and synchronous by design.

4. Serialization adapter layer
   - JSON support is intentionally split into separate packages instead of being embedded into the core package.
   - Each adapter references the core package and owns its own converters, settings, and serializer entry points.
   - This keeps the core assembly dependency-light.

5. Verification and performance layer
   - The test projects validate public behavior, serializer contracts, and regression scenarios.
   - The benchmark project is intentionally outside the default CI test gate.

## Internal Layering In The Core Package

Inside `Outsourced.DataCube`, the code is best understood as these logical layers:

- Model layer
  - Core entities such as cubes, dimensions, values, fact groups, metrics, hierarchies, and result objects.
- Builder layer
  - Fluent construction helpers for dimensions, metrics, fact groups, and grain definitions.
- Operation layer
  - Extension methods that implement analytics workflows over the model.
- Collection and support layer
  - Specialized collection types and helper utilities used to keep the in-memory operations efficient and predictable.
- Well-known definitions
  - Common dimension and metric definitions under `WellKnown/`.

## Dependency Direction

The dependency flow is intentionally simple:

- The core package does not depend on either JSON package.
- Each JSON package depends on the core package.
- Tests depend on the package under test and sometimes reuse shared test helpers from sibling test projects.
- Benchmarks depend on the core package only.

This means the domain/query engine stays reusable even when a consumer does not want JSON serialization.

## Runtime Flow

At runtime, the normal flow looks like this:

1. Create an `AnalyticsCube`.
2. Register dimensions and metrics.
3. Add fact groups, typically through fluent builders.
4. Run OLAP-style operations over the in-memory graph.
5. Optionally serialize or deserialize the cube through one of the JSON adapter packages.

There is no persistence layer, background processing layer, HTTP layer, or database abstraction in this repository.

## Design Principles

The current implementation leans on a few consistent design choices:

- In-memory first: the cube is designed to be queried directly in process.
- Low dependency core: the main package has no external runtime package dependencies.
- Adapter packages for integration concerns: JSON support is separated from the core domain model.
- Case-insensitive key handling where the public model uses string identifiers.
- Public API focus: tests and docs are organized around externally observable behavior rather than internal implementation details.

## Extension Points

The main extension points for future work are:

- new metrics and calculated metric behavior
- additional query helpers over `AnalyticsCube`
- additional serialization adapters
- richer documentation and usage guides

If you add major capabilities, prefer preserving the existing shape: keep the core package focused, and add optional concerns as separate packages when they would otherwise pull in new dependencies.
