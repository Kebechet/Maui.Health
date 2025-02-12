namespace Maui.Health.Services;

public partial class HealthService : IHealthService
{
    public partial bool IsSupported { get; }

    public partial Task<long?> GetStepsTodayAsync(CancellationToken cancellationToken = default);
}
