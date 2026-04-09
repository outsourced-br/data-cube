# Tech Stack

## Runtime And Language

- .NET 10 (`net10.0`)
- C# 14
- `ImplicitUsings` enabled
- Nullable reference types are globally disabled in shared build props, with selective `#nullable enable` adoption in files that already have targeted annotations

## Production Packages

### Core package: `Outsourced.DataCube`

- No external runtime NuGet dependencies
- Uses the .NET base class library only

### JSON adapter: `Outsourced.DataCube.Json.NewtonSoft`

- [`Newtonsoft.Json` 13.0.3](https://www.nuget.org/packages/Newtonsoft.Json/13.0.3)
- [`Microsoft.IO.RecyclableMemoryStream` 3.0.0](https://www.nuget.org/packages/Microsoft.IO.RecyclableMemoryStream/3.0.0)

### JSON adapter: `Outsourced.DataCube.Json.SystemText`

- `System.Text.Json` from the .NET 10 shared framework
- [`Microsoft.IO.RecyclableMemoryStream` 3.0.0](https://www.nuget.org/packages/Microsoft.IO.RecyclableMemoryStream/3.0.0)

## Test Stack

- `Microsoft.NET.Test.Sdk` 17.9.0
- `NUnit` 4.1.0
- `NUnit3TestAdapter` 4.5.0
- `NUnit.Analyzers` 4.1.0
- `coverlet.collector` 6.0.2

## Benchmarking

- `BenchmarkDotNet` 0.15.8

## Build, Packaging, And CI

- SDK-style `.csproj` projects
- NuGet packaging via `dotnet pack`
- XML docs enabled for the three packable library projects
- Package README packaging enabled
- Symbol packages generated as `.snupkg`
- Source Link metadata via the .NET SDK build pipeline and `PublishRepositoryUrl`
- GitHub Actions for restore, build, test, and pack

## Guidance For AI Agents

When suggesting changes, assume this repository wants low-dependency, package-friendly solutions.

Prefer:

- .NET 10 and BCL features
- SDK-style project settings
- NUnit for tests
- lightweight library patterns
- optional adapters instead of forcing new runtime dependencies into the core package

Avoid suggesting by default:

- ASP.NET Core hosting layers
- Entity Framework Core
- MediatR
- Autofac or other DI containers
- xUnit or MSTest migrations
- database-backed persistence abstractions
- message buses, hosted services, or HTTP APIs

Only introduce those kinds of technologies if the user explicitly asks for them.
