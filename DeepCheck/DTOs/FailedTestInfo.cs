namespace DeepCheck.DTOs;
public class FailedTestInfo
{
    public Guid TestId { get; set; }
    public required string TestName { get; set; }
    public required DateTime StartedAt { get; set; }
    public required long ElapsedMs { get; set; }
    public string? FailReason { get; set; } = string.Empty;
};
