using Microsoft.AspNetCore.Components;

using Maui.Health.Services;

namespace DemoApp.Components.Pages;

public partial class Home
{
    [Inject] public IHealthService _healthService { get; set; }

    private long _steps { get; set; } = 0;

    protected override async Task OnInitializedAsync()
    {
        var steps = await _healthService.GetStepsTodayAsync();
        if (steps.HasValue)
        {
            _steps = steps.Value;
        }
    }
}
