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

    /// <summary>
    /// Creates a unit object (Mass, Length, Energy) using Kotlin Companion factory method.
    /// </summary>
    public static T? CreateUnitViaCompanion<T>(string className, string factoryMethodName, double value) where T : Java.Lang.Object
    {
        try
        {
            var unitClass = Java.Lang.Class.ForName(className);
            if (unitClass is null)
            {
                Debug.WriteLine($"Failed to find class {className}");
                return default;
            }

            var companionField = unitClass.GetDeclaredField(KotlinCompanionFieldName);
            if (companionField is null)
            {
                Debug.WriteLine($"Failed to find Companion field in {className}");
                return default;
            }

            companionField.Accessible = true;
            var companion = companionField.Get(null);
            if (companion is null)
            {
                Debug.WriteLine($"Failed to get Companion instance from {className}");
                return default;
            }

            var factoryMethod = companion.Class?.GetDeclaredMethod(factoryMethodName, Java.Lang.Double.Type!);
            if (factoryMethod is null)
            {
                Debug.WriteLine($"Failed to find factory method {factoryMethodName} in {className}");
                return default;
            }

            factoryMethod.Accessible = true;
            var result = factoryMethod.Invoke(companion, Java.Lang.Double.ValueOf(value));
            if (result is null)
            {
                Debug.WriteLine($"Factory method {factoryMethodName} returned null for value {value}");
                return default;
            }

            return Java.Lang.Object.GetObject<T>(result.Handle, JniHandleOwnership.DoNotTransfer);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating {typeof(T).Name} via Companion.{factoryMethodName}({value}): {ex.Message}");
            return default;
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
            var unitClass = Java.Lang.Class.ForName(fullClassName);

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
    internal static async Task<bool> InsertRecord(this IHealthConnectClient healthConnectClient, Java.Lang.Object record)
    {
        try
        {
            // Wrap the Java object as IRecord using the JNI handle to avoid managed cast issues
            var irecord = Java.Lang.Object.GetObject<AndroidX.Health.Connect.Client.Records.IRecord>(
                record.Handle, JniHandleOwnership.DoNotTransfer);

            if (irecord is null)
            {
                Debug.WriteLine("Could not wrap record as IRecord");
                return false;
            }

            var recordsList = new System.Collections.Generic.List<AndroidX.Health.Connect.Client.Records.IRecord> { irecord };

            var response = await KotlinResolver.Process<InsertRecordsResponse, System.Collections.Generic.IList<AndroidX.Health.Connect.Client.Records.IRecord>>(
                healthConnectClient.InsertRecords, recordsList);

            return response is not null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error inserting record: {ex.Message}");
            return false;
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
        try
        {
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading records: {ex.Message}");
            return null;
        }
    }
#pragma warning restore CA1416

    internal static async Task<Java.Lang.Object?> ReadHealthRecord(
        this IHealthConnectClient healthConnectClient,
        IKClass recordClass,
        string recordId)
    {
        try
        {
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

            var getRecordMethod = result.Class.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == "getRecord")
                ?? result.Class.GetMethods()?.FirstOrDefault(m => m?.Name == "getRecord");
            if (getRecordMethod is null)
            {
                return null;
            }

            getRecordMethod.Accessible = true;
            return getRecordMethod.Invoke(result) as Java.Lang.Object;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading record by ID: {ex.Message}");
            return null;
        }
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
        try
        {
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error aggregating health records: {ex.Message}");
            return (null, []);
        }
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
        try
        {
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

            var requestClass = Java.Lang.Class.ForName(Reflection.AggregateGroupByDurationRequestClassName);
            if (requestClass is null)
            {
                Debug.WriteLine("Failed to find AggregateGroupByDurationRequest class");
                return [];
            }

            // Constructor: AggregateGroupByDurationRequest(Set<AggregateMetric>, TimeRangeFilter, Duration, Set<DataOrigin>)
            var requestConstructor = requestClass.GetConstructors()?.FirstOrDefault(c =>
                c.GetParameterTypes()?.Length == 4);
            if (requestConstructor is null)
            {
                Debug.WriteLine("Failed to find AggregateGroupByDurationRequest constructor");
                return [];
            }

            requestConstructor.Accessible = true;
            var request = requestConstructor.NewInstance(metricsSet, timeRangeFilter, duration, emptySet);
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

                var startInstant = InvokeAccessibleMethod(item, "getStartTime") as Java.Time.Instant;
                var endInstant = InvokeAccessibleMethod(item, "getEndTime") as Java.Time.Instant;

                // Get the AggregationResult from the grouped item
                var getResultMethod = item.Class?.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == "getResult")
                    ?? item.Class?.GetMethods()?.FirstOrDefault(m => m?.Name == "getResult");
                if (getResultMethod is null)
                {
                    continue;
                }

                getResultMethod.Accessible = true;
                var aggregationResult = getResultMethod.Invoke(item);
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error aggregating health records by duration: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Gets a changes token from Health Connect for tracking data changes.
    /// </summary>
    internal static async Task<string?> GetHealthChangesToken(
        this IHealthConnectClient healthConnectClient,
        IList<IKClass> recordTypes)
    {
        try
        {
            var recordTypesSet = new Java.Util.HashSet();
            foreach (var recordType in recordTypes)
            {
                var recordTypeObj = Java.Lang.Object.GetObject<Java.Lang.Object>(recordType.Handle, JniHandleOwnership.DoNotTransfer);
                if (recordTypeObj is not null)
                {
                    recordTypesSet.Add(recordTypeObj);
                }
            }

            var requestClass = Java.Lang.Class.ForName(Reflection.ChangesTokenRequestClassName);
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting changes token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets changes from Health Connect since the provided token.
    /// </summary>
    internal static async Task<HealthChangesResult?> GetHealthChanges(
        this IHealthConnectClient healthConnectClient,
        string token)
    {
        try
        {
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

            // Extract changes list
            var getChangesMethod = result.Class?.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == "getChanges")
                ?? result.Class?.GetMethods()?.FirstOrDefault(m => m?.Name == "getChanges");
            if (getChangesMethod is not null)
            {
                getChangesMethod.Accessible = true;
                var changesList = getChangesMethod.Invoke(result);

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
                            var getRecordMethod = change.Class?.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == "getRecord")
                                ?? change.Class?.GetMethods()?.FirstOrDefault(m => m?.Name == "getRecord");
                            if (getRecordMethod is not null)
                            {
                                getRecordMethod.Accessible = true;
                                var record = getRecordMethod.Invoke(change);
                                var metadataField = record?.Class?.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == "getMetadata")
                                    ?? record?.Class?.GetMethods()?.FirstOrDefault(m => m?.Name == "getMetadata");
                                string? recordId = null;

                                if (metadataField is not null)
                                {
                                    metadataField.Accessible = true;
                                    var metadata = metadataField.Invoke(record);
                                    var idMethod = metadata?.Class?.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == "getId")
                                        ?? metadata?.Class?.GetMethods()?.FirstOrDefault(m => m?.Name == "getId");
                                    if (idMethod is not null)
                                    {
                                        idMethod.Accessible = true;
                                        recordId = idMethod.Invoke(metadata)?.ToString();
                                    }
                                }

                                if (recordId is not null)
                                {
                                    changes.Add(new HealthChange
                                    {
                                        Type = HealthChangeType.Upsert,
                                        RecordId = recordId
                                    });
                                }
                            }
                        }
                        else if (className.Contains("DeletionChange"))
                        {
                            var getIdMethod = change.Class?.GetDeclaredMethods()?.FirstOrDefault(m =>
                                m?.Name == "getDeletedRecordId" || m?.Name == "getRecordId")
                                ?? change.Class?.GetMethods()?.FirstOrDefault(m =>
                                    m?.Name == "getDeletedRecordId" || m?.Name == "getRecordId");
                            if (getIdMethod is not null)
                            {
                                getIdMethod.Accessible = true;
                                var deletedId = getIdMethod.Invoke(change)?.ToString();
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
                }
            }

            // Extract next token
            var getNextTokenMethod = result.Class?.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == "getNextChangesToken")
                ?? result.Class?.GetMethods()?.FirstOrDefault(m => m?.Name == "getNextChangesToken");
            string? nextToken = null;
            if (getNextTokenMethod is not null)
            {
                getNextTokenMethod.Accessible = true;
                nextToken = getNextTokenMethod.Invoke(result)?.ToString();
            }

            // Extract hasMore
            var hasMoreMethod = result.Class?.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == "getHasMore" || m?.Name == "hasMore")
                ?? result.Class?.GetMethods()?.FirstOrDefault(m => m?.Name == "getHasMore" || m?.Name == "hasMore");
            bool hasMore = false;
            if (hasMoreMethod is not null)
            {
                hasMoreMethod.Accessible = true;
                var hasMoreResult = hasMoreMethod.Invoke(result);
                if (hasMoreResult is Java.Lang.Boolean jBool)
                {
                    hasMore = jBool.BooleanValue();
                }
            }

            return new HealthChangesResult
            {
                Changes = changes,
                NextToken = nextToken ?? token,
                HasMore = hasMore
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting health changes: {ex.Message}");
            return null;
        }
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
        var recordClass = Java.Lang.Class.ForName(recordClassName);
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

        var requestClass = Java.Lang.Class.ForName(Reflection.AggregateRequestClassName);
        if (requestClass is null)
        {
            Debug.WriteLine("Failed to find AggregateRequest class");
            return null;
        }

        // Constructor: AggregateRequest(Set<AggregateMetric>, TimeRangeFilter, Set<DataOrigin>)
        var requestConstructor = requestClass.GetConstructors()?.FirstOrDefault(c =>
            c.GetParameterTypes()?.Length == 3);
        if (requestConstructor is null)
        {
            Debug.WriteLine("Failed to find AggregateRequest constructor");
            return null;
        }

        requestConstructor.Accessible = true;
        var request = requestConstructor.NewInstance(metricsSet, timeRangeFilter, emptySet);
        if (request is null)
        {
            Debug.WriteLine("Failed to create AggregateRequest");
        }

        return request;
    }

    /// <summary>
    /// Extracts the aggregated value from an AggregationResult using the get() method.
    /// </summary>
    private static Java.Lang.Object? ExtractAggregateValue(Java.Lang.Object aggregationResult, Java.Lang.Object metric)
    {
        var aggregateMetricClass = Java.Lang.Class.ForName(Reflection.AggregateMetricClassName);
        if (aggregateMetricClass is null)
        {
            Debug.WriteLine("Failed to find AggregateMetric class");
            return null;
        }

        var getMethod = aggregationResult.Class?.GetDeclaredMethod("get", aggregateMetricClass);

        // Fallback: search all methods named "get" with one parameter
        getMethod ??= aggregationResult.Class?.GetDeclaredMethods()?.FirstOrDefault(m =>
            m?.Name == "get" && m.GetParameterTypes()?.Length == 1);

        if (getMethod is null)
        {
            Debug.WriteLine("Could not find get method on AggregationResult");
            return null;
        }

        getMethod.Accessible = true;
        return getMethod.Invoke(aggregationResult, metric);
    }

    /// <summary>
    /// Extracts data origins (contributing app package names) from an AggregationResult.
    /// Calls AggregationResult.getDataOrigins() → Set&lt;DataOrigin&gt;, then DataOrigin.getPackageName() on each.
    /// </summary>
    private static List<string> ExtractDataOrigins(Java.Lang.Object aggregationResult)
    {
        var dataOrigins = new List<string>();

        var originsObj = InvokeAccessibleMethod(aggregationResult, "getDataOrigins");
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

            var packageName = InvokeAccessibleMethod((Java.Lang.Object)origin, "getPackageName");
            if (packageName is not null)
            {
                dataOrigins.Add(packageName.ToString());
            }
        }

        return dataOrigins;
    }

    /// <summary>
    /// Finds and invokes a no-arg method on a Java object, setting it accessible first.
    /// </summary>
    private static Java.Lang.Object? InvokeAccessibleMethod(Java.Lang.Object obj, string methodName)
    {
        var method = obj.Class?.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == methodName)
            ?? obj.Class?.GetMethods()?.FirstOrDefault(m => m?.Name == methodName);
        if (method is null)
        {
            return null;
        }

        method.Accessible = true;
        return method.Invoke(obj);
    }
}
