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
    private WorkoutDto[] _workouts { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
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

        // Now fetch the data - permissions are already granted
        var today = DateTime.Today;
        var now = DateTime.Now;

        // Fetch heart rate data for exercise time (14:00 - 17:00)
        var exerciseStart = today.AddHours(14); // 14:00 (2:00 PM)
        var exerciseEnd = today.AddHours(17);   // 17:00 (5:00 PM)

        var stepsData = await _healthService.GetHealthDataAsync<StepsDto>(today, now);
        var weightData = await _healthService.GetHealthDataAsync<WeightDto>(today, now);
        var caloriesData = await _healthService.GetHealthDataAsync<ActiveCaloriesBurnedDto>(today, now);
        var heartRateData = await _healthService.GetHealthDataAsync<HeartRateDto>(exerciseStart, exerciseEnd);

        _steps = stepsData.Sum(s => s.Count);
        _weight = weightData.OrderByDescending(w => w.Timestamp).FirstOrDefault()?.Value ?? 0;
        _calories = caloriesData.Sum(c => c.Energy);

        // Calculate average heart rate during exercise time (14:00 - 17:00)
        if (heartRateData.Length > 0)
        {
            _averageHeartRate = heartRateData.Average(hr => hr.BeatsPerMinute);
            _heartRateCount = heartRateData.Length;
        }

        // Fetch today's workouts
        _workouts = await _healthService.GetHealthDataAsync<WorkoutDto>(today, now);
    }
}
