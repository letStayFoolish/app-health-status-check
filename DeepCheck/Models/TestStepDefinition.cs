namespace DeepCheck.Models;

public record TestStepDefinition(
  string TestStepName,
  string Description,
  int LatencyCriteria
);