using System.Text.Json;
using DeepCheck.Helpers;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace DeepCheck.Services.Puppeteer;

public sealed class BrowserProvider : IBrowserProvider, IAsyncDisposable
{
    private readonly ILogger<BrowserProvider> _logger;
    private Lazy<Task<IBrowser>> _browserLazy;

    private PuppeteerSettings _puppeteerSettings;

    public BrowserProvider(ILogger<BrowserProvider> logger, IOptions<PuppeteerSettings> configuration)
    {
        _logger = logger;
        _browserLazy = CreateLazy();
        _puppeteerSettings = configuration.Value;
    }

    public async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken = default)
    {
        // Snapshot the current lazy to avoid a race with Reset()
        var current = _browserLazy;

        try
        {
            var browser = await current.Value.WaitAsync(cancellationToken);

            if (!browser.IsClosed)
            {
                return browser;
            }

            // Closed -> re-create and return a fresh one
            ResetLazy();

            return await _browserLazy.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Don't reset on consumer cancellation
            throw;
        }
        catch (Exception ex)
        {
            // If init failed or faulted, reset so next call can retry
            _logger.LogError(ex, "Failed to initialize Puppeteer browser.");
            ResetLazy();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_browserLazy.IsValueCreated)
            {
                var task = _browserLazy.Value;

                if (task.IsCompletedSuccessfully)
                {
                    var browser = await task;
                    if (!browser.IsClosed)
                    {
                        await browser.CloseAsync();
                    }
                }
                // task didn't complete successfully, so it's already closed
            }
        }
        catch (Exception ex)
        {
            // Sanity check: if DisposeAsync fails, it's a bug
            _logger.LogError(ex, "Swallowing error on BrowserProvider.DisposeAsync");
        }
    }

    private void ResetLazy()
    {
        var next = CreateLazy();
        //we use interlocked to make sure that reference swap is atomic
        Interlocked.Exchange(ref _browserLazy, next);
    }

    private Lazy<Task<IBrowser>> CreateLazy() =>
      // We use a Lazy to avoid creating the browser until it's needed
      // Lazy is thread-safe, so we can safely use it from multiple threads
      // We use ExecutionAndPublication to make sure initialization is done only once
      new(() => InitializeBrowserAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    private async Task<IBrowser> InitializeBrowserAsync()
    {
        // Ensure Chromium is present
        if (string.IsNullOrWhiteSpace(_puppeteerSettings.LaunchOptions.ExecutablePath))
        {
            _logger.LogInformation("No executable path specified. Downloading Browser...");
            await new BrowserFetcher().DownloadAsync();
        }
        else
        {
            _logger.LogInformation("Executable path specified. Using Browser from path: {Path}", _puppeteerSettings.LaunchOptions.ExecutablePath);
        }

        _logger.LogInformation("Launching Puppeteer browser, options: {Options}", JsonSerializer.Serialize(_puppeteerSettings.LaunchOptions));
        var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(_puppeteerSettings.LaunchOptions);

        // If the browser disconnects, next GetBrowserAsync will recreate it
        browser.Disconnected += (_, _) =>
        {
            _logger.LogWarning("Puppeteer browser disconnected. Resetting initializer.");
            ResetLazy();
        };

        return browser;
    }
}
