using DeepCheck.DTOs;
using DeepCheck.Interfaces;
using DeepCheck.Models;
using DeepCheck.Repositories;

namespace DeepCheck.Services.TestRunService;

public class TestRunService : ITestRunService
{
  private readonly ITestRepository _testRepository;
  private readonly ITestRunner _testRunner;
  private readonly IEnumerable<ITest> _tests;

  public TestRunService(ITestRepository testRepository, IEnumerable<ITest> tests, ITestRunner testRunner)
  {
    _testRepository = testRepository;
    _tests = tests;
    _testRunner = testRunner;
  }

  public async Task<List<TestRunInfo?>> GetLatestResultByTestAsync(CancellationToken cancellationToken)
  {
    var response = (await _testRepository.GetAllTestRunsAsync(cancellationToken)).ToArray();
    var result = new List<TestRunInfo?>();

    if (!response.Any())
    {
      return [];
    }

    foreach (var test in _tests)
    {
      var latestByTest = response
        .Where(t => t.TestDefinition.TestName == test.TestDefinition.TestName)
        .OrderByDescending(t => t.StartedAt)
        .FirstOrDefault();


      result.Add(latestByTest);
    }

    return result;
  }

  public async Task<TestRunInfo?> ExecuteTestByNameAsync(string testName, CancellationToken cancellationToken = default)
  {
    return await _testRunner.ExecuteTestByName(testName, RunMethodEnum.Manual, cancellationToken);
  }

  public async Task<IEnumerable<TestRunInfo>?> QueryTestRunByNameAsync(string testName, DateTime? from, DateTime? to,
    CancellationToken cancellationToken = default)
  {
    return await _testRepository.GetFilteredTestRunsAsync(testName, from, to, cancellationToken);
  }

  public async Task<IEnumerable<TestStepRunInfo>?> GetAllTestRunsStepsAsync(
    CancellationToken cancellationToken = default)
  {
    return await _testRepository.GetTestStepsAsync(cancellationToken);
  }

  public async Task<TestStepRunInfo?> GetLastStepAsync(string testName, string stepName,
      CancellationToken cancellationToken = default)
  {
      return await this._testRepository.GetLastStepAsync(testName, stepName, cancellationToken);
  }

  public async Task<IEnumerable<FailedTestInfo>> GetFailedTestRunsAsync(CancellationToken cancellationToken = default)
  {
      return await this._testRepository.GetFailedTestRunsAsync(cancellationToken);
  }
}
