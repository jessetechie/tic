---
description: "Use when editing Tic.ResourceAccess, Tic.ResourceAccess.Tests, or Tic.Tests.Shared code that touches Dapper, SQLite schema initialization, query filtering, or persistence contracts."
name: "Tic Resource Access Guidelines"
applyTo: "Tic.ResourceAccess/**/*.cs,Tic.ResourceAccess.Tests/**/*.cs,Tic.Tests.Shared/**/*.cs"
---
# Resource Access Guidelines

- Keep persistence concerns in `Tic.ResourceAccess`; do not add manager or engine business rules in this layer.
- Follow the existing file pattern in resource classes: resource interface, request/response/command records, and implementation in the same file.
- Keep nullable reference type safety explicit: initialize non-nullable strings to `string.Empty` and arrays to `[]` in records.
- Keep command-style resource methods `Task<CommandResult>` and return `CommandResult.Success` or `CommandResult.Error(...)` instead of throwing for normal data-path failures.
- Keep SQL in C# raw string literals (`"""`) and use Dapper parameterization; never build SQL by string interpolation for user-provided values.
- Prefer `SqlBuilder` when optional filters are involved so predicates and parameter values stay centralized and safe.
- Open connections with `using var connection = _dataContext.Connect();` per operation.
- Keep schema creation in `Init()` methods through `IDatabaseInitializer`, using `CREATE TABLE IF NOT EXISTS` patterns.
- Preserve asynchronous APIs (`ExecuteAsync`, `QueryAsync`, `ExecuteScalarAsync`) across resource access implementations.

## Test-Specific Rules

- For in-memory SQLite tests (`Mode=Memory;Cache=Shared`), keep one connection open for the lifetime of each test class.
- Initialize schema in test setup (`Init()` for each relevant resource) before issuing reads/writes.
- Dispose the lifetime connection in `Dispose()` to keep test isolation deterministic.
