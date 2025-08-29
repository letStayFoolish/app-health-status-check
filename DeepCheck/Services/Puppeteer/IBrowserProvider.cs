using PuppeteerSharp;

namespace DeepCheck.Services.Puppeteer;

public interface IBrowserProvider : IAsyncDisposable
{
  Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken = default);
}