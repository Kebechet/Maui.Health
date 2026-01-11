using Android.Content;
using Android.Runtime;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Response;
using AndroidX.Health.Connect.Client.Time;
using Java.Time;
using Kotlin.Reflect;
using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android.Callbacks;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
            var inUnitMethod = objClass?.GetDeclaredMethods()?.FirstOrDefault(m =>
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

            return TryConvertToDouble(result, out value);
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

            return TryConvertToDouble(fieldValue, out value);
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
            var method = objClass?.GetDeclaredMethod(methodName);

            if (method is null)
            {
                return false;
            }

            method.Accessible = true;
            var result = method.Invoke(obj);

            return TryConvertToDouble(result, out value);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a double value from a string using regex.
    /// </summary>
    public static bool TryParseFromString(this string? stringValue, out double value)
    {
        value = 0;

        if (string.IsNullOrEmpty(stringValue))
        {
            return false;
        }

        var match = Regex.Match(stringValue, Reflection.NumberExtractionPattern);

        if (!match.Success)
        {
            return false;
        }

        return double.TryParse(match.Groups[1].Value, out value);
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

            var factoryMethod = companion.Class?.GetDeclaredMethod(factoryMethodName, Java.Lang.Double.Type);
            if (factoryMethod is null)
            {
                Debug.WriteLine($"Failed to find factory method {factoryMethodName} in {className}");
                return default;
            }

            factoryMethod.Accessible = true;
            var result = factoryMethod.Invoke(companion, new Java.Lang.Double(value));
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

    private static bool TryConvertToDouble(Java.Lang.Object? obj, out double value)
    {
        value = 0;

        if (obj is null)
        {
            return false;
        }

        // Use Java.Lang.Number base class for unified conversion
        if (obj is Java.Lang.Number javaNumber)
        {
            value = javaNumber.DoubleValue();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Inserts a record into Health Connect using reflection to call the Kotlin suspend function.
    /// </summary>
    /// <param name="healthConnectClient">The Health Connect client instance</param>
    /// <param name="record">The record to insert</param>
    /// <returns>True if the record was inserted successfully, false otherwise</returns>
    internal static async Task<bool> InsertRecord(this IHealthConnectClient healthConnectClient, Java.Lang.Object record)
    {
        try
        {
            var recordsList = new Java.Util.ArrayList();
            recordsList.Add(record);

            var clientType = healthConnectClient.GetType();
            var handleField = clientType.GetField(JniHandleFieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (handleField is null)
            {
                Debug.WriteLine("Could not find handle field via reflection");
                return false;
            }

            var handle = handleField.GetValue(healthConnectClient);
            if (handle is not IntPtr jniHandle || jniHandle == IntPtr.Zero)
            {
                Debug.WriteLine("Invalid JNI handle for Health Connect client");
                return false;
            }

            var classHandle = JNIEnv.GetObjectClass(jniHandle);
            var clientClass = Java.Lang.Object.GetObject<Java.Lang.Class>(classHandle, JniHandleOwnership.DoNotTransfer);
            var clientObject = Java.Lang.Object.GetObject<Java.Lang.Object>(jniHandle, JniHandleOwnership.DoNotTransfer);

            var insertMethod = clientClass?.GetDeclaredMethod("insertRecords",
                Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                Java.Lang.Class.FromType(typeof(Kotlin.Coroutines.IContinuation)));

            if (insertMethod is null || clientObject is null)
            {
                Debug.WriteLine("Could not find insertRecords method or client object");
                return false;
            }

            var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
            var continuation = new Continuation(taskCompletionSource, default);

            insertMethod.Accessible = true;
            var result = insertMethod.Invoke(clientObject, recordsList, continuation);

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
    internal static async Task<bool> DeleteWorkoutRecord(
        this IHealthConnectClient healthConnectClient,
        string workoutId)
    {
        try
        {
            var recordIdsList = new Java.Util.ArrayList();
            recordIdsList.Add(workoutId);

            var clientType = healthConnectClient.GetType();
            var handleField = clientType.GetField(JniHandleFieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (handleField is null)
            {
                Debug.WriteLine("Could not find handle field via reflection");
                return false;
            }

            var handle = handleField.GetValue(healthConnectClient);
            if (handle is not IntPtr jniHandle || jniHandle == IntPtr.Zero)
            {
                Debug.WriteLine("Invalid JNI handle for Health Connect client");
                return false;
            }

            var classHandle = JNIEnv.GetObjectClass(jniHandle);
            var clientClass = Java.Lang.Object.GetObject<Java.Lang.Class>(classHandle, JniHandleOwnership.DoNotTransfer);
            var clientObject = Java.Lang.Object.GetObject<Java.Lang.Object>(jniHandle, JniHandleOwnership.DoNotTransfer);

            var recordClass = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                Java.Lang.Class.FromType(typeof(AndroidX.Health.Connect.Client.Records.ExerciseSessionRecord)));

            var deleteMethod = clientClass?.GetDeclaredMethod("deleteRecords",
                Java.Lang.Class.FromType(typeof(IKClass)),
                Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                Java.Lang.Class.FromType(typeof(Kotlin.Coroutines.IContinuation)));

            if (deleteMethod is null || clientObject is null)
            {
                Debug.WriteLine("Could not find deleteRecords method or client object");
                return false;
            }

            var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
            var continuation = new Continuation(taskCompletionSource, default);

            deleteMethod.Accessible = true;
            var emptyList = new Java.Util.ArrayList();

            var recordClassObj = Java.Lang.Object.GetObject<Java.Lang.Object>(recordClass.Handle, JniHandleOwnership.DoNotTransfer);

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
    /// Checks if the Health Connect SDK is available on the device.
    /// </summary>
    /// <remarks>
    /// The Health Connect SDK supports Android 8 (API level 26) or higher, while the Health Connect app
    /// is only compatible with Android 9 (API level 28) or higher. This means that third-party apps can
    /// support users with Android 8, but only users with Android 9 or higher can use Health Connect.
    /// See: https://developer.android.com/health-and-fitness/guides/health-connect/develop/get-started
    /// </remarks>
    /// <param name="context">The Android context</param>
    /// <returns>A Result indicating success or the specific error</returns>
    internal static Result<SdkCheckError> CheckSdkAvailability(Context context)
    {
        try
        {
            var availabilityStatus = HealthConnectClient.GetSdkStatus(context);

            if (availabilityStatus == HealthConnectClient.SdkUnavailable)
            {
                return new() { Error = SdkCheckError.SdkUnavailable };
            }

            if (availabilityStatus == HealthConnectClient.SdkUnavailableProviderUpdateRequired)
            {
                return new() { Error = SdkCheckError.SdkUnavailableProviderUpdateRequired };
            }

            if (!OperatingSystem.IsAndroidVersionAtLeast(MinimumApiVersionRequired))
            {
                return new() { Error = SdkCheckError.AndroidVersionNotSupported };
            }

            return new();
        }
        catch (Exception ex)
        {
            return new() { ErrorException = ex };
        }
    }

    /// <summary>
    /// Opens the Google Play Store to the Health Connect app page for updating.
    /// Used when Health Connect is installed but needs an update.
    /// </summary>
    /// <param name="context">The Android context</param>
    internal static void OpenHealthConnectInPlayStore(Context context)
    {
        var uriString = string.Format(PlayStoreUriTemplate, HealthConnectPackage);

        var intent = new Intent(Intent.ActionView);
        intent.SetPackage(PlayStorePackage);
        intent.SetData(global::Android.Net.Uri.Parse(uriString));
        intent.PutExtra(IntentExtraOverlay, true);
        intent.PutExtra(IntentExtraCaller, context.PackageName);

        context.StartActivity(intent);
    }
}
