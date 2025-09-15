using DeepCheck.DTOs;
using DeepCheck.Entities;

namespace DeepCheck.Repositories;

public interface ITestRepository
{
    // Save Result To Database
    public Task AddTestResultAsync(TestRun testModel);

    // Fetching Test Definitions
    public Task<IEnumerable<TestRunInfo>> GetAllTestRunsAsync(CancellationToken cancellationToken);

    // Fetch failed Test Runs
    public Task<IEnumerable<FailedTestInfo>> GetFailedTestRunsAsync(CancellationToken cancellationToken);

    //
    public Task<IEnumerable<TestStepRunInfo>> GetTestStepsAsync(CancellationToken cancellationToken = default);

    // Fetching Test Runs
    public Task<IReadOnlyList<TestRunInfo>> GetFilteredTestRunsAsync(string? testName, DateTime? from, DateTime? to,
        CancellationToken cancellationToken, int take = 100, int skip = 0);

    public Task<UptimeTestRunInfo> GetUptimeTestRunInfoAsync(int countPerTestStepName, CancellationToken cancellationToken = default);

    // Fetch Step By Test Name/Step Name
    public Task<TestStepRunInfo?> GetLastStepAsync(string testName, string stepName, CancellationToken cancellationToken = default);

    // Remove test records older than specified date
    public Task RemoveOldSuccessfulTestRunsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    // Remove (all) failed test records older than specified date (7 days)
    public Task RemoveOldFailedTestRunsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
