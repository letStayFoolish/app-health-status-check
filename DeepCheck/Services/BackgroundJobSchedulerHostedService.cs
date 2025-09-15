using DeepCheck.Interfaces;
using Hangfire;

namespace DeepCheck.Services;

using Helpers;
using JobCleanup;
using Microsoft.Extensions.Options;

public class BackgroundJobSchedulerHostedService : IHostedService
{
    private readonly IRecurringJobManager jobManager;
    private readonly IBackgroundJobClient jobClient;
    private readonly ILogger<BackgroundJobSchedulerHostedService> logger;
    private readonly IEnumerable<ITest> tests;
    private readonly JobCleanupSettings cleanUpsettings;

    public BackgroundJobSchedulerHostedService(
        IRecurringJobManager recurringJobManager,
        IBackgroundJobClient jobClient,
        ILogger<BackgroundJobSchedulerHostedService> logger,
        IEnumerable<ITest> tests,
        IOptions<JobCleanupSettings> config)
    {
        this.jobManager = recurringJobManager;
        this.jobClient = jobClient;
        this.logger = logger;
        this.tests = tests;
        this.cleanUpsettings = config.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var test in tests)
        {
            var testName = test.TestDefinition.TestName;
            var testCronExpression = test.TestDefinition.CronExpression;

            logger.LogInformation("Adding test {TestName} to scheduler with cron {CronExpression}", testName, testCronExpression);

            jobManager.AddOrUpdate<ITestRunner>(testName, t => t.ExecuteTestByName(testName, Models.RunMethodEnum.Scheduled, CancellationToken.None), testCronExpression);

            //Run the test immediately on startup
            jobClient.Enqueue<ITestRunner>(t => t.ExecuteTestByName(testName, Models.RunMethodEnum.Scheduled, CancellationToken.None));
        }

        var now = DateTime.UtcNow;
        var successTestsCleanupTime = now.AddHours(-this.cleanUpsettings.SuccessTestsOlderThanInHours);
        var failedTestsCleanupTime = now.AddDays(-this.cleanUpsettings.FailedTestsOlderThanInDays);

        this.jobManager.AddOrUpdate<IJobCleanupService>("cleanup-job", service => service.CleanupAsync(successTestsCleanupTime, failedTestsCleanupTime), this.cleanUpsettings.CronExpression);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // No special shutdown logic required for this scheduler.
        return Task.CompletedTask;
    }
}
