using System.Diagnostics;
using DeepCheck.DTOs;
using DeepCheck.Entities;
using DeepCheck.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace DeepCheck.Helpers;

public class TestRunInfoBuilder
{
    private IState CurrentState { get; set; }

    public TestRunInfoBuilder(TestRunDefinition testDefinition)
    {
        if (testDefinition.Steps.Count == 0)
        {
            throw new ArgumentException("Test definition must have at least one step");
        }
        CurrentState = new RootState(this, testDefinition, new List<TestStepRunInfo>(), 0);
    }

    public void StartNextStep() => CurrentState.StartNextStep();
    public void FailStep(string message) => CurrentState.FailStep(message);
    public void StepDone() => CurrentState.StepDone();
    public TestRunInfo FinishTest() => CurrentState.FinishTest();

    private static TestStepRunInfo CheckLatencyOrOk(DateTime startedAt, long elapsedMs, TestStepDefinition testStepDefinition)
    {
        if (elapsedMs > testStepDefinition.LatencyCriteria)
        {
            return new TestStepRunInfo()
            {
                TestStepDefinition = testStepDefinition,
                StartedAt = startedAt,
                ElapsedMs = elapsedMs,
                Status = LastRunStatusEnum.Failed,
                FailReason = $"Latency of {elapsedMs} ms exceeded the allowed latency of {testStepDefinition.LatencyCriteria} ms."
            };
        }

        return new TestStepRunInfo()
        {
            TestStepDefinition = testStepDefinition,
            StartedAt = startedAt,
            ElapsedMs = elapsedMs,
            Status = LastRunStatusEnum.Ok,
            FailReason = null
        };
    }

    private interface IState
    {
        public void StartNextStep();
        public void FailStep(string message);
        public void StepDone();
        public TestRunInfo FinishTest();
    }

    public record RootState(TestRunInfoBuilder Builder, TestRunDefinition TestDefinition, IReadOnlyList<TestStepRunInfo> ExecutedSteps, int CurrentStepIndex) : IState
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly DateTime startedAt = DateTime.UtcNow;

        public void FailStep(string message)
        {
            //fail next step
            var nextStep = TestDefinition.Steps[CurrentStepIndex];
            var unfinishedStep = NeverRunStep(nextStep, message);
            Builder.CurrentState = this with { ExecutedSteps = ExecutedSteps.Append(unfinishedStep).ToList(), CurrentStepIndex = CurrentStepIndex + 1 };
        }

        public void StepDone()
        {
            var nextStep = TestDefinition.Steps[CurrentStepIndex];
            var unfinishedStep = NeverRunStep(nextStep, "Never run");
            Builder.CurrentState = this with { ExecutedSteps = ExecutedSteps.Append(unfinishedStep).ToList(), CurrentStepIndex = CurrentStepIndex + 1 };
        }

        public void StartNextStep()
        {
            var nextStep = TestDefinition.Steps[CurrentStepIndex];
            var updatedRoot = this with { CurrentStepIndex = CurrentStepIndex + 1 };
            Builder.CurrentState = new TestRunStepState(Builder, updatedRoot, nextStep);
        }

        public TestRunInfo FinishTest()
        {
            var result = GetTestRunInfo();
            Builder.CurrentState = new EndState(GetTestRunInfo());
            return result;
        }

        public TestRunInfo GetTestRunInfo()
        {
            return new TestRunInfo()
            {
                TestDefinition = TestDefinition,
                StartedAt = startedAt,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Steps = ExecutedSteps.Concat(GetUncompletedSteps()).ToList()
            };
        }

        public IEnumerable<TestStepRunInfo> GetUncompletedSteps() =>
            TestDefinition.Steps.Skip(CurrentStepIndex).Select(x => NeverRunStep(x, "Never run"));

        public TestStepRunInfo NeverRunStep(TestStepDefinition testStepDefinition, string msg)
            => new TestStepRunInfo()
            {
                TestStepDefinition = testStepDefinition,
                StartedAt = DateTime.UtcNow,
                ElapsedMs = 0,
                Status = LastRunStatusEnum.NeverRun,
                FailReason = msg
            };
    }

    public record TestRunStepState(TestRunInfoBuilder Builder, RootState RootState, TestStepDefinition testStepDefinition) : IState
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly DateTime startedAt = DateTime.UtcNow;

        public void FailStep(string message)
        {
            var stepResult = new TestStepRunInfo()
            {
                TestStepDefinition = testStepDefinition,
                StartedAt = startedAt,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Status = LastRunStatusEnum.Failed,
                FailReason = message
            };
            Builder.CurrentState = RootState with
            {
                ExecutedSteps = RootState.ExecutedSteps.Append(stepResult).ToList()
            };
        }

        public void StepDone()
        {
            var stepResult = TestRunInfoBuilder.CheckLatencyOrOk(startedAt, stopwatch.ElapsedMilliseconds, testStepDefinition);
            Builder.CurrentState = RootState with
            {
                ExecutedSteps = RootState.ExecutedSteps.Append(stepResult).ToList()
            };
        }

        public void StartNextStep()
        {
            StepDone();
            Builder.StartNextStep();
        }

        public TestRunInfo FinishTest()
        {
            var stepResult = TestRunInfoBuilder.CheckLatencyOrOk(startedAt, stopwatch.ElapsedMilliseconds, testStepDefinition);
            Builder.CurrentState = RootState with
            {
                ExecutedSteps = RootState.ExecutedSteps.Append(stepResult).ToList()
            };
            return Builder.FinishTest();
        }
    }

    public record EndState(TestRunInfo TestRunInfo) : IState
    {
        public TestRunInfo FinishTest() => TestRunInfo;
        public void FailStep(string message) => throw new InvalidOperationException("Test is over");
        public void StartNextStep() => throw new InvalidOperationException("Test is over");
        public void StepDone() => throw new InvalidOperationException("Test is over");
    }
}
