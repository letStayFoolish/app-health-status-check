namespace DeepCheck.Services.JobCleanup;

public interface IJobCleanupService
{
    /// <summary>
    /// Cleans up stale jobs older than specified date
    /// </summary>
    /// <param name="olderThan"></param>
    /// <returns></returns>
    public Task CleanupAsync(DateTime olderThan);
}
