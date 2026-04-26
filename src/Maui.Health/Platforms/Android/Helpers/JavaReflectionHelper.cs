using Android.Runtime;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Response;
using AndroidX.Health.Connect.Client.Time;
using Java.Time;
using Kotlin.Reflect;
using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Platforms.Android.Extensions;
using Maui.Health.Platforms.Android.Reflection;
using System.Diagnostics;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// Centralized helper for Java/Kotlin reflection operations.
/// Used to work around Xamarin binding limitations with Kotlin code.
/// </summary>
internal static class JavaReflectionHelper
{
    /// <summary>
    /// Tries to extract a double value using the official Units API with a unit constant.
    /// </summary>
    public static bool TryOfficialUnitsApi(this Java.Lang.Object obj, string unitName, out double value)
    {
        value = 0;

        try
        {
            var objClass = obj.Class;
            // Search declared methods first, then all methods (including inherited) as fallback
            var inUnitMethod = objClass?.GetDeclaredMethods()?.FirstOrDefault(m =>
                m != null && (m.Name.Equals("InUnit", StringComparison.OrdinalIgnoreCase) ||
                              m.Name.Equals("inUnit", StringComparison.OrdinalIgnoreCase)))
                ?? objClass?.GetMethods()?.FirstOrDefault(m =>
                    m != null && (m.Name.Equals("InUnit", StringComparison.OrdinalIgnoreCase) ||
                                  m.Name.Equals("inUnit", StringComparison.OrdinalIgnoreCase)));

            if (inUnitMethod is null)
            {
                return false;
            }

            if (!TryGetUnitConstant(unitName, out var unitConstant))
            {
                return false;
            }

            inUnitMethod.Accessible = true;
            var result = inUnitMethod.Invoke(obj, unitConstant!);

            return result.TryConvertToDouble(out value);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to get a property value from a Java object using field reflection.
    /// </summary>
    public static bool TryGetPropertyValue(this Java.Lang.Object obj, string propertyName, out double value)
    {
        value = 0;

        try
        {
            var objClass = obj.Class;
            var field = objClass?.GetDeclaredField(propertyName);

            if (field is null)
            {
                return false;
            }

            field.Accessible = true;
            var fieldValue = field.Get(obj);

            return fieldValue.TryConvertToDouble(out value);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to call a no-arg method on a Java object and extract a double value.
    /// </summary>
    public static bool TryCallMethod(this Java.Lang.Object obj, string methodName, out double value)
    {
        value = 0;

        try
        {
            var objClass = obj.Class;
            // GetDeclaredMethod only searches the exact class, not parent classes.
            // Fall back to searching all methods (including inherited) by name.
            var method = objClass?.GetDeclaredMethod(methodName)
                ?? objClass?.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == methodName && m.GetParameterTypes()?.Length == 0)
                ?? objClass?.GetMethods()?.FirstOrDefault(m => m?.Name == methodName && m.GetParameterTypes()?.Length == 0);

            if (method is null)
            {
                return false;
            }

            method.Accessible = true;
            var result = method.Invoke(obj);

            return result.TryConvertToDouble(out value);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetUnitConstant(string unitName, out Java.Lang.Object? unitConstant)
    {
        unitConstant = null;

        try
        {
            var className = unitName switch
            {
                _ when unitName.Contains("KILOGRAM") => "Mass",
                _ when unitName.Contains("KILOCALORIE") || unitName.Contains("CALORIE") => "Energy",
                _ when unitName.Contains("MERCURY") => "Pressure",
                _ => "Length"
            };

            var fullClassName = $"{HealthConnectUnitsNamespace}.{className}";
            var unitClass = JavaClassResolver.Resolve(fullClassName);

            if (unitClass is null)
            {
                return false;
            }

            var field = unitClass.GetDeclaredField(unitName);
            if (field is null)
            {
                return false;
            }

            field.Accessible = true;
            unitConstant = field.Get(null);

            return unitConstant != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Inserts a record into Health Connect using the direct binding method via KotlinResolver.
    /// </summary>
    /// <param name="healthConnectClient">The Health Connect client instance</param>
    /// <param name="record">The record to insert</param>
    /// <returns>True if the record was inserted successfully, false otherwise</returns>
    internal static Task<bool> InsertRecord(this IHealthConnectClient healthConnectClient, Java.Lang.Object record)
    {
        return healthConnectClient.InsertRecords([record]);
    }

    internal static async Task<bool> InsertRecords(this IHealthConnectClient healthConnectClient, IList<Java.Lang.Object> records)
    {
        var recordIds = await healthConnectClient.InsertRecordsWithIds(records);
        return recordIds is not null;
    }

    /// <summary>
    /// In-place update of existing Health Connect records via the native <c>updateRecords</c>
    /// coroutine. Each record must carry its target ID via <c>Metadata.manualEntryWithId</c>
    /// (built for us by <see cref="Extensions.HealthRecordExtensions.ToAndroidRecord"/> when a
    /// <c>recordId</c> is supplied). Returns <c>true</c> when the call completes normally —
    /// Health Connect's <c>updateRecords</c> returns <c>Unit</c> at the Kotlin level, so there
    /// is no ID list to hand back; the IDs are preserved on success.
    /// </summary>
    internal static async Task<bool> UpdateRecords(this IHealthConnectClient healthConnectClient, IList<Java.Lang.Object> records)
    {
        try
        {
            var irecords = new System.Collections.Generic.List<AndroidX.Health.Connect.Client.Records.IRecord>();

            foreach (var record in records)
            {
                var irecord = Java.Lang.Object.GetObject<AndroidX.Health.Connect.Client.Records.IRecord>(
                    record.Handle, JniHandleOwnership.DoNotTransfer);

                if (irecord is null)
                {
                    Debug.WriteLine("Could not wrap record as IRecord");
                    return false;
                }

                irecords.Add(irecord);
            }

            // updateRecords returns kotlin.Unit on success; we treat a non-null resolved result
            // as "the coroutine completed without throwing." Exceptions propagate via the
            // Continuation and land in the catch below.
            var result = await KotlinResolver.Process<Java.Lang.Object, System.Collections.Generic.IList<AndroidX.Health.Connect.Client.Records.IRecord>>(
                healthConnectClient.UpdateRecords, irecords);

            return result is not null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating records: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Inserts records into Health Connect and returns the platform-assigned record IDs
    /// from <c>InsertRecordsResponse.recordIdsList</c>. Returns <c>null</c> on failure so
    /// callers can distinguish "wrote zero records" from "write did not complete".
    /// </summary>
    internal static async Task<IReadOnlyList<string>?> InsertRecordsWithIds(this IHealthConnectClient healthConnectClient, IList<Java.Lang.Object> records)
    {
        try
        {
            var irecords = new System.Collections.Generic.List<AndroidX.Health.Connect.Client.Records.IRecord>();

            foreach (var record in records)
            {
                var irecord = Java.Lang.Object.GetObject<AndroidX.Health.Connect.Client.Records.IRecord>(
                    record.Handle, JniHandleOwnership.DoNotTransfer);

                if (irecord is null)
                {
                    Debug.WriteLine("Could not wrap record as IRecord");
                    return null;
                }

                irecords.Add(irecord);
            }

            var response = await KotlinResolver.Process<InsertRecordsResponse, System.Collections.Generic.IList<AndroidX.Health.Connect.Client.Records.IRecord>>(
                healthConnectClient.InsertRecords, irecords);

            if (response is null)
            {
                return null;
            }

            // RecordIdsList is IList<string> already — Health Connect exposes them as Kotlin
            // strings, the Xamarin binding surfaces them as .NET strings.
            return response.RecordIdsList.ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error inserting records: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Reads health records from Health Connect for a specified time range and record type.
    /// </summary>
    /// <param name="healthConnectClient">The Health Connect client instance</param>
    /// <param name="recordClass">The Kotlin class of the record type to read</param>
    /// <param name="timeRange">The time range to query</param>
    /// <returns>The ReadRecordsResponse containing the records, or null if the operation failed</returns>
#pragma warning disable CA1416
    internal static async Task<ReadRecordsResponse?> ReadHealthRecords(
        this IHealthConnectClient healthConnectClient,
        IKClass recordClass,
        HealthTimeRange timeRange)
    {
        // No try/catch: exceptions propagate to the outer service layer
        // (HealthService.GetHealthData) where they are wrapped in
        // HealthDataReadResult.ErrorException. An inner swallow here would hide real errors
        // behind a null return and recreate the "silent empty result" bug.
        var timeRangeFilter = TimeRangeFilter.Between(
            Instant.OfEpochMilli(timeRange.StartTime.ToUnixTimeMilliseconds())!,
            Instant.OfEpochMilli(timeRange.EndTime.ToUnixTimeMilliseconds())!
        );

        var request = new ReadRecordsRequest(
            recordClass,
            timeRangeFilter,
            [],
            true,
            MaxRecordsPerRequest,
            null
        );

        return await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(
            healthConnectClient.ReadRecords, request);
    }
#pragma warning restore CA1416

    internal static async Task<Java.Lang.Object?> ReadHealthRecord(
        this IHealthConnectClient healthConnectClient,
        IKClass recordClass,
        string recordId)
    {
        // No try/catch: exceptions propagate to the outer service layer
        // (HealthService.GetHealthRecord) which wraps them in
        // HealthRecordReadResult.ErrorException. Null returns below mean "reflection setup
        // could not bind" — preserved so the caller can treat those as "no such record."
        var (clientClass, clientObject) = healthConnectClient.GetJniClientObjects();
        if (clientClass is null || clientObject is null)
        {
            return null;
        }

        var recordClassObj = Java.Lang.Object.GetObject<Java.Lang.Object>(recordClass.Handle, JniHandleOwnership.DoNotTransfer);
        if (recordClassObj is null)
        {
            return null;
        }

        var result = await InvokeKotlinSuspendMethod(clientClass, clientObject, "readRecord", recordClassObj, new Java.Lang.String(recordId));
        if (result is null)
        {
            return null;
        }

        return ReadRecordResponseReflection.GetRecord.Invoke(result);
    }

    /// <summary>
    /// Deletes a workout record from Health Connect using reflection to call the Kotlin suspend function.
    /// </summary>
    /// <remarks>
    /// Note: You can only delete records that were created by your application.
    /// Attempting to delete records created by other apps will fail or be ignored by the platform.
    /// </remarks>
    /// <param name="healthConnectClient">The Health Connect client instance</param>
    /// <param name="workoutId">The ID of the workout record to delete</param>
    /// <returns>True if the record was deleted successfully, false otherwise</returns>
    internal static Task<bool> DeleteWorkoutRecord(
        this IHealthConnectClient healthConnectClient,
        string workoutId)
    {
        var recordClass = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
            Java.Lang.Class.FromType(typeof(AndroidX.Health.Connect.Client.Records.ExerciseSessionRecord)));

        return healthConnectClient.DeleteRecord(recordClass, workoutId);
    }

    /// <summary>
    /// Deletes a record of any type from Health Connect by its ID.
    /// You can only delete records that were created by your application.
    /// </summary>
    internal static async Task<bool> DeleteRecord(
        this IHealthConnectClient healthConnectClient,
        IKClass recordClass,
        string recordId)
    {
        try
        {
            var recordIdsList = new Java.Util.ArrayList();
            recordIdsList.Add(recordId);

            var (clientClass, clientObject) = healthConnectClient.GetJniClientObjects();
            if (clientClass is null || clientObject is null)
            {
                return false;
            }

            var deleteMethod = clientClass.GetDeclaredMethod("deleteRecords",
                Java.Lang.Class.FromType(typeof(IKClass)),
                Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                Java.Lang.Class.FromType(typeof(Kotlin.Coroutines.IContinuation)))
                ?? clientClass.GetMethods()?.FirstOrDefault(m =>
                    m?.Name == "deleteRecords" && m.GetParameterTypes()?.Length == 4);

            if (deleteMethod is null)
            {
                Debug.WriteLine("Could not find deleteRecords method");
                return false;
            }

            var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
            var continuation = new Continuation(taskCompletionSource, default);

            deleteMethod.Accessible = true;
            var emptyList = new Java.Util.ArrayList();

            var recordClassObj = Java.Lang.Object.GetObject<Java.Lang.Object>(recordClass.Handle, JniHandleOwnership.DoNotTransfer);
            if (recordClassObj is null)
            {
                return false;
            }

            var result = deleteMethod.Invoke(clientObject, recordClassObj, recordIdsList, emptyList, continuation);

            if (result is Java.Lang.Enum javaEnum)
            {
                var currentState = Enum.Parse<CoroutineState>(javaEnum.ToString());
                if (currentState == CoroutineState.COROUTINE_SUSPENDED)
                {
                    await taskCompletionSource.Task;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting record: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Aggregates health records using Health Connect's aggregate() API via JNI reflection.
    /// Properly deduplicates data across multiple health apps.
    /// </summary>
    internal static async Task<(Java.Lang.Object? Value, List<string> DataOrigins)> AggregateHealthRecords(
        this IHealthConnectClient healthConnectClient,
        string recordClassName,
        string metricFieldName,
        HealthTimeRange timeRange)
    {
        // No try/catch: exceptions propagate to the outer service layer
        // (HealthService.GetAggregatedHealthData) which wraps them in
        // AggregatedReadResult.ErrorException. Null returns below mean reflection setup or
        // bind failures — preserved so the caller treats those as "unsupported / no data."
        var metric = GetAggregateMetric(recordClassName, metricFieldName);
        if (metric is null)
        {
            return (null, []);
        }

        var timeRangeFilter = TimeRangeFilter.Between(
            Instant.OfEpochMilli(timeRange.StartTime.ToUnixTimeMilliseconds())!,
            Instant.OfEpochMilli(timeRange.EndTime.ToUnixTimeMilliseconds())!
        );

        var request = CreateAggregateRequest(metric, timeRangeFilter);
        if (request is null)
        {
            return (null, []);
        }

        var (clientClass, clientObject) = healthConnectClient.GetJniClientObjects();
        if (clientClass is null || clientObject is null)
        {
            return (null, []);
        }

        var aggregationResult = await InvokeKotlinSuspendMethod(clientClass, clientObject, "aggregate", request);
        if (aggregationResult is null)
        {
            return (null, []);
        }

        var value = ExtractAggregateValue(aggregationResult, metric);
        var dataOrigins = ExtractDataOrigins(aggregationResult);

        return (value, dataOrigins);
    }

    /// <summary>
    /// Aggregates health records grouped by a time duration using Health Connect's aggregateGroupByDuration() API.
    /// </summary>
    internal static async Task<List<AggregatedResult>> AggregateHealthRecordsByDuration(
        this IHealthConnectClient healthConnectClient,
        string recordClassName,
        string metricFieldName,
        HealthTimeRange timeRange,
        TimeSpan interval,
        HealthDataType dataType,
        string? unit)
    {
        // No try/catch here: the outer service layer (HealthService.GetAggregatedHealthDataByInterval)
        // is the single error boundary — it wraps platform exceptions in
        // AggregatedIntervalReadResult.ErrorException so callers can distinguish "platform
        // returned no data" from "platform call failed." An inner swallow here would hide
        // real errors behind an empty list, which is exactly the bug the Result refactor is
        // fixing.
        var metric = GetAggregateMetric(recordClassName, metricFieldName);
        if (metric is null)
        {
            return [];
        }

        var timeRangeFilter = TimeRangeFilter.Between(
            Instant.OfEpochMilli(timeRange.StartTime.ToUnixTimeMilliseconds())!,
            Instant.OfEpochMilli(timeRange.EndTime.ToUnixTimeMilliseconds())!
        );

        var duration = Java.Time.Duration.OfMillis((long)interval.TotalMilliseconds);

        var metricsSet = new Java.Util.HashSet();
        metricsSet.Add(metric);

        var emptySet = new Java.Util.HashSet();

        var request = AggregateGroupByDurationRequestReflection.Constructor
            .NewInstance(metricsSet, timeRangeFilter, duration, emptySet);
        if (request is null)
        {
            Debug.WriteLine("Failed to create AggregateGroupByDurationRequest");
            return [];
        }

        var (clientClass, clientObject) = healthConnectClient.GetJniClientObjects();
        if (clientClass is null || clientObject is null)
        {
            return [];
        }

        var result = await InvokeKotlinSuspendMethod(clientClass, clientObject, "aggregateGroupByDuration", request);
        if (result is null)
        {
            return [];
        }

        // Result is a List<AggregationResultGroupedByDuration>
        // May be JavaList or java.util.Arrays$ArrayList (IList)
        var results = new List<AggregatedResult>();

        Java.Util.IList javaList;
        if (result is Java.Util.IList iList)
        {
            javaList = iList;
        }
        else
        {
            return [];
        }

        for (int i = 0; i < javaList.Size(); i++)
        {
            var item = javaList.Get(i);
            if (item is null)
            {
                continue;
            }

            var startInstant = AggregationResultGroupedByDurationReflection.GetStartTime
                .Invoke(item) as Java.Time.Instant;
            var endInstant = AggregationResultGroupedByDurationReflection.GetEndTime
                .Invoke(item) as Java.Time.Instant;

            var aggregationResult = AggregationResultGroupedByDurationReflection.GetResult
                .Invoke(item);
            if (aggregationResult is null)
            {
                continue;
            }

            var value = ExtractAggregateValue(aggregationResult, metric);
            if (value is null)
            {
                continue;
            }

            double numericValue = 0;
            if (value is Java.Lang.Number number)
            {
                numericValue = number.DoubleValue();
            }
            else
            {
                numericValue = value.ExtractEnergyValue();
            }

            var bucketStart = startInstant is not null
                ? DateTimeOffset.FromUnixTimeMilliseconds(startInstant.ToEpochMilli())
                : timeRange.StartTime;
            var bucketEnd = endInstant is not null
                ? DateTimeOffset.FromUnixTimeMilliseconds(endInstant.ToEpochMilli())
                : timeRange.EndTime;

            var dataOrigins = ExtractDataOrigins(aggregationResult);

            results.Add(new AggregatedResult
            {
                StartTime = bucketStart,
                EndTime = bucketEnd,
                Value = numericValue,
                Unit = unit,
                DataType = dataType,
                DataSdk = HealthDataSdk.GoogleHealthConnect,
                DataOrigins = dataOrigins
            });
        }

        return results;
    }

    /// <summary>
    /// Gets a changes token from Health Connect for tracking data changes.
    /// </summary>
    internal static async Task<string?> GetHealthChangesToken(
        this IHealthConnectClient healthConnectClient,
        IList<IKClass> recordTypes)
    {
        // No try/catch: exceptions propagate to HealthService.GetChangesToken which wraps
        // them in ChangesTokenResult.ErrorException. Null returns below indicate reflection
        // setup could not find the expected Health Connect classes — preserved.
        var recordTypesSet = new Java.Util.HashSet();
        foreach (var recordType in recordTypes)
        {
            var recordTypeObj = Java.Lang.Object.GetObject<Java.Lang.Object>(recordType.Handle, JniHandleOwnership.DoNotTransfer);
            if (recordTypeObj is not null)
            {
                recordTypesSet.Add(recordTypeObj);
            }
        }

        var requestClass = JavaClassResolver.Resolve(JavaReflection.ChangesTokenRequestClassName);
        if (requestClass is null)
        {
            Debug.WriteLine("Failed to find ChangesTokenRequest class");
            return null;
        }

        // Constructor: ChangesTokenRequest(Set<KClass>, Set<DataOrigin>)
        var requestConstructor = requestClass.GetConstructors()?.FirstOrDefault(c =>
            c.GetParameterTypes()?.Length == 2);
        if (requestConstructor is null)
        {
            Debug.WriteLine("Failed to find ChangesTokenRequest constructor");
            return null;
        }

        requestConstructor.Accessible = true;
        var emptySet = new Java.Util.HashSet();
        var request = requestConstructor.NewInstance(recordTypesSet, emptySet);
        if (request is null)
        {
            Debug.WriteLine("Failed to create ChangesTokenRequest");
            return null;
        }

        var (clientClass, clientObject) = healthConnectClient.GetJniClientObjects();
        if (clientClass is null || clientObject is null)
        {
            return null;
        }

        var result = await InvokeKotlinSuspendMethod(clientClass, clientObject, "getChangesToken", request);

        return result?.ToString();
    }

    /// <summary>
    /// Gets changes from Health Connect since the provided token.
    /// </summary>
    internal static async Task<HealthChangesResult?> GetHealthChanges(
        this IHealthConnectClient healthConnectClient,
        string token)
    {
        // No try/catch: exceptions propagate to HealthService.GetChanges which wraps them in
        // ChangesReadResult.ErrorException. Null returns below mean reflection could not
        // bind — caller treats those as "token invalid or platform mismatch."
        var (clientClass, clientObject) = healthConnectClient.GetJniClientObjects();
            if (clientClass is null || clientObject is null)
            {
                return null;
            }

            var tokenObj = new Java.Lang.String(token);
            var result = await InvokeKotlinSuspendMethod(clientClass, clientObject, "getChanges", tokenObj);
            if (result is null)
            {
                return null;
            }

            var changes = new List<HealthChange>();

            var changesList = ChangesResponseReflection.GetChanges.Invoke(result);
            if (changesList is Java.Util.IList javaChangesList)
            {
                for (int i = 0; i < javaChangesList.Size(); i++)
                {
                    var change = javaChangesList.Get(i);
                    if (change is null)
                    {
                        continue;
                    }

                    var className = change.Class?.Name ?? "";

                    if (className.Contains("UpsertionChange"))
                    {
                        var record = UpsertionChangeReflection.GetRecord.Invoke(change);
                        if (record is null)
                        {
                            continue;
                        }
                        var metadata = RecordReflection.GetMetadata.Invoke(record);
                        var recordId = metadata is null
                            ? null
                            : MetadataReflection.GetId.Invoke(metadata)?.ToString();

                        if (recordId is not null)
                        {
                            changes.Add(new HealthChange
                            {
                                Type = HealthChangeType.Upsert,
                                RecordId = recordId
                            });
                        }
                    }
                    else if (className.Contains("DeletionChange"))
                    {
                        var deletedId = DeletionChangeReflection.GetDeletedRecordId.Invoke(change)?.ToString();
                        if (deletedId is not null)
                        {
                            changes.Add(new HealthChange
                            {
                                Type = HealthChangeType.Deletion,
                                RecordId = deletedId
                            });
                        }
                    }
                }
            }

            string? nextToken = ChangesResponseReflection.GetNextChangesToken.Invoke(result)?.ToString();

            bool hasMore = false;
            var hasMoreResult = ChangesResponseReflection.GetHasMore.Invoke(result);
            if (hasMoreResult is Java.Lang.Boolean jBool)
            {
                hasMore = jBool.BooleanValue();
            }

            return new HealthChangesResult
            {
                Changes = changes,
                NextToken = nextToken ?? token,
                HasMore = hasMore
            };
    }

    /// <summary>
    /// Gets the JNI client class and object from the Health Connect client via reflection.
    /// Shared helper to avoid duplicating this pattern across methods.
    /// </summary>
    private static (Java.Lang.Class? ClientClass, Java.Lang.Object? ClientObject) GetJniClientObjects(
        this IHealthConnectClient healthConnectClient)
    {
        var jniHandle = healthConnectClient.Handle;
        if (jniHandle == IntPtr.Zero)
        {
            // Fallback: try private field reflection for older bindings
            var clientType = healthConnectClient.GetType();
            var handleField = clientType.GetField(JniHandleFieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (handleField is null)
            {
                Debug.WriteLine("Could not find handle field via reflection");
                return (null, null);
            }

            var handle = handleField.GetValue(healthConnectClient);
            if (handle is not IntPtr fallbackHandle || fallbackHandle == IntPtr.Zero)
            {
                Debug.WriteLine("Invalid JNI handle for Health Connect client");
                return (null, null);
            }

            jniHandle = fallbackHandle;
        }

        var classHandle = JNIEnv.GetObjectClass(jniHandle);
        var clientClass = Java.Lang.Object.GetObject<Java.Lang.Class>(classHandle, JniHandleOwnership.DoNotTransfer);
        var clientObject = Java.Lang.Object.GetObject<Java.Lang.Object>(jniHandle, JniHandleOwnership.DoNotTransfer);

        return (clientClass, clientObject);
    }

    /// <summary>
    /// Invokes a Kotlin suspend method with one parameter on the Health Connect client.
    /// </summary>
    private static async Task<Java.Lang.Object?> InvokeKotlinSuspendMethod(
        Java.Lang.Class clientClass,
        Java.Lang.Object clientObject,
        string methodName,
        Java.Lang.Object parameter)
    {
        var method = clientClass.GetDeclaredMethods()?.FirstOrDefault(m =>
            m?.Name == methodName && m.GetParameterTypes()?.Length == 2)
            ?? clientClass.GetMethods()?.FirstOrDefault(m =>
                m?.Name == methodName && m.GetParameterTypes()?.Length == 2);

        if (method is null)
        {
            Debug.WriteLine($"Could not find {methodName} method on client");
            return null;
        }

        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
        var continuation = new Continuation(taskCompletionSource, default);

        method.Accessible = true;
        var result = method.Invoke(clientObject, parameter, continuation);

        if (result is Java.Lang.Enum javaEnum)
        {
            var currentState = Enum.Parse<CoroutineState>(javaEnum.ToString());
            if (currentState == CoroutineState.COROUTINE_SUSPENDED)
            {
                result = await taskCompletionSource.Task;
            }
        }

        return result;
    }

    private static async Task<Java.Lang.Object?> InvokeKotlinSuspendMethod(
        Java.Lang.Class clientClass,
        Java.Lang.Object clientObject,
        string methodName,
        Java.Lang.Object parameter1,
        Java.Lang.Object parameter2)
    {
        var method = clientClass.GetDeclaredMethods()?.FirstOrDefault(m =>
            m?.Name == methodName && m.GetParameterTypes()?.Length == 3)
            ?? clientClass.GetMethods()?.FirstOrDefault(m =>
                m?.Name == methodName && m.GetParameterTypes()?.Length == 3);

        if (method is null)
        {
            Debug.WriteLine($"Could not find {methodName} method on client");
            return null;
        }

        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
        var continuation = new Continuation(taskCompletionSource, default);

        method.Accessible = true;
        var result = method.Invoke(clientObject, parameter1, parameter2, continuation);

        if (result is Java.Lang.Enum javaEnum)
        {
            var currentState = Enum.Parse<CoroutineState>(javaEnum.ToString());
            if (currentState == CoroutineState.COROUTINE_SUSPENDED)
            {
                result = await taskCompletionSource.Task;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a static aggregate metric field from a record class via reflection.
    /// </summary>
    private static Java.Lang.Object? GetAggregateMetric(string recordClassName, string metricFieldName)
    {
        var recordClass = JavaClassResolver.Resolve(recordClassName);
        if (recordClass is null)
        {
            Debug.WriteLine($"Failed to find record class: {recordClassName}");
            return null;
        }

        var metricField = recordClass.GetDeclaredField(metricFieldName);
        if (metricField is null)
        {
            Debug.WriteLine($"Failed to find metric field: {metricFieldName}");
            return null;
        }

        metricField.Accessible = true;
        var metric = metricField.Get(null);
        if (metric is null)
        {
            Debug.WriteLine($"Metric field {metricFieldName} is null");
            return null;
        }

        return metric;
    }

    /// <summary>
    /// Creates an AggregateRequest with a single metric and time range filter.
    /// </summary>
    private static Java.Lang.Object? CreateAggregateRequest(Java.Lang.Object metric, TimeRangeFilter timeRangeFilter)
    {
        var metricsSet = new Java.Util.HashSet();
        metricsSet.Add(metric);

        var emptySet = new Java.Util.HashSet();

        var request = AggregateRequestReflection.Constructor
            .NewInstance(metricsSet, timeRangeFilter, emptySet);
        if (request is null)
        {
            Debug.WriteLine("Failed to create AggregateRequest");
        }

        return request;
    }

    /// <summary>
    /// Extracts the aggregated value from an AggregationResult using the get() method.
    /// Returns null when the bucket has no recorded data for the metric — this is the
    /// expected case for empty buckets and is distinct from a reflection-resolution failure
    /// (which would throw).
    /// </summary>
    private static Java.Lang.Object? ExtractAggregateValue(Java.Lang.Object aggregationResult, Java.Lang.Object metric)
        => AggregationResultReflection.Get.Invoke(aggregationResult, metric);

    /// <summary>
    /// Extracts contributing apps' package names from an AggregationResult.
    /// Calls AggregationResult.getDataOrigins() → Set&lt;DataOrigin&gt;, then DataOrigin.getPackageName() on each.
    /// </summary>
    private static List<string> ExtractDataOrigins(Java.Lang.Object aggregationResult)
    {
        var dataOrigins = new List<string>();

        var originsObj = AggregationResultReflection.GetDataOrigins.Invoke(aggregationResult);
        if (originsObj is not Java.Util.ISet originsSet)
        {
            return dataOrigins;
        }

        var iterator = originsSet.Iterator();
        while (iterator.HasNext)
        {
            var origin = iterator.Next();
            if (origin is null)
            {
                continue;
            }

            var packageName = DataOriginReflection.GetPackageName.Invoke((Java.Lang.Object)origin);
            if (packageName is not null)
            {
                dataOrigins.Add(packageName.ToString());
            }
        }

        return dataOrigins;
    }
}
