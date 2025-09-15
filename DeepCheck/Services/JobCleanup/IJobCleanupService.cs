namespace DeepCheck.Services.JobCleanup;

public interface IJobCleanupService
{
    /// <summary>
    /// Cleans up stale jobs older than specified date
    /// </summary>
    /// <param name="successfulTestsOlderThan"></param>
    /// <param name="failedTestsOlderThan"></param>
    /// <returns></returns>
    public Task CleanupAsync(DateTime successfulTestsOlderThan, DateTime failedTestsOlderThan);
}
