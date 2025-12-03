using Android.Runtime;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static Maui.Health.Platforms.Android.AndroidConstants;

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
}
