# Coding Style

## Formatting

- Use 2-space indentation.
- Keep braces on new lines.
- Preserve file-scoped namespaces where the file already uses them.
- Keep files UTF-8 and end with a trailing newline.

## Types And Locals

- Prefer `var` when the type is obvious from the right-hand side or when the exact type adds little value.
- Prefer explicit types when the concrete type carries meaning for readability.
- Do not rewrite whole files just to convert between `var` and explicit types.

## Null And Argument Handling

- Use guard clauses early.
- Prefer `ArgumentNullException.ThrowIfNull(...)` and `ArgumentException.ThrowIfNullOrEmpty(...)` where applicable.
- Be conservative with nullable changes. The repository is not fully migrated to nullable reference types, so prefer targeted `#nullable enable` updates instead of sweeping assembly-wide changes.
- When a public API can genuinely return no value, annotate it clearly rather than relying on convention alone.

## Collections And String Keys

- Use explicit string comparers when keys are intended to be case-insensitive.
- Favor `StringComparison.Ordinal` or `StringComparison.OrdinalIgnoreCase` over culture-sensitive comparisons unless there is a strong reason not to.
- Keep core data structures lightweight and predictable.

## Public API And Documentation

- This is a reusable library, so public API clarity matters.
- Add or keep XML docs where they explain intent, contracts, or non-obvious behavior.
- Avoid noisy comments that only restate the code.
- Prefer small, focused changes over broad refactors when touching public surface area.

## Async And Await

Most of the current codebase is synchronous. If async code is added:

- use `async` and `await` instead of blocking with `.Result` or `.Wait()`
- add a `CancellationToken` for longer-running or I/O-bound operations
- suffix asynchronous methods with `Async`
- consider `ConfigureAwait(false)` in library code when appropriate
- do not introduce async APIs unless the underlying work is genuinely asynchronous

## Exceptions

- Throw argument exceptions for invalid caller input.
- Keep exception messages specific and actionable.
- Prefer preserving current public behavior unless a bug fix clearly requires change.

## Tests

- Use NUnit.
- Prefer descriptive test names that read like behavior statements.
- Keep setup explicit and easy to follow.
- Reuse the shared serializer and fixture helpers that already exist under `tests/Outsourced.DataCube.Tests/Shared` and `tests/Shared.Json` when that keeps tests consistent.

## Package Hygiene

- Keep the core package free of unnecessary runtime dependencies.
- Put optional integration concerns in separate packages when possible.
- Avoid speculative abstractions that are not needed by the current public package surface.
