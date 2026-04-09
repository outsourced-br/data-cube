# Publish Checklist

Use this list before each NuGet publish.

- Choose and set an explicit package version.
- Set the final public GitHub remote before publishing. `PublishRepositoryUrl` is enabled, so the package manifest and Source Link metadata can flow from the real repository once that URL exists.
- Add a package icon only after the final asset is ready. The project files intentionally avoid a placeholder icon path.
- Run `dotnet test ./Outsourced.DataCube.sln -c Release`.
- Review code coverage for the core package and confirm new or changed code paths have direct unit or integration coverage.
- Verify both serializer packages still round-trip representative cubes and regression fixtures.
- Compare the exported public API against the previous release or `HEAD` and document any intentional changes.
- If a public API change was introduced for performance reasons, keep benchmark evidence showing the gain is worth the compatibility cost.
- Run the affected BenchmarkDotNet scenarios and compare throughput plus allocations against the previous baseline.
- Run `dotnet pack ./Outsourced.DataCube.sln -c Release`.
- Publish both the `.nupkg` and `.snupkg` files from `artifacts/packages`.
- Push the rewritten single-commit history with `--force` when the public GitHub repository is ready.
