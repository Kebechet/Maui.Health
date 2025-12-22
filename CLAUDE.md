# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the library (from repo root)
dotnet build src/Maui.Health/Maui.Health.csproj

# Build for specific platform
dotnet build src/Maui.Health/Maui.Health.csproj -f net9.0-android
dotnet build src/Maui.Health/Maui.Health.csproj -f net9.0-ios

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

## Code Style Preferences

- Prefer `is null` / `is not null` pattern matching over `.HasValue` for nullable value types
- File-scoped namespaces
- Private fields prefixed with underscore (`_fieldName`)
- Use `var` for type inference
- Always use braces for control flow statements
