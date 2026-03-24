using Android.Content;
using AndroidX.Health.Connect.Client;
using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ContextExtensions
{
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
    internal static Result<SdkStatus> CheckSdkAvailability(this Context context)
    {
        try
        {
            var availabilityStatus = HealthConnectClient.GetSdkStatus(context);
            if (availabilityStatus == (int)SdkStatus.SdkAvailable)
            {
                return new();
            }

            return new Result<SdkStatus>()
            {
                Error = (SdkStatus)availabilityStatus
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                ErrorException = ex
            };
        }
    }

    /// <summary>
    /// Opens the Google Play Store to the Health Connect app page for updating.
    /// Used when Health Connect is installed but needs an update.
    /// </summary>
    /// <param name="context">The Android context</param>
    internal static void OpenHealthConnectInPlayStore(this Context context)
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
