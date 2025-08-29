using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DeepCheck.Models;

namespace DeepCheck.Entities;

public class TestRunStep
{
  [Key] public Guid Id { get; init; } = Guid.CreateVersion7();

  [Required][MaxLength(200)] public required string TestStepName { get; init; }

  [Required] public required DateTime StartedAt { get; init; }

  [Required] public required long ElapsedMs { get; init; }

  [Required]
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public required LastRunStatusEnum Status { get; init; }

  [MaxLength(2000)] public string? FailReason { get; init; }

  //FK
  [Required] public Guid TestRunId { get; init; }

  // Navigation back to the parent TestRun (ignored in JSON to avoid cycles)
  [JsonIgnore] public TestRun TestRun { get; init; } = null!;
}