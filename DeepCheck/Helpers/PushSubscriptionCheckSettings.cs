namespace DeepCheck.Helpers;

public class PushSubscriptionCheckSettings
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required int LatencyCriteria { get; init; }
    public required string CronExpression { get; init; }
    public required string TtwsUrl { get; init; }
    public required string PushUrl { get; init; }
    public required string Symbol { get; init; }
}
