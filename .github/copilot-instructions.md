# Project Guidelines

## Code Style
- Target .NET 8 and C# 12.
- Keep nullable reference types enabled and satisfy nullability explicitly.
- Prefer records with init-only properties for commands, queries, and response DTOs.
- Use async Task-based APIs across manager/engine/resource-access layers.
- Keep SQL in Dapper raw string literals for readability and parameterization.

## Architecture
- Maintain current layering and dependency direction:
  - Tik.Shared: shared primitives only (no higher-layer dependencies)
  - Tik.ResourceAccess: persistence and schema initialization (Dapper + SQLite)
  - Tik.Engine: summary/domain calculations over resource-access data
  - Tik.Manager: command/query orchestration over engine/resource access
  - Tik.Client.CommandLine: Spectre.Console CLI entrypoint and DI composition
- Do not introduce reverse references from lower layers to higher layers.
- Keep business rules in Engine/Manager, and persistence concerns in ResourceAccess.

## Build and Test
- Preferred local commands from repository root:
  - dotnet build
  - dotnet test
- Useful focused test commands:
  - dotnet test Tik.Engine.Tests
  - dotnet test Tik.ResourceAccess.Tests

## Conventions
- Manager commands currently throw when underlying CommandResult is not successful; preserve this behavior unless intentionally refactoring error handling end-to-end.
- Use CommandResult success/failure semantics for command-style operations in resource and engine layers.
- Keep DI registration centralized in Bootstrapper, and ensure schema initialization remains part of CLI bootstrapping.

## Testing Pitfalls
- In-memory SQLite tests depend on an open connection for database lifetime (Mode=Memory;Cache=Shared).
- When adding resource-access tests, keep one connection open for the test class lifetime and dispose it in Dispose().
- Initialize schema explicitly in tests before issuing queries/commands (Init() for relevant resources).

## Key Pattern References
- Tik.Client.CommandLine/Bootstrapper.cs
- Tik.Manager/CommandManager.cs
- Tik.Manager/QueryManager.cs
- Tik.Engine/SummaryCalculator.cs
- Tik.ResourceAccess/Category.cs
- Tik.ResourceAccess/DataContext.cs
- Tik.ResourceAccess.Tests/LogResourceAccessTests.cs
- Tik.Tests.Shared/TestDataContext.cs
