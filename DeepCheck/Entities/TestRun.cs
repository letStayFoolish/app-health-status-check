using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DeepCheck.Models;

namespace DeepCheck.Entities;

public class TestRun
{
  [Key] public Guid Id { get; init; } = Guid.CreateVersion7();

  [Required][MaxLength(200)] public string TestName { get; init; } = string.Empty;

  [Required] public DateTime StartedAt { get; init; }

  [Required] public long ElapsedMs { get; init; }

  [Required]
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public RunMethodEnum RunMethod { get; init; }

  public ICollection<TestRunStep> Steps { get; init; } = new List<TestRunStep>();
}