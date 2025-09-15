namespace DeepCheck.Services.JobCleanup;

using Repositories;

public class JobCleanupService : IJobCleanupService
{
    private readonly ITestRepository _testRepository;

    public JobCleanupService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task CleanupAsync(DateTime successfulTestsOlderThan, DateTime failedTestsOlderThan)
    {
        await _testRepository.RemoveOldSuccessfulTestRunsAsync(successfulTestsOlderThan);
        await _testRepository.RemoveOldFailedTestRunsAsync(failedTestsOlderThan);
    }
}
