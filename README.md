[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/kebechet)

# Maui.Health
![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Maui.Health)
![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Maui.Health)

Abstraction around `Android Health Connect` and `iOS HealthKit`
⚠️ Beware, this package is currently just as **Proof of concept**. There is a lot of work required for proper stability and ease of use.
[Issues](https://github.com/Kebechet/Maui.Health/issues) will contain future tasks that should be implemented.

Feel free to contribute ❤️

## Usage
Firstly register package installer in your `MauiProgram.cs`
```csharp
 builder.Services.AddHealth();
```

Then setup all [Android and iOS necessities](https://github.com/Kebechet/Maui.Health/commit/139e69fade83f9133044910e47ad530f040b8021).
- Android (4) [docs](https://developer.android.com/jetpack/androidx/releases/health-connect), [docs2](https://learn.microsoft.com/en-us/dotnet/api/healthkit?view=xamarin-ios-sdk-12)
    - in Google Play console give [Health permissions to the app](https://support.google.com/googleplay/android-developer/answer/14738291?hl=en)
    - for successful app approval your Policy page must contain `Health data collection and use`, `Data retention policy`
    - change of `AndroidManifest.xml` + new activity showing [privacy policy](https://developer.android.com/health-and-fitness/guides/health-connect/develop/get-started#show-privacy-policy)
    - change of min. Android version to v26
- iOS (3)  [docs](https://learn.microsoft.com/en-us/previous-versions/xamarin/ios/platform/healthkit), [docs2](https://developer.apple.com/documentation/healthkit)
    - generating new provisioning profile containing HealthKit permissions. These permissions are changed in [Identifiers](https://developer.apple.com/account/resources/identifiers/list)
    - adding `Entitlements.plist`
    - adjustment of `Info.plist`
      -  ⚠️ Beware, if your app already exists and targets various devices adding `UIRequiredDeviceCapabilities` with `healthkit` can get your [release rejected](https://developer.apple.com/library/archive/qa/qa1623/_index.html). For that reason I ommited adding this requirement and I just make sure that I check if the device is capable of using `healthkit`.


After you have everzything setup correctly you can use `IHealthService` from DI container and call it's methods.
If you want an example there is a DemoApp project showing number of steps for Current day

## TIP
While you test your workflows on iOS your device sometimes doesnt have data necessary (e.g. number of steps is 0). In that case you can open `Health` app -> find Steps -> click on it -> and in right top corner is `Add data`. This way you can manually add some testing data

## Credits
- @aritchie - `https://github.com/shinyorg/Health`
- @0xc3u - `https://github.com/0xc3u/Plugin.Maui.Health`
- @EagleDelux - `https://github.com/EagleDelux/androidx.health-connect-demo-.net-maui`
- @b099l3 - `https://github.com/b099l3/ios-samples/tree/65a4ab1606cfd8beb518731075e4af526c4da4ad/ios8/Fit/Fit`

## Other sources
- https://pub.dev/packages/health
