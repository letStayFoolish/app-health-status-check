// using System.Diagnostics;
// using System.Text.Json;
// using ClientModel;
// using DeepCheck.DTOs;
// using DeepCheck.Helpers;
// using DeepCheck.Interfaces;
// using DeepCheck.Models;
// using DeepCheck.Services.Ttws;
// using Microsoft.AspNetCore.SignalR.Client;
// using Microsoft.Extensions.Options;
// using Teletrader.Push.ClientModel;
//
// namespace DeepCheck.Services.Jobs;
//
// public class PushTestJob : ITest
// {
//     private readonly PushSubscriptionCheckSettings settings;
//     private readonly ILogger<PushTestJob> logger;
//     private readonly ITtwsClient ttwsClient;
//     // private readonly SubscriptionKey subscriptionKey;
//     public TestRunDefinition TestDefinition { get; }
//     // private IReadOnlyList<QuoteUpdateSubscribeRequest> parameters;
//     private readonly JsonSerializerOptions jsonSerializerOptions = new()
//     {
//         PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//         DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//     };
//
//
//     public PushTestJob(IOptions<PushSubscriptionCheckSettings> config, ILogger<PushTestJob> logger, ITtwsClient ttwsClient)
//     {
//         this.settings = config.Value;
//         this.TestDefinition = new TestRunDefinition(
//             "push-subscribe-run",
//             "Runner for push subscription test",
//             settings.CronExpression,
//             new List<TestStepDefinition>
//             {
//                 new("push-subscribe-receive", "Subscribe to a push channel and receive notifications", settings.LatencyCriteria),
//             }
//         );
//         // this.logger = logger;
//         // this.ttwsClient = ttwsClient;
//         // this.subscriptionKey = new SubscriptionKey(settings.Symbol, AggregationPeriod.NoAggregation);
//         //
//         // parameters = [
//         //     new() {
//         //         Fids = [Fid.Ask, Fid.AskDateTime, Fid.Bid, Fid.Change, Fid.Volume],
//         //         IncludeSnapshot = true,
//         //         Key = subscriptionKey
//         //     }
//         // ];
//
//         logger.LogInformation("Push subscription test definition initialized. Settings: {Settings}", JsonSerializer.Serialize(settings));
//     }
//
//     // public async Task<TestRunInfo> ExecuteAsync(CancellationToken cancellationToken = default)
//     // {
//         // var testRunBuilder = new TestRunInfoBuilder(TestDefinition);
//         // try
//         // {
//         //     await using var connection = await PrepareConnectionAsync(cancellationToken);
//         //
//         //     testRunBuilder.StartNextStep();
//         //
//         //     using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//         //     linkedCts.CancelAfter(TimeSpan.FromMilliseconds(settings.LatencyCriteria));
//         //     cancellationToken = linkedCts.Token;
//         //
//         //     await connection.StartAsync(cancellationToken);
//         //     var stream = await connection.StreamAsChannelAsync<Message>("GetSymbolUpdateStream", DataQuality.Delayed, cancellationToken);
//         //     var subscriptionResult = await connection.InvokeAsync<SubscriptionResult>("Subscribe", parameters, cancellationToken);
//         //     if (subscriptionResult.Status != SubscriptionResult.ResultStatus.Success)
//         //     {
//         //         throw new DomainException($"Subscription failed with status {subscriptionResult.Status}.");
//         //     }
//         //
//         //     if (subscriptionResult.SymbolSubscriptions[0].Status != SymbolSubscriptionResult.ResultStatus.Success)
//         //     {
//         //         throw new DomainException($"Subscription failed with status {subscriptionResult.SymbolSubscriptions[0].Status}.");
//         //     }
//         //
//         //     logger.LogDebug("Subscription result: {Result}", JsonSerializer.Serialize(subscriptionResult.SymbolSubscriptions, jsonSerializerOptions));
//         //
//         //     var message = await stream.ReadAsync(cancellationToken);
//         //
//         //     while (message.SymbolId != settings.Symbol)
//         //     {
//         //         message = await stream.ReadAsync(cancellationToken);
//         //     }
//         //
//         //     logger.LogInformation("Message received about symbol {Symbol}: {Message}", settings.Symbol, JsonSerializer.Serialize(message, jsonSerializerOptions));
//         //
//         //     var unsubscriptionResult = await connection.InvokeAsync<SubscriptionResult>("Unsubscribe", new List<SubscriptionKey>() { subscriptionKey }, cancellationToken);
//         //
//         //     if (unsubscriptionResult.Status != SubscriptionResult.ResultStatus.Success)
//         //     {
//         //         throw new DomainException($"Unsubscription failed with status {unsubscriptionResult.Status}.");
//         //     }
//         //
//         //     if (unsubscriptionResult.SymbolSubscriptions[0].Status != SymbolSubscriptionResult.ResultStatus.Success)
//         //     {
//         //         throw new DomainException($"Unsubscription failed with status {unsubscriptionResult.SymbolSubscriptions[0].Status}.");
//         //     }
//         //
//         //     logger.LogDebug("Unsubscription result: {Result}", JsonSerializer.Serialize(unsubscriptionResult.SymbolSubscriptions, jsonSerializerOptions));
//         //
//         //     await connection.StopAsync(cancellationToken);
//         //
//         //     testRunBuilder.StepDone();
//         // }
//         // catch (Exception ex)
//         // {
//         //     logger.LogError(ex, "Test run {Name} failed.", TestDefinition.TestName);
//         //     testRunBuilder.FailStep(ex.Message);
//         // }
//         // return testRunBuilder.FinishTest();
//     // }
//
//     public async Task<HubConnection> PrepareConnectionAsync(CancellationToken cancellationToken = default)
//     {
//
//         var authToken = await ttwsClient.GetAuthTokenAsync(baseUrl: settings.TtwsUrl, userName: settings.Username, password: settings.Password, cancellationToken: cancellationToken);
//         var secToken = await ttwsClient.GetAsync(baseUrl: settings.TtwsUrl, authenticationToken: authToken, action: "getSecurityToken", cancellationToken: cancellationToken);
//         var token = secToken?.Root?.TtwsElement("SecurityToken")?.Value;
//
//         if (token == null)
//         {
//             throw new DomainException("Unable to retrieve security token from TTWS.");
//         }
//
//         var connection = new HubConnectionBuilder()
//             .WithUrl(settings.PushUrl, opts => opts.AccessTokenProvider = () => Task.FromResult(token)!)
//             .WithAutomaticReconnect()
//             .Build();
//
//         connection.Closed += (error) =>
//         {
//             logger.LogDebug(error, "TTWS connection closed.");
//             return Task.CompletedTask;
//         };
//
//         connection.On<ServiceStatusInfo>("ServiceStatusUpdated", x => logger.LogDebug("Service status updated: {Status}", JsonSerializer.Serialize(x)));
//         return connection;
//     }
// }
