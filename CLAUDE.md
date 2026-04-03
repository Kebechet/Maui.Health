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
