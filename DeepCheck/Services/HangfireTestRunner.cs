using DeepCheck.DTOs;
using DeepCheck.Helpers;
using DeepCheck.Interfaces;
using DeepCheck.Models;
using Hangfire;
using Hangfire.Storage;

namespace DeepCheck.Services;

/// <summary>
/// HangfireExclusiveTestRunner is a wrapper around ITestRunner that ensures that only one test per test name is executed at a time.
/// </summary>
public class HangfireExclusiveTestRunner : ITestRunner
{
    private readonly ITestRunner testRunner;
    private readonly ILogger<HangfireExclusiveTestRunner> logger;
    private readonly IEnumerable<ITest> tests;

    public HangfireExclusiveTestRunner(
        ITestRunner testRunner,
        ILogger<HangfireExclusiveTestRunner> logger,
        IEnumerable<ITest> tests)
    {
        this.testRunner = testRunner;
        this.logger = logger;
        this.tests = tests;
    }

    public async Task<TestRunInfo?> ExecuteTestByName(string name, RunMethodEnum runMethod = RunMethodEnum.Manual, CancellationToken cancellationToken = default)
    {
        var lockKey = $"test-lock-{name}";
        try
        {
            using var lockHandle = JobStorage.Current.GetConnection().AcquireDistributedLock(lockKey, TimeSpan.Zero);
            return await testRunner.ExecuteTestByName(name, runMethod, cancellationToken);
        }
        catch (DistributedLockTimeoutException ex)
        {
            logger.LogWarning(ex, "Test {TestName} is already running!", name);

            var testRunBuilder = new TestRunInfoBuilder(tests.First(t => t.TestDefinition.TestName == name).TestDefinition);
            testRunBuilder.FailStep("Test is already running");
            return testRunBuilder.FinishTest();
        }
    }
}
