using DeepCheck.DTOs;
using DeepCheck.Entities;

namespace DeepCheck.Repositories;

public interface ITestRepository
{
    // Save Result To Database
    Task AddTestResultAsync(TestRun testModel);

    // Fetching Test Definitions
    Task<IEnumerable<TestRunInfo>> GetAllTestRunsAsync(CancellationToken cancellationToken);

    //
    Task<IEnumerable<TestStepRunInfo>> GetTestStepsAsync(CancellationToken cancellationToken = default);

    // Fetching Test Runs
    Task<IReadOnlyList<TestRunInfo>> GetFilteredTestRunsAsync(string? testName, DateTime? from, DateTime? to,
      CancellationToken cancellationToken, int take = 100, int skip = 0);

    Task<UptimeTestRunInfo> GetUptimeTestRunInfoAsync(int countPerTestStepName, CancellationToken cancellationToken = default);

    // Fetch Step By Test Name/Step Name
    Task<TestStepRunInfo?> GetLastStepAsync(string testName, string stepName, CancellationToken cancellationToken = default);

    // Remove test records older than specified date
    Task RemoveOldTestRunsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
