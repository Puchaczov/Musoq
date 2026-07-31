using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void RecursivePriorMaterializedCteSample_ShouldBuildAndReuseSidecarBeforeFixedPointLoop()
    {
        var execution = ReadExecutionPlan("Q205_RecursivePriorMaterializedCte.cs");
        var createIndex = execution.IndexOf("CreateHash [cte0HashSidecar0Sourceid", StringComparison.Ordinal);
        var recursiveIndex = execution.IndexOf("RecursiveCte [reachable;", StringComparison.Ordinal);
        var loadIndex = execution.IndexOf("LoadCteIndex [cte1Invariant0Hash", StringComparison.Ordinal);
        var memberIndex = execution.IndexOf("RecursiveMember", loadIndex, StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, createIndex);
        Assert.IsGreaterThan(createIndex, recursiveIndex);
        Assert.IsGreaterThan(recursiveIndex, loadIndex);
        Assert.IsGreaterThan(loadIndex, memberIndex);
        Assert.AreEqual(1, CountText(execution, "CreateHash [cte0HashSidecar0Sourceid"));
        Assert.AreEqual(1, CountText(execution, "LoadCteIndex [cte1Invariant0Hash"));
        Assert.IsFalse(execution.Contains("CreateHash [cte1Invariant0Hash", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RecursiveCompositionSamples_ShouldKeepEachFixedPointAsDedicatedSequentialNode()
    {
        var independent = ReadExecutionPlan("Q207_RecursiveTwoIndependentCtes.cs");
        var dependent = ReadExecutionPlan("Q208_RecursiveDependsOnEarlierRecursive.cs");

        Assert.AreEqual(2, CountText(independent, "RecursiveCte ["));
        Assert.AreEqual(2, CountText(dependent, "RecursiveCte ["));
        Assert.AreEqual(2, CountText(ReadGeneratedCode("Q207_RecursiveTwoIndependentCtes.cs"), "while (cte"));
        Assert.AreEqual(2, CountText(ReadGeneratedCode("Q208_RecursiveDependsOnEarlierRecursive.cs"), "while (cte"));

        var firstStoreIndex = dependent.IndexOf("StoreTable [cte0", StringComparison.Ordinal);
        var secondRecursiveIndex = dependent.IndexOf("RecursiveCte [second;", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, firstStoreIndex);
        Assert.IsGreaterThan(firstStoreIndex, secondRecursiveIndex);
    }

    [TestMethod]
    public void RecursiveDeadAndPrunedSamples_ShouldRemoveUnusedLoopAndPayloadState()
    {
        var deadExecution = ReadExecutionPlan("Q209_RecursiveUnusedDefinition.cs");
        var deadCode = ReadGeneratedCode("Q209_RecursiveUnusedDefinition.cs");
        var prunedExecution = ReadExecutionPlan("Q210_RecursiveProjectionPrunedState.cs");
        var prunedCode = ReadGeneratedCode("Q210_RecursiveProjectionPrunedState.cs");

        Assert.IsFalse(deadExecution.Contains("RecursiveCte [", StringComparison.Ordinal));
        Assert.IsFalse(deadCode.Contains("while (cte", StringComparison.Ordinal));
        Assert.Contains("Id: int <- field Id", prunedExecution);
        Assert.Contains("Depth: int <- field Depth", prunedExecution);
        Assert.IsFalse(prunedExecution.Contains("Path:", StringComparison.Ordinal));
        Assert.IsFalse(prunedCode.Contains(" Path", StringComparison.Ordinal));
        Assert.IsFalse(prunedCode.Contains("->next", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RecursiveOuterConsumerSamples_ShouldRunAfterCompletedRecursiveResult()
    {
        AssertOuterConsumerAfterRecursion("Q211_RecursiveOuterFilterOrder.cs", "SortShapeRows [");
        AssertOuterConsumerAfterRecursion("Q212_RecursiveOuterJoin.cs", "CreateHash [lHash");
        AssertOuterConsumerAfterRecursion("Q213_RecursiveOuterAggregate.cs", "CreateAggregateContext [");
        AssertOuterConsumerAfterRecursion("Q214_RecursiveOuterWindowAndSet.cs", "ComputeRowNumberWindow [");

        var windowSet = ReadExecutionPlan("Q214_RecursiveOuterWindowAndSet.cs");
        Assert.Contains("SetOperation [result = left UnionAll right", windowSet);
    }

    private static void AssertOuterConsumerAfterRecursion(string fileName, string consumerMarker)
    {
        var execution = ReadExecutionPlan(fileName);
        var recursiveIndex = execution.IndexOf("RecursiveCte [walk;", StringComparison.Ordinal);
        var storeIndex = execution.IndexOf("StoreTable [cte0", recursiveIndex, StringComparison.Ordinal);
        var consumerIndex = execution.IndexOf(consumerMarker, StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, recursiveIndex, fileName);
        Assert.IsGreaterThan(recursiveIndex, storeIndex, fileName);
        Assert.IsGreaterThan(storeIndex, consumerIndex, fileName);
    }

    private static string ReadExecutionPlan(string fileName)
    {
        var content = ReadSample(fileName).Content;
        return ReadGeneratedSampleSection(content, "Execution Plan", "Generated C#");
    }

    private static string ReadGeneratedCode(string fileName)
    {
        var content = ReadSample(fileName).Content;
        return ReadGeneratedSampleSection(content, "Generated C#", null);
    }
}
