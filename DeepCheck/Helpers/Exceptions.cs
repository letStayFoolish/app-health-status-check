namespace DeepCheck.Helpers;

// Base for domain errors when a rule is violated
public class DomainException(string message, string? code = null) : Exception(message)
{
  public string? Code { get; } = code;
}

// Thrown when a resource cannot be found
public class NotFoundException(string message) : Exception(message);

// 409 Conflict
public class ConflictException(string message) : Exception(message);

// 403 Forbidden
public class ForbiddenException(string message) : Exception(message);

// 429 Too Many Requests
public class RateLimitExceededException(string message) : Exception(message);

// 422 Unprocessable Entity with detailed field errors
public class ValidationException : Exception
{
  public ValidationException(string message, IDictionary<string, string[]> errors) : base(message)
  {
    Errors = new Dictionary<string, string[]>(errors);
  }

  public IReadOnlyDictionary<string, string[]> Errors { get; }
}