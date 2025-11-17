using HealthKit;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HKWorkoutExtensions
{
    internal static async Task<WorkoutDto> ToWorkoutDtoAsync(
        this HKWorkout workout,
        Func<HealthTimeRange, CancellationToken, Task<HeartRateDto[]>> queryHeartRateFunc,
        CancellationToken cancellationToken)
    {
        var startTime = new DateTimeOffset(workout.StartDate.ToDateTime());
        var endTime = new DateTimeOffset(workout.EndDate.ToDateTime());
        var activityType = workout.WorkoutActivityType.ToActivityType();

        // Extract energy burned
        double? energyBurned = null;
        if (workout.TotalEnergyBurned != null)
        {
            energyBurned = workout.TotalEnergyBurned.GetDoubleValue(HKUnit.Kilocalorie);
        }

        // Extract distance
        double? distance = null;
        if (workout.TotalDistance != null)
        {
            distance = workout.TotalDistance.GetDoubleValue(HKUnit.Meter);
        }

        // Fetch heart rate data during the workout
        double? avgHeartRate = null;
        double? minHeartRate = null;
        double? maxHeartRate = null;

        try
        {
            System.Diagnostics.Debug.WriteLine($"iOS: Querying HR for workout {startTime:HH:mm} to {endTime:HH:mm}");
            var workoutTimeRange = HealthTimeRange.FromDateTimeOffset(startTime, endTime);
            var heartRateData = await queryHeartRateFunc(workoutTimeRange, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"iOS: Found {heartRateData.Length} HR samples for workout");

            if (heartRateData.Any())
            {
                avgHeartRate = heartRateData.Average(hr => hr.BeatsPerMinute);
                minHeartRate = heartRateData.Min(hr => hr.BeatsPerMinute);
                maxHeartRate = heartRateData.Max(hr => hr.BeatsPerMinute);
                System.Diagnostics.Debug.WriteLine($"iOS: Workout HR - Avg: {avgHeartRate:F0}, Min: {minHeartRate:F0}, Max: {maxHeartRate:F0}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"iOS: No HR data found for workout period");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"iOS: Error fetching heart rate for workout: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"iOS: Stack trace: {ex.StackTrace}");
        }

        return new WorkoutDto
        {
            Id = workout.Uuid.ToString(),
            DataOrigin = workout.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = startTime,
            ActivityType = activityType,
            StartTime = startTime,
            EndTime = endTime,
            EnergyBurned = energyBurned,
            Distance = distance,
            AverageHeartRate = avgHeartRate,
            MinHeartRate = minHeartRate,
            MaxHeartRate = maxHeartRate
        };
    }
}
