#pragma warning disable MH001, MH002, MH003, MH004, MH005, MH006

using Microsoft.AspNetCore.Components;
using Maui.Health.Services;
using Maui.Health.Models.Metrics;
using Maui.Health.Models;
using Maui.Health.Enums;
using Maui.Health.Enums.Errors;

using DuplicateWorkoutGroup = Maui.Health.Models.DuplicateWorkoutGroup;

namespace DemoApp.Components.Pages;

public partial class Home
{
#if ANDROID
    private static readonly HealthDataSdk sdk = HealthDataSdk.GoogleHealthConnect;
#elif IOS
    private static readonly HealthDataSdk sdk = HealthDataSdk.AppleHealthKit;
#else
    private static readonly HealthDataSdk sdk = HealthDataSdk.Unknown;
#endif

    [Inject] public required IHealthService _healthService { get; set; }

    private long _steps { get; set; } = 0;
    private double _weight { get; set; } = 0;
    private double _calories { get; set; } = 0;
    private double _averageHeartRate { get; set; } = 0;
    private int _heartRateCount { get; set; } = 0;
    private double _vo2Max { get; set; } = 0;
    private double _bodyFat { get; set; } = 0;
    private List<WorkoutDto> _workouts { get; set; } = [];
    private string _demoDataMessage { get; set; } = string.Empty;
    private bool _demoDataSuccess { get; set; } = false;

    // Raw records for detail view
    private List<StepsDto> _stepsRecords { get; set; } = [];
    private List<WeightDto> _weightRecords { get; set; } = [];
    private List<ActiveCaloriesBurnedDto> _caloriesRecords { get; set; } = [];
    private List<HeartRateDto> _heartRateRecords { get; set; } = [];
    private List<Vo2MaxDto> _vo2MaxRecords { get; set; } = [];
    private List<BodyFatDto> _bodyFatRecords { get; set; } = [];
    private HashSet<string> _expandedMetrics { get; set; } = [];

    // Tab tracking
    private int _activeTab { get; set; } = 0;

    // Duplicate detection
    private List<DuplicateWorkoutGroup> _duplicateGroups { get; set; } = [];
    private const string AppSource = "DemoApp";
    private int _duplicateThresholdMinutes { get; set; } = 5;

    // Session tracking
    private bool _isSessionRunning { get; set; } = false;
    private bool _isSessionPaused { get; set; } = false;
    private WorkoutSession? _activeSession { get; set; } = null;
    private string _sessionMessage { get; set; } = string.Empty;
    private bool _sessionSuccess { get; set; } = false;
    private string _sessionStatusMessage { get; set; } = string.Empty;

    // SDK status tracking
    private bool _needsRestart { get; set; } = false;
    private bool _wasSdkUnavailable { get; set; } = false;
    private string _sdkErrorMessage { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        // Check initial SDK availability
        _wasSdkUnavailable = !_healthService.IsSupported;

        // Load health data - permissions will be requested automatically if needed
        await LoadHealthDataAsync();

        // Check for active sessions on page load (will restore from preferences if available)
        await CheckSessionStatus();
    }

