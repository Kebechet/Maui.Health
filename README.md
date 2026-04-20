[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/kebechet)

# Maui.Health
[![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Maui.Health)](https://www.nuget.org/packages/Kebechet.Maui.Health)
![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Maui.Health)
![Last updated (main)](https://img.shields.io/github/last-commit/Kebechet/Maui.Health/main?label=last%20updated)
[![Twitter](https://img.shields.io/twitter/url/https/twitter.com/samuel_sidor.svg?style=social&label=Follow%20samuel_sidor)](https://x.com/samuel_sidor)

Abstraction around `Android Health Connect` and `iOS HealthKit` with unified API

Feel free to contribute ❤️

## Features
- **Cross-Platform**: Works with Android Health Connect and iOS HealthKit
- **Generic API**: Use `GetHealthDataAsync<TDto>()` for type-safe health data retrieval
- **Unified DTOs**: Platform-agnostic data transfer objects with common properties
- **Time Range Support**: Duration-based metrics implement `IHealthTimeRange` interface
- **Write/delete**: Possibility to write/delete any health record or activity to/from Android Health/iOS HealthKit
- **Aggregate**: Platform-native aggregation (sum, average) with cross-source deduplication
- **Aggregate by interval**: Bucketed aggregation (daily, hourly) using native APIs
- **Differential sync**: Track changes (upserts/deletions) since a token for efficient data synchronization
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
    - add `<queries>` element to `AndroidManifest.xml` (inside `<manifest>`, outside `<application>`):
      ```xml
      <queries>
          <package android:name="com.google.android.apps.healthdata" />
      </queries>
      ```
      This is required on Android 11–13 due to [package visibility filtering](https://developer.android.com/training/package-visibility). Without it, `getSdkStatus()` cannot detect Health Connect even when it's installed, causing permission requests to silently fail. On Android 14+ Health Connect is a system service so this isn't strictly needed, but it does no harm.
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
        if (!_healthService.IsSupported)
        {
            return [];
        }

        var timeRange = HealthTimeRange.FromDateTime(DateTime.Today, DateTime.Now);

        return await _healthService.GetHealthData<StepsDto>(timeRange);
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
        // DataOrigin is the stable bundle identifier (iOS) or package name (Android), or null
        // when the platform exposes no source metadata. Safe to compare against your own
        // bundle/package identifier to check ownership.
        Console.WriteLine($"Source: {stepRecord.DataOrigin ?? "<unknown>"}");
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
```

### 4. Read Single Record

Fetch a specific health record by its platform-specific ID (Android: Health Connect metadata ID, iOS: HealthKit UUID):

```csharp
public async Task<StepsDto?> GetSpecificRecord(string recordId)
{
    return await _healthService.GetHealthRecord<StepsDto>(recordId);
}
```

> **Note:** This API is marked `[Experimental("MH001")]`. Suppress the warning with `#pragma warning disable MH001`.

### 5. Delete Health Records

Delete any health record by its platform-specific ID. You can only delete records created by your application:

```csharp
public async Task DeleteRecord(string recordId)
{
    var isDeleted = await _healthService.DeleteHealthData<StepsDto>(recordId);

    if (isDeleted)
    {
        Console.WriteLine("Record deleted successfully");
    }
}
```

> **Note:** This API is marked `[Experimental("MH002")]`. Suppress the warning with `#pragma warning disable MH002`.

### 6. Aggregated Health Data

Get deduplicated totals or averages using platform-native aggregation. This uses Android's `aggregate()` API and iOS's `HKStatisticsQuery`, which properly handle data from multiple health apps (e.g., Samsung Health + Google Fit):

```csharp
public async Task ShowTodaysSummary()
{
    var todayRange = HealthTimeRange.FromDateTime(DateTime.Today, DateTime.Now);

    // Cumulative types (steps, calories) return a sum
    var steps = await _healthService.GetAggregatedHealthData<StepsDto>(todayRange);
    if (steps is not null)
    {
        Console.WriteLine($"Total steps today: {steps.Value}");
    }

    // Discrete types (weight, heart rate) return an average
    var weight = await _healthService.GetAggregatedHealthData<WeightDto>(todayRange);
    if (weight is not null)
    {
        Console.WriteLine($"Average weight: {weight.Value} {weight.Unit}");
    }
}
```

> **Note:** This API is marked `[Experimental("MH003")]`. Suppress the warning with `#pragma warning disable MH003`.

### 7. Aggregated Health Data by Interval

Get aggregated data bucketed by time intervals - ideal for charts and day-by-day views. Uses Android's `aggregateGroupByDuration()` and iOS's `HKStatisticsCollectionQuery`:

```csharp
public async Task ShowWeeklySteps()
{
    var weekRange = HealthTimeRange.FromDateTime(
        DateTime.Today.AddDays(-6), DateTime.Now);

    var dailySteps = await _healthService.GetAggregatedHealthDataByInterval<StepsDto>(
        weekRange, TimeSpan.FromDays(1));

    foreach (var bucket in dailySteps)
    {
        Console.WriteLine($"{bucket.StartTime:ddd MMM dd}: {bucket.Value:N0} steps");
    }
}
```

> **Note:** This API is marked `[Experimental("MH004")]`. Suppress the warning with `#pragma warning disable MH004`.

### 8. Differential Sync (Change Tracking)

Track changes (upserts and deletions) to health data since a given point in time. Uses Android's `getChangesToken()`/`getChanges()` and iOS's `HKAnchoredObjectQuery`. Tokens expire after 30 days.

```csharp
public class HealthSyncService
{
    private readonly IHealthService _healthService;
    private string? _syncToken;

    // Call once to establish a baseline - captures the current state
    public async Task InitializeSync()
    {
        var dataTypes = new List<HealthDataType>
        {
            HealthDataType.Steps,
            HealthDataType.Weight,
            HealthDataType.ActiveCaloriesBurned
        };

        _syncToken = await _healthService.GetChangesToken(dataTypes);
        // Store _syncToken persistently (e.g., Preferences, database)
    }

    // Call periodically to get new changes since last sync
    public async Task SyncChanges()
    {
        if (_syncToken is null) return;

        var result = await _healthService.GetChanges(_syncToken);
        if (result is null) return;

        foreach (var change in result.Changes)
        {
            Console.WriteLine($"{change.Type}: {change.RecordId}");
        }

        // Update token for next call
        _syncToken = result.NextToken;

        // If more changes available, keep fetching
        if (result.HasMore)
        {
            await SyncChanges();
        }
    }
}
```

> **Note:** These APIs are marked `[Experimental("MH005")]` and `[Experimental("MH006")]`. Suppress with `#pragma warning disable MH005, MH006`.

### 9. Permission Handling

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

#### Handling Health Connect Updates (Android)

On Android devices with API < 34, Health Connect is a separate app that may need to be installed or updated. The library returns a specific error so you can show custom UI before opening the Play Store:

```csharp
public async Task RequestPermissionsWithUpdateHandling()
{
    var permissions = new List<HealthPermissionDto>
    {
        new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Read }
    };

    var result = await _healthService.RequestPermissions(permissions);

    if (result.Error == RequestPermissionError.SdkUnavailableProviderUpdateRequired)
    {
        // Show your custom UI explaining the update requirement
        bool userConfirmed = await DisplayAlert(
            "Update Required",
            "Health Connect needs to be updated to use health features.",
            "Update", "Cancel");

        if (userConfirmed)
        {
            _healthService.OpenStorePageOfHealthProvider(); // Opens Play Store
        }
    }
}
```

### 10. Workout Management (IHealthWorkoutService)

The `Activity` property on `IHealthService` provides workout/exercise session management (`IHealthWorkoutService`) with support for real-time tracking, pause/resume functionality, and duplicate detection.

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
        Console.WriteLine($"Source: {workout.DataOrigin ?? "<unknown>"}");

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
        DataOrigin = "com.companyname.MyApp", // Stable bundle identifier (iOS) / package name (Android). Ignored on write — platform stamps its own source.
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
            title: "Morning Run"
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
        dataOrigin: "com.companyname.MyApp",  // Your app's bundle identifier (iOS) / package name (Android)
        timeThresholdMinutes: 5               // Max time difference to consider as duplicate
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

## Comparison with Flutter `health` plugin (gap analysis)

After reviewing [`pub.dev/packages/health`](https://pub.dev/packages/health) and [`carp-dk/carp-health-flutter`](https://github.com/carp-dk/carp-health-flutter), these are the most relevant improvements for `Maui.Health`:

1. **Permission lifecycle parity**
   - Add explicit APIs for checking revoked permissions and guiding users to system settings for manual revocation, plus checking/requesting Android background-read authorization (`READ_HEALTH_DATA_IN_BACKGROUND`), similar to Flutter's permission helpers.
2. **Workout route support**
   - Add iOS/Android workout route APIs (start/append/finish/discard route points) to support GPS route persistence with workouts.
3. **Bulk delete by type + time range**
   - Add a first-class API to delete data by metric type + time window (currently delete is single-record by id).
4. **Broader metric coverage**
   - Prioritize missing DTOs already available cross-platform (for example: blood glucose, hydration, respiratory rate, oxygen saturation, basal metabolic rate, resting heart rate, heart rate variability (HRV)).
5. **Query filters for manual vs automatic entries**
   - Expose recording-method filters on read APIs so callers can include/exclude manually entered data without post-filtering.

These items are intentionally ordered by practical impact and implementation risk (permission/workflow improvements first, then additional DTO breadth).

## Credits
- @aritchie - `https://github.com/shinyorg/Health`
- @0xc3u - `https://github.com/0xc3u/Plugin.Maui.Health`
- @EagleDelux - `https://github.com/EagleDelux/androidx.health-connect-demo-.net-maui`
- @b099l3 - `https://github.com/b099l3/ios-samples/tree/65a4ab1606cfd8beb518731075e4af526c4da4ad/ios8/Fit/Fit`

## Other Sources
- https://pub.dev/packages/health
- [Android Health Connect Documentation](https://developer.android.com/health-and-fitness/guides/health-connect)
- [iOS HealthKit Documentation](https://developer.apple.com/documentation/healthkit)
