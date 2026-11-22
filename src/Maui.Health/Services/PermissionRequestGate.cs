namespace Maui.Health.Services;

/// <summary>
/// Runs permission requests one at a time. Parallel requests on Android each launched their own request: one screen
/// showed and the rest stayed on top of the app as invisible activities that swallowed every tap. Queued, a later
/// request asks only for what the earlier one left missing.
/// </summary>
internal sealed class PermissionRequestGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Runs <paramref name="request"/> once every earlier request has finished. The caller's token stops the waiting,
    /// never the request: a permission screen cannot be withdrawn, so the gate stays held until
    /// <paramref name="request"/> finishes even after the caller has stopped waiting, and the next request cannot
    /// stack a second screen on top of the first.
    /// </summary>
    /// <returns>
    /// The result of <paramref name="request"/>, or <paramref name="createCancelledResult"/>'s when the token fired
    /// first, whether while queued or while the request ran.
    /// </returns>
    internal async Task<TResult> Run<TResult>(Func<Task<TResult>> request, Func<TResult> createCancelledResult, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return createCancelledResult();
        }

        Task<TResult> requestTask;
        try
        {
            requestTask = request();
        }
        catch
        {
            _semaphore.Release();
            throw;
        }

        _ = requestTask.ContinueWith(
            _ => _semaphore.Release(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        try
        {
            return await requestTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return createCancelledResult();
        }
    }
}
