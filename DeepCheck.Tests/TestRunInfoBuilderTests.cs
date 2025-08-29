namespace DeepCheck.Tests;

using System;
using System.Linq;
using System.Collections.Generic;
using DeepCheck.Helpers;
using DeepCheck.Models;
using DeepCheck.DTOs;
using Xunit;
using Microsoft.AspNetCore.Identity.Data;

public class TestRunInfoBuilderTests
{
    [Fact]
    public void FinishWithoutStarting_AllStepsNeverRun()
    {
        var steps = new List<TestStepDefinition>
        {
            new TestStepDefinition("step1", "desc", 1000),
            new TestStepDefinition("step2", "desc", 1000)
        };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        var info = builder.FinishTest();

        Assert.Equal(2, info.Steps.Count);
        Assert.All(info.Steps, s => Assert.Equal(LastRunStatusEnum.NeverRun, s.Status));
        Assert.Equal(LastRunStatusEnum.NeverRun, info.Status);
    }

    [Fact]
    public void StartStep_Then_StepDone_MarksOk_And_UncompletedRemain()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("step1", "desc", int.MaxValue), new TestStepDefinition("step2", "desc", int.MaxValue) };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        builder.StartNextStep();
        builder.StepDone();
        var info = builder.FinishTest();

        Assert.Equal(2, info.Steps.Count);
        Assert.Equal(LastRunStatusEnum.Ok, info.Steps[0].Status);
        Assert.Equal(LastRunStatusEnum.NeverRun, info.Steps[1].Status);
        Assert.Equal(LastRunStatusEnum.Failed, info.Status);
    }

    [Fact]
    public void StartStep_Then_FailStep_MarksFailed_WithMessage()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("step1", "desc", 1000) };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        builder.StartNextStep();
        builder.FailStep("something bad");
        var info = builder.FinishTest();

        Assert.Single(info.Steps);
        Assert.Equal(LastRunStatusEnum.Failed, info.Steps[0].Status);
        Assert.Equal("something bad", info.Steps[0].FailReason);
        Assert.Equal(LastRunStatusEnum.Failed, info.Status);
    }

    [Fact]
    public void RootFailStep_WithoutStart_MarksStepFailed_And_OthersNeverRun()
    {
        var steps = new List<TestStepDefinition>
        {
            new TestStepDefinition("step1", "desc", 1000),
            new TestStepDefinition("step2", "desc", 1000)
        };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        builder.FailStep("fatal");
        var info = builder.FinishTest();

        Assert.Equal(2, info.Steps.Count);
        Assert.Equal(LastRunStatusEnum.NeverRun, info.Steps[0].Status);
        Assert.Equal("fatal", info.Steps[0].FailReason);
        Assert.Equal(LastRunStatusEnum.NeverRun, info.Steps[1].Status);
        Assert.Equal(LastRunStatusEnum.NeverRun, info.Status);
    }

    [Fact]
    public void ActionsAfterFinish_ThrowInvalidOperationException()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("step1", "desc", 1000) };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        var info = builder.FinishTest();

        Assert.Throws<InvalidOperationException>(() => builder.StartNextStep());
        Assert.Throws<InvalidOperationException>(() => builder.StepDone());
        Assert.Throws<InvalidOperationException>(() => builder.FailStep("x"));
    }
    [Fact]
    public void Constructor_Throws_WhenNoSteps()
    {
        var steps = new List<TestStepDefinition>();
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);

        Assert.Throws<ArgumentException>(() => new TestRunInfoBuilder(def));
    }

    [Fact]
    public void StepLatency_Exceeds_Fails()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("step1", "desc", 1) }; // 1 ms allowed
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        builder.StartNextStep();
        System.Threading.Thread.Sleep(50); // ensure elapsed > latency
        builder.StepDone();
        var info = builder.FinishTest();

        Assert.Single(info.Steps);
        Assert.Equal(LastRunStatusEnum.Failed, info.Steps[0].Status);
        Assert.Contains("exceeded the allowed latency", info.Steps[0].FailReason);
    }

    [Fact]
    public void StepLatency_Within_Ok()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("step1", "desc", 10000) }; //large latency
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        builder.StartNextStep();
        // no sleep -> should be within allowed latency
        builder.StepDone();

        var info = builder.FinishTest();

        Assert.Single(info.Steps);
        Assert.Equal(LastRunStatusEnum.Ok, info.Steps[0].Status);
        Assert.Equal(LastRunStatusEnum.Ok, info.Status);
    }


    [Fact]
    public void StepLatency_2Steps_First_Within_Ok_Second_Fail()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("step1", "desc", 1), new TestStepDefinition("step1", "desc", 10000) };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        builder.StartNextStep();
        Thread.Sleep(50);
        builder.StepDone();
        builder.StartNextStep();
        Thread.Sleep(50);
        builder.StepDone();
        var info = builder.FinishTest();

        Assert.Equal(2, info.Steps.Count);
        Assert.Equal(LastRunStatusEnum.Failed, info.Steps[0].Status);
        Assert.Contains("exceeded the allowed latency", info.Steps[0].FailReason);
        Assert.Equal(LastRunStatusEnum.Ok, info.Steps[1].Status);
        Assert.Equal(LastRunStatusEnum.Failed, info.Status);
    }

    [Fact]
    public void ShortCutBuilding_2Steps_Success()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("step1", "desc", int.MaxValue), new TestStepDefinition("step2", "desc", int.MaxValue) };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);

        var builder = new TestRunInfoBuilder(def);

        builder.StartNextStep();
        builder.StartNextStep();
        var info = builder.FinishTest();

        Assert.Equal(2, info.Steps.Count);
        Assert.Equal(LastRunStatusEnum.Ok, info.Steps[0].Status);
        Assert.Equal(LastRunStatusEnum.Ok, info.Steps[1].Status);
        Assert.Equal(LastRunStatusEnum.Ok, info.Status);
    }

    [Fact]
    public void FinishTest_WhileStepRunning_AppendsCurrentStepResult()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("step1", "desc", int.MaxValue) };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        builder.StartNextStep();
        var info = builder.FinishTest();

        Assert.Single(info.Steps);
        Assert.Equal(LastRunStatusEnum.Ok, info.Steps[0].Status);
        Assert.Equal(LastRunStatusEnum.Ok, info.Status);
    }

    [Fact]
    public void TestRunInfo_FailReason_ReturnsFirstFailedStepReason()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("s1", "d", 10000), new TestStepDefinition("s2", "d", 10000) };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        builder.StartNextStep();
        builder.FailStep("boom");
        var info = builder.FinishTest();

        Assert.Equal("boom", info.FailReason);
        Assert.Equal(LastRunStatusEnum.Failed, info.Steps[0].Status);
        Assert.Equal(LastRunStatusEnum.NeverRun, info.Steps[1].Status);
        Assert.Equal(LastRunStatusEnum.Failed, info.Status);
    }

    [Fact]
    public void Root_StepDone_WithoutStart_MarksNeverRun()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("s1", "d", 10000) };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        builder.StepDone();
        var info = builder.FinishTest();

        Assert.Single(info.Steps);
        Assert.Equal(LastRunStatusEnum.NeverRun, info.Steps[0].Status);
        Assert.Equal("Never run", info.Steps[0].FailReason);
        Assert.Equal(LastRunStatusEnum.NeverRun, info.Status);
    }
    [Fact]
    public void StepLatency_Measured_Is_CloseToRealLatency()
    {
        var steps = new List<TestStepDefinition> { new TestStepDefinition("step1", "desc", 10000), new TestStepDefinition("step2", "desc", 20000) };
        var def = new TestRunDefinition("t", "d", "* * * * *", steps);
        var builder = new TestRunInfoBuilder(def);

        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        builder.StartNextStep();
        System.Threading.Thread.Sleep(75);
        builder.StepDone();
        sw1.Stop();

        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        builder.StartNextStep();
        System.Threading.Thread.Sleep(150);
        builder.StepDone();
        sw2.Stop();

        var info = builder.FinishTest();

        Assert.Equal(2, info.Steps.Count);
        AssertMesuredLatency(sw1.ElapsedMilliseconds, info.Steps[0].ElapsedMs);
        AssertMesuredLatency(sw2.ElapsedMilliseconds, info.Steps[1].ElapsedMs);
    }

    private static void AssertMesuredLatency(long measured, long actual)
    {
        var diff = Math.Abs(measured - actual);
        Assert.True(diff <= 30, $"Measured latency {measured} differs from actual {actual} by {diff} ms (tolerance 30ms)");
    }
}
