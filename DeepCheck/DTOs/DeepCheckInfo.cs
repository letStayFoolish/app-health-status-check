namespace DeepCheck.DTOs;

using DeepCheck.Models;

public class DeepCheckInfo
{
    public required IReadOnlyList<TestRunDefinition> TestRunDefinitions { get; init; }
    public required UptimeTestRunInfo UptimeTestRunInfo { get; init; }
    public required AppVersion AppVersion { get; init; }
}
