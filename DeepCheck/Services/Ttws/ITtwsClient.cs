using System.Xml.Linq;

namespace DeepCheck.Services.Ttws;

public interface ITtwsClient
{
    Task<string> GetAuthTokenAsync(string baseUrl, string userName, string password, int customerID = 250, CancellationToken cancellationToken = default);

    Task<XDocument> GetAsync(string baseUrl, string action, string? authenticationToken, int customerId = 250, IReadOnlyDictionary<string, object?>? parameters = default, CancellationToken cancellationToken = default);
}
