using System.Diagnostics;
using DeepCheck.Data;
using DeepCheck.DTOs;
using DeepCheck.Entities;
using DeepCheck.Interfaces;
using DeepCheck.Models;
using Microsoft.EntityFrameworkCore;

namespace DeepCheck.Repositories;

public class TestRepository : ITestRepository
{
    private const int DefaultPageSize = 100;
    private const int MaxPageSize = 1000;
    private readonly AppDbContext _dbContext;
    private readonly IEnumerable<ITest> tests;
    private readonly ILogger<TestRepository> logger;
    private readonly IReadOnlyDictionary<(string TestName, string TestStepName), TestStepDefinition> testSteps;

    public TestRepository(AppDbContext dbContext, IEnumerable<ITest> tests, ILogger<TestRepository> logger)
    {
        _dbContext = dbContext;
        this.tests = tests;
        this.logger = logger;
        this.testSteps = tests
            .SelectMany(t =>
                t.TestDefinition.Steps.ToDictionary(s => (t.TestDefinition.TestName, s.TestStepName), s => s))
            .ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task AddTestResultAsync(TestRun testModel)
    {
        var stopwatch = Stopwatch.StartNew();
        await _dbContext.TestRuns.AddAsync(testModel);
        await _dbContext.SaveChangesAsync();
        var stopwatchElapsed = stopwatch.ElapsedMilliseconds;
        logger.LogInformation("Test run {TestRunId} added to DB in {ElapsedMs} ms", testModel.Id, stopwatchElapsed);
    }

    public async Task<IEnumerable<TestRunInfo>> GetAllTestRunsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _dbContext.TestRuns.AsNoTracking().Include(testRun => testRun.Steps)
            .ToListAsync(cancellationToken);
        return response.Select(x =>
                MapToTestRunInfo(x, tests.First(t => t.TestDefinition.TestName == x.TestName).TestDefinition))
            .ToList();
    }

    public async Task<IEnumerable<FailedTestInfo>> GetFailedTestRunsAsync(CancellationToken cancellationToken)
    {
        var response = await this._dbContext.TestRuns
            .AsNoTracking()
            .Include(tr => tr.Steps)
            .Where(tr => tr.Steps.Any(ts =>
                ts.Status == LastRunStatusEnum.Failed && !string.IsNullOrWhiteSpace(ts.FailReason)))
            .ToListAsync(cancellationToken);

        return response.Select(tr => MapToFailedTestInfo(tr)).ToList();
    }

