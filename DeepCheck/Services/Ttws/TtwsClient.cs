using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using DeepCheck.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace DeepCheck.Services.Ttws;

public class TtwsClient : ITtwsClient
{
    private const int AuthExpirationTimeSeconds = 3600;
    private readonly IMemoryCache memoryCache;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<TtwsClient> logger;

    public TtwsClient(IHttpClientFactory httpClientFactory, ILogger<TtwsClient> logger, IMemoryCache memoryCache)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
        this.memoryCache = memoryCache;
    }

    public async Task<string> GetAuthTokenAsync(string baseUrl, string userName, string password, int customerID = 250, CancellationToken cancellationToken = default)
    {
        var key = $"ttws-auth-token-{customerID}-{userName}-{baseUrl}";
        var x = await memoryCache.GetOrCreateAsync(key, async cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(AuthExpirationTimeSeconds / 2);
            return await FetchAuthTokenAsyncInternal(baseUrl, userName, password, customerID, cancellationToken);
        });
        return x ?? throw new DomainException("Unable to retrieve authentication token from Cache!");
    }

    private async Task<string> FetchAuthTokenAsyncInternal(string baseUrl, string userName, string password, int customerID = 250, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "userName", userName },
            { "password", password },
            { "expirationTime", AuthExpirationTimeSeconds },
            { "clientType", "User" }
        };
        var doc = await GetAsync(baseUrl, "getAuthenticationToken", null, customerID, parameters, cancellationToken);
        var token = doc.Root?.TtwsElement("AuthenticationResponse")?.Attribute("authenticationToken")?.Value ?? throw new DomainException("Unable to retrieve authentication token from TTWS!");
        return token;
    }

    public async Task<XDocument> GetAsync(string baseUrl, string action, string? authenticationToken, int customerId = 250, IReadOnlyDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var ttwsParams = parameters?
            .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, TtwsParamToString(kvp.Value)))
            .Where(kvp => kvp.Value is not null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!) ?? new Dictionary<string, string>();

        ttwsParams["action"] = action;

        if (authenticationToken is not null)
        {
            ttwsParams["authenticationToken"] = authenticationToken;
        }

        ttwsParams["customerID"] = customerId.ToString(CultureInfo.InvariantCulture);

        FormUrlEncodedContent content = new FormUrlEncodedContent(ttwsParams);

        var watch = Stopwatch.StartNew();
        var httpResponse = await httpClient.PostAsync(baseUrl, content, cancellationToken);
        watch.Stop();
        httpResponse.EnsureSuccessStatusCode();
        logger.LogInformation("TTWS request took {ElapsedMilliseconds} ms for action {Action}", watch.ElapsedMilliseconds, action);
        var doc = XDocument.Parse(await httpResponse.Content.ReadAsStringAsync(cancellationToken));

        var errorNumber = doc.Root?.Attribute("errorNumber")?.Value;
        var hasErrors = errorNumber is not null && errorNumber != "0";

        if (hasErrors)
        {
            var msg = doc.Root!.TtwsElement("ResultError")?.Value;
            throw new DomainException($"TTWS request failed with error number {errorNumber}. Msg: {msg}");
        }

        return doc;
    }

    private string? TtwsParamToString(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            bool b => b ? "1" : "0",
            DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}
