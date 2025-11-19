using Microsoft.AspNetCore.Components;
using Maui.Health.Services;
using Maui.Health.Models.Metrics;
using Maui.Health.Models;
using Maui.Health.Enums;

namespace DemoApp.Components.Pages;

public partial class Home
{
    [Inject] public required IHealthService _healthService { get; set; }

    private long _steps { get; set; } = 0;
    private double _weight { get; set; } = 0;
    private double _calories { get; set; } = 0;
    private double _averageHeartRate { get; set; } = 0;
    private int _heartRateCount { get; set; } = 0;
    private List<WorkoutDto> _workouts { get; set; } = [];
    private string _demoDataMessage { get; set; } = string.Empty;
    private bool _demoDataSuccess { get; set; } = false;
    private bool _isAndroid { get; set; } = false;
    private bool _isIOS { get; set; } = false;
    private string _iosStrengthTrainingMessage { get; set; } = string.Empty;
    private bool _iosStrengthTrainingSuccess { get; set; } = false;

    // Tab tracking
    private int _activeTab { get; set; } = 0;

    // Session tracking
    private bool _isSessionRunning { get; set; } = false;
    private bool _isSessionPaused { get; set; } = false;
    private WorkoutDto? _activeWorkout { get; set; } = null;
    private string _sessionMessage { get; set; } = string.Empty;
    private bool _sessionSuccess { get; set; } = false;
    private string _sessionStatusMessage { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        // Check if running on Android or iOS
        _isAndroid = DeviceInfo.Platform == DevicePlatform.Android;
        _isIOS = DeviceInfo.Platform == DevicePlatform.iOS;

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
                new() { HealthDataType = HealthDataType.HeartRate, PermissionType = PermissionType.Write }
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

            var today = DateTime.Today;
            var now = DateTime.Now;
            var localOffset = DateTimeOffset.Now.Offset;

            // Write Steps data (multiple entries throughout the day)
            var stepsData = new[]
            {
                new StepsDto { Id = "", DataOrigin = "DemoApp", Count = 1500, StartTime = new DateTimeOffset(today.AddHours(8), localOffset), EndTime = new DateTimeOffset(today.AddHours(9), localOffset), Timestamp = new DateTimeOffset(today.AddHours(8), localOffset) },
                new StepsDto { Id = "", DataOrigin = "DemoApp", Count = 2300, StartTime = new DateTimeOffset(today.AddHours(10), localOffset), EndTime = new DateTimeOffset(today.AddHours(12), localOffset), Timestamp = new DateTimeOffset(today.AddHours(10), localOffset) },
                new StepsDto { Id = "", DataOrigin = "DemoApp", Count = 3200, StartTime = new DateTimeOffset(today.AddHours(14), localOffset), EndTime = new DateTimeOffset(today.AddHours(16), localOffset), Timestamp = new DateTimeOffset(today.AddHours(14), localOffset) },
                new StepsDto { Id = "", DataOrigin = "DemoApp", Count = 1800, StartTime = new DateTimeOffset(today.AddHours(17), localOffset), EndTime = new DateTimeOffset(today.AddHours(18), localOffset), Timestamp = new DateTimeOffset(today.AddHours(17), localOffset) }
            };

            foreach (var step in stepsData)
            {
                await _healthService.WriteHealthData(step);
            }

            // Write Weight data
            var weightData = new WeightDto
            {
                Id = "",
                DataOrigin = "DemoApp",
                Value = 75.5,
                Timestamp = new DateTimeOffset(today.AddHours(7), localOffset),
                Unit = "kg"
            };
            await _healthService.WriteHealthData(weightData);

            // Write Active Calories Burned data (multiple sessions)
            var caloriesData = new[]
            {
                new ActiveCaloriesBurnedDto { Id = "", DataOrigin = "DemoApp", Energy = 120, StartTime = new DateTimeOffset(today.AddHours(8), localOffset), EndTime = new DateTimeOffset(today.AddHours(9), localOffset), Timestamp = new DateTimeOffset(today.AddHours(8), localOffset), Unit = "kcal" },
            };

            foreach (var calories in caloriesData)
            {
                await _healthService.WriteHealthData(calories);
            }

            // Write Heart Rate data during exercise time (14:00-17:00)
            var heartRateData = new[]
            {
                new HeartRateDto { Id = "", DataOrigin = "DemoApp", BeatsPerMinute = 125, Timestamp = new DateTimeOffset(today.AddHours(14).AddMinutes(5), localOffset), Unit = "BPM" },
                new HeartRateDto { Id = "", DataOrigin = "DemoApp", BeatsPerMinute = 138, Timestamp = new DateTimeOffset(today.AddHours(14).AddMinutes(15), localOffset), Unit = "BPM" },
                new HeartRateDto { Id = "", DataOrigin = "DemoApp", BeatsPerMinute = 145, Timestamp = new DateTimeOffset(today.AddHours(14).AddMinutes(25), localOffset), Unit = "BPM" },
                new HeartRateDto { Id = "", DataOrigin = "DemoApp", BeatsPerMinute = 142, Timestamp = new DateTimeOffset(today.AddHours(14).AddMinutes(35), localOffset), Unit = "BPM" },
                new HeartRateDto { Id = "", DataOrigin = "DemoApp", BeatsPerMinute = 135, Timestamp = new DateTimeOffset(today.AddHours(14).AddMinutes(45), localOffset), Unit = "BPM" },
                new HeartRateDto { Id = "", DataOrigin = "DemoApp", BeatsPerMinute = 128, Timestamp = new DateTimeOffset(today.AddHours(14).AddMinutes(55), localOffset), Unit = "BPM" }
            };

            foreach (var heartRate in heartRateData)
            {
                await _healthService.WriteHealthData(heartRate);
            }

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

    private async Task LoadHealthDataAsync()
    {
        try
        {
            // Request read permissions for all health data types
            var permissions = new List<HealthPermissionDto>
            {
                new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.Weight, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.ActiveCaloriesBurned, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.HeartRate, PermissionType = PermissionType.Read },
                new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Read }
            };

            // Request permissions - if denied, individual reads will fail gracefully
            await _healthService.RequestPermissions(permissions);

            var today = DateTime.Today;
            var now = DateTime.Now;

            // Create time ranges
            var todayRange = HealthTimeRange.FromDateTime(today, now);
            var exerciseRange = HealthTimeRange.FromDateTime(today.AddHours(14), today.AddHours(17)); // 14:00 - 17:00

            // Load data with individual try-catch to continue on permission errors
            try
            {
                var stepsData = await _healthService.GetHealthData<StepsDto>(todayRange);
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

            // Fetch today's workouts using ActivityService
            try
            {
                _workouts = await _healthService.Activity.Read(todayRange);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading workouts: {ex.Message}");
                _workouts = [];
            }

            // Check if there's an active session
            try
            {
                _isSessionRunning = await _healthService.Activity.IsRunning();
                if (_isSessionRunning)
                {
                    _activeWorkout = await _healthService.Activity.GetActive(todayRange);
                    _isSessionPaused = await _healthService.Activity.IsPaused();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking session status: {ex.Message}");
                _isSessionRunning = false;
                _isSessionPaused = false;
                _activeWorkout = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadHealthDataAsync: {ex.Message}");
        }
    }

    private async Task CreateIOSStrengthTraining()
    {
        var today = DateTime.Today;
        var now = DateTime.Now;
        var localOffset = DateTimeOffset.Now.Offset;
        // Write Weight data
        var weightData = new WeightDto
        {
            Id = "",
            DataOrigin = "DemoApp",
            Value = 75.5,
            Timestamp = new DateTimeOffset(today.AddHours(7), localOffset),
            Unit = "kg"
        };
        await _healthService.WriteHealthData(weightData);

        try
        {
            _iosStrengthTrainingMessage = "Creating strength training workout...";
            _iosStrengthTrainingSuccess = false;
            StateHasChanged();

            var workoutStart = now.AddHours(-1); // 1 hour ago
            var workoutEnd = now; // Now

            // Create a strength training workout
            var strengthTrainingWorkout = new WorkoutDto
            {
                Id = "",
                DataOrigin = "DemoApp",
                ActivityType = ActivityType.StrengthTraining,
                Title = "Strength Training",
                StartTime = new DateTimeOffset(workoutStart, localOffset),
                EndTime = new DateTimeOffset(workoutEnd, localOffset),
                Timestamp = new DateTimeOffset(workoutStart, localOffset),
                EnergyBurned = 250, // 250 kcal
                Distance = null // No distance for strength training
            };

            // Use ActivityService.Write instead
            await _healthService.Activity.Write(strengthTrainingWorkout);

            _iosStrengthTrainingMessage = "Strength training workout created successfully! Refreshing...";
            _iosStrengthTrainingSuccess = true;
            StateHasChanged();

            // Wait a moment for HealthKit to process
            await Task.Delay(500);

            // Reload the data
            await LoadHealthDataAsync();

            _iosStrengthTrainingMessage = "Strength training workout created and loaded successfully!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _iosStrengthTrainingMessage = $"Error: {ex.Message}";
            _iosStrengthTrainingSuccess = false;
            StateHasChanged();
        }
    }

    private async Task CheckSessionStatus()
    {
        try
        {
            _sessionStatusMessage = "Checking session status...";
            StateHasChanged();

            var todayRange = HealthTimeRange.FromDateTime(DateTime.Today, DateTime.Now);

            // Check if there's an active session
            _isSessionRunning = await _healthService.Activity.IsRunning();

            if (_isSessionRunning)
            {
                _activeWorkout = await _healthService.Activity.GetActive(todayRange);
                _isSessionPaused = await _healthService.Activity.IsPaused();

                if (_activeWorkout != null)
                {
                    var duration = (int)(DateTimeOffset.Now - _activeWorkout.StartTime).TotalMinutes;
                    var statusText = _isSessionPaused ? "paused" : "running";
                    _sessionStatusMessage = $"Active session detected: {_activeWorkout.ActivityType} {statusText} for {duration} minutes";
                }
                else
                {
                    _sessionStatusMessage = "Active session detected but no workout details available";
                }
            }
            else
            {
                _activeWorkout = null;
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

            var now = DateTimeOffset.UtcNow;
            var localOffset = DateTimeOffset.Now.Offset;

            // Create a new workout session (EndTime is null for active sessions)
            var newWorkout = new WorkoutDto
            {
                Id = Guid.NewGuid().ToString(),
                DataOrigin = "DemoApp",
                ActivityType = ActivityType.Running, // Default to running
                Title = "Test of start and stop session",
                StartTime = now,
                EndTime = null, // Active session - no end time yet
                Timestamp = now
            };

            await _healthService.Activity.Start(newWorkout);

            // Update session status
            await CheckSessionStatus();

            _sessionMessage = $"✓ Workout session started at {now.ToLocalTime():HH:mm:ss}";
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

            // End the active session - this will internally convert WorkoutSession to WorkoutDto
            // and write it to the health platform with pause/resume metadata
            await _healthService.Activity.End();

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
}