    public async Task<IEnumerable<TestStepRunInfo>> GetTestStepsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _dbContext.TestRunSteps
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return response.Select(s => MapToTestStepRunInfo(s, testSteps[(s.TestStepName, s.TestRun.TestName)]));
    }


    public async Task<IReadOnlyList<TestRunInfo>> GetFilteredTestRunsAsync(
        string? testName,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken,
        int take = DefaultPageSize,
        int skip = 0
    )
    {
        var query = _dbContext.TestRuns.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(testName))
        {
            var normalizedTestName = testName.Trim();
            query = query.Where(t => t.TestName == normalizedTestName);
        }

        if (from.HasValue)
        {
            var fromDate = from.Value;
            query = query.Where(t => t.StartedAt >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value;
            query = query.Where(t => t.StartedAt <= toDate);
        }

        // Normalize paging input
        skip = Math.Max(0, skip);
        take = take <= 0 ? DefaultPageSize : Math.Min(take, MaxPageSize);

        query = query
            .OrderByDescending(x => x.StartedAt)
            .Skip(skip)
            .Take(take);
        return await query
            .Select(x => MapToTestRunInfo(x, tests.First(t => t.TestDefinition.TestName == x.TestName).TestDefinition))
            .ToListAsync(cancellationToken);
    }

    public async Task<TestStepRunInfo?> GetLastStepAsync(string testName, string stepName,
        CancellationToken cancellationToken = default)
    {
        var step = await (
            from s in this._dbContext.TestRunSteps.AsNoTracking()
            join r in this._dbContext.TestRuns.AsNoTracking()
                on s.TestRunId equals r.Id
            where r.TestName == testName && s.TestStepName == stepName
            orderby r.StartedAt descending, s.StartedAt descending
            select s
        ).FirstOrDefaultAsync(cancellationToken);

        if (step is null)
        {
            return null;
        }

        if (!this.testSteps.TryGetValue((testName, stepName), out var def))
        {
            return null;
        }

        return MapToTestStepRunInfo(step, def);
    }

    public async Task<UptimeTestRunInfo> GetUptimeTestRunInfoAsync(int countPerTestStepName,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, IReadOnlyList<TestRunStep>> testSteps = await _dbContext.TestRunSteps
            .GroupBy(s => s.TestStepName)
            .Select(g => new
            {
                g.Key, Items = g.OrderByDescending(s => s.StartedAt).Take(countPerTestStepName).ToList()
            })
            // optionally choose which groups to include:
            .ToDictionaryAsync(x => x.Key, x => (IReadOnlyList<TestRunStep>)x.Items, cancellationToken);

        return new UptimeTestRunInfo { Steps = testSteps };
    }

    public async Task RemoveOldSuccessfulTestRunsAsync(DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var oldSuccessfulRuns = await this._dbContext.TestRuns
                .Where(tr => tr.StartedAt.AddMilliseconds(tr.ElapsedMs) < olderThan)
                .Where(tr => tr.Steps.All(ts => ts.Status == LastRunStatusEnum.Ok))
                .ToListAsync(cancellationToken);

            if (oldSuccessfulRuns.Count > 0)
            {
                var oldTestRunIds = oldSuccessfulRuns.Select(tr => tr.Id).ToList();

                var stepsToDelete
                    = await this._dbContext.TestRunSteps
                        .Where(ts => oldTestRunIds.Contains(ts.TestRunId))
                        .ToListAsync(cancellationToken);

                this._dbContext.TestRunSteps.RemoveRange(stepsToDelete);
                this._dbContext.TestRuns.RemoveRange(oldSuccessfulRuns);

                await this._dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RemoveOldFailedTestRunsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var oldFailedRuns = await this._dbContext.TestRuns
                .Where(tr => tr.StartedAt.AddMilliseconds(tr.ElapsedMs) < olderThan)
                // .Where(tr => tr.Steps.Any(ts => ts.Status == LastRunStatusEnum.Failed))
                .ToListAsync(cancellationToken);

            if (oldFailedRuns.Count > 0)
            {
                var oldFailedTestRunIds = oldFailedRuns.Select(tr => tr.Id).ToList();

                var stepsToDelete = await this._dbContext.TestRunSteps
                    .Where(s => oldFailedTestRunIds.Contains(s.TestRunId))
                    .ToListAsync(cancellationToken);

                this._dbContext.TestRunSteps.RemoveRange(stepsToDelete);
                this._dbContext.TestRuns.RemoveRange(oldFailedRuns);

                await this._dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private TestRunInfo MapToTestRunInfo(TestRun testRun, TestRunDefinition testDefinition)
    {
        return new TestRunInfo
        {
            ElapsedMs = testRun.ElapsedMs,
            TestDefinition = testDefinition,
            StartedAt = testRun.StartedAt,
            Steps = testRun.Steps
                .Select(s => MapToTestStepRunInfo(s, testSteps[(testDefinition.TestName, s.TestStepName)])).ToList(),
        };
    }

    private FailedTestInfo MapToFailedTestInfo(TestRun testRun)
    {
        return new FailedTestInfo
        {
            TestId = testRun.Id,
            TestName = testRun.TestName,
            StartedAt = testRun.StartedAt,
            ElapsedMs = testRun.ElapsedMs,
            FailReason = testRun.Steps
                .Where(s => s.Status == LastRunStatusEnum.Failed)
                .Select(s => s.FailReason)
                .FirstOrDefault(r => !string.IsNullOrWhiteSpace(r))
        };
    }
    private static TestStepRunInfo MapToTestStepRunInfo(TestRunStep testStep, TestStepDefinition testStepDefinition)
    {
        return new TestStepRunInfo
        {
            TestStepDefinition = testStepDefinition,
            StartedAt = testStep.StartedAt,
            ElapsedMs = testStep.ElapsedMs,
            FailReason = testStep.FailReason,
            Status = testStep.Status,
        };
    }
}