    private async Task PopulateDemoData()
    {
        try
        {
            _demoDataMessage = "Requesting permissions...";
            _demoDataSuccess = false;
            StateHasChanged();

            // Request write permissions for all data types we'll be writing
            var permissions = new List<HealthPermissionDto>
            {
                new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.Weight, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.ActiveCaloriesBurned, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.HeartRate, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.Vo2Max, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.BodyFat, PermissionType = PermissionType.Write }
            };

            var permissionResult = await _healthService.RequestPermissions(permissions);

            if (!permissionResult.IsSuccess)
            {
                _demoDataMessage = "✗ Permissions denied. Please grant write permissions.";
                _demoDataSuccess = false;
                StateHasChanged();
                return;
            }

            _demoDataMessage = "Writing demo data...";
            StateHasChanged();

            // Use timestamps relative to now (in the past) so Android Health Connect accepts them
            var now = DateTimeOffset.Now;

            // Write Steps data (multiple entries spread over the last 40 minutes)
            var stepsData = new[]
            {
                new StepsDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    Count = 1500,
                    StartTime = now.AddMinutes(-40),
                    EndTime = now.AddMinutes(-30),
                    Timestamp = now.AddMinutes(-40)
                },
                new StepsDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    Count = 2300,
                    StartTime = now.AddMinutes(-30),
                    EndTime = now.AddMinutes(-20),
                    Timestamp = now.AddMinutes(-30)
                },
                new StepsDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    Count = 3200,
                    StartTime = now.AddMinutes(-20),
                    EndTime = now.AddMinutes(-15),
                    Timestamp = now.AddMinutes(-20)
                },
                new StepsDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    Count = 1800,
                    StartTime = now.AddMinutes(-15),
                    EndTime = now.AddMinutes(-10),
                    Timestamp = now.AddMinutes(-15)
                }
            };

            foreach (var step in stepsData)
            {
                await _healthService.WriteHealthData(step);
            }

            // Write Weight data
            var weightData = new WeightDto
            {
                Id = "",
                DataSdk = sdk,
                DataOrigin = "DemoApp",
                Value = 75.5,
                Timestamp = now.AddMinutes(-10),
                Unit = "kg"
            };
            await _healthService.WriteHealthData(weightData);

            // Write Active Calories Burned data
            var caloriesData = new[]
            {
                new ActiveCaloriesBurnedDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    Energy = 120,
                    StartTime = now.AddMinutes(-30),
                    EndTime = now.AddMinutes(-20),
                    Timestamp = now.AddMinutes(-30),
                    Unit = "kcal"
                },
            };

            foreach (var calories in caloriesData)
            {
                await _healthService.WriteHealthData(calories);
            }

            // Write Heart Rate data (spread over the last hour)
            var heartRateData = new[]
            {
                new HeartRateDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    BeatsPerMinute = 125,
                    Timestamp = now.AddMinutes(-55),
                    Unit = "BPM"
                },
                new HeartRateDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    BeatsPerMinute = 138,
                    Timestamp = now.AddMinutes(-45),
                    Unit = "BPM"
                },
                new HeartRateDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    BeatsPerMinute = 145,
                    Timestamp = now.AddMinutes(-35),
                    Unit = "BPM"
                },
                new HeartRateDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    BeatsPerMinute = 142,
                    Timestamp = now.AddMinutes(-25),
                    Unit = "BPM"
                },
                new HeartRateDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    BeatsPerMinute = 135,
                    Timestamp = now.AddMinutes(-15),
                    Unit = "BPM"
                },
                new HeartRateDto
                {
                    Id = "",
                    DataSdk = sdk,
                    DataOrigin = "DemoApp",
                    BeatsPerMinute = 128,
                    Timestamp = now.AddMinutes(-5),
                    Unit = "BPM"
                }
            };

            foreach (var heartRate in heartRateData)
            {
                await _healthService.WriteHealthData(heartRate);
            }

            // Write VO2 Max data
            var vo2MaxData = new Vo2MaxDto
            {
                Id = "",
                DataSdk = sdk,
                DataOrigin = "DemoApp",
                Value = 42.5,
                Timestamp = now.AddMinutes(-10),
                Unit = "ml/kg/min"
            };
            await _healthService.WriteHealthData(vo2MaxData);

            // Write Body Fat data
            var bodyFatData = new BodyFatDto
            {
                Id = "",
                DataSdk = sdk,
                DataOrigin = "DemoApp",
                Percentage = 18.5,
                Timestamp = now.AddMinutes(-10),
                Unit = "%"
            };
            await _healthService.WriteHealthData(bodyFatData);

            // Write a strength training workout
            var strengthTrainingWorkout = new WorkoutDto
            {
                Id = "",
                DataSdk = sdk,
                DataOrigin = "DemoApp",
                ActivityType = ActivityType.StrengthTraining,
                Title = "Strength Training",
                StartTime = now.AddMinutes(-30),
                EndTime = now.AddMinutes(-10),
                Timestamp = now.AddMinutes(-30),
                EnergyBurned = 250,
                Distance = null
            };
            await _healthService.Activity.Write(strengthTrainingWorkout);

            _demoDataMessage = "Demo data written successfully! Refreshing...";
            _demoDataSuccess = true;
            StateHasChanged();

            // Wait a moment for Health Connect to process the writes
            await Task.Delay(500);

            // Reload the data
            await LoadHealthDataAsync();

            _demoDataMessage = "Demo data populated and loaded successfully!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _demoDataMessage = $"Error: {ex.Message}";
            _demoDataSuccess = false;
            StateHasChanged();
        }
    }

    private async Task ClearAllDemoData()
    {
        try
        {
            _demoDataMessage = "Clearing all demo data...";
            _demoDataSuccess = false;
            StateHasChanged();

            var today = DateTime.Today;
            var todayRange = HealthTimeRange.FromDateTime(today, today.AddDays(1));

            foreach (var record in _stepsRecords)
            {
                await _healthService.DeleteHealthData<StepsDto>(record.Id);
            }

            foreach (var record in _weightRecords)
            {
                await _healthService.DeleteHealthData<WeightDto>(record.Id);
            }

            foreach (var record in _caloriesRecords)
            {
                await _healthService.DeleteHealthData<ActiveCaloriesBurnedDto>(record.Id);
            }

            foreach (var record in _heartRateRecords)
            {
                await _healthService.DeleteHealthData<HeartRateDto>(record.Id);
            }

            foreach (var record in _vo2MaxRecords)
            {
                await _healthService.DeleteHealthData<Vo2MaxDto>(record.Id);
            }

            foreach (var record in _bodyFatRecords)
            {
                await _healthService.DeleteHealthData<BodyFatDto>(record.Id);
            }

            foreach (var workout in _workouts)
            {
                await _healthService.Activity.Delete(workout);
            }

            _demoDataMessage = "All demo data cleared! Refreshing...";
            _demoDataSuccess = true;
            StateHasChanged();

            await LoadHealthDataAsync();

            _demoDataMessage = "All demo data cleared successfully!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _demoDataMessage = $"Error clearing: {ex.Message}";
            _demoDataSuccess = false;
            StateHasChanged();
        }
    }

    private async Task LoadHealthDataAsync()
    {
        try
        {
            _sdkErrorMessage = string.Empty;

            if (!_healthService.IsSupported)
            {
                _sdkErrorMessage = "Health tracking is not supported on this device or Health Connect is not installed.";
                StateHasChanged();
                return;
            }

            // If it was previously unavailable but is now supported, we might need a restart on Android 13-
            // (Health Connect doesn't always discover the app immediately after installation while running)
            if (_wasSdkUnavailable)
            {
                _needsRestart = true;
                _wasSdkUnavailable = false; // Only show once per session transition
            }

            // Request read permissions for all health data types
            var permissions = new List<HealthPermissionDto>
            {
                new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.Weight, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.ActiveCaloriesBurned, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.HeartRate, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.Vo2Max, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.BodyFat, PermissionType = PermissionType.Read }
            };

            // Request permissions - if denied, individual reads will fail gracefully
            var permissionResult = await _healthService.RequestPermissions(permissions);

            if (permissionResult.Error == RequestPermissionError.SdkUnavailableProviderUpdateRequired)
            {
                _sdkErrorMessage = "Health Connect needs to be updated to continue.";
                StateHasChanged();
                return;
            }

            var today = DateTime.Today;

            var todayRange = HealthTimeRange.FromDateTime(today, today.AddDays(1));
            // Heart rate: use full day range to capture demo data written relative to now
            var exerciseRange = todayRange;

            // Load data with individual try-catch to continue on permission errors
            try
            {
                var stepsData = await _healthService.GetHealthData<StepsDto>(todayRange);
                _stepsRecords = stepsData;
                _steps = stepsData.Sum(s => s.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading steps: {ex.Message}");
                _steps = 0;
            }

            try
            {
                var weightData = await _healthService.GetHealthData<WeightDto>(todayRange);
                _weightRecords = weightData;
                _weight = weightData.OrderByDescending(w => w.Timestamp).FirstOrDefault()?.Value ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading weight: {ex.Message}");
                _weight = 0;
            }

            try
            {
                var caloriesData = await _healthService.GetHealthData<ActiveCaloriesBurnedDto>(todayRange);
                _caloriesRecords = caloriesData;
                _calories = caloriesData.Sum(c => c.Energy);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading calories: {ex.Message}");
                _calories = 0;
            }

            try
            {
                var heartRateData = await _healthService.GetHealthData<HeartRateDto>(exerciseRange);
                _heartRateRecords = heartRateData;
                if (heartRateData.Count > 0)
                {
                    _averageHeartRate = heartRateData.Average(hr => hr.BeatsPerMinute);
                    _heartRateCount = heartRateData.Count;
                }
                else
                {
                    _averageHeartRate = 0;
                    _heartRateCount = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading heart rate: {ex.Message}");
                _averageHeartRate = 0;
                _heartRateCount = 0;
            }

            try
            {
                var vo2MaxData = await _healthService.GetHealthData<Vo2MaxDto>(todayRange);
                _vo2MaxRecords = vo2MaxData;
                var firstVo2Max = vo2MaxData.OrderByDescending(v => v.Timestamp).FirstOrDefault();
                System.Diagnostics.Debug.WriteLine($"VO2 Max records: {vo2MaxData.Count}, First value: {firstVo2Max?.Value ?? -1}");
                _vo2Max = firstVo2Max?.Value ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading VO2 Max: {ex.Message}");
                _vo2Max = 0;
            }

            try
            {
                var bodyFatData = await _healthService.GetHealthData<BodyFatDto>(todayRange);
                _bodyFatRecords = bodyFatData;
                _bodyFat = bodyFatData.OrderByDescending(b => b.Timestamp).FirstOrDefault()?.Percentage ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading body fat: {ex.Message}");
                _bodyFat = 0;
            }

            // Fetch today's workouts using ActivityService
            try
            {
                _workouts = await _healthService.Activity.Read(todayRange);

                // Detect duplicate workouts using ActivityService
                _duplicateGroups = _healthService.Activity.FindDuplicates(_workouts, AppSource, _duplicateThresholdMinutes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading workouts: {ex.Message}");
                _workouts = [];
                _duplicateGroups = [];
            }

            // Check if there's an active session
            try
            {
                _isSessionRunning = await _healthService.Activity.IsRunning();
                if (_isSessionRunning)
                {
                    _activeSession = await _healthService.Activity.GetActive();
                    _isSessionPaused = await _healthService.Activity.IsPaused();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking session status: {ex.Message}");
                _isSessionRunning = false;
                _isSessionPaused = false;
                _activeSession = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadHealthDataAsync: {ex.Message}");
        }
    }

    private async Task CheckSessionStatus()
    {
        try
        {
            _sessionStatusMessage = "Checking session status...";
            StateHasChanged();

            // Check if there's an active session
            _isSessionRunning = await _healthService.Activity.IsRunning();

            if (_isSessionRunning)
            {
                _activeSession = await _healthService.Activity.GetActive();
                _isSessionPaused = await _healthService.Activity.IsPaused();

                if (_activeSession != null)
                {
                    var duration = (int)(DateTimeOffset.Now - _activeSession.StartTime).TotalMinutes;
                    var statusText = _isSessionPaused ? "paused" : "running";
                    _sessionStatusMessage = $"Active session detected: {_activeSession.ActivityType} {statusText} for {duration} minutes";
                }
                else
                {
                    _sessionStatusMessage = "Active session detected but no workout details available";
                }
            }
            else
            {
                _activeSession = null;
                _isSessionPaused = false;
                _sessionStatusMessage = "No active session detected";
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            _sessionStatusMessage = $"Error checking status: {ex.Message}";
            StateHasChanged();
        }
    }

    private async Task StartWorkoutSession()
    {
        try
        {
            _sessionMessage = "Starting workout session...";
            _sessionSuccess = false;
            StateHasChanged();

            await _healthService.Activity.Start(
                ActivityType.Running,
                "Test of start and stop session",
                "DemoApp"
            );

            // Update session status
            await CheckSessionStatus();

            _sessionMessage = $"✓ Workout session started at {DateTimeOffset.Now:HH:mm:ss}";
            _sessionSuccess = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _sessionMessage = $"Error starting session: {ex.Message}";
            _sessionSuccess = false;
            StateHasChanged();
        }
    }

    private async Task StopWorkoutSession()
    {
        try
        {
            _sessionMessage = "Requesting permissions...";
            _sessionSuccess = false;
            StateHasChanged();

            // Request write permission for ExerciseSession
            var permissions = new List<HealthPermissionDto>
            {
                new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Write }
            };

            var permissionResult = await _healthService.RequestPermissions(permissions);

            if (!permissionResult.IsSuccess)
            {
                _sessionMessage = "✗ Permission denied. Please grant Exercise write permission.";
                _sessionSuccess = false;
                StateHasChanged();
                return;
            }

            _sessionMessage = "Stopping workout session and saving to Health Connect...";
            StateHasChanged();

            // End the active session - returns the completed WorkoutDto
            var completedWorkout = await _healthService.Activity.End();

            if (completedWorkout is null)
            {
                _sessionMessage = "✗ No active session to end.";
                _sessionSuccess = false;
                StateHasChanged();
                return;
            }

            // Write the completed workout to the health store
            await _healthService.Activity.Write(completedWorkout);

            // Update session status
            await CheckSessionStatus();

            _sessionMessage = $"✓ Workout saved to Health Connect! Ended at {DateTimeOffset.Now:HH:mm:ss}. Refreshing...";
            _sessionSuccess = true;
            StateHasChanged();

            // Wait a moment for the health platform to process
            await Task.Delay(1000);

            // Reload the data
            await LoadHealthDataAsync();

            _sessionMessage = "✓ Workout session saved to Health Connect and loaded successfully! Check Health Connect app to verify.";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _sessionMessage = $"✗ Error saving to Health Connect: {ex.Message}. Check permissions in Health Connect app.";
            _sessionSuccess = false;
            StateHasChanged();
        }
    }

    private async Task PauseWorkoutSession()
    {
        try
        {
            _sessionMessage = "Pausing workout session...";
            _sessionSuccess = false;
            StateHasChanged();

            await _healthService.Activity.Pause();

            // Update session status
            await CheckSessionStatus();

            _sessionMessage = $"⏸ Workout paused at {DateTimeOffset.Now:HH:mm:ss}";
            _sessionSuccess = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _sessionMessage = $"Error pausing session: {ex.Message}";
            _sessionSuccess = false;
            StateHasChanged();
        }
    }

    private async Task ResumeWorkoutSession()
    {
        try
        {
            _sessionMessage = "Resuming workout session...";
            _sessionSuccess = false;
            StateHasChanged();

            await _healthService.Activity.Resume();

            // Update session status
            await CheckSessionStatus();

            _sessionMessage = $"▶ Workout resumed at {DateTimeOffset.Now:HH:mm:ss}";
            _sessionSuccess = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _sessionMessage = $"Error resuming session: {ex.Message}";
            _sessionSuccess = false;
            StateHasChanged();
        }
    }

    private async Task WriteManualWorkout()
    {
        try
        {
            _sessionMessage = "Requesting permissions...";
            _sessionSuccess = false;
            StateHasChanged();

            // Request write permission for ExerciseSession
            var permissions = new List<HealthPermissionDto>
            {
                new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Write }
            };

            var permissionResult = await _healthService.RequestPermissions(permissions);

            if (!permissionResult.IsSuccess)
            {
                _sessionMessage = "✗ Permission denied. Please grant Exercise write permission.";
                _sessionSuccess = false;
                StateHasChanged();
                return;
            }

            _sessionMessage = "Writing manual workout to Health Connect...";
            StateHasChanged();

            var now = DateTimeOffset.UtcNow;
            var localOffset = DateTimeOffset.Now.Offset;

            // Create a completed workout (30 minutes ago to 5 minutes ago)
            var workout = new WorkoutDto
            {
                Id = Guid.NewGuid().ToString(),
                DataSdk = sdk,
                DataOrigin = "DemoApp",
                ActivityType = ActivityType.Cycling,
                Title = "Manual Cycling Session",
                StartTime = now.AddMinutes(-30),
                EndTime = now.AddMinutes(-5),
                Timestamp = now.AddMinutes(-30),
                EnergyBurned = 180,
                Distance = 8500 // 8.5 km in meters
            };

            await _healthService.Activity.Write(workout);

            _sessionMessage = "✓ Manual workout saved to Health Connect! Refreshing...";
            _sessionSuccess = true;
            StateHasChanged();

            // Wait a moment for the health platform to process
            await Task.Delay(1000);

            // Reload the data
            await LoadHealthDataAsync();

            _sessionMessage = "✓ Manual workout saved to Health Connect successfully! Check Health Connect app to verify.";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _sessionMessage = $"✗ Error saving to Health Connect: {ex.Message}. Check permissions.";
            _sessionSuccess = false;
            StateHasChanged();
        }
    }

    private async Task DeleteWorkout(WorkoutDto workout)
    {
        try
        {
            _sessionMessage = "Deleting workout...";
            _sessionSuccess = false;
            StateHasChanged();

            await _healthService.Activity.Delete(workout);

            _sessionMessage = "✓ Workout deleted! Refreshing...";
            _sessionSuccess = true;
            StateHasChanged();

            // Wait a moment for the health platform to process
            await Task.Delay(500);

            // Reload the data
            await LoadHealthDataAsync();

            _sessionMessage = "✓ Workout deleted successfully!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _sessionMessage = $"✗ Error deleting workout: {ex.Message}";
            _sessionSuccess = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Checks if a workout is part of a duplicate group
    /// </summary>
    private bool IsDuplicate(WorkoutDto workout)
    {
        return _duplicateGroups.Any(group => group.Workouts.Any(w => w.Id == workout.Id));
    }

    /// <summary>
    /// Gets the duplicate group for a workout, if any
    /// </summary>
    private DuplicateWorkoutGroup? GetDuplicateGroup(WorkoutDto workout)
    {
        return _duplicateGroups.FirstOrDefault(group => group.Workouts.Any(w => w.Id == workout.Id));
    }

    // --- Experimental API properties ---
    private AggregatedResult? _aggregateSteps { get; set; }
    private AggregatedResult? _aggregateCalories { get; set; }
    private bool _isAggregateLoading { get; set; }
    private string _aggregateMessage { get; set; } = string.Empty;
    private bool _aggregateSuccess { get; set; }

    private List<AggregatedResult> _intervalResults { get; set; } = [];
    private bool _isIntervalLoading { get; set; }
    private string _intervalMessage { get; set; } = string.Empty;
    private bool _intervalSuccess { get; set; }

    private const string TypeSteps = nameof(StepsDto);
    private const string TypeWeight = nameof(WeightDto);
    private const string TypeCalories = nameof(ActiveCaloriesBurnedDto);
    private const string TypeHeartRate = nameof(HeartRateDto);
    private const string TypeVo2Max = nameof(Vo2MaxDto);
    private const string TypeBodyFat = nameof(BodyFatDto);

    private string _deleteRecordId { get; set; } = string.Empty;
    private string _deleteRecordType { get; set; } = TypeSteps;
    private string _deleteMessage { get; set; } = string.Empty;
    private bool _deleteSuccess { get; set; }
    private List<StepsDto> _latestRecords { get; set; } = [];
    private bool _isLoadingLatest { get; set; }
    private Dictionary<string, string> _verifyResults { get; set; } = [];

    private string? _changesToken { get; set; }
    private HealthChangesResult? _changesResult { get; set; }
    private string _changesMessage { get; set; } = string.Empty;
    private bool _changesSuccess { get; set; }

    // Permission statuses
    private IList<HealthPermissionStatusResult> _permissionStatuses { get; set; } = [];
    private bool _isPermissionStatusLoading { get; set; }
    private string _permissionStatusMessage { get; set; } = string.Empty;
    private bool _permissionStatusSuccess { get; set; }

    private async Task LoadAggregatedDataAsync()
    {
        try
        {
            _isAggregateLoading = true;
            _aggregateMessage = "";
            StateHasChanged();

            var todayRange = HealthTimeRange.FromDateTime(DateTime.Today, DateTime.Now);

            _aggregateSteps = await _healthService.GetAggregatedHealthData<StepsDto>(todayRange);
            _aggregateCalories = await _healthService.GetAggregatedHealthData<ActiveCaloriesBurnedDto>(todayRange);

            _aggregateMessage = $"Aggregation complete. Steps: {(_aggregateSteps is not null ? "OK" : "no data")}, Calories: {(_aggregateCalories is not null ? "OK" : "no data")}";
            _aggregateSuccess = true;
        }
        catch (Exception ex)
        {
            _aggregateMessage = $"Error: {ex.Message}";
            _aggregateSuccess = false;
        }
        finally
        {
            _isAggregateLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadAggregatedByIntervalAsync()
    {
        try
        {
            _isIntervalLoading = true;
            _intervalMessage = "";
            StateHasChanged();

            var weekRange = HealthTimeRange.FromDateTime(DateTime.Today.AddDays(-6), DateTime.Now);
            _intervalResults = await _healthService.GetAggregatedHealthDataByInterval<StepsDto>(weekRange, TimeSpan.FromDays(1));

            _intervalMessage = $"Found {_intervalResults.Count} daily buckets";
            _intervalSuccess = true;
        }
        catch (Exception ex)
        {
            _intervalMessage = $"Error: {ex.Message}";
            _intervalSuccess = false;
        }
        finally
        {
            _isIntervalLoading = false;
            StateHasChanged();
        }
    }

    private async Task DeleteRecordByIdAsync()
    {
        try
        {
            _deleteMessage = "Deleting...";
            StateHasChanged();

            var isDeleted = _deleteRecordType switch
            {
                nameof(WeightDto) => await _healthService.DeleteHealthData<WeightDto>(_deleteRecordId),
                nameof(ActiveCaloriesBurnedDto) => await _healthService.DeleteHealthData<ActiveCaloriesBurnedDto>(_deleteRecordId),
                nameof(HeartRateDto) => await _healthService.DeleteHealthData<HeartRateDto>(_deleteRecordId),
                nameof(Vo2MaxDto) => await _healthService.DeleteHealthData<Vo2MaxDto>(_deleteRecordId),
                nameof(BodyFatDto) => await _healthService.DeleteHealthData<BodyFatDto>(_deleteRecordId),
                _ => await _healthService.DeleteHealthData<StepsDto>(_deleteRecordId),
            };

            _deleteMessage = isDeleted ? $"Record {_deleteRecordId} deleted successfully" : $"Failed to delete record {_deleteRecordId}";
            _deleteSuccess = isDeleted;
        }
        catch (Exception ex)
        {
            _deleteMessage = $"Error: {ex.Message}";
            _deleteSuccess = false;
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task LoadLatestRecordsAsync()
    {
        try
        {
            _isLoadingLatest = true;
            StateHasChanged();

            var range = new HealthTimeRange
            {
                StartTime = DateTimeOffset.Now.AddDays(-30),
                EndTime = DateTimeOffset.Now
            };
            var records = await _healthService.GetHealthData<StepsDto>(range);

            _latestRecords = records
                .OrderByDescending(r => r.Timestamp)
                .Take(3)
                .ToList();
        }
        catch (Exception ex)
        {
            _deleteMessage = $"Error loading records: {ex.Message}";
            _deleteSuccess = false;
        }
        finally
        {
            _isLoadingLatest = false;
            StateHasChanged();
        }
    }

    private void UseRecordId(string id, string type = "Steps")
    {
        _deleteRecordId = id;
        _deleteRecordType = type;
        StateHasChanged();
    }

    private void UseRecordIdAndSwitch(string id, string type)
    {
        _deleteRecordId = id;
        _deleteRecordType = type;
        _activeTab = 2;
        StateHasChanged();
    }

    private void ToggleMetric(string metric)
    {
        if (!_expandedMetrics.Remove(metric))
            _expandedMetrics.Add(metric);
    }

    private async Task VerifyRecordByIdAsync(string id)
    {
        try
        {
            _verifyResults[id] = "Verifying...";
            StateHasChanged();

            var record = await _healthService.GetHealthRecord<StepsDto>(id);

            _verifyResults[id] = record is not null
                ? $"Found: {record.Count} steps, {record.DataOrigin}"
                : "Not found via GetHealthRecord";
        }
        catch (Exception ex)
        {
            _verifyResults[id] = $"Error: {ex.Message}";
        }
        finally
        {
            StateHasChanged();
        }
    }


    private async Task WriteSyncStepsAsync()
    {
        try
        {

            var now = DateTime.Now;
            var stepsDto = new StepsDto
            {
                Id = "",
                DataSdk = sdk,
                DataOrigin = "DemoApp",
                Count = 10,
                StartTime = now.AddMinutes(-1),
                EndTime = now,
                Timestamp = now
            };

            await _healthService.WriteHealthData(stepsDto);

            var today = DateTime.Today;
            var todayRange = HealthTimeRange.FromDateTime(today, today.AddDays(1));
            var stepsData = await _healthService.GetHealthData<StepsDto>(todayRange, shouldCheckPermissions: false);
            _stepsRecords = stepsData;
            _steps = stepsData.Sum(s => s.Count);

            _changesMessage = "Wrote 10 steps";
            _changesSuccess = true;
        }
        catch (Exception ex)
        {
            _changesMessage = $"Error writing steps: {ex.Message}";
            _changesSuccess = false;
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task GetChangesTokenAsync()
    {
        try
        {
            _changesMessage = "Getting token...";
            StateHasChanged();

            var dataTypes = new List<HealthDataType>
            {
                HealthDataType.Steps,
                HealthDataType.ActiveCaloriesBurned,
                HealthDataType.Weight
            };

            _changesToken = await _healthService.GetChangesToken(dataTypes);

            _changesMessage = _changesToken is not null ? "Token acquired" : "Failed to get token";
            _changesSuccess = _changesToken is not null;
        }
        catch (Exception ex)
        {
            _changesMessage = $"Error: {ex.Message}";
            _changesSuccess = false;
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task LoadPermissionStatusesAsync()
    {
        try
        {
            _isPermissionStatusLoading = true;
            _permissionStatusMessage = "Checking permission statuses...";
            StateHasChanged();

            var permissions = new List<HealthPermissionDto>
            {
                new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.Weight, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.Weight, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.ActiveCaloriesBurned, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.ActiveCaloriesBurned, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.HeartRate, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Write },
            };

            _permissionStatuses = await _healthService.GetPermissionStatuses(permissions);
            _permissionStatusMessage = $"Checked {_permissionStatuses.Count} permissions";
            _permissionStatusSuccess = true;
        }
        catch (Exception ex)
        {
            _permissionStatusMessage = $"Error: {ex.Message}";
            _permissionStatusSuccess = false;
        }
        finally
        {
            _isPermissionStatusLoading = false;
            StateHasChanged();
        }
    }

    private async Task GetChangesAsync()
    {
        if (_changesToken is null)
        {
            return;
        }

        try
        {
            _changesMessage = "Fetching changes...";
            StateHasChanged();

            _changesResult = await _healthService.GetChanges(_changesToken);

            if (_changesResult is not null)
            {
                _changesToken = _changesResult.NextToken;
                _changesMessage = $"Got {_changesResult.Changes.Count} changes";
                _changesSuccess = true;
            }
            else
            {
                _changesMessage = "Token expired or invalid";
                _changesSuccess = false;
            }
        }
        catch (Exception ex)
        {
            _changesMessage = $"Error: {ex.Message}";
            _changesSuccess = false;
        }
        finally
        {
            StateHasChanged();
        }
    }
}
