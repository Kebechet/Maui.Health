using Foundation;
using HealthKit;
using Maui.Health.Enums;
using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Extensions;
using Maui.Health.Platforms.iOS.Extensions;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => HKHealthStore.IsHealthDataAvailable;

    public async partial Task<TDto[]> GetHealthDataAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        if (!IsSupported)
        {
            return [];
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"iOS GetHealthDataAsync<{typeof(TDto).Name}>:");
            System.Diagnostics.Debug.WriteLine($"  StartTime: {timeRange.StartTime} (Local: {timeRange.StartDateTime})");
            System.Diagnostics.Debug.WriteLine($"  EndTime: {timeRange.EndTime} (Local: {timeRange.EndDateTime})");

            // Request permission for the specific metric
            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var permissionResult = await RequestPermissions([permission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                return [];
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();

            // Special handling for WorkoutDto - uses HKWorkout instead of HKQuantitySample
            if (typeof(TDto) == typeof(WorkoutDto))
            {
                return await GetWorkoutsAsync<TDto>(timeRange.StartDateTime, timeRange.EndDateTime, cancellationToken);
            }

            // Special handling for BloodPressureDto - split into systolic/diastolic on iOS
            //if (typeof(TDto) == typeof(BloodPressureDto))
            //{
            //    return await GetBloodPressureAsync<TDto>(from, to, cancellationToken);
            //}

            var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier())!;

            var predicate = HKQuery.GetPredicateForSamples(
                (NSDate)timeRange.StartDateTime,
                (NSDate)timeRange.EndDateTime,
                HKQueryOptions.StrictStartDate
            );

            var tcs = new TaskCompletionSource<TDto[]>();

            // Use HKSampleQuery to get individual records
            var query = new HKSampleQuery(
                quantityType,
                predicate,
                0, // No limit
                new[] { new NSSortDescriptor(HKSample.SortIdentifierStartDate, false) },
                (HKSampleQuery sampleQuery, HKSample[] results, NSError error) =>
                {
                    if (error != null)
                    {
                        tcs.TrySetResult([]);
                        return;
                    }

                    var dtos = new List<TDto>();
                    foreach (var sample in results.OfType<HKQuantitySample>())
                    {
                        var dto = ConvertToDto<TDto>(sample, healthDataType);
                        if (dto is not null)
                        {
                            dtos.Add(dto);
                        }
                    }

                    tcs.TrySetResult(dtos.ToArray());
                }
            );

            using var store = new HKHealthStore();
            using var ct = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                store.StopQuery(query);
            });

            store.ExecuteQuery(query);
            var results = await tcs.Task;
            System.Diagnostics.Debug.WriteLine($"  Found {results.Length} {typeof(TDto).Name} records");
            return results;
        }
        catch (Exception)
        {
            return [];
        }
    }

    private async Task<TDto[]> GetWorkoutsAsync<TDto>(DateTime from, DateTime to, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        var predicate = HKQuery.GetPredicateForSamples(
            (NSDate)from,
            (NSDate)to,
            HKQueryOptions.StrictStartDate
        );

        var tcs = new TaskCompletionSource<HKWorkout[]>();
        var workoutType = HKWorkoutType.WorkoutType;

        var query = new HKSampleQuery(
            workoutType,
            predicate,
            0, // No limit
            new[] { new NSSortDescriptor(HKSample.SortIdentifierStartDate, false) },
            (HKSampleQuery sampleQuery, HKSample[] results, NSError error) =>
            {
                if (error != null)
                {
                    tcs.TrySetResult([]);
                    return;
                }

                var workouts = results.OfType<HKWorkout>().ToArray();
                tcs.TrySetResult(workouts);
            }
        );

        using var store = new HKHealthStore();
        using var ct = cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled();
            store.StopQuery(query);
        });

        store.ExecuteQuery(query);
        var workouts = await tcs.Task;

        // Now fetch heart rate data for each workout and convert to DTOs
        var dtos = new List<TDto>();
        foreach (var workout in workouts)
        {
            var dto = await ConvertWorkoutToDtoAsync(workout, cancellationToken) as TDto;
            if (dto is not null)
            {
                dtos.Add(dto);
            }
        }

        return dtos.ToArray();
    }

    private TDto? ConvertToDto<TDto>(HKQuantitySample sample, HealthDataType healthDataType) where TDto : HealthMetricBase
    {
        return typeof(TDto).Name switch
        {
            nameof(StepsDto) => ConvertStepsSample(sample) as TDto,
            nameof(WeightDto) => ConvertWeightSample(sample) as TDto,
            nameof(HeightDto) => ConvertHeightSample(sample) as TDto,
            nameof(ActiveCaloriesBurnedDto) => ConvertActiveCaloriesBurnedSample(sample) as TDto,
            nameof(HeartRateDto) => ConvertHeartRateSample(sample) as TDto,
            //nameof(BodyFatDto) => ConvertBodyFatSample(sample) as TDto,
            //nameof(Vo2MaxDto) => ConvertVo2MaxSample(sample) as TDto,
            _ => null
        };
    }

    private StepsDto ConvertStepsSample(HKQuantitySample sample)
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

    private WeightDto ConvertWeightSample(HKQuantitySample sample)
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

    private HeightDto ConvertHeightSample(HKQuantitySample sample)
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

    private ActiveCaloriesBurnedDto ConvertActiveCaloriesBurnedSample(HKQuantitySample sample)
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

    private HeartRateDto ConvertHeartRateSample(HKQuantitySample sample)
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

    //private BodyFatDto ConvertBodyFatSample(HKQuantitySample sample)
    //{
    //    var percentage = sample.Quantity.GetDoubleValue(HKUnit.Percent) * 100; // HKUnit.Percent is 0-1, convert to 0-100
    //    var timestamp = new DateTimeOffset(sample.StartDate.ToDateTime());

    //    return new BodyFatDto
    //    {
    //        Id = sample.Uuid.ToString(),
    //        DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
    //        Timestamp = timestamp,
    //        Percentage = percentage,
    //        Unit = "%"
    //    };
    //}

    //private Vo2MaxDto ConvertVo2MaxSample(HKQuantitySample sample)
    //{
    //    var value = sample.Quantity.GetDoubleValue(HKUnit.FromString("ml/kg*min"));
    //    var timestamp = new DateTimeOffset(sample.StartDate.ToDateTime());

    //    return new Vo2MaxDto
    //    {
    //        Id = sample.Uuid.ToString(),
    //        DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
    //        Timestamp = timestamp,
    //        Value = value,
    //        Unit = "ml/kg/min"
    //    };
    //}

    private async Task<WorkoutDto> ConvertWorkoutToDtoAsync(HKWorkout workout, CancellationToken cancellationToken)
    {
        var startTime = new DateTimeOffset(workout.StartDate.ToDateTime());
        var endTime = new DateTimeOffset(workout.EndDate.ToDateTime());
        var activityType = MapHKWorkoutActivityType(workout.WorkoutActivityType);

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
            var heartRateData = await QueryHeartRateSamplesAsync(startTime.UtcDateTime, endTime.UtcDateTime, cancellationToken);
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

    private async Task<HeartRateDto[]> QueryHeartRateSamplesAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        // Ensure DateTime is treated as UTC for NSDate conversion
        var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!;
        var predicate = HKQuery.GetPredicateForSamples((NSDate)fromUtc, (NSDate)toUtc, HKQueryOptions.StrictStartDate);
        var tcs = new TaskCompletionSource<HeartRateDto[]>();

        var query = new HKSampleQuery(
            quantityType,
            predicate,
            0,
            new[] { new NSSortDescriptor(HKSample.SortIdentifierStartDate, false) },
            (HKSampleQuery sampleQuery, HKSample[] results, NSError error) =>
            {
                if (error != null)
                {
                    tcs.TrySetResult([]);
                    return;
                }

                var dtos = new List<HeartRateDto>();
                foreach (var sample in results.OfType<HKQuantitySample>())
                {
                    var dto = ConvertHeartRateSample(sample);
                    dtos.Add(dto);
                }

                tcs.TrySetResult(dtos.ToArray());
            }
        );

        using var store = new HKHealthStore();
        using var ct = cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled();
            store.StopQuery(query);
        });

        store.ExecuteQuery(query);
        return await tcs.Task;
    }

    private ActivityType MapHKWorkoutActivityType(HKWorkoutActivityType hkType)
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

    /// <summary>
    /// <param name="canRequestFullHistoryPermission">iOS has this by default as TRUE</param>
    /// <returns></returns>
    public async partial Task<RequestPermissionResult> RequestPermissions(
        IList<HealthPermissionDto> healthPermissions,
        bool canRequestFullHistoryPermission,
        CancellationToken cancellationToken)
    {
        if (!IsSupported)
        {
            return new RequestPermissionResult()
            {
                Error = Enums.Errors.RequestPermissionError.IsNotSupported
            };
        }

        var readTypes = new List<HKObjectType>();
        var writeTypes = new List<HKObjectType>();

        foreach (var permission in healthPermissions)
        {
            HKObjectType? type = null;

            // Special handling for workout/exercise session
            if (permission.HealthDataType == HealthDataType.ExerciseSession)
            {
                type = HKWorkoutType.WorkoutType;
            }
            else
            {
                type = HKQuantityType.Create(permission.HealthDataType.ToHKQuantityTypeIdentifier());
            }

            if (type != null)
            {
                if (permission.PermissionType.HasFlag(PermissionType.Read))
                {
                    readTypes.Add(type);
                }
                if (permission.PermissionType.HasFlag(PermissionType.Write))
                {
                    writeTypes.Add(type);
                }
            }
        }

        var nsTypesToRead = new NSSet<HKObjectType>(readTypes.ToArray());
        var nsTypesToWrite = new NSSet<HKObjectType>(writeTypes.ToArray());

        try
        {
            using var healthStore = new HKHealthStore();


            //https://developer.apple.com/documentation/healthkit/hkhealthstore/1614152-requestauthorization
            var (isSuccess, error) = await healthStore.RequestAuthorizationToShareAsync(nsTypesToWrite, nsTypesToRead);
            if (!isSuccess)
            {
                return new RequestPermissionResult()
                {
                    Error = RequestPermissionError.ProblemWhileGrantingPermissions
                };
            }

            //https://developer.apple.com/documentation/healthkit/hkhealthstore/1614154-authorizationstatus#discussion
            //To help prevent possible leaks of sensitive health information, your app cannot determine whether or not
            //a user has granted permission to read data.If you are not given permission, it simply appears as if there
            //is no data of the requested type in the HealthKit store.If your app is given share permission but not read
            //permission, you see only the data that your app has written to the store.Data from other sources remains
            //hidden.

            if (writeTypes.Any())
            {
                foreach (var typeToWrite in writeTypes)
                {
                    var status = healthStore.GetAuthorizationStatus(typeToWrite);
                    if (status != HKAuthorizationStatus.SharingAuthorized)
                    {
                        return new RequestPermissionResult()
                        {
                            Error = RequestPermissionError.MissingPermissions
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return new RequestPermissionResult()
            {
                Error = RequestPermissionError.ProblemWhileGrantingPermissions,
                ErrorException = ex
            };
        }
        return new();
    }
}
