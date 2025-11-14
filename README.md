[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/kebechet)

# Maui.Health
![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Maui.Health)
![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Maui.Health)

Abstraction around `Android Health Connect` and `iOS HealthKit` with unified DTO-based API
⚠️ Beware, this package is currently just as **Proof of concept**. There is a lot of work required for proper stability and ease of use.
[Issues](https://github.com/Kebechet/Maui.Health/issues) will contain future tasks that should be implemented.

Feel free to contribute ❤️

## Features

- **Generic API**: Use `GetHealthDataAsync<TDto>()` for type-safe health data retrieval
- **Unified DTOs**: Platform-agnostic data transfer objects with common properties
- **Time Range Support**: Duration-based metrics implement `IHealthTimeRange` interface
- **Cross-Platform**: Works with Android Health Connect and iOS HealthKit

## Platform Support & Health Data Mapping

| Health Data Type | Android Health Connect | iOS HealthKit | Wrapper Implementation |
|-----------------|------------------------|---------------|----------------------|
| **Steps** | ✅ StepsRecord | ✅ StepCount | ✅ [`StepsDto`](src/Maui.Health/Models/Metrics/StepsDto.cs) |
| **Weight** | ✅ WeightRecord | ✅ BodyMass | ✅ [`WeightDto`](src/Maui.Health/Models/Metrics/WeightDto.cs) |
| **Height** | ✅ HeightRecord | ✅ Height | ✅ [`HeightDto`](src/Maui.Health/Models/Metrics/HeightDto.cs) |
| **Heart Rate** | ✅ HeartRateRecord | ✅ HeartRate | ✅ [`HeartRateDto`](src/Maui.Health/Models/Metrics/HeartRateDto.cs) |
| **Active Calories** | ✅ ActiveCaloriesBurnedRecord | ✅ ActiveEnergyBurned | ✅ [`ActiveCaloriesBurnedDto`](src/Maui.Health/Models/Metrics/ActiveCaloriesBurnedDto.cs) |
| **Exercise Session** | ✅ ExerciseSessionRecord | ✅ Workout | ✅ [`WorkoutDto`](src/Maui.Health/Models/Metrics/WorkoutDto.cs) |
| **Blood Glucose** | ✅ BloodGlucoseRecord | ✅ BloodGlucose | ❌ N/A |
| **Body Temperature** | ✅ BodyTemperatureRecord | ✅ BodyTemperature | ❌ N/A |
| **Oxygen Saturation** | ✅ OxygenSaturationRecord | ✅ OxygenSaturation | ❌ N/A |
| **Respiratory Rate** | ✅ RespiratoryRateRecord | ✅ RespiratoryRate | ❌ N/A |
| **Basal Metabolic Rate** | ✅ BasalMetabolicRateRecord | ✅ BasalEnergyBurned | ❌ N/A |
| **Body Fat** | ✅ BodyFatRecord | ✅ BodyFatPercentage | 🚧 WIP (commented out) |
| **Lean Body Mass** | ✅ LeanBodyMassRecord | ✅ LeanBodyMass | ❌ N/A |
| **Hydration** | ✅ HydrationRecord | ✅ DietaryWater | ❌ N/A |
| **VO2 Max** | ✅ Vo2MaxRecord | ✅ VO2Max | 🚧 WIP (commented out) |
| **Resting Heart Rate** | ✅ RestingHeartRateRecord | ✅ RestingHeartRate | ❌ N/A |
| **Heart Rate Variability** | ✅ HeartRateVariabilityRmssdRecord | ✅ HeartRateVariabilitySdnn | ❌ N/A |
| **Blood Pressure** | ✅ BloodPressureRecord | ✅ Split into Systolic/Diastolic | 🚧 WIP (commented out) |

## Usage

### 1. Registration
Register the health service in your `MauiProgram.cs`:
```csharp
builder.Services.AddHealth();
```

### 2. Platform Setup
Follow the [platform setup guide](https://github.com/Kebechet/Maui.Health/commit/139e69fade83f9133044910e47ad530f040b8021):

**Android (4 steps):**
- Google Play console Health permissions
- Privacy policy requirements  
- AndroidManifest.xml changes
- Minimum Android API 26

**iOS (3 steps):**
- Provisioning profile with HealthKit
- Entitlements.plist
- Info.plist adjustments

### 3. Basic Usage

```csharp
public class HealthExampleService
{
    private readonly IHealthService _healthService;

    public HealthExampleService(IHealthService healthService)
    {
        _healthService = healthService;
    }

    public async Task<List<StepsDto>> GetTodaysStepsAsync()
    {
        var timeRange = HealthTimeRange.FromDateTime(DateTime.Today, DateTime.Now);

        var steps = await _healthService.GetHealthDataAsync<StepsDto>(timeRange);
        return steps.ToList();
    }

    public async Task<List<WeightDto>> GetRecentWeightAsync()
    {
        var timeRange = HealthTimeRange.FromDateTime(DateTime.Now.AddDays(-7), DateTime.Now);

        var weights = await _healthService.GetHealthDataAsync<WeightDto>(timeRange);
        return weights.ToList();
    }
}
```

### 4. Working with Time Ranges

Duration-based metrics implement `IHealthTimeRange`:

```csharp
public async Task AnalyzeStepsData()
{
    var timeRange = HealthTimeRange.FromDateTime(DateTime.Today, DateTime.Now);
    var steps = await _healthService.GetHealthDataAsync<StepsDto>(timeRange);
    
    foreach (var stepRecord in steps)
    {
        // Common properties from BaseHealthMetricDto
        Console.WriteLine($"ID: {stepRecord.Id}");
        Console.WriteLine($"Source: {stepRecord.DataOrigin}");
        Console.WriteLine($"Recorded: {stepRecord.Timestamp}");
        
        // Steps-specific data
        Console.WriteLine($"Steps: {stepRecord.Count}");
        
        // Time range data (IHealthTimeRange)
        Console.WriteLine($"Period: {stepRecord.StartTime} to {stepRecord.EndTime}");
        Console.WriteLine($"Duration: {stepRecord.Duration}");
        
        // Type-safe duration checking
        if (stepRecord is IHealthTimeRange timeRange)
        {
            Console.WriteLine($"This measurement lasted {timeRange.Duration.TotalMinutes} minutes");
        }
    }
}

public async Task AnalyzeWeightData()
{
    var timeRange = HealthTimeRange.FromDateTime(DateTime.Today.AddDays(-30), DateTime.Now);
    var weights = await _healthService.GetHealthDataAsync<WeightDto>(timeRange);
    
    foreach (var weightRecord in weights)
    {
        // Instant measurements only have Timestamp
        Console.WriteLine($"Weight: {weightRecord.Value} {weightRecord.Unit}");
        Console.WriteLine($"Measured at: {weightRecord.Timestamp}");
        Console.WriteLine($"Source: {weightRecord.DataOrigin}");
    }
}
```

### 5. Permission Handling

```csharp
public async Task RequestPermissions()
{
    var permissions = new List<HealthPermissionDto>
    {
        new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Read },
        new() { HealthDataType = HealthDataType.Weight, PermissionType = PermissionType.Read },
        new() { HealthDataType = HealthDataType.Height, PermissionType = PermissionType.Read }
    };

    var result = await _healthService.RequestPermissions(permissions);
    
    if (result.IsSuccess)
    {
        Console.WriteLine("Permissions granted!");
    }
    else
    {
        Console.WriteLine($"Permission error: {result.Error}");
    }
}
```

## DTO Architecture

### Base Classes and Interfaces

All health metric DTOs inherit from [`BaseHealthMetricDto`](src/Maui.Health/Models/Metrics/BaseHealthMetricDto.cs):

```csharp
public abstract class BaseHealthMetricDto
{
    public required string Id { get; init; }
    public required string DataOrigin { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? RecordingMethod { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
```

Duration-based metrics also implement [`IHealthTimeRange`](src/Maui.Health/Models/Metrics/IHealthTimeRange.cs):

```csharp
public interface IHealthTimeRange
{
    DateTimeOffset StartTime { get; }
    DateTimeOffset EndTime { get; }
    TimeSpan Duration => EndTime - StartTime;
}
```

### Metric Categories

**Duration-Based Metrics** (implement `IHealthTimeRange`):
- Steps - counted over time periods
- Exercise sessions - have start/end times
- Sleep sessions - duration-based

**Instant Metrics** (timestamp only):
- Weight - measured at specific moment
- Height - measured at specific moment  
- Blood pressure - instant reading
- Heart rate - point-in-time measurement

## Testing Tips

**iOS Simulator/Device:**
- If no health data exists, open the Health app
- Navigate to the desired metric (e.g., Steps)
- Tap "Add Data" in the top-right corner
- Manually add test data for development

**Android Emulator:**
- Install Google Health Connect app
- Add sample health data for testing
- Ensure proper permissions are granted

## Credits
- @aritchie - `https://github.com/shinyorg/Health`
- @0xc3u - `https://github.com/0xc3u/Plugin.Maui.Health`
- @EagleDelux - `https://github.com/EagleDelux/androidx.health-connect-demo-.net-maui`
- @b099l3 - `https://github.com/b099l3/ios-samples/tree/65a4ab1606cfd8beb518731075e4af526c4da4ad/ios8/Fit/Fit`

## Other Sources
- https://pub.dev/packages/health
- [Android Health Connect Documentation](https://developer.android.com/health-and-fitness/guides/health-connect)
- [iOS HealthKit Documentation](https://developer.apple.com/documentation/healthkit)
