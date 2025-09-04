namespace DeepCheck.Services.User;

public interface IUserService
{
    public Task<UserDto> GetUserAsync(CancellationToken cancellationToken = default);
}

public record UserDto(string Name, int Age);
