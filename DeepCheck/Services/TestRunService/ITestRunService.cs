using DeepCheck.DTOs;

namespace DeepCheck.Services.TestRunService;

public interface ITestRunService
{
    Task<List<TestRunInfo?>> GetLatestResultByTestAsync(CancellationToken cancellationToken = default);
    Task<TestRunInfo?> ExecuteTestByNameAsync(string testName, CancellationToken cancellationToken = default);
    Task<IEnumerable<TestRunInfo>?> QueryTestRunByNameAsync(string testName, DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TestStepRunInfo>?> GetAllTestRunsStepsAsync(CancellationToken cancellationToken = default);

    Task<TestStepRunInfo?> GetLastStepAsync(string testName, string stepName,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<FailedTestInfo>> GetFailedTestRunsAsync(CancellationToken cancellationToken = default);
}
