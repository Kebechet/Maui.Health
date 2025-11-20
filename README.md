[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/kebechet)

# Maui.Health
![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Maui.Health)
![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Maui.Health)

Abstraction around `Android Health Connect` and `iOS HealthKit` with unified DTO-based API
⚠️ Beware, this package is currently just as **Proof of concept**. There is a lot of work required for proper stability and ease of use.
[Issues](https://github.com/Kebechet/Maui.Health/issues) will contain future tasks that should be implemented.

Feel free to contribute ❤️

## Features
- **Cross-Platform**: Works with Android Health Connect and iOS HealthKit
- **Generic API**: Use `GetHealthDataAsync<TDto>()` for type-safe health data retrieval
- **Unified DTOs**: Platform-agnostic data transfer objects with common properties
- **Time Range Support**: Duration-based metrics implement `IHealthTimeRange` interface
- **Write/delete**: Possibility to write/delete activity to/from Android Health/iOS HealthKit.
- **Duplication detection**: If you write activity under your app to the ios/android health and at same time you start activity on watch/phone natively. You have possibility to detect these workouts and synchronize it as you need.

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
| **Body Fat** | ✅ BodyFatRecord | ✅ BodyFatPercentage | ✅ [`BodyFatDto`](src/Maui.Health/Models/Metrics/BodyFatDto.cs) |
| **Lean Body Mass** | ✅ LeanBodyMassRecord | ✅ LeanBodyMass | ❌ N/A |
| **Hydration** | ✅ HydrationRecord | ✅ DietaryWater | ❌ N/A |
| **VO2 Max** | ✅ Vo2MaxRecord | ✅ VO2Max | ✅ [`Vo2MaxDto`](src/Maui.Health/Models/Metrics/Vo2MaxDto.cs) |
| **Resting Heart Rate** | ✅ RestingHeartRateRecord | ✅ RestingHeartRate | ❌ N/A |
| **Heart Rate Variability** | ✅ HeartRateVariabilityRmssdRecord | ✅ HeartRateVariabilitySdnn | ❌ N/A |
| **Blood Pressure** | ✅ BloodPressureRecord | ✅ Split into Systolic/Diastolic | 🚧 WIP (commented out) |

## Usage

### 1. Registration
Register the health service in your `MauiProgram.cs`:
```csharp
builder.Services.AddHealth();
```

Then setup all [Android and iOS necessities](https://github.com/Kebechet/Maui.Health/commit/139e69fade83f9133044910e47ad530f040b8021).
- Android (4) [docs](https://developer.android.com/jetpack/androidx/releases/health-connect), [docs2](https://learn.microsoft.com/en-us/dotnet/api/healthkit?view=xamarin-ios-sdk-12)
    - in Google Play console give [Health permissions to the app](https://support.google.com/googleplay/android-developer/answer/14738291?hl=en)
    - for successful app approval your Policy page must contain `Health data collection and use`, `Data retention policy`
    - change of `AndroidManifest.xml` + new activity showing [privacy policy](https://developer.android.com/health-and-fitness/guides/health-connect/develop/get-started#show-privacy-policy)
    - change of min. Android version to v26
- iOS (3)  [docs](https://learn.microsoft.com/en-us/previous-versions/xamarin/ios/platform/healthkit), [docs2](https://developer.apple.com/documentation/healthkit)
    - generating new provisioning profile containing HealthKit permissions. These permissions are changed in [Identifiers](https://developer.apple.com/account/resources/identifiers/list)
    - adding `Entitlements.plist`
    - adjustment of `Info.plist`
      -  ⚠️ Beware, if your app already exists and targets various devices adding `UIRequiredDeviceCapabilities` with `healthkit` can get your [release rejected](https://developer.apple.com/library/archive/qa/qa1623/_index.html). For that reason I ommited adding this requirement and I just make sure that I check if the device is capable of using `healthkit`.


After you have everything setup correctly you can use `IHealthService` from DI container and call it's methods.
If you want an example there is a DemoApp project showing number of steps for Current day

### 2. Basic Usage

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

        var steps = await _healthService.GetHealthData<StepsDto>(timeRange);
        return steps.ToList();
    }
}
```

### 3. Working with Time Ranges

Duration-based metrics implement `IHealthTimeRange`:

```csharp
public async Task AnalyzeStepsData()
{
    var timeRange = HealthTimeRange.FromDateTime(DateTime.Today, DateTime.Now);
    var steps = await _healthService.GetHealthData<StepsDto>(timeRange);
    
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

}
```

### 4. Permission Handling

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

### 5. Activity Service (Workout Management)

The `ActivityService` provides workout/exercise session management with support for real-time tracking, pause/resume functionality, and duplicate detection.

#### Reading Workouts

```csharp
public async Task<List<WorkoutDto>> GetTodaysWorkouts()
{
    var timeRange = HealthTimeRange.FromDateTime(DateTime.Today, DateTime.Now);
    var workouts = await _healthService.Activity.Read(timeRange);

    foreach (var workout in workouts)
    {
        Console.WriteLine($"{workout.ActivityType}: {workout.StartTime:HH:mm} - {workout.EndTime:HH:mm}");
        Console.WriteLine($"Duration: {workout.DurationSeconds / 60} minutes");
        Console.WriteLine($"Source: {workout.DataOrigin}");

        if (workout.EnergyBurned.HasValue)
            Console.WriteLine($"Calories: {workout.EnergyBurned:F0} kcal");
        if (workout.AverageHeartRate.HasValue)
            Console.WriteLine($"Avg HR: {workout.AverageHeartRate:F0} BPM");
    }

    return workouts;
}
```

#### Writing Workouts

```csharp
public async Task WriteCompletedWorkout()
{
    var workout = new WorkoutDto
    {
        Id = Guid.NewGuid().ToString(),
        DataOrigin = "MyApp", -> Your APP data source.
        ActivityType = ActivityType.Running,
        Title = "Morning Run",
        StartTime = DateTimeOffset.Now.AddMinutes(-30),
        EndTime = DateTimeOffset.Now,
        EnergyBurned = 250,
        Distance = 5000 // meters
    };

    await _healthService.Activity.Write(workout);
}
```

#### Live Workout Session (Start/Pause/Resume/End)

Track workouts in real-time with pause/resume support:

```csharp
public class WorkoutTracker
{
    private readonly IHealthService _healthService;

    // Start a new workout session
    public async Task StartWorkout()
    {
        await _healthService.Activity.Start(
            ActivityType.Running,
            title: "Morning Run",
            dataOrigin: "MyApp"
        );
    }

    // Pause the active session
    public async Task PauseWorkout()
    {
        await _healthService.Activity.Pause();
    }

    // Resume from pause
    public async Task ResumeWorkout()
    {
        await _healthService.Activity.Resume();
    }

    // End session and save to health store
    public async Task<WorkoutDto?> EndWorkout()
    {
        // Returns the completed workout saved to Health Connect/HealthKit
        return await _healthService.Activity.End();
    }

    // Check session status
    public async Task<bool> IsWorkoutRunning() => await _healthService.Activity.IsRunning();
    public async Task<bool> IsWorkoutPaused() => await _healthService.Activity.IsPaused();
}
```

#### Duplicate Detection

When users track workouts from both your app and a smartwatch, duplicates can occur. The `FindDuplicates` method identifies these by matching:
- Same activity type
- Different data sources (e.g., "MyApp" vs "Apple Watch")
- Start/end times within a configurable threshold

```csharp
public async Task DetectDuplicateWorkouts()
{
    var timeRange = HealthTimeRange.FromDateTime(DateTime.Today, DateTime.Now);
    var workouts = await _healthService.Activity.Read(timeRange);

    // Find duplicates with 5-minute threshold
    var duplicates = _healthService.Activity.FindDuplicates(
        workouts,
        appSource: "MyApp",      // Your app's DataOrigin
        timeThresholdMinutes: 5  // Max time difference to consider as duplicate
    );

    foreach (var group in duplicates)
    {
        // Get the workout from your app
        var appWorkout = group.AppWorkout;

        // Get the workout from watch/other source
        var externalWorkout = group.ExternalWorkout;

        Console.WriteLine($"Duplicate found:");
        Console.WriteLine($"  App: {appWorkout?.DataOrigin} at {appWorkout?.StartTime:HH:mm}");
        Console.WriteLine($"  External: {externalWorkout?.DataOrigin} at {externalWorkout?.StartTime:HH:mm}");
        Console.WriteLine($"  Time diff: {group.StartTimeDifferenceMinutes:F1} minutes");

        // User can decide which to keep - typically keep the watch data
        // as it has more accurate heart rate and calorie data
        if (appWorkout != null)
        {
            await _healthService.Activity.Delete(appWorkout);
        }
    }
}
```

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
