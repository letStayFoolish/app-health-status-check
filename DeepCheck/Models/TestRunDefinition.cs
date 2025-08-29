namespace DeepCheck.Models;

public record TestRunDefinition(
  string TestName,
  string Description,
  string CronExpression,
  IList<TestStepDefinition> Steps);