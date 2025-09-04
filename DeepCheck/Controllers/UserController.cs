using Microsoft.AspNetCore.Mvc;

namespace DeepCheck.Controllers;

using Services.User;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService userService;
    private readonly ILogger<UserController> logger;
    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        this.userService = userService;
        this.logger = logger;
    }
    // GET
    [HttpGet]
    [Route("")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken = default)
    {

        try
        {
            var response = await this.userService.GetUserAsync(cancellationToken);

            return Ok(response);
        }
        catch (UserServiceException)
        {
            this.logger.LogError("Failed to get users.");
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to get users.");
            throw;
        }
    }
}
