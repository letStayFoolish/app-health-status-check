namespace DeepCheck.DTOs;

using DeepCheck.Entities;
using DeepCheck.Models;

public class UptimeTestRunInfo
{
    public required IReadOnlyDictionary<string, IReadOnlyList<TestRunStep>> Steps { get; init; }
}
