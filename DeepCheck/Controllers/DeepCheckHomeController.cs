namespace DeepCheck.Controllers;

using DeepCheck.Services;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("/")]
public class DeepCheckHomeController : ControllerBase
{
    private readonly IDeepCheckInfoService deepCheckInfoService;

    public DeepCheckHomeController(IDeepCheckInfoService deepCheckInfoService)
    {
        this.deepCheckInfoService = deepCheckInfoService;
    }

    [HttpGet]
    [Route("/api/deepcheck")]
    public async Task<IActionResult> Get()
    {
        var response = await deepCheckInfoService.GetDeepCheckInfo();
        return Ok(response);
    }

    [HttpGet]
    [Route("/")]
    public IActionResult Home()
    {
        return Redirect("/uptime");
    }
}
