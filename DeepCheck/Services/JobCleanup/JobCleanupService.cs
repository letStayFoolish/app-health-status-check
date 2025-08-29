namespace DeepCheck.Services.JobCleanup;

using Repositories;

public class JobCleanupService : IJobCleanupService
{
    private readonly ITestRepository _testRepository;

    public JobCleanupService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task CleanupAsync(DateTime olderThan) => await _testRepository.RemoveOldTestRunsAsync(olderThan);
}
