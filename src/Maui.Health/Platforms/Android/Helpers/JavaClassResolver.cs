namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// Resolves Java classes by fully-qualified name using the app's <c>PathClassLoader</c>
/// instead of the calling thread's context classloader.
/// </summary>
/// <remarks>
/// The 1-arg <see cref="Java.Lang.Class.ForName(string)"/> overload uses the calling
/// thread's context classloader. The Android main thread inherits the app's
/// <c>PathClassLoader</c>, so reflection resolves AndroidX classes correctly there. .NET
/// threadpool threads, however, aren't created via Java's <c>Thread</c> machinery — JNI
/// initializes their context classloader to <c>BootClassLoader</c>, which only sees
/// framework classes. The moment a reflection lookup runs on a non-main thread (e.g.
/// inside <c>Task.Run</c>), every AndroidX class lookup throws <c>ClassNotFoundException</c>.
/// Routing through this helper makes the lib thread-agnostic by pinning the lookup to the
/// app classloader, which can see both framework and AndroidX classes.
/// </remarks>
internal static class JavaClassResolver
{
    private static Java.Lang.ClassLoader? _appClassLoader;

    /// <summary>
    /// The app's <c>PathClassLoader</c>, lazily resolved from <see cref="global::Android.App.Application.Context"/>.
    /// Cached because the value is process-wide and never changes during the app's lifetime.
    /// </summary>
    private static Java.Lang.ClassLoader AppClassLoader
        => _appClassLoader ??= global::Android.App.Application.Context.ClassLoader
            ?? throw new InvalidOperationException(
                $"{nameof(global::Android.App.Application)}.{nameof(global::Android.App.Application.Context)}.{nameof(Java.Lang.ClassLoader)} is null; cannot resolve AndroidX classes off the main thread.");

    /// <summary>
    /// Resolves <paramref name="fullClassName"/> against the app's <c>PathClassLoader</c>.
    /// Safe to call from any thread.
    /// </summary>
    public static Java.Lang.Class? Resolve(string fullClassName)
        => Java.Lang.Class.ForName(fullClassName, initialize: true, AppClassLoader);
}
