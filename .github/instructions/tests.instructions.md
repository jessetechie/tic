---
description: "Use when creating or modifying test code in Tik.Engine.Tests, Tik.ResourceAccess.Tests, or Tik.Tests.Shared, including xUnit tests and in-memory SQLite setup."
name: "Tik Test Guidelines"
applyTo: "Tik.Engine.Tests/**/*.cs,Tik.ResourceAccess.Tests/**/*.cs,Tik.Tests.Shared/**/*.cs"
---
# Test Guidelines

- Use xUnit attributes and assertions (`[Fact]`, `Assert.*`) consistently with existing test classes.
- Name test methods in descriptive, behavior-first style (for example: `can_add_time_log`).
- Keep test methods `async Task` when calling async APIs.
- Keep Arrange/Act/Assert flow clear with minimal incidental logic inside each test.
- Prefer deterministik test data (fixed dates/times and explicit expected durations/counts).

## SQLite Test Setup

- Use `TestDataContext` for in-memory SQLite test databases.
- Keep at least one connection open for the full test class lifetime when using `Mode=Memory;Cache=Shared`.
- Initialize required schema in test setup by calling `Init()` on each relevant resource before assertions.
- Dispose and close the lifetime connection in `Dispose()`.

## Scope and Boundaries

- Keep cross-layer integration in tests intentional: Engine tests may use ResourceAccess + TestDataContext, while ResourceAccess tests should validate persistence behavior directly.
- Do not move business logic into tests; tests should verify externally observable behavior.
- Keep shared test infrastructure concerns in `Tik.Tests.Shared` and keep it lightweight.
