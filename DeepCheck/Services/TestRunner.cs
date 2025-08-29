using DeepCheck.DTOs;
using DeepCheck.Entities;
using DeepCheck.Helpers;
using DeepCheck.Hubs;
using DeepCheck.Interfaces;
using DeepCheck.Models;
using DeepCheck.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace DeepCheck.Services;

public class TestRunner : ITestRunner
{
    private readonly ILogger<TestRunner> _logger;
    private readonly ITestRepository _testRepository;
    private readonly IHubContext<UptimeHub> uptimeHub;
    private readonly IEnumerable<ITest> _tests;

    public TestRunner(IEnumerable<ITest> tests, ILogger<TestRunner> logger, ITestRepository testRepository, IHubContext<UptimeHub> uptimeHub)
    {
        _tests = tests;
        _logger = logger;
        _testRepository = testRepository;
        this.uptimeHub = uptimeHub;
    }

    public async Task<TestRunInfo?> ExecuteTestByName(string name, RunMethodEnum runMethod = RunMethodEnum.Manual,
        CancellationToken cancellationToken = default)
    {
        var testRunId = Guid.CreateVersion7(); //create ID now for logging purposes
        using var loggingScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            { "TestName", name }, { "RunMethod", runMethod }, { "TestRunId", testRunId }
        });
        TestRunInfo? response = null;
        var testToExecute = _tests.FirstOrDefault(t => t.TestDefinition.TestName == name);
        if (testToExecute is null)
        {
            _logger.LogWarning("Test {TestName} not found", name);
            throw new ArgumentNullException(name, "Test not found");
        }

        try
        {
            response = await testToExecute.ExecuteAsync(cancellationToken);
            //run method probably shouldn't be returned from the test, but only used by the test runner
            _logger.LogInformation("Test {TestName} executed", name);
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing test {TestName}", name);
            var testRunBuilder = new TestRunInfoBuilder(testToExecute.TestDefinition);
            testRunBuilder.FailStep(e.Message);
            response = testRunBuilder.FinishTest();
            throw;
        }
        finally
        {
            if (response is not null)
            {
                var entity = new TestRun
                {
                    Id = testRunId,
                    TestName = response.TestDefinition.TestName,
                    StartedAt = response.StartedAt,
                    ElapsedMs = response.ElapsedMs,
                    RunMethod = runMethod,
                    Steps = response.Steps.Select(s => new TestRunStep
                    {
                        TestStepName = s.TestStepDefinition.TestStepName,
                        StartedAt = s.StartedAt,
                        ElapsedMs = s.ElapsedMs,
                        Status = s.Status,
                        FailReason = s.FailReason
                    }).ToList()
                };

                await _testRepository.AddTestResultAsync(entity);
                await uptimeHub.Clients.All.SendAsync("CheckResult", entity, cancellationToken);
            }
        }
    }
}
