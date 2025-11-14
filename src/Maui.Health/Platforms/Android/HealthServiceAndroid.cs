using Android.Content;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Time;
using AndroidX.Health.Connect.Client.Units;
using Kotlin.Jvm;
using Java.Time;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using Java.Util;
using Maui.Health.Platforms.Android.Extensions;
using AndroidX.Health.Connect.Client.Response;
using System.Diagnostics;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Models;
using Maui.Health.Enums.Errors;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;
using Maui.Health.Extensions;
using StepsRecord = AndroidX.Health.Connect.Client.Records.StepsRecord;
using WeightRecord = AndroidX.Health.Connect.Client.Records.WeightRecord;
using HeightRecord = AndroidX.Health.Connect.Client.Records.HeightRecord;
using ActiveCaloriesBurnedRecord = AndroidX.Health.Connect.Client.Records.ActiveCaloriesBurnedRecord;
using HeartRateRecord = AndroidX.Health.Connect.Client.Records.HeartRateRecord;
using ExerciseSessionRecord = AndroidX.Health.Connect.Client.Records.ExerciseSessionRecord;
using BodyFatRecord = AndroidX.Health.Connect.Client.Records.BodyFatRecord;
using Vo2MaxRecord = AndroidX.Health.Connect.Client.Records.Vo2MaxRecord;
using BloodPressureRecord = AndroidX.Health.Connect.Client.Records.BloodPressureRecord;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => IsSdkAvailable().IsSuccess;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    public async partial Task<TDto[]> GetHealthDataAsync<TDto>(DateTime from, DateTime to, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        try
        {
            var sdkCheckResult = IsSdkAvailable();
            if (!sdkCheckResult.IsSuccess)
            {
                return [];
            }

            // Request permission for the specific metric
            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var requestPermissionResult = await RequestPermissions([permission], false, cancellationToken);
            if (requestPermissionResult.IsError)
            {
                return [];
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var recordClass = healthDataType.ToKotlinClass();

            var timeRangeFilter = TimeRangeFilter.Between(
                Instant.OfEpochMilli(((DateTimeOffset)from).ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(((DateTimeOffset)to).ToUnixTimeMilliseconds())!
            );

            var request = new ReadRecordsRequest(
                recordClass,
                timeRangeFilter,
                [],
                true,
                1000,
                null
            );

            var response = await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(_healthConnectClient.ReadRecords, request);
            if (response is null)
            {
                return [];
            }

            var results = new List<TDto>();

            // Special handling for WorkoutDto to add heart rate data
            if (typeof(TDto) == typeof(WorkoutDto))
            {
                for (int i = 0; i < response.Records.Count; i++)
                {
                    var record = response.Records[i];
                    if (record is Java.Lang.Object javaObject && record is ExerciseSessionRecord exerciseRecord)
                    {
                        var dto = await ConvertExerciseSessionRecordAsync(exerciseRecord, cancellationToken) as TDto;
                        if (dto is not null)
                        {
                            results.Add(dto);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < response.Records.Count; i++)
                {
                    var record = response.Records[i];
                    if (record is Java.Lang.Object javaObject)
                    {
                        var dto = ConvertToDto<TDto>(javaObject);
                        if (dto is not null)
                        {
                            results.Add(dto);
                        }
                    }
                }
            }

            return results.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching health data: {ex}");
            return [];
        }
    }

    private TDto? ConvertToDto<TDto>(Java.Lang.Object record) where TDto : HealthMetricBase
    {
        return typeof(TDto).Name switch
        {
            nameof(StepsDto) => ConvertStepsRecord(record) as TDto,
            nameof(WeightDto) => ConvertWeightRecord(record) as TDto,
            nameof(HeightDto) => ConvertHeightRecord(record) as TDto,
            nameof(ActiveCaloriesBurnedDto) => ConvertActiveCaloriesBurnedRecord(record) as TDto,
            nameof(HeartRateDto) => ConvertHeartRateRecord(record) as TDto,
            nameof(WorkoutDto) => ConvertExerciseSessionRecord(record) as TDto,
            //nameof(BodyFatDto) => ConvertBodyFatRecord(record) as TDto,
            //nameof(Vo2MaxDto) => ConvertVo2MaxRecord(record) as TDto,
            //nameof(BloodPressureDto) => ConvertBloodPressureRecord(record) as TDto,
            _ => null
        };
    }

    private StepsDto? ConvertStepsRecord(Java.Lang.Object record)
    {
        if (record is not StepsRecord stepsRecord)
            return null;

        var startTime = DateTimeOffset.FromUnixTimeMilliseconds(stepsRecord.StartTime.ToEpochMilli());
        var endTime = DateTimeOffset.FromUnixTimeMilliseconds(stepsRecord.EndTime.ToEpochMilli());

        return new StepsDto
        {
            Id = stepsRecord.Metadata.Id,
            DataOrigin = stepsRecord.Metadata.DataOrigin.PackageName,
            Timestamp = startTime, // Use start time as the representative timestamp
            Count = stepsRecord.Count,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    private WeightDto? ConvertWeightRecord(Java.Lang.Object record)
    {
        if (record is not WeightRecord weightRecord)
            return null;

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(weightRecord.Time.ToEpochMilli());

        // Try multiple approaches to extract the mass value
        var weightValue = ExtractMassValue(weightRecord.Weight);

        return new WeightDto
        {
            Id = weightRecord.Metadata.Id,
            DataOrigin = weightRecord.Metadata.DataOrigin.PackageName,
            Timestamp = timestamp,
            Value = weightValue,
            Unit = "kg"
        };
    }

    private HeightDto? ConvertHeightRecord(Java.Lang.Object record)
    {
        if (record is not HeightRecord heightRecord)
            return null;

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(heightRecord.Time.ToEpochMilli());

        // Try multiple approaches to extract the length value
        var heightValue = ExtractLengthValue(heightRecord.Height);

        return new HeightDto
        {
            Id = heightRecord.Metadata.Id,
            DataOrigin = heightRecord.Metadata.DataOrigin.PackageName,
            Timestamp = timestamp,
            Value = heightValue,
            Unit = "cm"
        };
    }

    private ActiveCaloriesBurnedDto? ConvertActiveCaloriesBurnedRecord(Java.Lang.Object record)
    {
        if (record is not ActiveCaloriesBurnedRecord caloriesRecord)
            return null;

        var startTime = DateTimeOffset.FromUnixTimeMilliseconds(caloriesRecord.StartTime.ToEpochMilli());
        var endTime = DateTimeOffset.FromUnixTimeMilliseconds(caloriesRecord.EndTime.ToEpochMilli());

        // Try to extract the energy value (in kilocalories)
        var energyValue = ExtractEnergyValue(caloriesRecord.Energy);

        return new ActiveCaloriesBurnedDto
        {
            Id = caloriesRecord.Metadata.Id,
            DataOrigin = caloriesRecord.Metadata.DataOrigin.PackageName,
            Timestamp = startTime, // Use start time as the representative timestamp
            Energy = energyValue,
            Unit = "kcal",
            StartTime = startTime,
            EndTime = endTime
        };
    }

    private HeartRateDto? ConvertHeartRateRecord(Java.Lang.Object record)
    {
        if (record is not HeartRateRecord heartRateRecord)
            return null;

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(heartRateRecord.StartTime.ToEpochMilli());

        // Heart rate records contain samples - get the first sample's BPM
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
            Unit = "BPM"
        };
    }

    private WorkoutDto? ConvertExerciseSessionRecord(Java.Lang.Object record)
    {
        if (record is not ExerciseSessionRecord exerciseRecord)
            return null;

        var startTime = DateTimeOffset.FromUnixTimeMilliseconds(exerciseRecord.StartTime.ToEpochMilli());
        var endTime = DateTimeOffset.FromUnixTimeMilliseconds(exerciseRecord.EndTime.ToEpochMilli());

        // Map exercise type
        var activityType = MapAndroidExerciseType(exerciseRecord.ExerciseType);

        // Extract title if available
        string? title = exerciseRecord.Title;

        return new WorkoutDto
        {
            Id = exerciseRecord.Metadata.Id,
            DataOrigin = exerciseRecord.Metadata.DataOrigin.PackageName,
            Timestamp = startTime,
            ActivityType = activityType,
            Title = title,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    //private BodyFatDto? ConvertBodyFatRecord(Java.Lang.Object record)
    //{
    //    if (record is not BodyFatRecord bodyFatRecord)
    //        return null;

    //    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(bodyFatRecord.Time.ToEpochMilli());
    //    var percentage = ExtractPercentageValue(bodyFatRecord.Percentage);

    //    return new BodyFatDto
    //    {
    //        Id = bodyFatRecord.Metadata.Id,
    //        DataOrigin = bodyFatRecord.Metadata.DataOrigin.PackageName,
    //        Timestamp = timestamp,
    //        Percentage = percentage,
    //        Unit = "%"
    //    };
    //}

    //private Vo2MaxDto? ConvertVo2MaxRecord(Java.Lang.Object record)
    //{
    //    if (record is not Vo2MaxRecord vo2MaxRecord)
    //        return null;

    //    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(vo2MaxRecord.Time.ToEpochMilli());
    //    var value = ExtractVo2MaxValue(vo2MaxRecord.Vo2MillilitersPerMinuteKilogram);

    //    return new Vo2MaxDto
    //    {
    //        Id = vo2MaxRecord.Metadata.Id,
    //        DataOrigin = vo2MaxRecord.Metadata.DataOrigin.PackageName,
    //        Timestamp = timestamp,
    //        Value = value,
    //        Unit = "ml/kg/min"
    //    };
    //}

    //private BloodPressureDto? ConvertBloodPressureRecord(Java.Lang.Object record)
    //{
    //    if (record is not BloodPressureRecord bpRecord)
    //        return null;

    //    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(bpRecord.Time.ToEpochMilli());
    //    var systolic = ExtractPressureValue(bpRecord.Systolic);
    //    var diastolic = ExtractPressureValue(bpRecord.Diastolic);

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

    private async Task<WorkoutDto> ConvertExerciseSessionRecordAsync(ExerciseSessionRecord exerciseRecord, CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.FromUnixTimeMilliseconds(exerciseRecord.StartTime.ToEpochMilli());
        var endTime = DateTimeOffset.FromUnixTimeMilliseconds(exerciseRecord.EndTime.ToEpochMilli());

        // Map exercise type
        var activityType = MapAndroidExerciseType(exerciseRecord.ExerciseType);

        // Extract title if available
        string? title = exerciseRecord.Title;

        // Fetch heart rate data during the workout
        double? avgHeartRate = null;
        double? minHeartRate = null;
        double? maxHeartRate = null;

        try
        {
            Debug.WriteLine($"Android: Querying HR for workout {startTime:HH:mm} to {endTime:HH:mm}");
            var heartRateData = await QueryHeartRateRecordsAsync(startTime.UtcDateTime, endTime.UtcDateTime, cancellationToken);
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
            MaxHeartRate = maxHeartRate
        };
    }

    private async Task<HeartRateDto[]> QueryHeartRateRecordsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure DateTime is treated as UTC
            var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);

            var timeRangeFilter = TimeRangeFilter.Between(
                Instant.OfEpochMilli(new DateTimeOffset(fromUtc).ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(new DateTimeOffset(toUtc).ToUnixTimeMilliseconds())!
            );

            var recordClass = JvmClassMappingKt.GetKotlinClass(Java.Lang.Class.FromType(typeof(HeartRateRecord)));

            var request = new ReadRecordsRequest(
                recordClass,
                timeRangeFilter,
                [],
                true,
                1000,
                null
            );

            var response = await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(_healthConnectClient.ReadRecords, request);
            if (response is null)
            {
                return [];
            }

            var results = new List<HeartRateDto>();
            for (int i = 0; i < response.Records.Count; i++)
            {
                var record = response.Records[i];
                if (record is Java.Lang.Object javaObject)
                {
                    var dto = ConvertHeartRateRecord(javaObject);
                    if (dto != null)
                    {
                        results.Add(dto);
                    }
                }
            }

            return results.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error querying heart rate records: {ex}");
            return [];
        }
    }

    private ActivityType MapAndroidExerciseType(int exerciseType)
    {
        // Android Health Connect exercise types
        // Reference: https://developer.android.com/reference/kotlin/androidx/health/connect/client/records/ExerciseSessionRecord
        return exerciseType switch
        {
            7 => ActivityType.Running, // EXERCISE_TYPE_RUNNING
            8 => ActivityType.Cycling, // EXERCISE_TYPE_BIKING
            79 => ActivityType.Walking, // EXERCISE_TYPE_WALKING
            68 => ActivityType.Swimming, // EXERCISE_TYPE_SWIMMING
            36 => ActivityType.Hiking, // EXERCISE_TYPE_HIKING
            81 => ActivityType.Yoga, // EXERCISE_TYPE_YOGA
            28 => ActivityType.FunctionalStrengthTraining, // EXERCISE_TYPE_CALISTHENICS
            71 => ActivityType.TraditionalStrengthTraining, // EXERCISE_TYPE_STRENGTH_TRAINING
            25 => ActivityType.Elliptical, // EXERCISE_TYPE_ELLIPTICAL
            61 => ActivityType.Rowing, // EXERCISE_TYPE_ROWING
            54 => ActivityType.Pilates, // EXERCISE_TYPE_PILATES
            19 => ActivityType.Dancing, // EXERCISE_TYPE_DANCING
            62 => ActivityType.Soccer, // EXERCISE_TYPE_SOCCER
            9 => ActivityType.Basketball, // EXERCISE_TYPE_BASKETBALL
            5 => ActivityType.Baseball, // EXERCISE_TYPE_BASEBALL
            73 => ActivityType.Tennis, // EXERCISE_TYPE_TENNIS
            32 => ActivityType.Golf, // EXERCISE_TYPE_GOLF
            3 => ActivityType.Badminton, // EXERCISE_TYPE_BADMINTON
            72 => ActivityType.TableTennis, // EXERCISE_TYPE_TABLE_TENNIS
            78 => ActivityType.Volleyball, // EXERCISE_TYPE_VOLLEYBALL
            18 => ActivityType.Cricket, // EXERCISE_TYPE_CRICKET
            63 => ActivityType.Rugby, // EXERCISE_TYPE_RUGBY
            1 => ActivityType.AmericanFootball, // EXERCISE_TYPE_FOOTBALL_AMERICAN
            64 => ActivityType.Skiing, // EXERCISE_TYPE_SKIING
            66 => ActivityType.Snowboarding, // EXERCISE_TYPE_SNOWBOARDING
            40 => ActivityType.IceSkating, // EXERCISE_TYPE_ICE_SKATING
            67 => ActivityType.Surfing, // EXERCISE_TYPE_SURFING
            53 => ActivityType.Paddling, // EXERCISE_TYPE_PADDLING
            65 => ActivityType.Sailing, // EXERCISE_TYPE_SAILING
            47 => ActivityType.MartialArts, // EXERCISE_TYPE_MARTIAL_ARTS
            11 => ActivityType.Boxing, // EXERCISE_TYPE_BOXING
            82 => ActivityType.Wrestling, // EXERCISE_TYPE_WRESTLING
            59 => ActivityType.Climbing, // EXERCISE_TYPE_ROCK_CLIMBING
            20 => ActivityType.CrossTraining, // EXERCISE_TYPE_DANCING
            70 => ActivityType.StairClimbing, // EXERCISE_TYPE_STAIR_CLIMBING
            44 => ActivityType.JumpRope, // EXERCISE_TYPE_JUMPING_ROPE
            0 => ActivityType.Other, // EXERCISE_TYPE_OTHER_WORKOUT
            _ => ActivityType.Unknown
        };
    }

    private double ExtractMassValue(Java.Lang.Object mass)
    {
        try
        {
            Debug.WriteLine($"Mass object type: {mass.GetType().Name}");
            Debug.WriteLine($"Mass object class: {mass.Class.Name}");

            // Approach 0: Try official Android Health Connect Units API
            if (TryOfficialUnitsApi(mass, "KILOGRAMS", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            // Approach 1: Try common Kotlin/Java property patterns
            if (TryGetPropertyValue(mass, "value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1;
            }

            if (TryGetPropertyValue(mass, "inKilograms", out double value2))
            {
                Debug.WriteLine($"Found value via 'inKilograms' property: {value2}");
                return value2;
            }

            // Approach 2: Try method calls
            if (TryCallMethod(mass, "inKilograms", out double value3))
            {
                Debug.WriteLine($"Found value via 'inKilograms()' method: {value3}");
                return value3;
            }

            if (TryCallMethod(mass, "getValue", out double value4))
            {
                Debug.WriteLine($"Found value via 'getValue()' method: {value4}");
                return value4;
            }

            // Approach 3: Try toString and parse (as last resort)
            var stringValue = mass.ToString();
            Debug.WriteLine($"Mass toString(): {stringValue}");

            if (TryParseFromString(stringValue, out double value5))
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

    private double ExtractLengthValue(Java.Lang.Object length)
    {
        try
        {
            Debug.WriteLine($"Length object type: {length.GetType().Name}");
            Debug.WriteLine($"Length object class: {length.Class.Name}");

            // Approach 0: Try official Android Health Connect Units API
            if (TryOfficialUnitsApi(length, "METERS", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue * 100; // Convert meters to cm
            }

            // Approach 1: Try common Kotlin/Java property patterns
            if (TryGetPropertyValue(length, "value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1 * 100; // Convert meters to cm
            }

            if (TryGetPropertyValue(length, "inMeters", out double value2))
            {
                Debug.WriteLine($"Found value via 'inMeters' property: {value2}");
                return value2 * 100; // Convert meters to cm
            }

            // Approach 2: Try method calls
            if (TryCallMethod(length, "inMeters", out double value3))
            {
                Debug.WriteLine($"Found value via 'inMeters()' method: {value3}");
                return value3 * 100; // Convert meters to cm
            }

            if (TryCallMethod(length, "getValue", out double value4))
            {
                Debug.WriteLine($"Found value via 'getValue()' method: {value4}");
                return value4 * 100; // Convert meters to cm
            }

            // Approach 3: Try toString and parse (as last resort)
            var stringValue = length.ToString();
            Debug.WriteLine($"Length toString(): {stringValue}");

            if (TryParseFromString(stringValue, out double value5))
            {
                Debug.WriteLine($"Found value via string parsing: {value5}");
                return value5 * 100; // Convert meters to cm
            }

            Debug.WriteLine("All approaches failed for Length extraction");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting length value: {ex}");
        }

        return 175.0; // Default fallback value in cm
    }

    private double ExtractEnergyValue(Java.Lang.Object energy)
    {
        try
        {
            Debug.WriteLine($"Energy object type: {energy.GetType().Name}");
            Debug.WriteLine($"Energy object class: {energy.Class.Name}");

            // Approach 0: Try official Android Health Connect Units API
            if (TryOfficialUnitsApi(energy, "KILOCALORIES", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            // Approach 1: Try common Kotlin/Java property patterns
            if (TryGetPropertyValue(energy, "value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1;
            }

            if (TryGetPropertyValue(energy, "inKilocalories", out double value2))
            {
                Debug.WriteLine($"Found value via 'inKilocalories' property: {value2}");
                return value2;
            }

            // Approach 2: Try method calls
            if (TryCallMethod(energy, "inKilocalories", out double value3))
            {
                Debug.WriteLine($"Found value via 'inKilocalories()' method: {value3}");
                return value3;
            }

            if (TryCallMethod(energy, "getValue", out double value4))
            {
                Debug.WriteLine($"Found value via 'getValue()' method: {value4}");
                return value4;
            }

            // Approach 3: Try toString and parse (as last resort)
            var stringValue = energy.ToString();
            Debug.WriteLine($"Energy toString(): {stringValue}");

            if (TryParseFromString(stringValue, out double value5))
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

    private double ExtractPercentageValue(Java.Lang.Object percentage)
    {
        try
        {
            Debug.WriteLine($"Percentage object type: {percentage.GetType().Name}");
            Debug.WriteLine($"Percentage object class: {percentage.Class.Name}");

            // Try official Units API with "PERCENT"
            if (TryOfficialUnitsApi(percentage, "PERCENT", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            // Try common property patterns
            if (TryGetPropertyValue(percentage, "value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1;
            }

            if (TryCallMethod(percentage, "getValue", out double value2))
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

    private double ExtractVo2MaxValue(Java.Lang.Object vo2Max)
    {
        try
        {
            Debug.WriteLine($"VO2Max object type: {vo2Max.GetType().Name}");
            Debug.WriteLine($"VO2Max object class: {vo2Max.Class.Name}");

            // Try official Units API
            if (TryOfficialUnitsApi(vo2Max, "MILLILITERS_PER_MINUTE_KILOGRAM", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            // Try common property patterns
            if (TryGetPropertyValue(vo2Max, "value", out double value1))
            {
                Debug.WriteLine($"Found value via 'value' property: {value1}");
                return value1;
            }

            if (TryCallMethod(vo2Max, "getValue", out double value2))
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

    private double ExtractPressureValue(Java.Lang.Object pressure)
    {
        try
        {
            Debug.WriteLine($"Pressure object type: {pressure.GetType().Name}");
            Debug.WriteLine($"Pressure object class: {pressure.Class.Name}");

            // Try official Units API with "MILLIMETERS_OF_MERCURY"
            if (TryOfficialUnitsApi(pressure, "MILLIMETERS_OF_MERCURY", out double officialValue))
            {
                Debug.WriteLine($"Found value via official Units API: {officialValue}");
                return officialValue;
            }

            // Try common property patterns
            if (TryGetPropertyValue(pressure, "inMillimetersOfMercury", out double value1))
            {
                Debug.WriteLine($"Found value via 'inMillimetersOfMercury' property: {value1}");
                return value1;
            }

            if (TryCallMethod(pressure, "inMillimetersOfMercury", out double value2))
            {
                Debug.WriteLine($"Found value via 'inMillimetersOfMercury()' method: {value2}");
                return value2;
            }

            if (TryGetPropertyValue(pressure, "value", out double value3))
            {
                Debug.WriteLine($"Found value via 'value' property: {value3}");
                return value3;
            }

            if (TryCallMethod(pressure, "getValue", out double value4))
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

    private bool TryOfficialUnitsApi(Java.Lang.Object obj, string unitName, out double value)
    {
        value = 0;
        try
        {
            // Try to use the official Android Health Connect Units API
            // This might work if the object has methods like InUnit(Mass.KILOGRAMS) or similar

            var objClass = obj.Class;

            // Look for InUnit method
            var inUnitMethod = objClass.GetDeclaredMethods()?.FirstOrDefault(m =>
                m.Name.Equals("InUnit", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Equals("inUnit", StringComparison.OrdinalIgnoreCase));

            if (inUnitMethod != null)
            {
                Debug.WriteLine($"Found InUnit method: {inUnitMethod.Name}");

                // Try to get the unit constant
                if (TryGetUnitConstant(unitName, out Java.Lang.Object? unitConstant))
                {
                    inUnitMethod.Accessible = true;
                    var result = inUnitMethod.Invoke(obj, unitConstant);

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

    private bool TryGetUnitConstant(string unitName, out Java.Lang.Object? unitConstant)
    {
        unitConstant = null;
        try
        {
            // Try to get Mass.KILOGRAMS or Length.METERS constants
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
                    unitConstant = field.Get(null); // Static field
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

    private bool TryGetPropertyValue(Java.Lang.Object obj, string propertyName, out double value)
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

    private bool TryCallMethod(Java.Lang.Object obj, string methodName, out double value)
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

    private bool TryParseFromString(string stringValue, out double value)
    {
        value = 0;

        if (string.IsNullOrEmpty(stringValue))
            return false;

        // Try to extract number from string representations like "70.5 kg" or "1.75 m" etc.
        var numberPattern = @"(\d+\.?\d*)";
        var match = System.Text.RegularExpressions.Regex.Match(stringValue, numberPattern);

        if (match.Success && double.TryParse(match.Groups[1].Value, out value))
        {
            return true;
        }

        return false;
    }

    public async partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission, CancellationToken cancellationToken)
    {
        try
        {
            var sdkCheckResult = IsSdkAvailable();
            if (!sdkCheckResult.IsSuccess)
            {
                return new()
                {
                    Error = RequestPermissionError.IsNotSupported
                };
            }

            var permissionsToGrant = healthPermissions
                .SelectMany(healthPermission => healthPermission.ToStrings())
                .ToList();

            if (canRequestFullHistoryPermission)
            {
                //https://developer.android.com/health-and-fitness/guides/health-connect/plan/data-types#alpha10
                permissionsToGrant.Add("android.permission.health.READ_HEALTH_DATA_HISTORY");
            }

            var grantedPermissions = await KotlinResolver.ProcessList<Java.Lang.String>(_healthConnectClient.PermissionController.GetGrantedPermissions);
            if (grantedPermissions is null)
            {
                return new()
                {
                    Error = RequestPermissionError.ProblemWhileFetchingAlreadyGrantedPermissions
                };
            }

            var missingPermissions = permissionsToGrant
                .Where(permission => !grantedPermissions.ToList().Contains(permission))
                .ToList();

            if (!missingPermissions.Any())
            {
                return new();
            }

            var key = Guid.NewGuid().ToString();
            var requestPermissionActivityContract = PermissionController.CreateRequestPermissionResultContract();
            var callback = new AndroidActivityResultCallback<ISet?>(cancellationToken);

            ActivityResultLauncher? launcher = null;
            ISet? newlyGrantedPermissions = null;
            ActivityResultRegistry? activityResultRegistry = null;
            try
            {
                activityResultRegistry = ((ComponentActivity)_activityContext).ActivityResultRegistry;
                launcher = activityResultRegistry.Register(key, requestPermissionActivityContract, callback);
                launcher.Launch(new HashSet(missingPermissions));

                newlyGrantedPermissions = await callback.Task;
            }
            finally
            {
                launcher?.Unregister();
            }

            var stillMissingPermissions = newlyGrantedPermissions is null
                ? missingPermissions
                : missingPermissions
                    .Where(permission => !newlyGrantedPermissions.ToList().Contains(permission))
                    .ToList();

            if (stillMissingPermissions.Any())
            {
                return new()
                {
                    Error = RequestPermissionError.MissingPermissions,
                    DeniedPermissions = stillMissingPermissions
                };
            }

            return new();
        }
        catch (Exception e)
        {
            return new()
            {
                ErrorException = e
            };
        }
    }

    private Result<SdkCheckError> IsSdkAvailable()
    {
        try
        {
            var availabilityStatus = HealthConnectClient.GetSdkStatus(_activityContext);
            if (availabilityStatus == HealthConnectClient.SdkUnavailable)
            {
                return new()
                {
                    Error = SdkCheckError.SdkUnavailable
                };
            }

            if (availabilityStatus == HealthConnectClient.SdkUnavailableProviderUpdateRequired)
            {
                string providerPackageName = "com.google.android.apps.healthdata";
                // Optionally redirect to package installer to find a provider, for example:
                var uriString = $"market://details?id={providerPackageName}&url=healthconnect%3A%2F%2Fonboarding";

                var intent = new Intent(Intent.ActionView);
                intent.SetPackage("com.android.vending");
                intent.SetData(Android.Net.Uri.Parse(uriString));
                intent.PutExtra("overlay", true);
                intent.PutExtra("callerId", _activityContext.PackageName);

                _activityContext.StartActivity(intent);

                return new()
                {
                    Error = SdkCheckError.SdkUnavailableProviderUpdateRequired
                };
            }

            //The Health Connect SDK supports Android 8(API level 26) or higher, while the Health Connect app is only compatible with Android 9(API level 28) or higher.
            //This means that third-party apps can support users with Android 8, but only users with Android 9 or higher can use Health Connect.
            //https://developer.android.com/health-and-fitness/guides/health-connect/develop/get-started#:~:text=the%20latest%20version.-,Note,-%3A%20The%20Health
            if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                return new()
                {
                    Error = SdkCheckError.AndroidVersionNotSupported
                };
            }

            return new();
        }
        catch (Exception e)
        {
            return new()
            {
                ErrorException = e
            };
        }
    }
}

//public async partial Task<ReadRecordResult> ReadRecords(HealthDataType healthDataType, DateTime from, DateTime until, CancellationToken cancellationToken)
//{
//    var permissionToGrant = new HealthPermissionDto
//    {
//        HealthDataType = healthDataType,
//        PermissionType = PermissionType.Read
//    };

//    var requestPermissionResult = await RequestPermissions([permissionToGrant], false, cancellationToken);
//    if (requestPermissionResult.IsError)
//    {
//        return new()
//        {
//            Error = ReadRecordError.PermissionProblem
//        };
//    }

//    var timeRangeFilter = TimeRangeFilter.Between(
//        Instant.OfEpochMilli(((DateTimeOffset)from).ToUnixTimeMilliseconds())!,
//        Instant.OfEpochMilli(((DateTimeOffset)until).ToUnixTimeMilliseconds())!
//    );

//    var request = new ReadRecordsRequest(
//        healthDataType.ToKotlinClass(),
//        timeRangeFilter,
//        [],
//        true,
//        1000, // default
//        null
//    );

//    var response = await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(_healthConnectClient.ReadRecords, request);
//    if (response is null)
//    {
//        return new()
//        {
//            Error = ReadRecordError.ProblemDuringReading
//        };
//    }

//    //var res = new List<StepsRecord>();

//    //for (int i = 0; i < response.Records.Count; i++)
//    //{
//    //    if (response.Records[i] is StepsRecord item)
//    //    {
//    //        var healthRecord = new HealthRecord
//    //        {
//    //            Id = item.Metadata.Id,
//    //            DataOrigin = item.Metadata.DataOrigin.PackageName,
    //            //lastModifiedTime
//    //            //recordingMethod
//    //        };

//    //        item.Metadata.

//    //        res.Add(item);
//    //        Debug.WriteLine($"{item.StartTime} - {item.EndTime}, {item.Count}: {item.Metadata.DataOrigin.PackageName}");
//    //    }
//    //}

//    //var groupedByOrigin = res.GroupBy(x => x.Metadata.DataOrigin.PackageName)
//    //    .OrderBy(x => x.Key.Contains("google"))
//    //    .ThenBy(x => x.Key.Contains("samsung"));

//    //return groupedByOrigin
//    //    .FirstOrDefault()?
//    //    .Sum(x => x.Count)
//    //    ?? 0;

//    return new();
//}//}