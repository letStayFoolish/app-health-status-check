using Cronos;
using DeepCheck.DTOs;
using DeepCheck.Helpers;
using DeepCheck.Interfaces;
using DeepCheck.Models;
using DeepCheck.Services.Puppeteer;
using Microsoft.Extensions.Options;

namespace DeepCheck.Services.Jobs;

public class WsUserLoginAndMarketOverview : ITest
{
    private readonly ILogger<WsUserLoginAndMarketOverview> _logger;
    private readonly IPuppeteerService _puppeteerService;
    public TestRunDefinition TestDefinition { get; }

    public WsUserLoginAndMarketOverview(ILogger<WsUserLoginAndMarketOverview> logger, IPuppeteerService puppeteerService,
        IOptions<WsChecksSettings> config)
    {
        _logger = logger;
        _puppeteerService = puppeteerService;
        var settings = config.Value;

        this.TestDefinition = new TestRunDefinition(

            TestName: settings.Name,
            Description: settings.Description,
            CronExpression: settings.CronExpression,
            Steps: new List<TestStepDefinition>
            {
              new("ws-user-login", "Login to WS", settings.LatencyCriteriaLogin ),
              new("ws-market-overview", "Navigate to market overview",  settings.LatencyCriteriaMarketOverview)
            }
        );
    }

    public async Task<TestRunInfo> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Execute puppeteer workflow which returns per-step results
        return await _puppeteerService.LoginAndNavigateToMarketOverviewAsync(TestDefinition, cancellationToken);
    }
}
