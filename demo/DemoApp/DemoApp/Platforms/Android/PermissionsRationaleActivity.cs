using Android.App;
using Android.OS;

namespace DemoApp;

/// <summary>
/// Activity required by Health Connect to show the rationale for health permissions.
/// This is launched when the user clicks the privacy policy link in Health Connect settings.
/// </summary>
[Activity(Exported = true)]
public class PermissionsRationaleActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // In a production app, you should display your privacy policy here.
        // For the demo, we simply close the activity.
        Finish();
    }
}
