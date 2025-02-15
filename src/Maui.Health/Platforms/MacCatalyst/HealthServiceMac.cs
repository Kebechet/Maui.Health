using Maui.Health.Models;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => false;

    public partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RequestPermissionResult());
    }

    public partial Task<long?> GetStepsTodayAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long?)0);
    }
}
