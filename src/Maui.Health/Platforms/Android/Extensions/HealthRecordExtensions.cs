using AndroidX.Health.Connect.Client.Records;
using Maui.Health.Models.Metrics;
using System.Diagnostics;
using Android.Runtime;
using AndroidX.Health.Connect.Client.Changes;
using Maui.Health.Models;
using StepsRecord = AndroidX.Health.Connect.Client.Records.StepsRecord;
using WeightRecord = AndroidX.Health.Connect.Client.Records.WeightRecord;
using HeightRecord = AndroidX.Health.Connect.Client.Records.HeightRecord;
using ActiveCaloriesBurnedRecord = AndroidX.Health.Connect.Client.Records.ActiveCaloriesBurnedRecord;
using HeartRateRecord = AndroidX.Health.Connect.Client.Records.HeartRateRecord;
using ExerciseSessionRecord = AndroidX.Health.Connect.Client.Records.ExerciseSessionRecord;
using Maui.Health.Platforms.Android.Enums;
using Device = AndroidX.Health.Connect.Client.Records.Metadata.Device;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class HealthRecordExtensions
{
    public static TDto? ConvertToDto<TDto>(this Java.Lang.Object record)
        where TDto : HealthMetricBase
    {
        if (record is UpsertionChange change)
        {
            record = (Java.Lang.Object)change.Record;
        }

        return typeof(TDto).Name switch
        {
            nameof(StepsDto) => record.ToStepsDto() as TDto,
            nameof(WeightDto) => record.ToWeightDto() as TDto,
            nameof(HeightDto) => record.ToHeightDto() as TDto,
            nameof(ActiveCaloriesBurnedDto) => record.ToActiveCaloriesBurnedDto() as TDto,
            nameof(HeartRateDto) => record.ToHeartRateDto() as TDto,
            nameof(WorkoutDto) => record.ToWorkoutDto() as TDto,
            nameof(BodyFatDto) => record.ToBodyFatDto() as TDto,
            nameof(Vo2MaxDto) => record.ToVo2MaxDto() as TDto,
            //nameof(BloodPressureDto) => record.ToBloodPressureDto() as TDto,
            _ => null
        };
    }

    public static StepsDto? ToStepsDto(this Java.Lang.Object record)
    {
        StepsRecord? stepsRecord =
            record as StepsRecord ??
            record.TryJavaCast<StepsRecord>();

        if (stepsRecord is null)
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
            EndTime = endTime,
            RecordingMethod = GetRecordingMethod(stepsRecord.Metadata.RecordingMethod),
            DeviceDetails = CreateDeviceDetails(stepsRecord.Metadata.Device)
        };
    }

    public static WeightDto? ToWeightDto(this Java.Lang.Object record)
    {
        var weightRecord =
            record as WeightRecord ??
            record.TryJavaCast<WeightRecord>();

        if (weightRecord is null)
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
            Unit = "kg",
            RecordingMethod = GetRecordingMethod(weightRecord.Metadata.RecordingMethod),
            DeviceDetails = CreateDeviceDetails(weightRecord.Metadata.Device)
        };
    }

    public static HeightDto? ToHeightDto(this Java.Lang.Object record)
    {
        var heightRecord =
            record as HeightRecord ??
            record.TryJavaCast<HeightRecord>();

        if (heightRecord is null)
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
            Unit = "cm",
            RecordingMethod = GetRecordingMethod(heightRecord.Metadata.RecordingMethod),
            DeviceDetails = CreateDeviceDetails(heightRecord.Metadata.Device)
        };
    }

    public static ActiveCaloriesBurnedDto? ToActiveCaloriesBurnedDto(this Java.Lang.Object record)
    {
        var caloriesRecord =
            record as ActiveCaloriesBurnedRecord ??
            record.TryJavaCast<ActiveCaloriesBurnedRecord>();

        if (caloriesRecord is null)
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
            Unit = "kcal",
            StartTime = startTime,
            EndTime = endTime,
            RecordingMethod = GetRecordingMethod(caloriesRecord.Metadata.RecordingMethod),
            DeviceDetails = CreateDeviceDetails(caloriesRecord.Metadata.Device)
        };
    }

    public static HeartRateDto? ToHeartRateDto(this Java.Lang.Object record)
    {
        var heartRateRecord =
            record as HeartRateRecord ??
            record.TryJavaCast<HeartRateRecord>();

        if (heartRateRecord is null)
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
            Unit = "BPM",
            RecordingMethod = GetRecordingMethod(heartRateRecord.Metadata.RecordingMethod),
            DeviceDetails = CreateDeviceDetails(heartRateRecord.Metadata.Device)
        };
    }

    public static WorkoutDto? ToWorkoutDto(this Java.Lang.Object record)
    {
        var exerciseRecord =
            record as ExerciseSessionRecord ??
            record.TryJavaCast<ExerciseSessionRecord>();

        if (exerciseRecord is null)
        {
            return null;
        }

        var startTime = exerciseRecord.StartTime.ToDateTimeOffset();
        var endTime = exerciseRecord.EndTime.ToDateTimeOffset();
        var activityType = ((ExerciseType)exerciseRecord.ExerciseType).ToActivityType();
        string? title = exerciseRecord.Title;

        return new WorkoutDto
        {
            Id = exerciseRecord.Metadata.Id,
            DataOrigin = exerciseRecord.Metadata.DataOrigin.PackageName,
            Timestamp = startTime,
            ActivityType = activityType,
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            RecordingMethod = GetRecordingMethod(exerciseRecord.Metadata.RecordingMethod),
            DeviceDetails = CreateDeviceDetails(exerciseRecord.Metadata.Device)
        };
    }

    public static async Task<WorkoutDto> ToWorkoutDtoAsync(
        this ExerciseSessionRecord exerciseRecord,
        Func<HealthTimeRange, CancellationToken, Task<HeartRateDto[]>> queryHeartRateFunc,
        CancellationToken cancellationToken)
    {
        var startTime = exerciseRecord.StartTime.ToDateTimeOffset();
        var endTime = exerciseRecord.EndTime.ToDateTimeOffset();
        var activityType = ((ExerciseType)exerciseRecord.ExerciseType).ToActivityType();
        string? title = exerciseRecord.Title;

        double? avgHeartRate = null;
        double? minHeartRate = null;
        double? maxHeartRate = null;

        try
        {
            Debug.WriteLine($"Android: Querying HR for workout {startTime:HH:mm} to {endTime:HH:mm}");
            var workoutTimeRange = HealthTimeRange.FromDateTimeOffset(startTime, endTime);
            var heartRateData = await queryHeartRateFunc(workoutTimeRange, cancellationToken);
            Debug.WriteLine($"Android: Found {heartRateData.Length} HR samples for workout");

            if (heartRateData.Any())
            {
                avgHeartRate = heartRateData.Average(hr => hr.BeatsPerMinute);
                minHeartRate = heartRateData.Min(hr => hr.BeatsPerMinute);
                maxHeartRate = heartRateData.Max(hr => hr.BeatsPerMinute);
                Debug.WriteLine($"Android: Workout HR - Avg: {avgHeartRate:F0}, Min: {minHeartRate:F0}, Max: {maxHeartRate:F0}");
            }
            else
            {
                Debug.WriteLine($"Android: No HR data found for workout period");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Android: Error fetching heart rate for workout: {ex.Message}");
            Debug.WriteLine($"Android: Stack trace: {ex.StackTrace}");
        }

        return new WorkoutDto
        {
            Id = exerciseRecord.Metadata.Id,
            DataOrigin = exerciseRecord.Metadata.DataOrigin.PackageName,
            Timestamp = startTime,
            ActivityType = activityType,
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            AverageHeartRate = avgHeartRate,
            MinHeartRate = minHeartRate,
            MaxHeartRate = maxHeartRate,
            RecordingMethod = GetRecordingMethod(exerciseRecord.Metadata.RecordingMethod),
            DeviceDetails = CreateDeviceDetails(exerciseRecord.Metadata.Device)
        };
    }

    public static BodyFatDto? ToBodyFatDto(this Java.Lang.Object record)
    {
        var bodyFatRecord =
            record as BodyFatRecord ??
            record.TryJavaCast<BodyFatRecord>();

        if (bodyFatRecord is null)
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
            Unit = "%",
            RecordingMethod = GetRecordingMethod(bodyFatRecord.Metadata.RecordingMethod),
            DeviceDetails = CreateDeviceDetails(bodyFatRecord.Metadata.Device)
        };
    }

    public static Vo2MaxDto? ToVo2MaxDto(this Java.Lang.Object record)
    {
        var vo2MaxRecord =
            record as Vo2MaxRecord ??
            record.TryJavaCast<Vo2MaxRecord>();

        if (vo2MaxRecord is null)
        {
            return null;
        }

        var timestamp = vo2MaxRecord.Time.ToDateTimeOffset();
        var value = vo2MaxRecord.ExtractVo2MaxValue();

        return new Vo2MaxDto
        {
            Id = vo2MaxRecord.Metadata.Id,
            DataOrigin = vo2MaxRecord.Metadata.DataOrigin.PackageName,
            Timestamp = timestamp,
            Value = value,
            Unit = "ml/kg/min",
            RecordingMethod = GetRecordingMethod(vo2MaxRecord.Metadata.RecordingMethod),
            DeviceDetails = CreateDeviceDetails(vo2MaxRecord.Metadata.Device)
        };
    }

    private static string? GetRecordingMethod(int metadataRecordingMethod)
    {
        var recordingMethod = "Unknown";
        if (Enum.IsDefined(typeof(global::Android.Health.Connect.DataTypes.HealthRecordingMethod), metadataRecordingMethod))
        {
            var healthRecordingMethod = (global::Android.Health.Connect.DataTypes.HealthRecordingMethod)metadataRecordingMethod;
            recordingMethod = healthRecordingMethod.ToString();
        }

        return recordingMethod;
    }

    private static DeviceDetails? CreateDeviceDetails(Device? metadataDevice)
    {
        if (metadataDevice == null)
        {
            return null;
        }

        var deviceType = "Unknown";
        if (Enum.IsDefined(typeof(global::Android.Health.Connect.DataTypes.HealthDeviceType), metadataDevice.Type))
        {
            var healthDeviceType = (global::Android.Health.Connect.DataTypes.HealthDeviceType)metadataDevice.Type;
            deviceType = healthDeviceType.ToString();
        }

        return new DeviceDetails
        {
            DeviceType = deviceType,
            Manufacturer = metadataDevice.Manufacturer,
            Model = metadataDevice.Model
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

    #region Value Extraction Helpers

    public static double ExtractMassValue(this Java.Lang.Object mass)
    {
        try
        {
            Debug.WriteLine($"Mass object type: {mass.GetType().Name}");
            Debug.WriteLine($"Mass object class: {mass.Class.Name}");

            if (mass.TryOfficialUnitsApi("KILOGRAMS", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            if (mass.TryGetPropertyValue("value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1;
            }

            if (mass.TryGetPropertyValue("inKilograms", out double value2))
            {
                Debug.WriteLine($"Found value via 'inKilograms' property: {value2}");
                return value2;
            }

            if (mass.TryCallMethod("inKilograms", out double value3))
            {
                Debug.WriteLine($"Found value via 'inKilograms()' method: {value3}");
                return value3;
            }

            if (mass.TryCallMethod("getValue", out double value4))
            {
                Debug.WriteLine($"Found value via 'getValue()' method: {value4}");
                return value4;
            }

            var stringValue = mass.ToString();
            Debug.WriteLine($"Mass toString(): {stringValue}");

            if (stringValue.TryParseFromString(out double value5))
            {
                Debug.WriteLine($"Found value via string parsing: {value5}");
                return value5;
            }

            Debug.WriteLine("All approaches failed for Mass extraction");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting mass value: {ex}");
        }

        return 70.0; // Default fallback value
    }

    public static double ExtractLengthValue(this Java.Lang.Object length)
    {
        try
        {
            Debug.WriteLine($"Length object type: {length.GetType().Name}");
            Debug.WriteLine($"Length object class: {length.Class.Name}");

            if (length.TryOfficialUnitsApi("METERS", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue * 100; // Convert meters to cm
            }

            if (length.TryGetPropertyValue("value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1 * 100;
            }

            if (length.TryGetPropertyValue("inMeters", out double value2))
            {
                Debug.WriteLine($"Found value via 'inMeters' property: {value2}");
                return value2 * 100;
            }

            if (length.TryCallMethod("inMeters", out double value3))
            {
                Debug.WriteLine($"Found value via 'inMeters()' method: {value3}");
                return value3 * 100;
            }

            if (length.TryCallMethod("getValue", out double value4))
            {
                Debug.WriteLine($"Found value via 'getValue()' method: {value4}");
                return value4 * 100;
            }

            var stringValue = length.ToString();
            Debug.WriteLine($"Length toString(): {stringValue}");

            if (stringValue.TryParseFromString(out double value5))
            {
                Debug.WriteLine($"Found value via string parsing: {value5}");
                return value5 * 100;
            }

            Debug.WriteLine("All approaches failed for Length extraction");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting length value: {ex}");
        }

        return 175.0; // Default fallback value in cm
    }

    public static double ExtractEnergyValue(this Java.Lang.Object energy)
    {
        try
        {
            Debug.WriteLine($"Energy object type: {energy.GetType().Name}");
            Debug.WriteLine($"Energy object class: {energy.Class.Name}");

            if (energy.TryOfficialUnitsApi("KILOCALORIES", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            if (energy.TryGetPropertyValue("value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1;
            }

            if (energy.TryGetPropertyValue("inKilocalories", out double value2))
            {
                Debug.WriteLine($"Found value via 'inKilocalories' property: {value2}");
                return value2;
            }

            if (energy.TryCallMethod("inKilocalories", out double value3))
            {
                Debug.WriteLine($"Found value via 'inKilocalories()' method: {value3}");
                return value3;
            }

            if (energy.TryCallMethod("getValue", out double value4))
            {
                Debug.WriteLine($"Found value via 'getValue()' method: {value4}");
                return value4;
            }

            var stringValue = energy.ToString();
            Debug.WriteLine($"Energy toString(): {stringValue}");

            if (stringValue.TryParseFromString(out double value5))
            {
                Debug.WriteLine($"Found value via string parsing: {value5}");
                return value5;
            }

            Debug.WriteLine("All approaches failed for Energy extraction");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting energy value: {ex}");
        }

        return 0.0; // Default fallback value
    }

    public static double ExtractPercentageValue(this Java.Lang.Object percentage)
    {
        try
        {
            Debug.WriteLine($"Percentage object type: {percentage.GetType().Name}");
            Debug.WriteLine($"Percentage object class: {percentage.Class.Name}");

            if (percentage.TryOfficialUnitsApi("PERCENT", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            if (percentage.TryGetPropertyValue("value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1;
            }

            if (percentage.TryCallMethod("getValue", out double value2))
            {
                Debug.WriteLine($"Found value via 'getValue()' method: {value2}");
                return value2;
            }

            Debug.WriteLine("All approaches failed for Percentage extraction");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting percentage value: {ex}");
        }

        return 0.0; // Default fallback value
    }

    public static double ExtractVo2MaxValue(this Java.Lang.Object vo2Max)
    {
        try
        {
            Debug.WriteLine($"VO2Max object type: {vo2Max.GetType().Name}");
            Debug.WriteLine($"VO2Max object class: {vo2Max.Class.Name}");

            if (vo2Max.TryOfficialUnitsApi("MILLILITERS_PER_MINUTE_KILOGRAM", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            if (vo2Max.TryGetPropertyValue("value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1;
            }

            if (vo2Max.TryCallMethod("getValue", out double value2))
            {
                Debug.WriteLine($"Found value via 'getValue()' method: {value2}");
                return value2;
            }

            Debug.WriteLine("All approaches failed for VO2Max extraction");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting VO2Max value: {ex}");
        }

        return 0.0; // Default fallback value
    }

    public static double ExtractPressureValue(this Java.Lang.Object pressure)
    {
        try
        {
            Debug.WriteLine($"Pressure object type: {pressure.GetType().Name}");
            Debug.WriteLine($"Pressure object class: {pressure.Class.Name}");

            if (pressure.TryOfficialUnitsApi("MILLIMETERS_OF_MERCURY", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            if (pressure.TryGetPropertyValue("inMillimetersOfMercury", out double value1))
            {
                Debug.WriteLine($"Found value via 'inMillimetersOfMercury' property: {value1}");
                return value1;
            }

            if (pressure.TryCallMethod("inMillimetersOfMercury", out double value2))
            {
                Debug.WriteLine($"Found value via 'inMillimetersOfMercury()' method: {value2}");
                return value2;
            }

            if (pressure.TryGetPropertyValue("value", out double value3))
            {
                Debug.WriteLine($"Found value via 'value' property: {value3}");
                return value3;
            }

            if (pressure.TryCallMethod("getValue", out double value4))
            {
                Debug.WriteLine($"Found value via 'getValue()' method: {value4}");
                return value4;
            }

            Debug.WriteLine("All approaches failed for Pressure extraction");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting pressure value: {ex}");
        }

        return 0.0; // Default fallback value
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

            if (inUnitMethod != null)
            {
                Debug.WriteLine($"Found InUnit method: {inUnitMethod.Name}");

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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error trying official Units API: {ex.Message}");
        }
        return false;
    }

    private static bool TryGetUnitConstant(string unitName, out Java.Lang.Object? unitConstant)
    {
        unitConstant = null;
        try
        {
            var unitsNamespace = "AndroidX.Health.Connect.Client.Units";
            var className = unitName.Contains("KILOGRAM") ? "Mass" : "Length";
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting unit constant '{unitName}': {ex.Message}");
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting property '{propertyName}': {ex.Message}");
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error calling method '{methodName}': {ex.Message}");
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

        var numberPattern = @"(\d+\.?\d*)";
        var match = System.Text.RegularExpressions.Regex.Match(stringValue, numberPattern);

        if (match.Success && double.TryParse(match.Groups[1].Value, out value))
        {
            return true;
        }

        return false;
    }

    #endregion

    private static T? TryJavaCast<T>(this Java.Lang.Object obj)
        where T : Java.Lang.Object
    {
        try
        {
            return obj.JavaCast<T>();
        }
        catch (InvalidCastException e)
        {
            return null;
        }
    }
}
