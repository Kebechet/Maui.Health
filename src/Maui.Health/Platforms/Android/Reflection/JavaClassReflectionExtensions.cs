using Java.Lang.Reflect;
using JClass = Java.Lang.Class;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Resolution helpers for the per-class reflection holders. Each method walks the JVM method /
/// constructor table once, sets the resulting handle accessible, and returns it. Holders cache
/// the returned handle in a static field so subsequent calls reuse it for the process lifetime.
/// </summary>
/// <remarks>
/// Resolution failures throw <see cref="MissingMethodException"/>. The Health Connect SDK and
/// these class shapes are fixed at build time, so a missing method/constructor here means the
/// binding doesn't match the platform we're running on — surfacing it as an exception lets the
/// outer service-layer try/catch convert it to a <see cref="Maui.Health.Models.Result"/> error
/// instead of silently masking it as "no data".
/// </remarks>
internal static class JavaClassReflectionExtensions
{
    /// <summary>
    /// Resolves a method by name, preferring declared methods over inherited ones. Mirrors the
    /// existing helper's lookup order — declared first, then public — so behavior matches the
    /// pre-cache code path on cache miss.
    /// </summary>
    public static Method ResolveAccessibleMethod(this JClass cls, string methodName)
    {
        var method = cls.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == methodName)
                  ?? cls.GetMethods()?.FirstOrDefault(m => m?.Name == methodName);
        if (method is null)
        {
            throw new MissingMethodException(
                $"Method '{methodName}' not found on {cls.Name}.");
        }
        method.Accessible = true;
        return method;
    }

    /// <summary>
    /// Resolves a single-parameter method by name and parameter type. Falls back to a
    /// declared-method scan that matches by name + parameter count when the typed lookup misses
    /// (Kotlin generic erasure can hide the typed signature on some SDK versions).
    /// </summary>
    public static Method ResolveAccessibleMethod(this JClass cls, string methodName, JClass paramType)
    {
        var method = cls.GetDeclaredMethod(methodName, paramType)
                  ?? cls.GetDeclaredMethods()?.FirstOrDefault(m =>
                      m?.Name == methodName && m.GetParameterTypes()?.Length == 1);
        if (method is null)
        {
            throw new MissingMethodException(
                $"Method '{methodName}' with parameter '{paramType.Name}' not found on {cls.Name}.");
        }
        method.Accessible = true;
        return method;
    }

    /// <summary>
    /// Resolves a method by trying each candidate name in order; returns the first match.
    /// Used to absorb name drift across SDK versions (e.g. <c>getDeletedRecordId</c> vs
    /// <c>getRecordId</c> on <c>DeletionChange</c>) without leaking that branching to call
    /// sites.
    /// </summary>
    public static Method ResolveAccessibleMethodOneOf(this JClass cls, params string[] candidateNames)
    {
        foreach (var candidate in candidateNames)
        {
            var method = cls.GetDeclaredMethods()?.FirstOrDefault(m => m?.Name == candidate)
                      ?? cls.GetMethods()?.FirstOrDefault(m => m?.Name == candidate);
            if (method is not null)
            {
                method.Accessible = true;
                return method;
            }
        }
        throw new MissingMethodException(
            $"None of [{string.Join(", ", candidateNames)}] found on {cls.Name}.");
    }

    /// <summary>
    /// Resolves the unique constructor with the given parameter count. Mirrors the existing
    /// "first constructor with N parameters" lookup pattern used across the file.
    /// </summary>
    public static Constructor ResolveAccessibleConstructor(this JClass cls, int parameterCount)
    {
        var ctor = cls.GetConstructors()?.FirstOrDefault(c => c?.GetParameterTypes()?.Length == parameterCount);
        if (ctor is null)
        {
            throw new MissingMethodException(
                $"Constructor with {parameterCount} parameters not found on {cls.Name}.");
        }
        ctor.Accessible = true;
        return ctor;
    }
}
