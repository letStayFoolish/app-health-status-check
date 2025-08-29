using DeepCheck.DTOs;
using DeepCheck.Models;

namespace DeepCheck.Services;

public interface ITestRunner
{
  Task<TestRunInfo?> ExecuteTestByName(string name, RunMethodEnum runMethod = RunMethodEnum.Manual, CancellationToken cancellationToken = default);
}
