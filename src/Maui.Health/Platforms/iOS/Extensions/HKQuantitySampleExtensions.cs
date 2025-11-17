using HealthKit;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HKQuantitySampleExtensions
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

}
