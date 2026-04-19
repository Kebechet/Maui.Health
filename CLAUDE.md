# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the library (from repo root)
dotnet build src/Maui.Health/Maui.Health.csproj

# Build for specific platform
dotnet build src/Maui.Health/Maui.Health.csproj -f net10.0-android
dotnet build src/Maui.Health/Maui.Health.csproj -f net10.0-ios

# Build the demo app
dotnet build demo/DemoApp/DemoApp/DemoApp.csproj
```

## Architecture

### Platform Abstraction via Partial Classes

The codebase uses C# partial classes to separate platform-specific code:

- **HealthService**: Core service with partial method declarations
  - `Services/HealthService.cs` - shared interface + partial signatures
  - `Platforms/Android/HealthServiceAndroid.cs` - Android Health Connect implementation
  - `Platforms/iOS/HealthServiceiOS.cs` - iOS HealthKit implementation
  - `Platforms/MacCatalyst/` and `Platforms/Windows/` - stub implementations

- **HealthWorkoutService**: Same pattern for workout/session management
  - Base class contains shared logic (duplicate detection, session persistence via Preferences)
  - Platform implementations handle actual health store operations

### Extension Methods for DTO Mapping

Platform-specific conversions use extension methods in `Platforms/{Platform}/Extensions/`:
- `HealthRecordExtensions.cs` (Android) - converts Java health records to/from DTOs
- `HKQuantitySampleExtensions.cs` (iOS) - converts HKQuantitySample to/from DTOs
- Type dispatch via switch expressions on `typeof(TDto).Name`

### DTO Structure

- `HealthMetricBase` - abstract base with Id, DataOrigin, Timestamp
- Metric DTOs (StepsDto, WeightDto, etc.) inherit from HealthMetricBase
- `WorkoutDto` - standalone workout model with activity type, time range, energy/distance
- `WorkoutSession` - in-memory model for active sessions with pause/resume state

### Key Services

- `IHealthService` - generic health data API: `GetHealthData<TDto>()`, `WriteHealthData<TDto>()`, permissions
- `IHealthWorkoutService` (via `IHealthService.Activity`) - workout CRUD + session management

## Test Commands

```bash
# Run all tests
dotnet test tests/Maui.Health.Tests/Maui.Health.Tests.csproj
```

## Test Conventions

- Test projects go in `tests/` directory and are nested under a `Tests` solution folder in the .sln
- Use xUnit
- Test method names use three-part underscore format: `MethodUnderTest_Scenario_ExpectedResult`
- Use `// Arrange`, `// Act`, `// Assert` comments in every test

### Why tests use `<Compile Include>` instead of `<ProjectReference>`

`Maui.Health.csproj` targets MAUI TFMs (`net10.0-android`, `-ios`, `-maccatalyst`, `-windows`). `Maui.Health.Tests.csproj` targets plain `net9.0` because xUnit runners don't run under MAUI TFMs. You can't `<ProjectReference>` across that boundary, so the test csproj pulls in individual platform-agnostic source files via `<Compile Include="..\..\src\Maui.Health\..." Link="..." />`.

**Drift hazard.** Nothing enforces the link list or the test files against library changes. Two ways it goes stale:
1. A new library type (or a new dependency of an already-linked type) isn't added to the test csproj's `<Compile Include>` list → build fails with `CS0246: type not found`.
2. A library type gains a new `required` member or a method gains a new parameter → existing test constructors/call sites fall behind → `CS9035` / `CS1503`.

Both kinds of drift are silent until someone runs `dotnet test`. Pre-my-work the project had been accumulating this for multiple releases.

