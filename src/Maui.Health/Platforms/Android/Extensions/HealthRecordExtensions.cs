using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Records.Metadata;
using AndroidX.Health.Connect.Client.Units;
using Maui.Health.Constants;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android.Helpers;
using System.Diagnostics;
using static Maui.Health.Platforms.Android.AndroidConstant;
using StepsRecord = AndroidX.Health.Connect.Client.Records.StepsRecord;
using WeightRecord = AndroidX.Health.Connect.Client.Records.WeightRecord;
using HeightRecord = AndroidX.Health.Connect.Client.Records.HeightRecord;
using ActiveCaloriesBurnedRecord = AndroidX.Health.Connect.Client.Records.ActiveCaloriesBurnedRecord;
using HeartRateRecord = AndroidX.Health.Connect.Client.Records.HeartRateRecord;
using ExerciseSessionRecord = AndroidX.Health.Connect.Client.Records.ExerciseSessionRecord;
using HealthDeviceType = Android.Health.Connect.DataTypes.HealthDeviceType;
using System.Collections;
using Device = AndroidX.Health.Connect.Client.Records.Metadata.Device;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class HealthRecordExtensions
{
    public static List<TDto> ToDtoList<TDto>(this IList records)
        where TDto : HealthMetricBase
    {
        var results = new List<TDto>();

        foreach (var record in records)
        {
            if (record is not Java.Lang.Object javaObject)
            {
                continue;
            }

            var dto = javaObject.ToDto<TDto>();
            if (dto is not null)
            {
                results.Add(dto);
            }
        }

        return results;
    }

    public static TDto? ToDto<TDto>(this Java.Lang.Object record)
        where TDto : HealthMetricBase
    {
        return typeof(TDto).Name switch
        {
            nameof(StepsDto) => record.ToStepsDto() as TDto,
            nameof(WeightDto) => record.ToWeightDto() as TDto,
            nameof(HeightDto) => record.ToHeightDto() as TDto,
            nameof(ActiveCaloriesBurnedDto) => record.ToActiveCaloriesBurnedDto() as TDto,
            nameof(HeartRateDto) => record.ToHeartRateDto() as TDto,
            nameof(BodyFatDto) => record.ToBodyFatDto() as TDto,
            nameof(Vo2MaxDto) => record.ToVo2MaxDto() as TDto,
            //nameof(BloodPressureDto) => record.ToBloodPressureDto() as TDto,
            _ => null
        };
    }

    private static DeviceDetail? CreateDeviceDetail(Device? metadataDevice)
    {
        if (metadataDevice is null)
        {
            return null;
        }

        var deviceType = DataOrigin.Unknown;
        if (Enum.IsDefined(typeof(HealthDeviceType), metadataDevice.Type))
        {
            var healthDeviceType = (HealthDeviceType)metadataDevice.Type;
            deviceType = healthDeviceType.ToString();
        }

        return new DeviceDetail
        {
            DeviceType = deviceType,
            Manufacturer = metadataDevice.Manufacturer,
            Model = metadataDevice.Model
        };
    }

    public static StepsDto? ToStepsDto(this Java.Lang.Object record)
    {
        if (record is not StepsRecord stepsRecord)
        {
            return null;
        }

        var startTime = stepsRecord.StartTime.ToDateTimeOffset();
        var endTime = stepsRecord.EndTime.ToDateTimeOffset();

        return new StepsDto
        {
            Id = stepsRecord.Metadata.Id,
            DataOrigin = stepsRecord.Metadata.DataOrigin.PackageName,
            Device = CreateDeviceDetail(stepsRecord.Metadata.Device),
            Timestamp = startTime,
            Count = stepsRecord.Count,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public static WeightDto? ToWeightDto(this Java.Lang.Object record)
    {
        if (record is not WeightRecord weightRecord)
        {
            return null;
        }

        var timestamp = weightRecord.Time.ToDateTimeOffset();
        var weightValue = weightRecord.Weight.ExtractMassValue();

        return new WeightDto
        {
            Id = weightRecord.Metadata.Id,
            DataOrigin = weightRecord.Metadata.DataOrigin.PackageName,
            Device = CreateDeviceDetail(weightRecord.Metadata.Device),
            Timestamp = timestamp,
            Value = weightValue,
            Unit = Units.Kilogram
        };
    }

    public static HeightDto? ToHeightDto(this Java.Lang.Object record)
    {
        if (record is not HeightRecord heightRecord)
        {
            return null;
        }

        var timestamp = heightRecord.Time.ToDateTimeOffset();
        var heightValue = heightRecord.Height.ExtractLengthValue();

        return new HeightDto
        {
            Id = heightRecord.Metadata.Id,
            DataOrigin = heightRecord.Metadata.DataOrigin.PackageName,
            Device = CreateDeviceDetail(heightRecord.Metadata.Device),
            Timestamp = timestamp,
            Value = heightValue,
            Unit = Units.Centimeter
        };
    }

    public static ActiveCaloriesBurnedDto? ToActiveCaloriesBurnedDto(this Java.Lang.Object record)
    {
        if (record is not ActiveCaloriesBurnedRecord caloriesRecord)
        {
            return null;
        }

        var startTime = caloriesRecord.StartTime.ToDateTimeOffset();
        var endTime = caloriesRecord.EndTime.ToDateTimeOffset();
        var energyValue = caloriesRecord.Energy.ExtractEnergyValue();

        return new ActiveCaloriesBurnedDto
        {
            Id = caloriesRecord.Metadata.Id,
            DataOrigin = caloriesRecord.Metadata.DataOrigin.PackageName,
            Device = CreateDeviceDetail(caloriesRecord.Metadata.Device),
            Timestamp = startTime,
            Energy = energyValue,
            Unit = Units.Kilocalorie,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public static HeartRateDto? ToHeartRateDto(this Java.Lang.Object record)
    {
        if (record is not HeartRateRecord heartRateRecord)
        {
            return null;
        }

        var timestamp = heartRateRecord.StartTime.ToDateTimeOffset();

        var beatsPerMinute = 0.0;
        if (heartRateRecord.Samples.Count > 0)
        {
            var firstSample = heartRateRecord.Samples[0];
            if (firstSample != null)
            {
                beatsPerMinute = firstSample.BeatsPerMinute;
            }
        }

        return new HeartRateDto
        {
            Id = heartRateRecord.Metadata.Id,
            DataOrigin = heartRateRecord.Metadata.DataOrigin.PackageName,
            Device = CreateDeviceDetail(heartRateRecord.Metadata.Device),
            Timestamp = timestamp,
            BeatsPerMinute = beatsPerMinute,
            Unit = Units.BeatsPerMinute
        };
    }


    public static BodyFatDto? ToBodyFatDto(this Java.Lang.Object record)
    {
        if (record is not BodyFatRecord bodyFatRecord)
        {
            return null;
        }

        var timestamp = bodyFatRecord.Time.ToDateTimeOffset();
        var percentage = bodyFatRecord.Percentage.ExtractPercentageValue();

        return new BodyFatDto
        {
            Id = bodyFatRecord.Metadata.Id,
            DataOrigin = bodyFatRecord.Metadata.DataOrigin.PackageName,
            Device = CreateDeviceDetail(bodyFatRecord.Metadata.Device),
            Timestamp = timestamp,
            Percentage = percentage,
            Unit = Units.Percent
        };
    }

    public static Vo2MaxDto? ToVo2MaxDto(this Java.Lang.Object record)
    {
        if (record is not Vo2MaxRecord vo2MaxRecord)
        {
            return null;
        }

        var timestamp = vo2MaxRecord.Time.ToDateTimeOffset();
        // Extract VO2Max value using reflection - try common property names
        var value = ((Java.Lang.Object)vo2MaxRecord).ExtractVo2MaxValue();

        return new Vo2MaxDto
        {
            Id = vo2MaxRecord.Metadata.Id,
            DataOrigin = vo2MaxRecord.Metadata.DataOrigin.PackageName,
            Device = CreateDeviceDetail(vo2MaxRecord.Metadata.Device),
            Timestamp = timestamp,
            Value = value,
            Unit = Units.Vo2Max
        };
    }

    //public static BloodPressureDto? ToBloodPressureDto(this Java.Lang.Object record)
    //{
    //    if (record is not BloodPressureRecord bpRecord)
    //        return null;

    //    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(bpRecord.Time.ToEpochMilli());
    //    var systolic = bpRecord.Systolic.ExtractPressureValue();
    //    var diastolic = bpRecord.Diastolic.ExtractPressureValue();

    //    return new BloodPressureDto
    //    {
    //        Id = bpRecord.Metadata.Id,
    //        DataOrigin = bpRecord.Metadata.DataOrigin.PackageName,
    //        Timestamp = timestamp,
    //        Systolic = systolic,
    //        Diastolic = diastolic,
    //        Unit = "mmHg"
    //    };
    //}

    public static double ExtractMassValue(this Java.Lang.Object mass)
    {
        try
        {
            if (mass.TryOfficialUnitsApi("KILOGRAMS", out double officialValue))
            {
                return officialValue;
            }

            if (mass.TryGetPropertyValue("inKilograms", out double value1))
            {
                return value1;
            }

            if (mass.TryCallMethod("inKilograms", out double value2))
            {
                return value2;
            }

            if (mass.TryCallMethod("getInKilograms", out double value3))
            {
                return value3;
            }

            if (mass.TryGetPropertyValue("value", out double value4))
            {
                return UnitsNet.Mass.FromGrams(value4).Kilograms;
            }

            if (mass.TryCallMethod("getValue", out double value5))
            {
                return UnitsNet.Mass.FromGrams(value5).Kilograms;
            }

            var stringValue = mass.ToString();
            if (stringValue.TryParseFromString(out double value6))
            {
                return UnitsNet.Mass.FromGrams(value6).Kilograms;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting mass value: {ex.Message}");
        }

        return Defaults.FallbackWeightKg;
    }

    public static double ExtractLengthValue(this Java.Lang.Object length)
    {
        try
        {
            if (length.TryOfficialUnitsApi("METERS", out double officialValue))
            {
                return UnitsNet.Length.FromMeters(officialValue).Centimeters;
            }

            if (length.TryGetPropertyValue("value", out double value1))
            {
                return UnitsNet.Length.FromMeters(value1).Centimeters;
            }

            if (length.TryGetPropertyValue("inMeters", out double value2))
            {
                return UnitsNet.Length.FromMeters(value2).Centimeters;
            }

            if (length.TryCallMethod("inMeters", out double value3))
            {
                return UnitsNet.Length.FromMeters(value3).Centimeters;
            }

            if (length.TryCallMethod("getValue", out double value4))
            {
                return UnitsNet.Length.FromMeters(value4).Centimeters;
            }

            var stringValue = length.ToString();
            if (stringValue.TryParseFromString(out double value5))
            {
                return UnitsNet.Length.FromMeters(value5).Centimeters;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting length value: {ex.Message}");
        }

        return Defaults.FallbackHeightCm;
    }

    public static double ExtractEnergyValue(this Java.Lang.Object energy)
    {
        try
        {
            if (energy.TryOfficialUnitsApi("KILOCALORIES", out double officialValue))
            {
                return officialValue;
            }

            if (energy.TryGetPropertyValue("inKilocalories", out double value1))
            {
                return value1;
            }

            if (energy.TryCallMethod("inKilocalories", out double value2))
            {
                return value2;
            }

            if (energy.TryCallMethod("getInKilocalories", out double value3))
            {
                return value3;
            }

            if (energy.TryGetPropertyValue("value", out double value4))
            {
                return UnitsNet.Energy.FromCalories(value4).Kilocalories;
            }

            if (energy.TryCallMethod("getValue", out double value5))
            {
                return UnitsNet.Energy.FromCalories(value5).Kilocalories;
            }

            var stringValue = energy.ToString();
            if (stringValue.TryParseFromString(out double value6))
            {
                return UnitsNet.Energy.FromCalories(value6).Kilocalories;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting energy value: {ex.Message}");
        }

        return Defaults.FallbackValue;
    }

    public static double ExtractPercentageValue(this Java.Lang.Object percentage)
    {
        try
        {
            if (percentage.TryOfficialUnitsApi("PERCENT", out double officialValue))
            {
                return officialValue;
            }

            if (percentage.TryGetPropertyValue("value", out double value1))
            {
                return value1;
            }

            if (percentage.TryCallMethod("getValue", out double value2))
            {
                return value2;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting percentage value: {ex.Message}");
        }

        return Defaults.FallbackValue;
    }

    public static double ExtractVo2MaxValue(this Java.Lang.Object vo2Max)
    {
        try
        {
            // Field name is vo2MillilitersPerMinuteKilogram (not vo2MaxMillilitersPerMinuteKilogram)
            if (vo2Max.TryGetPropertyValue("vo2MillilitersPerMinuteKilogram", out double vo2Value))
            {
                return vo2Value;
            }

            if (vo2Max.TryCallMethod("getVo2MillilitersPerMinuteKilogram", out double vo2Value2))
            {
                return vo2Value2;
            }

            if (vo2Max.TryGetPropertyValue("value", out double value1))
            {
                return value1;
            }

            if (vo2Max.TryCallMethod("getValue", out double value2))
            {
                return value2;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting VO2Max value: {ex.Message}");
        }

        return Defaults.FallbackValue;
    }

    public static double ExtractPressureValue(this Java.Lang.Object pressure)
    {
        try
        {
            if (pressure.TryOfficialUnitsApi("MILLIMETERS_OF_MERCURY", out double officialValue))
            {
                return officialValue;
            }

            if (pressure.TryGetPropertyValue("inMillimetersOfMercury", out double value1))
            {
                return value1;
            }

            if (pressure.TryCallMethod("inMillimetersOfMercury", out double value2))
            {
                return value2;
            }

            if (pressure.TryGetPropertyValue("value", out double value3))
            {
                return value3;
            }

            if (pressure.TryCallMethod("getValue", out double value4))
            {
                return value4;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting pressure value: {ex.Message}");
        }

        return Defaults.FallbackValue;
    }

    public static Java.Lang.Object? ToAndroidRecord(this HealthMetricBase dto)
    {
        return dto switch
        {
            StepsDto stepsDto => stepsDto.ToStepsRecord(),
            WeightDto weightDto => weightDto.ToWeightRecord(),
            HeightDto heightDto => heightDto.ToHeightRecord(),
            ActiveCaloriesBurnedDto caloriesDto => caloriesDto.ToActiveCaloriesBurnedRecord(),
            HeartRateDto heartRateDto => heartRateDto.ToHeartRateRecord(),
            BodyFatDto bodyFatDto => bodyFatDto.ToBodyFatRecord(),
            Vo2MaxDto vo2MaxDto => vo2MaxDto.ToVo2MaxRecord(),
            _ => null
        };
    }

    public static StepsRecord ToStepsRecord(this StepsDto dto)
    {
        var offset = ZoneOffsetExtensions.GetCurrent();

        return new StepsRecord(
            dto.StartTime.ToJavaInstant()!,
            offset,
            dto.EndTime.ToJavaInstant()!,
            offset,
            dto.Count,
            Metadata.ManualEntry()
        );
    }

    public static WeightRecord ToWeightRecord(this WeightDto dto)
    {
        var mass = JavaReflectionHelper.CreateUnitViaCompanion<Mass>(
            Reflection.MassClassName,
            Reflection.KilogramsMethodName,
            dto.Value);

        return new WeightRecord(
            dto.Timestamp.ToJavaInstant()!,
            ZoneOffsetExtensions.GetCurrent(),
            mass!,
            Metadata.ManualEntry()
        );
    }

    public static HeightRecord ToHeightRecord(this HeightDto dto)
    {
        var valueInMeters = UnitsNet.Length.FromCentimeters(dto.Value).Meters;

        var length = JavaReflectionHelper.CreateUnitViaCompanion<Length>(
            Reflection.LengthClassName,
            Reflection.MetersMethodName,
            valueInMeters);

        return new HeightRecord(
            dto.Timestamp.ToJavaInstant()!,
            ZoneOffsetExtensions.GetCurrent(),
            length!,
            Metadata.ManualEntry()
        );
    }

    public static ActiveCaloriesBurnedRecord ToActiveCaloriesBurnedRecord(this ActiveCaloriesBurnedDto dto)
    {
        var offset = ZoneOffsetExtensions.GetCurrent();
        var energy = JavaReflectionHelper.CreateUnitViaCompanion<Energy>(
            Reflection.EnergyClassName,
            Reflection.KilocaloriesMethodName,
            dto.Energy);

        return new ActiveCaloriesBurnedRecord(
            dto.StartTime.ToJavaInstant()!,
            offset,
            dto.EndTime.ToJavaInstant()!,
            offset,
            energy!,
            Metadata.ManualEntry()
        );
    }

    public static HeartRateRecord ToHeartRateRecord(this HeartRateDto dto)
    {
        var time = dto.Timestamp.ToJavaInstant();
        var offset = ZoneOffsetExtensions.GetCurrent();

        return new HeartRateRecord(
            time!,
            offset,
            time!,
            offset,
            [new(time!, (long)dto.BeatsPerMinute)],
            Metadata.ManualEntry()
        );
    }

    public static BodyFatRecord ToBodyFatRecord(this BodyFatDto dto)
    {
        return new BodyFatRecord(
            dto.Timestamp.ToJavaInstant()!,
            ZoneOffsetExtensions.GetCurrent(),
            new Percentage(dto.Percentage),
            Metadata.ManualEntry()
        );
    }

    public static Vo2MaxRecord ToVo2MaxRecord(this Vo2MaxDto dto)
    {
        // MeasurementMethod constants: 0 = Other, 1 = Metabolic cart, 2 = Heart rate ratio, 3 = Cooper test, 4 = Multistage fitness test, 5 = Rockport fitness test
        const int measurementMethodOther = 0;

        return new Vo2MaxRecord(
            dto.Timestamp.ToJavaInstant()!,
            ZoneOffsetExtensions.GetCurrent(),
            Metadata.ManualEntry(),
            dto.Value,
            measurementMethodOther
        );
    }
}
