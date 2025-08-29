namespace DeepCheck.Helpers;

public class JobCleanupSettings
{
    public required string CronExpression { get; init; }
    public required int OlderThanInHours { get; init; }
}
