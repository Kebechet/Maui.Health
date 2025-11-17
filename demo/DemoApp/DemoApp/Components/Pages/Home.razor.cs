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
    private double _bodyFat { get; set; } = 0;
    private double _vo2Max { get; set; } = 0;
    private List<WorkoutDto> _workouts { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        // Request all permissions upfront in a single dialog
        var permissions = new List<HealthPermissionDto>
        {
            new() { HealthDataType = HealthDataType.Steps, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.Weight, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.ActiveCaloriesBurned, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.HeartRate, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.ExerciseSession, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.BodyFat, PermissionType = PermissionType.Read },
            new() { HealthDataType = HealthDataType.Vo2Max, PermissionType = PermissionType.Read }
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

        // Create time ranges
        var todayRange = HealthTimeRange.FromDateTime(today, now);
        var exerciseRange = HealthTimeRange.FromDateTime(today.AddHours(14), today.AddHours(17)); // 14:00 - 17:00

        var stepsData = await _healthService.GetHealthDataAsync<StepsDto>(todayRange);
        var weightData = await _healthService.GetHealthDataAsync<WeightDto>(todayRange);
        var caloriesData = await _healthService.GetHealthDataAsync<ActiveCaloriesBurnedDto>(todayRange);
        var heartRateData = await _healthService.GetHealthDataAsync<HeartRateDto>(exerciseRange);
        var bodyFatData = await _healthService.GetHealthDataAsync<BodyFatDto>(todayRange);
        var vo2MaxData = await _healthService.GetHealthDataAsync<Vo2MaxDto>(todayRange);

        _steps = stepsData.Sum(s => s.Count);
        _weight = weightData.OrderByDescending(w => w.Timestamp).FirstOrDefault()?.Value ?? 0;
        _calories = caloriesData.Sum(c => c.Energy);
        _bodyFat = bodyFatData.OrderByDescending(b => b.Timestamp).FirstOrDefault()?.Percentage ?? 0;
        _vo2Max = vo2MaxData.OrderByDescending(v => v.Timestamp).FirstOrDefault()?.Value ?? 0;

        // Calculate average heart rate during exercise time (14:00 - 17:00)
        if (heartRateData.Count > 0)
        {
            _averageHeartRate = heartRateData.Average(hr => hr.BeatsPerMinute);
            _heartRateCount = heartRateData.Count;
        }

        // Fetch today's workouts
        _workouts = await _healthService.GetHealthDataAsync<WorkoutDto>(todayRange);
    }
}
