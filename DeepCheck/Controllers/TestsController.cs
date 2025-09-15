using DeepCheck.DTOs;
using DeepCheck.Interfaces;
using DeepCheck.Services.TestRunService;
using Microsoft.AspNetCore.Mvc;

namespace DeepCheck.Controllers;

using Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using Models;
using Repositories;

[ApiController]
[Route("api/tests")]
public class TestsController : ControllerBase
{
    private readonly ILogger<TestsController> _logger;
    private readonly ITestRunService _service;

    private readonly IEnumerable<ITest> _tests;

    // private readonly JobCleanupSettings _settings;
    private readonly KumaServiceSettings _kumaServiceSettings;

    public TestsController(ILogger<TestsController> logger, IEnumerable<ITest> tests, ITestRunService service,
        IOptions<KumaServiceSettings> config)
    {
        _logger = logger;
        _tests = tests;
        _service = service;
        this._kumaServiceSettings = config.Value;
    }

    // Fetch Test Definitions GET /api/tests
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    [Route("")]
    public async Task<ActionResult<IEnumerable<TestRunInfo>>> GetResultsByTest([FromServices] IHostEnvironment env,
        CancellationToken cancellationToken = default)
    {
        // TODO: Shouldn't we turn it off for PRODUCTION mode or !DEVELOPMENT?!
        if (env.IsDevelopment())
        {
            return BadRequest("Test execution is not supported in development mode.");
        }

        var response = await _service.GetLatestResultByTestAsync(cancellationToken);

        return Ok(response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost]
    [Route("execute/test/{testName}")]
    public async Task<ActionResult<TestRunInfo>> ExecuteTestByName([FromRoute] string testName,
        [FromServices] IHostEnvironment env,
        CancellationToken cancellationToken = default)
    {
        // TODO: Shouldn't we turn it off for PRODUCTION mode or !DEVELOPMENT?!
        if (env.IsDevelopment())
        {
            return BadRequest("Test execution is not supported in development mode.");
        }

        if (!_tests.Any()) return BadRequest("No tests to execute.");
        // testName = "ws-market-overview-with-login";
        var response = await _service.ExecuteTestByNameAsync(testName, cancellationToken);
        if (response is null) return NotFound("No tests to execute.");

        _logger.LogInformation("All tests executed via TestRunner.");
        return Ok(response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    [Route("run/failed")]
    public async Task<IActionResult> GetFailedTestRuns(CancellationToken cancellationToken = default)
    {
        var response = await this._service.GetFailedTestRunsAsync(cancellationToken);
        if (response is null)
        {
            return this.NotFound();
        }

        return Ok(response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    [Route("{testName}/run/results")]
    public async Task<IActionResult> GetTestRunResultsByName([FromRoute] string testName, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromServices] IHostEnvironment env, CancellationToken cancellationToken = default)
    {
        // TODO: Shouldn't we turn it off for PRODUCTION mode or !DEVELOPMENT?!
        if (env.IsDevelopment())
        {
            return BadRequest("Test execution is not supported in development mode.");
        }

        var response = await _service.QueryTestRunByNameAsync(testName, from, to, cancellationToken);

        if (response is null) return NotFound();

        return Ok(response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    [Route("steps")]
    public async Task<IActionResult> GetAllTestRunSteps(CancellationToken cancellationToken = default)
    {
        var steps = await _service.GetAllTestRunsStepsAsync(cancellationToken);

        if (steps is null) return NotFound();

        return Ok(steps);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    [Route("{testName}/{stepName}")]
    public async Task<IActionResult> GetLastStep(string testName, string stepName,
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;

        var response = await this._service.GetLastStepAsync(testName, stepName, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        // if found but not ok
        if (response.Status != LastRunStatusEnum.Ok || (response.FinishedAt != default &&
                                                        timestamp - response.FinishedAt >
                                                        TimeSpan.FromSeconds(this._kumaServiceSettings
                                                            .OlderThanInSeconds)
            ))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }

        // If found and ok
        return Ok(response);
    }
}
