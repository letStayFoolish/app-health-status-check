using System.Text.Json.Serialization;

namespace DeepCheck.Models;

public class RunInfo
{
  public string? NextRun { get; set; } // ISO-8601 UTC or null

  [JsonConverter(typeof(JsonStringEnumConverter))]
  public LastRunStatusEnum LastRun { get; set; } = LastRunStatusEnum.NeverRun;
}