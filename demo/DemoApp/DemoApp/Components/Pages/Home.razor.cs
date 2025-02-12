using Microsoft.AspNetCore.Components;

#if ANDROID
using Maui.Health.Services;
#endif

namespace DemoApp.Components.Pages;

public partial class Home
{
#if ANDROID
    [Inject] public HealthService _healthService { get; set; }
#endif

    private long _steps { get; set; } = 0;

    protected override async Task OnInitializedAsync()
    {
#if ANDROID
        var steps = await _healthService.GetStepsTodayAsync();
        if (steps.HasValue)
        {
            _steps = steps.Value;
        }
#endif
    }
}
