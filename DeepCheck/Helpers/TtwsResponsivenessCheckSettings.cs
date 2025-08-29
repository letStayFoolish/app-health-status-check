namespace DeepCheck.Helpers;

public class TtwsResponsivenessCheckSettings
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required int LatencyCriteria { get; init; }
    public required string CronExpression { get; init; }
    public required string TtwsUrl { get; init; }
}
