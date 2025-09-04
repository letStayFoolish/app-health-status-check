using System.Diagnostics;
using DeepCheck.Helpers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DeepCheck.Middlewares;

using Services.User;

//TODO: Logging eand exception at the end
//TODO: Might be to verbose and force
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _env;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IOptions<ApiBehaviorOptions> _options;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment env, IOptions<ApiBehaviorOptions> options)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
        _env = env;
        _options = options;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken = default)
    {
        // 1. Log with corellation
        this._logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", Activity.Current.Id ?? httpContext.TraceIdentifier);;

        // 2. Map exception to ProblemDetails
        var (status, title, detail, extensions) = MapException(exception, httpContext);

        // 3. Build ProblemDetails (don't leak internals in production)
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = exception.GetType().FullName,
            Detail = _env.IsDevelopment() ? detail : null, // avoid leaking internals in production
            Instance = httpContext.Request.Path
        };

        var traceId = Activity.Current.Id ?? httpContext.TraceIdentifier;
        problem.Extensions["traceId"] = traceId;

        if (extensions is not null)
            foreach (var kvp in extensions)
                problem.Extensions[kvp.Key] = kvp.Value;

        httpContext.Response.StatusCode = status;

        problem.Extensions["timestamp"] = DateTime.UtcNow;

        // Log by severity (4xx => Warning, 5xx => Error)
        if (status is >= 500)
            _logger.LogError(exception, "Unhandled server error. TraceId={TraceId}", traceId);
        else if (status >= 400)
            _logger.LogWarning(exception, "Request failed. Status={Status} TraceId={TraceId}", status, traceId);

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = _env.IsDevelopment() ? exception : null // only include exception on dev...
        }); // cancellationToken
    }

    private static (int Status, string Title, string? Detail, Dictionary<string, object>? Extensions)
        MapException(Exception exception, HttpContext httpContext)
    {
        // Default
        const int defaultStatus = StatusCodes.Status500InternalServerError;
        const string defaultTitle = "An unexpected error occurred.";
        var detail = exception.Message;
        Dictionary<string, object>? extensions = null;

        return exception switch
        {
            // AuthN/AuthZ
            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", detail, null),

            ForbiddenException =>
                (StatusCodes.Status403Forbidden, "Forbidden", detail, null),

            // Resource not found
            KeyNotFoundException or NotFoundException =>
                (StatusCodes.Status404NotFound, "Resource not found", detail, null),

            // Bad input or domain rules
            ArgumentException or ArgumentNullException =>
                (StatusCodes.Status400BadRequest, "Invalid argument", detail, null),

            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation failed",
                detail,
                new Dictionary<string, object> { ["errors"] = ve.InnerException } // e.g. Dictionary<string, string[]>
            ),

            // Concurrency/Conflict
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, "Concurrency conflict", detail, null),

            ConflictException =>
                (StatusCodes.Status409Conflict, "Conflict", detail, null),

            DbUpdateException =>
                (StatusCodes.Status409Conflict, "Database update failure", detail, null),

            // Client cancellations
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
                (StatusCodes.Status499ClientClosedRequest, "Client closed request", null, null),

            // HTTP downstream calls
            HttpRequestException hre => (
                (int?)(hre.StatusCode is { } sc ? (int)sc : null) ?? StatusCodes.Status503ServiceUnavailable,
                "Upstream service error",
                detail,
                new Dictionary<string, object> { ["upstreamStatus"] = (int?)hre.StatusCode ?? 0 }
            ),

            // Timeout patterns
            TimeoutException =>
                (StatusCodes.Status504GatewayTimeout, "Operation timed out", detail, null),

            NotImplementedException =>
                (StatusCodes.Status501NotImplemented, "Not implemented", detail, null),

            DomainException de => (
                StatusCodes.Status400BadRequest,
                "Domain rule violation",
                detail,
                new Dictionary<string, object> { ["code"] = de.Code }
            ),

            UserServiceException => (StatusCodes.Status400BadRequest, "User service error", detail, null),

            RateLimitExceededException =>
                (StatusCodes.Status429TooManyRequests, "Rate limit exceeded", detail, null),

            _ => (defaultStatus, defaultTitle, detail, null)
        };
    }
}
