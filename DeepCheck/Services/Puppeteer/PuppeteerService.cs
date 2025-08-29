using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using DeepCheck.DTOs;
using DeepCheck.Helpers;
using DeepCheck.Interfaces;
using DeepCheck.Models;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace DeepCheck.Services.Puppeteer;

public sealed class PuppeteerService : IPuppeteerService
{
    private const int Timeout = 60_000; // fail fast if Chromium can't launch

    // Login Form Selectors
    private const string UsernameSelector = ".username_password fieldset.input input[type='text']";
    private const string PasswordSelector = ".username_password fieldset.input input[type='password']";
    private const string LoginButtonSelector = ".button_holder button, .button_holder";
    private const string LoggedInMarkerSelector = "button.reactor.button.header-control.open_drawer.plain";

    /// Market Overview Selectors
    private const string MarketOverviewTableReadySelector = @"[data-testid=""marketOverview-tableWidget/symbolList""]";

    private const string MarketOverviewColumnReadySelector = @"[role=""gridcell""][col-id=""last""]";

    private const string MarketOverviewRowBaseSelector =
        @"[data-testid=""marketOverview-tableWidget/symbolList""] [role=""row""][row-index]";

    private const string MarketOverviewNumericValueSelector =
        @"[role=""gridcell""][col-id=""last""] span.base_cell_wrapper span:not([class]):not([role])";

    private readonly IBrowserProvider _browserProvider;
    private readonly ILogger<PuppeteerService> _logger;
    private readonly IHostEnvironment env;
    private readonly WsChecksSettings _settings;

    public PuppeteerService(IBrowserProvider browserProvider, ILogger<PuppeteerService> logger, IOptions<WsChecksSettings> config, IHostEnvironment env)
    {
        _browserProvider = browserProvider;
        _logger = logger;
        this.env = env;
        _settings = config.Value;
    }

    public async Task<TestRunInfo> LoginAndNavigateToMarketOverviewAsync(TestRunDefinition testDefinition,
        CancellationToken ct = default)
    {
        var testRunBuilder = new TestRunInfoBuilder(testDefinition);

        // Acquire singleton browser
        var browser = await _browserProvider.GetBrowserAsync(ct);

        // Create an isolated context per operation
        var context = await browser.CreateBrowserContextAsync();
        using var page = await context.NewPageAsync();

        try
        {
            await ConfigurePage(page, ct);
            //// LOGIN PROCESS
            // Step 1: Go to the home page and login
            await GoToAsync(page, "https://wealth.baha.com/equities/overview", ct);
            var userInput = await WaitForSelectorAsync(page, UsernameSelector, ct);
            var passInput = await WaitForSelectorAsync(page, PasswordSelector, ct);
            var loginBtn = await WaitForSelectorAsync(page, LoginButtonSelector, ct);

            await userInput.ClickAsync();
            await userInput.TypeAsync(_settings.Username);
            await passInput.ClickAsync();
            await passInput.TypeAsync(_settings.Password);

            testRunBuilder.StartNextStep();

            await loginBtn.ClickAsync();
            await WaitForSelectorAsync(page, LoggedInMarkerSelector, ct);

            testRunBuilder.StepDone();

            // Optional take screenshot
            await ScreenshotAsync(page, "login", ct);


            //// MARKET OVERVIEW
            // Step 2: Navigate to market overview (same page/context to keep session)
            testRunBuilder.StartNextStep();

            // Wait for a marker that indicates the overview is loaded
            var passedTestsCount = await MarketOverviewReadyAsync(page, 5, ct);
            if (passedTestsCount < 5)
            {
                throw new DomainException($"Market overview not loaded. Expected at least 5 rows, got {passedTestsCount}.");
            }
            testRunBuilder.StepDone();

            // Optional take screenshot
            await ScreenshotAsync(page, "mop", ct);
        }
        catch (Exception ex)
        {
            await ScreenshotAsync(page, "error", ct);
            _logger.LogError(ex, "Test run {Name} failed.", testDefinition.TestName);
            testRunBuilder.FailStep(ex.Message);
        }
        return testRunBuilder.FinishTest();
    }

    // Internals
    private static async Task ConfigurePage(IPage page, CancellationToken ct)
    {
        page.DefaultTimeout = 30_000;
        await page.SetViewportAsync(new ViewPortOptions { Width = 1366, Height = 768 });
        // Optional: SetUserAgent to reduce bot detection
        // await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125 Safari/537.36");
    }

    private static async Task GoToAsync(IPage page, string url, CancellationToken ct)
    {
        var navOptions = new NavigationOptions
        {
            Timeout = Timeout,
            WaitUntil = [WaitUntilNavigation.Networkidle0] // wait until the page is "quiet"
        };

        var response = await page.GoToAsync(url, navOptions);
        if (response is null || !response.Ok)
            throw new DomainException($"Navigation to '{url}' failed with status {(int?)response?.Status}.");
    }

    private static async Task<IElementHandle> WaitForSelectorAsync(IPage page, string selector, CancellationToken ct)
    {
        var handle = await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
        {
            Timeout = Timeout,
            Visible = true
        });

        if (handle is null)
            throw new NotFoundException($"Expected element not found: '{selector}'.");

        return handle;
    }

    private async Task<string> ScreenshotAsync(IPage page, string prefix, CancellationToken ct)
    {
        if (!env.IsDevelopment())
        {
            return string.Empty;
        }
        var dir = _settings.ScreenShotsDir;
        Directory.CreateDirectory(dir);
        var id = new TimeSpan(DateTime.UtcNow.Ticks).Ticks.ToString();
        var fullPath = Path.Combine(dir, $"screenshot-{prefix}-{id}.png");
        await page.ScreenshotAsync(fullPath, new ScreenshotOptions { FullPage = true });
        return fullPath;
    }

    private static async Task<string?> GetInnerTextAsync(IElementHandle element)
    {
        var prop = await element.GetPropertyAsync("textContent");
        return await prop.JsonValueAsync<string>();
    }

    private async Task<int> MarketOverviewReadyAsync(IPage page, int requiredCount, CancellationToken ct)
    {
        // Ensure a table root is mounted
        await WaitForSelectorAsync(page, MarketOverviewTableReadySelector, ct);
        await WaitForSelectorAsync(page, MarketOverviewColumnReadySelector, ct); // checking if Last column is visible

        // Count currently rendered rows matching the required class pattern (ag-Grid is virtualized)
        var renderedRows = await page.QuerySelectorAllAsync(MarketOverviewRowBaseSelector);
        if (renderedRows is null || renderedRows.Length == 0 || requiredCount > renderedRows.Length)
            return 0;

        var validRowsCount = 0;
        foreach (var row in renderedRows)
        {
            if (validRowsCount >= requiredCount)
                break;

            var valueHandle = await row.QuerySelectorAsync(MarketOverviewNumericValueSelector);
            if (valueHandle is null)
                continue;

            var rawText = await GetInnerTextAsync(valueHandle);
            if (string.IsNullOrEmpty(rawText))
                continue;

            // Normalize spaces and parse
            var normalized = rawText.Replace("\u00A0", "").Replace(" ", "");
            if (double.TryParse(normalized,
                    NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint |
                    NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
            {
                _logger.LogInformation("Found a valid number: {Value}", normalized);
                validRowsCount++;
            }
        }

        return validRowsCount;
    }
}
