namespace DeepCheck.Helpers;

public class JobCleanupSettings
{
    public required string CronExpression { get; init; }
    public required int SuccessTestsOlderThanInHours { get; init; }
    public required int FailedTestsOlderThanInDays { get; init; }
}
