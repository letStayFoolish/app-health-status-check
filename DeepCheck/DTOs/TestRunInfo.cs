using System.Text.Json.Serialization;
using DeepCheck.Models;

namespace DeepCheck.DTOs;

public class TestRunInfo
{
    public required TestRunDefinition TestDefinition { get; init; }
    public required DateTime StartedAt { get; init; }
    public required long ElapsedMs { get; set; }
    public DateTime FinishedAt => StartedAt.AddMilliseconds(ElapsedMs);

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LastRunStatusEnum Status
    {
        get
        {
            if (Steps.All(s => s.Status == LastRunStatusEnum.NeverRun))
            {
                return LastRunStatusEnum.NeverRun;
            }
            if (Steps.All(s => s.Status == LastRunStatusEnum.Ok))
            {
                return LastRunStatusEnum.Ok;
            }
            return LastRunStatusEnum.Failed;
        }
    }

    public string? FailReason => Steps.FirstOrDefault(s => s.Status == LastRunStatusEnum.Failed)?.FailReason;

    public IList<TestStepRunInfo> Steps { get; init; } = new List<TestStepRunInfo>();
}
