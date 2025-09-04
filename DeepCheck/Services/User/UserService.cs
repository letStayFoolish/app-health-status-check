namespace DeepCheck.Services.User;

public class UserService : IUserService
{
    private readonly bool hasError = true;

    /// <summary>
    /// This method is designed to always throw an exception to test the global exception handler
    /// </summary>
    public Task<UserDto?> GetUserAsync(CancellationToken cancellationToken = default)
    {
        throw new UserServiceException("This is a test exception to verify global exception handling");
    }
}

public class UserServiceException : Exception
{
    public UserServiceException(string message) : base(message)
    {
    }
}
