using System.Diagnostics;
using System.Text.Json;
using DeepCheck.DTOs;
using DeepCheck.Interfaces;
using DeepCheck.Models;
using DeepCheck.Services.Ttws;
using DeepCheck.Helpers;
using Microsoft.Extensions.Options;

namespace DeepCheck.Services.Jobs;

public class TtwsResponsivenessCheck : ITest
{
    private readonly TtwsResponsivenessCheckSettings _settings;
    private readonly ILogger<TtwsResponsivenessCheck> _logger;
    private readonly ITtwsClient _ttwsClient;
    public TestRunDefinition TestDefinition { get; }

    public TtwsResponsivenessCheck(IOptions<TtwsResponsivenessCheckSettings> config,
        ILogger<TtwsResponsivenessCheck> logger, ITtwsClient ttwsClient)
    {
        _settings = config.Value;
        _logger = logger;
        _ttwsClient = ttwsClient;

        this.TestDefinition = new TestRunDefinition(
            TestName: _settings.Name,
            Description: _settings.Description,
            CronExpression: _settings.CronExpression,
            new List<TestStepDefinition>
            {
                new("ttws-responsiveness-receive",
                    "Retrieve the userId using getUserProfile, use it to query symbols with getSymbols, and then fetch historical quotes using getHistoricQuotes.",
                    _settings.LatencyCriteria),
            }
        );

        _logger.LogInformation("Test {TestName} created. Settings: {Settings}", TestDefinition.TestName,
            JsonSerializer.Serialize(_settings));
    }

    public async Task<TestRunInfo> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var testRunBuilder = new TestRunInfoBuilder(TestDefinition);
        testRunBuilder.StartNextStep();
        try
        {
            var authToken = await _ttwsClient.GetAuthTokenAsync(_settings.TtwsUrl, this._settings.Username,
                this._settings.Password, 250, cancellationToken);

            // getUserProfile
            // Await the TTWS call; TtwsClient throws if errorNumber != "0"
            // https://ttwsxml.ttweb.net/ttws-net/?action=getUserProfile&customerID=250&userName={AccountUserName}
            var userProfileXmlDoc = await _ttwsClient.GetAsync(_settings.TtwsUrl, "getUserProfile", authToken, 250,
                null,
                cancellationToken);

            // Extract user information:
            var userElement = userProfileXmlDoc.Root?.TtwsElement("User");
            if (userElement is null)
            {
                throw new DomainException("Unable to retrieve user information from TTWS.");
            }
            var userId = userElement.Attribute("id")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new DomainException("Unable to retrieve user ID from TTWS.");
            }

            // getSymbols
            // https://ttwsxml.ttweb.net/ttws-net?action=getSymbols&id=tts-675717&userID={USERID}&resultLanguageID=2&resultAltLanguageID=1&customerID=250
            var symbolsXmlDoc = await _ttwsClient.GetAsync(_settings.TtwsUrl, "getSymbols", authToken, 250,
                new Dictionary<string, object?>()
                {
                    ["id"] = "tts-675717",
                    ["resultLanguageID"] = "2",
                    ["resultAltLanguageID"] = "1"
                }, cancellationToken);
            var symbolEl = symbolsXmlDoc.Root?
                .TtwsElement("SymbolList")?
                .TtwsElement("Symbol");
            if (symbolEl is null)
            {
                throw new DomainException("Unable to retrieve symbols from TTWS.");
            }
            var symbolId = symbolEl.Attribute("id")?.Value;
            if (string.IsNullOrWhiteSpace(symbolId) || symbolId != "tts-675717")
            {
                throw new DomainException("Unable to retrieve symbol ID from TTWS.");
            }
            var quoteEl = symbolsXmlDoc.Root?
                    .TtwsElement("SymbolList")?
                    .TtwsElement("Symbol")?
                .TtwsElement("Quote");
            if (quoteEl is null)
            {
                throw new DomainException("Unable to retrieve quote from TTWS.");
            }
            var quoteLastNumber = quoteEl.Attribute("last")?.Value;
            if (string.IsNullOrWhiteSpace(quoteLastNumber) || !decimal.TryParse(quoteLastNumber, out _))
            {
                throw new DomainException("Unable to retrieve quote last from TTWS.");
            }

            // getHistoricQuotes
            // https://ttwsxml.ttweb.net/ttws-net/?action=getHistoricQuotes&id=tts-675717&maxNumOfBars=100&pageRecords=1&numRecords=100&userID={USERID}&timeZone=GMT&customerID=250
            var historicQuotesXmlDoc = await _ttwsClient.GetAsync(_settings.TtwsUrl, "getHistoricQuotes", authToken,
                250,
                new Dictionary<string, object?>()
                {
                    ["id"] = "tts-675717",
                    ["maxNumOfBars"] = 100,
                    ["pageRecords"] = 1,
                    ["numRecords"] = 100
                }, cancellationToken);
            var historyEl = historicQuotesXmlDoc.Root?.TtwsElement("History");
            if (historyEl is null)
            {
                throw new DomainException("Unable to retrieve history from TTWS.");
            }
            var hsitoryId = historyEl.Attribute("id")?.Value;
            if (string.IsNullOrWhiteSpace(hsitoryId) || hsitoryId != "tts-675717")
            {
                throw new DomainException("Unable to retrieve history ID from TTWS.");
            }

            testRunBuilder.StepDone();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test run {Name} failed.", TestDefinition.TestName);
            testRunBuilder.FailStep(ex.Message);
        }

        return testRunBuilder.FinishTest();
    }
}
