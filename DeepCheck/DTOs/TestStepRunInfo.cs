using System.Text.Json.Serialization;
using DeepCheck.Models;

namespace DeepCheck.DTOs;

public class TestStepRunInfo
{
  public required TestStepDefinition TestStepDefinition { get; init; }
  public required DateTime StartedAt { get; init; }
  public required long ElapsedMs { get; init; }
  public DateTime FinishedAt => StartedAt.AddMilliseconds(ElapsedMs);
  public LastRunStatusEnum Status { get; init; }
  public string? FailReason { get; init; }
}
