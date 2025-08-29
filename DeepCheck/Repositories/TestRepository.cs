using System.Diagnostics;
using System.Text.Json;
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

    public async Task<UptimeTestRunInfo> GetUptimeTestRunInfoAsync(int countPerTestStepName, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, IReadOnlyList<TestRunStep>> testSteps = await _dbContext.TestRunSteps
            .GroupBy(s => s.TestStepName)
            .Select(g => new
            {
                g.Key,
                Items = g.OrderByDescending(s => s.StartedAt).Take(countPerTestStepName).ToList()
            })
            // optionally choose which groups to include:
            .ToDictionaryAsync(x => x.Key, x => (IReadOnlyList<TestRunStep>)x.Items, cancellationToken);

        return new UptimeTestRunInfo { Steps = testSteps };
    }

    public async Task RemoveOldTestRunsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var oldTestRuns = await _dbContext.TestRuns
                .Where(tr => tr.StartedAt.AddMilliseconds(tr.ElapsedMs) < olderThan)
                .ToListAsync(cancellationToken);

            if (oldTestRuns.Any())
            {
                var oldTestRunIds = oldTestRuns.Select(tr => tr.Id).ToList();

                var oldSteps = await _dbContext.TestRunSteps
                    .Where(s => oldTestRunIds.Contains(s.TestRunId))
                    .ToListAsync(cancellationToken);

                _dbContext.TestRunSteps.RemoveRange(oldSteps);
                _dbContext.TestRuns.RemoveRange(oldTestRuns);

                await _dbContext.SaveChangesAsync(cancellationToken);
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
