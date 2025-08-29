using DeepCheck.DTOs;
using DeepCheck.Models;

namespace DeepCheck.Interfaces;

public interface ITest
{
  TestRunDefinition TestDefinition { get; }
  Task<TestRunInfo> ExecuteAsync(CancellationToken cancellationToken = default);
}