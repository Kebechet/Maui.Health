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

    protected override async Task OnInitializedAsync()
    {
        // Check if running on Android or iOS
        _isAndroid = DeviceInfo.Platform == DevicePlatform.Android;
        _isIOS = DeviceInfo.Platform == DevicePlatform.iOS;

        // Request all permissions upfront in a single dialog
        var permissions = new List<HealthPermissionDto>
        {
            new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.Weight, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.ActiveCaloriesBurned, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.HeartRate, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Read }
        };

        var permissionResult = await _healthService.RequestPermissions(permissions);

        if (!permissionResult.IsSuccess)
        {
            // Handle permission denial if needed
            return;
        }

        // Load health data
        await LoadHealthDataAsync();
    }

    private async Task PopulateDemoData()
    {
        try
        {
            _demoDataMessage = "Writing demo data...";
            _demoDataSuccess = false;
            StateHasChanged();

            // Request write permissions
            var writePermissions = new List<HealthPermissionDto>
            {
                new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.Weight, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.ActiveCaloriesBurned, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.HeartRate, PermissionType = PermissionType.Write },
                new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Write }
            };

            var permissionResult = await _healthService.RequestPermissions(writePermissions);
            if (!permissionResult.IsSuccess)
            {
                _demoDataMessage = "Failed to get write permissions";
                StateHasChanged();
                return;
            }

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
                await _healthService.WriteHealthDataAsync(step);
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
            await _healthService.WriteHealthDataAsync(weightData);

            // Write Active Calories Burned data (multiple sessions)
            var caloriesData = new[]
            {
                new ActiveCaloriesBurnedDto { Id = "", DataOrigin = "DemoApp", Energy = 120, StartTime = new DateTimeOffset(today.AddHours(8), localOffset), EndTime = new DateTimeOffset(today.AddHours(9), localOffset), Timestamp = new DateTimeOffset(today.AddHours(8), localOffset), Unit = "kcal" },
            };

            foreach (var calories in caloriesData)
            {
                await _healthService.WriteHealthDataAsync(calories);
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
                await _healthService.WriteHealthDataAsync(heartRate);
            }

            // Write Workout data - Strength Training Session
            var workoutData = new WorkoutDto
            {
                Id = "",
                DataOrigin = "DemoApp",
                ActivityType = ActivityType.StrengthTraining,
                Title = "Strength Training",
                StartTime = new DateTimeOffset(today.AddHours(9), localOffset),
                EndTime = new DateTimeOffset(today.AddHours(10), localOffset),
                Timestamp = new DateTimeOffset(today.AddHours(9), localOffset)
            };
            await _healthService.WriteHealthDataAsync(workoutData);

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
        var today = DateTime.Today;
        var now = DateTime.Now;

        // Create time ranges
        var todayRange = HealthTimeRange.FromDateTime(today, now);
        var exerciseRange = HealthTimeRange.FromDateTime(today.AddHours(14), today.AddHours(17)); // 14:00 - 17:00

        var stepsData = await _healthService.GetHealthDataAsync<StepsDto>(todayRange);
        var weightData = await _healthService.GetHealthDataAsync<WeightDto>(todayRange);
        var caloriesData = await _healthService.GetHealthDataAsync<ActiveCaloriesBurnedDto>(todayRange);
        var heartRateData = await _healthService.GetHealthDataAsync<HeartRateDto>(exerciseRange);

        _steps = stepsData.Sum(s => s.Count);
        _weight = weightData.OrderByDescending(w => w.Timestamp).FirstOrDefault()?.Value ?? 0;
        _calories = caloriesData.Sum(c => c.Energy);

        // Calculate average heart rate during exercise time (14:00 - 17:00)
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

        // Fetch today's workouts
        _workouts = await _healthService.GetHealthDataAsync<WorkoutDto>(todayRange);
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
        await _healthService.WriteHealthDataAsync(weightData);

        try
        {
            _iosStrengthTrainingMessage = "Creating strength training workout...";
            _iosStrengthTrainingSuccess = false;
            StateHasChanged();

            // Request write permission for workouts
            var writePermission = new HealthPermissionDto
            {
                HealthDataType = HealthDataType.ExerciseSession,
                PermissionType = PermissionType.Write
            };

            var permissionResult = await _healthService.RequestPermissions([writePermission]);
            if (!permissionResult.IsSuccess)
            {
                _iosStrengthTrainingMessage = "Failed to get write permission for workouts";
                StateHasChanged();
                return;
            }
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

            var result = await _healthService.WriteHealthDataAsync(strengthTrainingWorkout);

            if (result)
            {
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
            else
            {
                _iosStrengthTrainingMessage = "Failed to create strength training workout";
                _iosStrengthTrainingSuccess = false;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            _iosStrengthTrainingMessage = $"Error: {ex.Message}";
            _iosStrengthTrainingSuccess = false;
            StateHasChanged();
        }
    }
}
