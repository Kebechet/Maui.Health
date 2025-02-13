using Foundation;
using HealthKit;
using Maui.Health.Enums;
using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using Maui.Health.Platforms.iOS.Extensions;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => HKHealthStore.IsHealthDataAvailable;

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

        var typesToRead = healthPermissions
            .Where(x => x.PermissionType.HasFlag(PermissionType.Read))
            .Select(healthPermission => HKQuantityType.Create(
                healthPermission.HealthDataType.ToHKQuantityTypeIdentifier())!
            )
            .ToArray();
        var nsTypesToRead = new NSSet<HKQuantityType>(typesToRead);

        var typesToWrite = healthPermissions
            .Where(x => x.PermissionType.HasFlag(PermissionType.Write))
            .Select(healthPermission => HKQuantityType.Create(
                healthPermission.HealthDataType.ToHKQuantityTypeIdentifier())!
            )
            .ToArray();
        var nsTypesToWrite = new NSSet<HKQuantityType>(typesToWrite);

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

            if (typesToWrite.Any())
            {
                foreach (var typeToWrite in typesToWrite)
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

    /// <summary>
    /// </summary>
    /// <returns>NULL == something didnt work correctly</returns>
    public async partial Task<long?> GetStepsTodayAsync(CancellationToken cancellationToken)
    {
        try
        {
            var healthPermission = new HealthPermissionDto
            {
                HealthDataType = HealthDataType.Steps,
                PermissionType = PermissionType.Read
            };

            var permissionResult = await RequestPermissions([healthPermission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                return null;
            }

            var now = (NSDate)DateTime.UtcNow;
            var startOfDay = NSCalendar.CurrentCalendar.StartOfDayForDate(now);

            var predicate = HKQuery.GetPredicateForSamples(
                startOfDay,
                now,
                HKQueryOptions.StrictStartDate
            );

            var tcs = new TaskCompletionSource<long?>();
            var stepsCountType = HKQuantityType.Create(HealthDataType.Steps.ToHKQuantityTypeIdentifier())!;
            var sumOptions = HKStatisticsOptions.CumulativeSum;

            var query = new HKStatisticsQuery(
                stepsCountType,
                predicate,
                sumOptions,
                (HKStatisticsQuery resultQuery, HKStatistics results, NSError error) =>
                {
                    if (error != null)
                    {
                        tcs.TrySetResult(null);
                    }
                    else
                    {
                        var totalSteps = (int)(results.SumQuantity()?.GetDoubleValue(HKUnit.Count) ?? 0);
                        tcs.TrySetResult(totalSteps);
                    }

                });

            using var store = new HKHealthStore();

            using var ct = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                store.StopQuery(query);
            });

            store.ExecuteQuery(query);

            var totalSteps = await tcs.Task;

            return totalSteps;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}

//var authorizatioNRequestStatus = await healthStore
//    .GetRequestStatusForAuthorizationToShareAsync(nsTypesToWrite, nsTypesToRead);
//if (authorizatioNRequestStatus != HKAuthorizationRequestStatus.ShouldRequest)
//{
//    //https://developer.apple.com/documentation/healthkit/hksampletype
//}


//public async partial Task<ReadRecordResult> ReadRecords(
//    HealthDataType healthDataType,
//    DateTime from,
//    DateTime until,
//    CancellationToken cancellationToken)
//{
//    var healthPermission = new HealthPermissionDto
//    {
//        HealthDataType = healthDataType,
//        PermissionType = PermissionType.Read | PermissionType.Write
//    };

//    var permissionResult = await RequestPermissions([healthPermission], cancellationToken: cancellationToken);
//    if (!permissionResult.IsSuccess)
//    {
//        return new()
//        {
//            Error = ReadRecordError.PermissionProblem
//        };
//    }

//    var tcs = new TaskCompletionSource<ReadRecordResult>();
//    var calendar = NSCalendar.CurrentCalendar;

//    var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier())!;

//    var now = (NSDate)DateTime.UtcNow;
//    var startOfDay = NSCalendar.CurrentCalendar.StartOfDayForDate(now);

//    var predicate = HKQuery.GetPredicateForSamples(
//        startOfDay,
//        now,
//        HKQueryOptions.StrictStartDate
//    );

//    var query = new HKStatisticsQuery(
//        quantityType,
//        predicate,
//        HKStatisticsOptions.CumulativeSum,
//        new HKStatisticsQueryHandler((hkStatisticsQuery, hkStatistics, nsError) =>
//        {
//            if (nsError != null)
//            {
//                tcs.TrySetResult(new ReadRecordResult()
//                {
//                    ErrorException = new Exception(nsError.Description)
//                });
//            }
//            else
//            {
//                var result = new ReadRecordResult();

//                //var res = hkStatistics.

//                //hkStatistics.EnumerateStatistics(
//                //    (NSDate)start.LocalDateTime,
//                //    (NSDate)end.LocalDateTime,
//                //    (result, stop) =>
//                //    {
//                //        try
//                //        {
//                //            var value = transform(result);
//                //            if (value != null)
//                //                list.Add(value);
//                //        }
//                //        catch (Exception ex)
//                //        {
//                //            tcs.TrySetException(ex);
//                //        }
//                //    }
//                //);
//                tcs.TrySetResult(result);
//            }
//        })
//    );

//    using var store = new HKHealthStore();

//    using var ct = cancellationToken.Register(() =>
//    {
//        tcs.TrySetCanceled();
//        store.StopQuery(query);
//    });

//    store.ExecuteQuery(query);

//    var result = await tcs.Task.ConfigureAwait(false);

//    return result;

//}





//public async Task<List<StepRecord>> GetStepRecordsTodayAsync(CancellationToken cancellationToken)
//{
//    // First, request the HealthKit permission to read steps if needed.
//    var healthPermission = new HealthPermissionDto
//    {
//        HealthDataType = HealthDataType.Steps,
//        PermissionType = PermissionType.Read
//    };

//    var permissionResult = await RequestPermissions(
//        new[] { healthPermission },
//        cancellationToken: cancellationToken);

//    if (!permissionResult.IsSuccess)
//    {
//        return null;
//    }

//    var now = (NSDate)DateTime.UtcNow;
//    var startOfDay = NSCalendar.CurrentCalendar.StartOfDayForDate(now);

//    var predicate = HKQuery.GetPredicateForSamples(
//        startOfDay,
//        now,
//        HKQueryOptions.StrictStartDate);

//    var tcs = new TaskCompletionSource<List<StepRecord>>();
//    var stepsCountType = HKQuantityType.Create(HealthDataType.Steps.ToHKQuantityTypeIdentifier())!;

//    // Define the query: HKSampleQuery will return individual sample records
//    var query = new HKSampleQuery(
//        stepsCountType,
//        predicate,
//        0,  // 0 means 'no limit' (i.e., return all matching samples)
//        new[] { new NSSortDescriptor(HKSample.SortIdentifierStartDate, false) }, // sort descending if you want newest first
//        (HKSampleQuery sampleQuery, HKSample[] results, NSError error) =>
//        {
//            if (error != null)
//            {
//                tcs.TrySetException(new Exception(error.LocalizedDescription));
//                return;
//            }

//            var stepRecords = new List<StepRecord>();

//            // Each result should be an HKQuantitySample for steps
//            foreach (var sample in results.OfType<HKQuantitySample>())
//            {
//                var value = sample.Quantity.GetDoubleValue(HKUnit.Count);
//                //var start = sample.StartDate.ToDateTimeUtc();
//                //var end = sample.EndDate.ToDateTimeUtc();

//                stepRecords.Add(new StepRecord
//                {
//                    //StartDate = start,
//                    //EndDate = end,
//                    Steps = (long)value
//                });
//            }

//            tcs.TrySetResult(stepRecords);
//        }
//    );

//    using var store = new HKHealthStore();

//    // Cancellation token logic
//    using var reg = cancellationToken.Register(() =>
//    {
//        tcs.TrySetCanceled();
//        store.StopQuery(query);
//    });

//    store.ExecuteQuery(query);

//    var res = await tcs.Task;

//    return res;
//}

///// <summary>
///// Simple model to hold step records.
///// </summary>
//public class StepRecord
//{
//    public DateTime StartDate { get; set; }
//    public DateTime EndDate { get; set; }
//    public long Steps { get; set; }
//}
