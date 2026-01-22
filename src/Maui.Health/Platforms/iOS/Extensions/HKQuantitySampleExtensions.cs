using HealthKit;
using Maui.Health.Constants;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using UnitsNet;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HKQuantitySampleExtensions
{
    public static TDto? ToDto<TDto>(this HKQuantitySample sample)
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

    private static DeviceDetail? CreateDeviceDetail(HKDevice? sampleDevice)
    {
        if (sampleDevice is null)
        {
            return null;
        }

        return new DeviceDetail
        {
            Name = sampleDevice.Name,
            Manufacturer = sampleDevice.Manufacturer,
            Model = sampleDevice.Model,
            Id = sampleDevice.UdiDeviceIdentifier,
            HardwareVersion = sampleDevice.HardwareVersion,
            SoftwareVersion = sampleDevice.SoftwareVersion
        };
    }

    public static StepsDto ToStepsDto(this HKQuantitySample sample)
    {
        var value = sample.Quantity.GetDoubleValue(HKUnit.Count);
        var startTime = sample.StartDate.ToDateTimeOffset();
        var endTime = sample.EndDate.ToDateTimeOffset();

        return new StepsDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? DataOrigin.HealthKitOrigin,
            Device = CreateDeviceDetail(sample.Device),
            Timestamp = startTime, // Use start time as the representative timestamp
            Count = (long)value,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public static WeightDto ToWeightDto(this HKQuantitySample sample)
    {
        var valueInGrams = sample.Quantity.GetDoubleValue(HKUnit.Gram);
        var value = Mass.FromGrams(valueInGrams).Kilograms;
        var timestamp = sample.StartDate.ToDateTimeOffset();

        return new WeightDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? DataOrigin.HealthKitOrigin,
            Device = CreateDeviceDetail(sample.Device),
            Timestamp = timestamp,
            Value = value,
            Unit = Units.Kilogram
        };
    }

    public static HeightDto ToHeightDto(this HKQuantitySample sample)
    {
        var valueInMeters = sample.Quantity.GetDoubleValue(HKUnit.Meter);
        var value = Length.FromMeters(valueInMeters).Centimeters;
        var timestamp = sample.StartDate.ToDateTimeOffset();

        return new HeightDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? DataOrigin.HealthKitOrigin,
            Device = CreateDeviceDetail(sample.Device),
            Timestamp = timestamp,
            Value = value,
            Unit = Units.Centimeter
        };
    }

    public static ActiveCaloriesBurnedDto ToActiveCaloriesBurnedDto(this HKQuantitySample sample)
    {
        var valueInKilocalories = sample.Quantity.GetDoubleValue(HKUnit.Kilocalorie);
        var startTime = sample.StartDate.ToDateTimeOffset();
        var endTime = sample.EndDate.ToDateTimeOffset();

        return new ActiveCaloriesBurnedDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? DataOrigin.HealthKitOrigin,
            Device = CreateDeviceDetail(sample.Device),
            Timestamp = startTime, // Use start time as the representative timestamp
            Energy = valueInKilocalories,
            Unit = Units.Kilocalorie,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public static HeartRateDto ToHeartRateDto(this HKQuantitySample sample)
    {
        var beatsPerMinute = sample.Quantity.GetDoubleValue(HKUnit.Count.UnitDividedBy(HKUnit.Minute));
        var timestamp = sample.StartDate.ToDateTimeOffset();

        return new HeartRateDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? DataOrigin.HealthKitOrigin,
            Device = CreateDeviceDetail(sample.Device),
            Timestamp = timestamp,
            BeatsPerMinute = beatsPerMinute,
            Unit = Units.BeatsPerMinute
        };
    }

    public static BodyFatDto ToBodyFatDto(this HKQuantitySample sample)
    {
        var decimalValue = sample.Quantity.GetDoubleValue(HKUnit.Percent); // HKUnit.Percent is 0-1
        var percentage = Ratio.FromDecimalFractions(decimalValue).Percent;
        var timestamp = sample.StartDate.ToDateTimeOffset();

        return new BodyFatDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? DataOrigin.HealthKitOrigin,
            Device = CreateDeviceDetail(sample.Device),
            Timestamp = timestamp,
            Percentage = percentage,
            Unit = Units.Percent
        };
    }

    public static Vo2MaxDto ToVo2MaxDto(this HKQuantitySample sample)
    {
        var value = sample.Quantity.GetDoubleValue(HKUnit.FromString(Units.HKVo2Max));
        var timestamp = sample.StartDate.ToDateTimeOffset();

        return new Vo2MaxDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? DataOrigin.HealthKitOrigin,
            Device = CreateDeviceDetail(sample.Device),
            Timestamp = timestamp,
            Value = value,
            Unit = Units.Vo2Max
        };
    }

    public static HKObject? ToHKObject(this HealthMetricBase dto)
    {
        return dto switch
        {
            StepsDto stepsDto => stepsDto.ToHKQuantitySample(),
            WeightDto weightDto => weightDto.ToHKQuantitySample(),
            HeightDto heightDto => heightDto.ToHKQuantitySample(),
            ActiveCaloriesBurnedDto caloriesDto => caloriesDto.ToHKQuantitySample(),
            HeartRateDto heartRateDto => heartRateDto.ToHKQuantitySample(),
            BodyFatDto bodyFatDto => bodyFatDto.ToHKQuantitySample(),
            Vo2MaxDto vo2MaxDto => vo2MaxDto.ToHKQuantitySample(),
            _ => throw new NotImplementedException($"DTO type {dto.GetType().Name} is not implemented for write operation")
        };
    }

    public static HKQuantitySample ToHKQuantitySample(this StepsDto dto)
    {
        var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount)!;
        var quantity = HKQuantity.FromQuantity(HKUnit.Count, dto.Count);
        var startDate = dto.StartTime.ToNSDate();
        var endDate = dto.EndTime.ToNSDate();

        return HKQuantitySample.FromType(quantityType, quantity, startDate, endDate);
    }

    public static HKQuantitySample ToHKQuantitySample(this WeightDto dto)
    {
        var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.BodyMass)!;
        var valueInGrams = Mass.FromKilograms(dto.Value).Grams;
        var quantity = HKQuantity.FromQuantity(HKUnit.Gram, valueInGrams);
        var date = dto.Timestamp.ToNSDate();

        return HKQuantitySample.FromType(quantityType, quantity, date, date);
    }

    public static HKQuantitySample ToHKQuantitySample(this HeightDto dto)
    {
        var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.Height)!;
        var valueInMeters = Length.FromCentimeters(dto.Value).Meters;
        var quantity = HKQuantity.FromQuantity(HKUnit.Meter, valueInMeters);
        var date = dto.Timestamp.ToNSDate();

        return HKQuantitySample.FromType(quantityType, quantity, date, date);
    }

    public static HKQuantitySample ToHKQuantitySample(this ActiveCaloriesBurnedDto dto)
    {
        var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned)!;
        var quantity = HKQuantity.FromQuantity(HKUnit.Kilocalorie, dto.Energy);
        var startDate = dto.StartTime.ToNSDate();
        var endDate = dto.EndTime.ToNSDate();

        return HKQuantitySample.FromType(quantityType, quantity, startDate, endDate);
    }

    public static HKQuantitySample ToHKQuantitySample(this HeartRateDto dto)
    {
        var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!;
        var unit = HKUnit.Count.UnitDividedBy(HKUnit.Minute);
        var quantity = HKQuantity.FromQuantity(unit, dto.BeatsPerMinute);
        var date = dto.Timestamp.ToNSDate();

        return HKQuantitySample.FromType(quantityType, quantity, date, date);
    }

    public static HKQuantitySample ToHKQuantitySample(this BodyFatDto dto)
    {
        var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.BodyFatPercentage)!;
        var quantity = HKQuantity.FromQuantity(HKUnit.Percent, dto.Percentage / 100.0); // HealthKit expects 0-1 range
        var date = dto.Timestamp.ToNSDate();

        return HKQuantitySample.FromType(quantityType, quantity, date, date);
    }

    public static HKQuantitySample ToHKQuantitySample(this Vo2MaxDto dto)
    {
        var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.VO2Max)!;
        var unit = HKUnit.FromString(Units.HKVo2Max);
        var quantity = HKQuantity.FromQuantity(unit, dto.Value);
        var date = dto.Timestamp.ToNSDate();

        return HKQuantitySample.FromType(quantityType, quantity, date, date);
    }
}
