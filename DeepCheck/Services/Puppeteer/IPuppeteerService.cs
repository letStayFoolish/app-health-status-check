using DeepCheck.DTOs;
using DeepCheck.Models;

namespace DeepCheck.Services.Puppeteer;

public interface IPuppeteerService
{
  Task<TestRunInfo> LoginAndNavigateToMarketOverviewAsync(TestRunDefinition testDefinition,
    CancellationToken ct = default);
}
