namespace DeepCheck.Helpers;

public class WsChecksSettings
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public required int LatencyCriteriaLogin { get; init; }
    public required int LatencyCriteriaMarketOverview { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CronExpression { get; init; } = string.Empty;
    public string ScreenShotsDir { get; init; } = string.Empty;
}