**Rules when touching the library:**
- After adding any new type under `src/Maui.Health/Models/`, `Enums/`, or shared `Extensions/`, immediately add a matching `<Compile Include>` line to `tests/Maui.Health.Tests/Maui.Health.Tests.csproj` — even if no test uses it yet, because another linked file probably does.
- After making a property `required` or changing a public method signature, grep the test folder for constructor and call-site drift (`grep -rn "new TypeName\s*{" tests/` / `grep -rn "\.MethodName(" tests/`) and fix the callers in the same commit.
- Run `dotnet test tests/Maui.Health.Tests/Maui.Health.Tests.csproj` before committing any change to public types in `src/Maui.Health/Models/` or `Extensions/`. It's the only thing that catches drift early.

**Longer-term fix** (not yet applied): split into `Maui.Health` (platform-dependent) + `Maui.Health.Core` (pure POCO). Tests would `<ProjectReference>` the Core project, drift disappears, IDE rename works across the boundary.

## Code Style Preferences

- Prefer `is null` / `is not null` pattern matching over `.HasValue` for nullable value types
- File-scoped namespaces
- Private fields prefixed with underscore (`_fieldName`)
- Use `var` for type inference
- Always use braces for control flow statements
- Collection expressions:
  - Use `[]` for return statements: `return [];`
  - Keep `var x = new List<T>();` for variable declarations (to preserve `var`)
- **Docstring inheritance**: When a class implements an interface, add `/// <inheritdoc/>` to the class and its public members instead of duplicating docstrings
- **External SDK type documentation**: When documenting types/enums that map to external SDK constants (e.g., Android Health Connect), use the official description text from the docs and include the reference URL on a separate line within the `<summary>` block:
  ```csharp
  /// <summary>
  /// The Health Connect SDK is unavailable on this device at the time.
  /// https://developer.android.com/reference/androidx/health/connect/client/HealthConnectClient#SDK_UNAVAILABLE()
  /// </summary>
  SdkUnavailable = 1,
  ```

- **Evidence-backed decisions**: For constants, claims, and non-obvious business/architectural decisions backed by external documentation (e.g., Android/iOS system rules, API documentation), always include a comment with a reference link to the source. This ensures decisions are verifiable and grounded in evidence.
  ```csharp
  // The single-anchor first-grant timestamp matches Health Connect's
  // "30 days prior to when any permission was first granted" rule:
  // https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#read-data-older-than-30-days
  ```

- **Public API result types subclass `Result<TError>`**: Every public async operation that can fail returns a result type derived from `Models/Result.cs` — never a bare `bool`, `Task<T?>` where `null` means failure, or a tuple. This keeps failure surfaces uniform and lets callers choose between a terse `.IsSuccess` / `.IsError` check and a typed `switch` on the error enum.
  - Define a small enum in `Enums/Errors/` that enumerates the failure modes of that one operation — one case per distinct branch that returns an error, plus `UnexpectedException` for the `catch`. Do **not** reuse an unrelated operation's enum.
  - Subclass `Result<TError>` (or add `Models/XxxResult.cs`) with any extra success payload (e.g. `RecordIds`, `DeniedPermissions`) as `init`-only properties. Success-only payloads default to empty collections, not null.
  - At the producer side, construct with object initializers — not positional ctors — so the reader sees the semantics inline:
    ```csharp
    return new WriteHealthDataResult { RecordIds = recordIds };                    // success
    return new WriteHealthDataResult { Error = WriteHealthDataError.SdkUnavailable };
    return new WriteHealthDataResult
    {
        Error = WriteHealthDataError.UnexpectedException,
        ErrorException = ex,
    };
    ```
  - At every failure branch, emit a specific `Error` value. Never return `new XxxResult()` as a generic failure — the enum exists so callers can distinguish "permission denied" from "SDK down" without parsing logs.
  - In `catch` blocks, always pass the caught exception on `ErrorException` alongside `Error = ...UnexpectedException`. Don't swallow it into the log only.
  - When a sub-operation (e.g. `RequestPermissions`) returns its own `Result<TError>`, forward its `ErrorException` upward so the chain doesn't lose the original throw site.
  - Consumers should prefer `.IsError` over `!.IsSuccess` for early returns — reads better and matches the existing idiom in this codebase.
