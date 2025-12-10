using Foundation;
using HealthKit;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Platforms.iOS.Extensions;

/// <summary>
/// Extension methods for HKHealthStore to simplify common HealthKit operations.
/// </summary>
internal static class HKHealthStoreExtensions
{
    /// <summary>
    /// No limit on query results (returns all matching records).
    /// </summary>
    private const nuint NoLimit = 0;
    /// <summary>
    /// Saves an HKObject to HealthKit asynchronously.
    /// </summary>
    /// <param name="healthStore">The HKHealthStore instance</param>
    /// <param name="sample">The HKObject to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if saved successfully, false otherwise</returns>
    internal static async Task<bool> Save(
        this HKHealthStore healthStore,
        HKObject sample,
        CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>();

        healthStore.SaveObject(sample, (success, error) =>
        {
            if (error is not null)
            {
                tcs.TrySetResult(false);
            }
            else
            {
                tcs.TrySetResult(success);
            }
        });

        using var ct = cancellationToken.Register(() => tcs.TrySetCanceled());
        return await tcs.Task;
    }

    /// <summary>
    /// Deletes an HKObject from HealthKit asynchronously.
    /// </summary>
    /// <remarks>
    /// Note: You can only delete objects that were created by your application.
    /// </remarks>
    /// <param name="healthStore">The HKHealthStore instance</param>
    /// <param name="sample">The HKObject to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    internal static async Task<bool> Delete(
        this HKHealthStore healthStore,
        HKObject sample,
        CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>();

        healthStore.DeleteObject(sample, (success, error) =>
        {
            if (error is not null)
            {
                tcs.TrySetResult(false);
            }
            else
            {
                tcs.TrySetResult(success);
            }
        });

        using var ct = cancellationToken.Register(() => tcs.TrySetCanceled());
        return await tcs.Task;
    }

    /// <summary>
    /// Reads workout records from HealthKit for a specified time range.
    /// </summary>
    /// <param name="healthStore">The HKHealthStore instance</param>
    /// <param name="timeRange">The time range to query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of HKWorkout records matching the query</returns>
    internal static async Task<HKWorkout[]> ReadWorkouts(
        this HKHealthStore healthStore,
        HealthTimeRange timeRange,
        CancellationToken cancellationToken = default)
    {
        var predicate = HKQuery.GetPredicateForSamples(
            timeRange.StartTime.ToNSDate(),
            timeRange.EndTime.ToNSDate(),
            HKQueryOptions.StrictStartDate
        );

        var tcs = new TaskCompletionSource<HKWorkout[]>();
        var workoutType = HKWorkoutType.WorkoutType;

        var query = new HKSampleQuery(
            workoutType,
            predicate,
            NoLimit,
            [new NSSortDescriptor(HKSample.SortIdentifierStartDate, false)],
            (_, results, error) =>
            {
                if (error is not null)
                {
                    tcs.TrySetResult([]);
                    return;
                }

                var workouts = results?.OfType<HKWorkout>().ToArray() ?? [];
                tcs.TrySetResult(workouts);
            }
        );

        using var ct = cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled();
            healthStore.StopQuery(query);
        });

        healthStore.ExecuteQuery(query);
        return await tcs.Task;
    }

    /// <summary>
    /// Finds a workout by its UUID.
    /// </summary>
    /// <param name="healthStore">The HKHealthStore instance</param>
    /// <param name="workoutId">The workout UUID string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The HKWorkout if found, null otherwise</returns>
    internal static async Task<HKWorkout?> FindWorkoutById(
        this HKHealthStore healthStore,
        string workoutId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(workoutId, out var workoutGuid))
        {
            return null;
        }

        var uuid = new NSUuid(workoutGuid.ToString());
        var predicate = HKQuery.GetPredicateForObject(uuid);
        var workoutType = HKWorkoutType.WorkoutType;

        var tcs = new TaskCompletionSource<HKWorkout?>();

        var query = new HKSampleQuery(
            workoutType,
            predicate,
            1,
            null,
            (_, results, error) =>
            {
                if (error is not null)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                var workout = results?.FirstOrDefault() as HKWorkout;
                tcs.TrySetResult(workout);
            }
        );

        using var ct = cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled();
            healthStore.StopQuery(query);
        });

        healthStore.ExecuteQuery(query);
        return await tcs.Task;
    }
}
