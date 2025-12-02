using Android.Runtime;
using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Records.Metadata;
using AndroidX.Health.Connect.Client.Units;
using Java.Time;
using Maui.Health.Constants;
using Maui.Health.Models.Metrics;
using System.Diagnostics;
using static Maui.Health.Platforms.Android.AndroidConstants;
using StepsRecord = AndroidX.Health.Connect.Client.Records.StepsRecord;
using WeightRecord = AndroidX.Health.Connect.Client.Records.WeightRecord;
using HeightRecord = AndroidX.Health.Connect.Client.Records.HeightRecord;
using ActiveCaloriesBurnedRecord = AndroidX.Health.Connect.Client.Records.ActiveCaloriesBurnedRecord;
using HeartRateRecord = AndroidX.Health.Connect.Client.Records.HeartRateRecord;
using ExerciseSessionRecord = AndroidX.Health.Connect.Client.Records.ExerciseSessionRecord;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class HealthRecordExtensions
{
    public static TDto? ConvertToDto<TDto>(this Java.Lang.Object record)
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
                return value4 / UnitConversions.GramsPerKilogram;
            }

            if (mass.TryCallMethod("getValue", out double value5))
            {
                return value5 / UnitConversions.GramsPerKilogram;
            }

            var stringValue = mass.ToString();
            if (stringValue.TryParseFromString(out double value6))
            {
                return value6 / UnitConversions.GramsPerKilogram;
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
                return officialValue * UnitConversions.CentimetersPerMeter;
            }

            if (length.TryGetPropertyValue("value", out double value1))
            {
                return value1 * UnitConversions.CentimetersPerMeter;
            }

            if (length.TryGetPropertyValue("inMeters", out double value2))
            {
                return value2 * UnitConversions.CentimetersPerMeter;
            }

            if (length.TryCallMethod("inMeters", out double value3))
            {
                return value3 * UnitConversions.CentimetersPerMeter;
            }

            if (length.TryCallMethod("getValue", out double value4))
            {
                return value4 * UnitConversions.CentimetersPerMeter;
            }

            var stringValue = length.ToString();
            if (stringValue.TryParseFromString(out double value5))
            {
                return value5 * UnitConversions.CentimetersPerMeter;
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
                return value4 / UnitConversions.CaloriesPerKilocalorie;
            }

            if (energy.TryCallMethod("getValue", out double value5))
            {
                return value5 / UnitConversions.CaloriesPerKilocalorie;
            }

            var stringValue = energy.ToString();
            if (stringValue.TryParseFromString(out double value6))
            {
                return value6 / UnitConversions.CaloriesPerKilocalorie;
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

    private static bool TryOfficialUnitsApi(this Java.Lang.Object obj, string unitName, out double value)
    {
        value = 0;
        try
        {
            var objClass = obj.Class;

            var inUnitMethod = objClass.GetDeclaredMethods()?.FirstOrDefault(m =>
                m.Name.Equals("InUnit", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Equals("inUnit", StringComparison.OrdinalIgnoreCase));

            if(inUnitMethod is null)
            {
                return false;
            }

            if (TryGetUnitConstant(unitName, out Java.Lang.Object? unitConstant))
            {
                inUnitMethod.Accessible = true;
                var result = inUnitMethod.Invoke(obj, unitConstant!);

                if (result is Java.Lang.Double javaDouble)
                {
                    value = javaDouble.DoubleValue();
                    return true;
                }
                if (result is Java.Lang.Float javaFloat)
                {
                    value = javaFloat.DoubleValue();
                    return true;
                }
            }
        }
        catch
        {
            // API call failed
        }

        return false;
    }

    private static bool TryGetUnitConstant(string unitName, out Java.Lang.Object? unitConstant)
    {
        unitConstant = null;
        try
        {
            var unitsNamespace = HealthConnectUnitsNamespace;
            var className = unitName.Contains("KILOGRAM") ? "Mass"
                : unitName.Contains("KILOCALORIE") || unitName.Contains("CALORIE") ? "Energy"
                : "Length";
            var fullClassName = $"{unitsNamespace}.{className}";

            var unitClass = Java.Lang.Class.ForName(fullClassName);
            if (unitClass != null)
            {
                var field = unitClass.GetDeclaredField(unitName);
                if (field != null)
                {
                    field.Accessible = true;
                    unitConstant = field.Get(null);
                    return unitConstant != null;
                }
            }
        }
        catch
        {
            // Failed to get unit constant
        }
        return false;
    }

    private static bool TryGetPropertyValue(this Java.Lang.Object obj, string propertyName, out double value)
    {
        value = 0;
        try
        {
            var objClass = obj.Class;
            var field = objClass.GetDeclaredField(propertyName);
            if (field != null)
            {
                field.Accessible = true;
                var fieldValue = field.Get(obj);

                if (fieldValue is Java.Lang.Double javaDouble)
                {
                    value = javaDouble.DoubleValue();
                    return true;
                }
                if (fieldValue is Java.Lang.Float javaFloat)
                {
                    value = javaFloat.DoubleValue();
                    return true;
                }
                if (fieldValue is Java.Lang.Integer javaInt)
                {
                    value = javaInt.DoubleValue();
                    return true;
                }
            }
        }
        catch
        {
            // Property access failed
        }
        return false;
    }

    private static bool TryCallMethod(this Java.Lang.Object obj, string methodName, out double value)
    {
        value = 0;
        try
        {
            var objClass = obj.Class;
            var method = objClass.GetDeclaredMethod(methodName);
            if (method != null)
            {
                method.Accessible = true;
                var result = method.Invoke(obj);

                if (result is Java.Lang.Double javaDouble)
                {
                    value = javaDouble.DoubleValue();
                    return true;
                }
                if (result is Java.Lang.Float javaFloat)
                {
                    value = javaFloat.DoubleValue();
                    return true;
                }
                if (result is Java.Lang.Integer javaInt)
                {
                    value = javaInt.DoubleValue();
                    return true;
                }
            }
        }
        catch
        {
            // Method call failed
        }
        return false;
    }

    private static bool TryParseFromString(this string stringValue, out double value)
    {
        value = 0;

        if (string.IsNullOrEmpty(stringValue))
        {
            return false;
        }

        var numberPattern = Reflection.NumberExtractionPattern;
        var match = System.Text.RegularExpressions.Regex.Match(stringValue, numberPattern);

        if (match.Success && double.TryParse(match.Groups[1].Value, out value))
        {
            return true;
        }

        return false;
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
        var startTime = dto.StartTime.ToJavaInstant();
        var endTime = dto.EndTime.ToJavaInstant();

        var metadata = new Metadata();
        var offset = ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());

        var record = new StepsRecord(
            startTime!,
            offset,
            endTime!,
            offset,
            dto.Count,
            metadata
        );

        return record;
    }

    public static WeightRecord ToWeightRecord(this WeightDto dto)
    {
        var time = dto.Timestamp.ToJavaInstant();

        var metadata = new Metadata();
        var offset = ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());

        // Create Mass from kilograms using Companion factory method via reflection
        var massClass = Java.Lang.Class.ForName(Reflection.MassClassName);
        var companionField = massClass!.GetDeclaredField(KotlinCompanionFieldName);
        companionField!.Accessible = true;
        var companion = companionField.Get(null);

        var kilogramsMethod = companion!.Class!.GetDeclaredMethod(Reflection.KilogramsMethodName, Java.Lang.Double.Type);
        kilogramsMethod!.Accessible = true;
        var massObj = kilogramsMethod.Invoke(companion, new Java.Lang.Double(dto.Value));
        var mass = Java.Lang.Object.GetObject<Mass>(massObj!.Handle, JniHandleOwnership.DoNotTransfer);

        var record = new WeightRecord(
            time!,
            offset,
            mass!,
            metadata
        );

        return record;
    }

    public static HeightRecord ToHeightRecord(this HeightDto dto)
    {
        var time = dto.Timestamp.ToJavaInstant();

        var metadata = new Metadata();
        var offset = ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());

        // Create Length from meters using Companion factory method via reflection
        var lengthClass = Java.Lang.Class.ForName(Reflection.LengthClassName);
        var companionField = lengthClass!.GetDeclaredField(KotlinCompanionFieldName);
        companionField!.Accessible = true;
        var companion = companionField.Get(null);

        var metersMethod = companion!.Class!.GetDeclaredMethod(Reflection.MetersMethodName, Java.Lang.Double.Type);
        metersMethod!.Accessible = true;
        var lengthObj = metersMethod.Invoke(companion, new Java.Lang.Double(dto.Value / UnitConversions.CentimetersPerMeter)); // Convert cm to meters
        var length = Java.Lang.Object.GetObject<Length>(lengthObj!.Handle, JniHandleOwnership.DoNotTransfer);

        var record = new HeightRecord(
            time!,
            offset,
            length!,
            metadata
        );

        return record;
    }

    public static ActiveCaloriesBurnedRecord ToActiveCaloriesBurnedRecord(this ActiveCaloriesBurnedDto dto)
    {
        var startTime = dto.StartTime.ToJavaInstant();
        var endTime = dto.EndTime.ToJavaInstant();

        var metadata = new Metadata();
        var offset = ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());

        // Create Energy from kilocalories using Companion factory method via reflection
        var energyClass = Java.Lang.Class.ForName(Reflection.EnergyClassName);
        var companionField = energyClass!.GetDeclaredField(KotlinCompanionFieldName);
        companionField!.Accessible = true;
        var companion = companionField.Get(null);

        var kilocaloriesMethod = companion!.Class!.GetDeclaredMethod(Reflection.KilocaloriesMethodName, Java.Lang.Double.Type);
        kilocaloriesMethod!.Accessible = true;
        var energyObj = kilocaloriesMethod.Invoke(companion, new Java.Lang.Double(dto.Energy));
        var energy = Java.Lang.Object.GetObject<Energy>(energyObj!.Handle, JniHandleOwnership.DoNotTransfer);

        var record = new ActiveCaloriesBurnedRecord(
            startTime!,
            offset,
            endTime!,
            offset,
            energy!,
            metadata
        );

        return record;
    }

    public static HeartRateRecord ToHeartRateRecord(this HeartRateDto dto)
    {
        var time = dto.Timestamp.ToJavaInstant();

        var metadata = new Metadata();
        var offset = ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());

        var sample = new HeartRateRecord.Sample(time!, (long)dto.BeatsPerMinute);
        var samplesList = new List<HeartRateRecord.Sample> { sample };

        var record = new HeartRateRecord(
            time!,
            offset,
            time!,
            offset,
            samplesList,
            metadata
        );

        return record;
    }

    public static BodyFatRecord ToBodyFatRecord(this BodyFatDto dto)
    {
        var time = dto.Timestamp.ToJavaInstant();

        var metadata = new Metadata();
        var offset = ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());

        // Percentage is a simple value class, create directly
        var percentage = new Percentage(dto.Percentage);

        var record = new BodyFatRecord(
            time!,
            offset,
            percentage,
            metadata
        );

        return record;
    }

    public static Vo2MaxRecord ToVo2MaxRecord(this Vo2MaxDto dto)
    {
        var time = dto.Timestamp.ToJavaInstant();

        var metadata = new Metadata();
        var offset = ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());

        // MeasurementMethod constants: 0 = Other, 1 = Metabolic cart, 2 = Heart rate ratio, 3 = Cooper test, 4 = Multistage fitness test, 5 = Rockport fitness test
        const int measurementMethodOther = 0;

        var record = new Vo2MaxRecord(
            time!,
            offset,
            dto.Value,
            measurementMethodOther,
            metadata
        );

        return record;
    }
}
