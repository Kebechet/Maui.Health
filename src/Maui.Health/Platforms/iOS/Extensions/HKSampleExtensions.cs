using Foundation;
using HealthKit;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HKSampleExtensions
{
    public static TDto? ConvertToDto<TDto>(this HKQuantitySample sample, HealthDataType healthDataType)
        where TDto : HealthMetricBase
    {
        return typeof(TDto).Name switch
        {
            nameof(StepsDto) => sample.ToStepsDto() as TDto,
            nameof(WeightDto) => sample.ToWeightDto() as TDto,
            nameof(HeightDto) => sample.ToHeightDto() as TDto,
            nameof(ActiveCaloriesBurnedDto) => sample.ToActiveCaloriesBurnedDto() as TDto,
            nameof(HeartRateDto) => sample.ToHeartRateDto() as TDto,
            nameof(BodyFatDto) => sample.ToBodyFatDto() as TDto,
            nameof(Vo2MaxDto) => sample.ToVo2MaxDto() as TDto,
            _ => null
        };
    }

    public static StepsDto ToStepsDto(this HKQuantitySample sample)
    {
        var value = sample.Quantity.GetDoubleValue(HKUnit.Count);
        var startTime = new DateTimeOffset(sample.StartDate.ToDateTime());
        var endTime = new DateTimeOffset(sample.EndDate.ToDateTime());

        return new StepsDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = startTime, // Use start time as the representative timestamp
            Count = (long)value,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public static WeightDto ToWeightDto(this HKQuantitySample sample)
    {
        var value = sample.Quantity.GetDoubleValue(HKUnit.Gram) / 1000.0; // Convert grams to kg
        var timestamp = new DateTimeOffset(sample.StartDate.ToDateTime());

        return new WeightDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = timestamp,
            Value = value,
            Unit = "kg"
        };
    }

    public static HeightDto ToHeightDto(this HKQuantitySample sample)
    {
        var valueInMeters = sample.Quantity.GetDoubleValue(HKUnit.Meter);
        var timestamp = new DateTimeOffset(sample.StartDate.ToDateTime());

        return new HeightDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = timestamp,
            Value = valueInMeters * 100, // Convert to cm
            Unit = "cm"
        };
    }

    public static ActiveCaloriesBurnedDto ToActiveCaloriesBurnedDto(this HKQuantitySample sample)
    {
        var valueInKilocalories = sample.Quantity.GetDoubleValue(HKUnit.Kilocalorie);
        var startTime = new DateTimeOffset(sample.StartDate.ToDateTime());
        var endTime = new DateTimeOffset(sample.EndDate.ToDateTime());

        return new ActiveCaloriesBurnedDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = startTime, // Use start time as the representative timestamp
            Energy = valueInKilocalories,
            Unit = "kcal",
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public static HeartRateDto ToHeartRateDto(this HKQuantitySample sample)
    {
        var beatsPerMinute = sample.Quantity.GetDoubleValue(HKUnit.Count.UnitDividedBy(HKUnit.Minute));
        var timestamp = new DateTimeOffset(sample.StartDate.ToDateTime());

        return new HeartRateDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = timestamp,
            BeatsPerMinute = beatsPerMinute,
            Unit = "BPM"
        };
    }

    public static BodyFatDto ToBodyFatDto(this HKQuantitySample sample)
    {
        var percentage = sample.Quantity.GetDoubleValue(HKUnit.Percent) * 100; // HKUnit.Percent is 0-1, convert to 0-100
        var timestamp = new DateTimeOffset(sample.StartDate.ToDateTime());

        return new BodyFatDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = timestamp,
            Percentage = percentage,
            Unit = "%"
        };
    }

    public static Vo2MaxDto ToVo2MaxDto(this HKQuantitySample sample)
    {
        var value = sample.Quantity.GetDoubleValue(HKUnit.FromString("ml/kg*min"));
        var timestamp = new DateTimeOffset(sample.StartDate.ToDateTime());

        return new Vo2MaxDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = timestamp,
            Value = value,
            Unit = "ml/kg/min"
        };
    }

    public static async Task<WorkoutDto> ToWorkoutDtoAsync(
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

    public static ActivityType ToActivityType(this HKWorkoutActivityType hkType)
    {
        return hkType switch
        {
            HKWorkoutActivityType.Running => ActivityType.Running,
            HKWorkoutActivityType.Cycling => ActivityType.Cycling,
            HKWorkoutActivityType.Walking => ActivityType.Walking,
            HKWorkoutActivityType.Swimming => ActivityType.Swimming,
            HKWorkoutActivityType.Hiking => ActivityType.Hiking,
            HKWorkoutActivityType.Yoga => ActivityType.Yoga,
            HKWorkoutActivityType.FunctionalStrengthTraining => ActivityType.FunctionalStrengthTraining,
            HKWorkoutActivityType.TraditionalStrengthTraining => ActivityType.TraditionalStrengthTraining,
            HKWorkoutActivityType.Elliptical => ActivityType.Elliptical,
            HKWorkoutActivityType.Rowing => ActivityType.Rowing,
            HKWorkoutActivityType.Pilates => ActivityType.Pilates,
            HKWorkoutActivityType.Dance => ActivityType.Dancing,
            HKWorkoutActivityType.Soccer => ActivityType.Soccer,
            HKWorkoutActivityType.Basketball => ActivityType.Basketball,
            HKWorkoutActivityType.Baseball => ActivityType.Baseball,
            HKWorkoutActivityType.Tennis => ActivityType.Tennis,
            HKWorkoutActivityType.Golf => ActivityType.Golf,
            HKWorkoutActivityType.Badminton => ActivityType.Badminton,
            HKWorkoutActivityType.TableTennis => ActivityType.TableTennis,
            HKWorkoutActivityType.Volleyball => ActivityType.Volleyball,
            HKWorkoutActivityType.Cricket => ActivityType.Cricket,
            HKWorkoutActivityType.Rugby => ActivityType.Rugby,
            HKWorkoutActivityType.AmericanFootball => ActivityType.AmericanFootball,
            HKWorkoutActivityType.DownhillSkiing => ActivityType.Skiing,
            HKWorkoutActivityType.Snowboarding => ActivityType.Snowboarding,
            HKWorkoutActivityType.SurfingSports => ActivityType.Surfing,
            HKWorkoutActivityType.Sailing => ActivityType.Sailing,
            HKWorkoutActivityType.MartialArts => ActivityType.MartialArts,
            HKWorkoutActivityType.Boxing => ActivityType.Boxing,
            HKWorkoutActivityType.Wrestling => ActivityType.Wrestling,
            HKWorkoutActivityType.Climbing => ActivityType.Climbing,
            HKWorkoutActivityType.CrossTraining => ActivityType.CrossTraining,
            HKWorkoutActivityType.StairClimbing => ActivityType.StairClimbing,
            HKWorkoutActivityType.JumpRope => ActivityType.JumpRope,
            HKWorkoutActivityType.Other => ActivityType.Other,
            _ => ActivityType.Unknown
        };
    }
}
