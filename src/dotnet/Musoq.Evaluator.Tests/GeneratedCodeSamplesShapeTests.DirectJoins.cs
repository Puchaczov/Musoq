using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void DirectSemiJoin_WhenCompiledForInspection_ShouldUsePayloadFreeKeySet()
    {
        var result = CompileDirectJoinForInspection(
            "select a.Name from #A.entities() a semi join #B.entities() b on a.Id = b.Id");

        Assert.Contains("PhysicalHashJoin [LeftSemi]", result.PhysicalPlanText);
        Assert.Contains("CreateKeySet [", result.ExecutionPlanText);
        Assert.Contains("KeySetAdd [", result.ExecutionPlanText);
        Assert.Contains("KeySetProbe [", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateHash [", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("HashJoinBucket", StringComparison.Ordinal));
        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
    }

    [TestMethod]
    public void DirectSemiJoin_WhenEquiPredicateHasResidual_ShouldUseHashPayload()
    {
        var result = CompileDirectJoinForInspection(
            "select a.Name from #A.entities() a semi join #B.entities() b on a.Id = b.Id and b.Population > 100");

        Assert.Contains("PhysicalHashJoin [LeftSemi]", result.PhysicalPlanText);
        Assert.Contains("CreateHash [", result.ExecutionPlanText);
        Assert.Contains("HashProbe [", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateKeySet [", StringComparison.Ordinal));
        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
    }

    [TestMethod]
    public void DirectAntiJoin_WhenCompiledForInspection_ShouldUsePayloadFreeKeySetNoMatchProbe()
    {
        var result = CompileDirectJoinForInspection(
            "select a.Name from #A.entities() a anti join #B.entities() b on a.Id = b.Id");

        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", result.PhysicalPlanText);
        Assert.Contains("CreateKeySet [", result.ExecutionPlanText);
        Assert.Contains("KeySetAdd [", result.ExecutionPlanText);
        Assert.Contains("KeySetProbe [", result.ExecutionPlanText);
        Assert.Contains("KeySetProbeNoMatch", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateHash [", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("HashJoinBucket", StringComparison.Ordinal));
        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
    }

    [TestMethod]
    public void DirectAntiJoin_WhenEquiPredicateHasResidual_ShouldUseHashPayloadNoMatchProbe()
    {
        var result = CompileDirectJoinForInspection(
            "select a.Name from #A.entities() a anti join #B.entities() b on a.Id = b.Id and b.Population > 100");

        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", result.PhysicalPlanText);
        Assert.Contains("CreateHash [", result.ExecutionPlanText);
        Assert.Contains("HashProbe [", result.ExecutionPlanText);
        Assert.Contains("HashProbeNoMatch", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateKeySet [", StringComparison.Ordinal));
        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
    }

    [TestMethod]
    public void DirectCrossJoin_WhenCompiledForInspection_ShouldUseNestedLoopWithoutHashState()
    {
        var result = CompileDirectJoinForInspection(
            "select a.Name, b.Name from #A.entities() a cross join #B.entities() b");

        Assert.Contains("PhysicalNestedLoopJoin [Cross] [TRUE]", result.PhysicalPlanText);
        Assert.Contains("MaterializeChunked [bRows -> bRowsBuffer]", result.ExecutionPlanText);
        Assert.Contains("ChunkedForEach [a in aRows]", result.ExecutionPlanText);
        Assert.Contains("ForEach [b in bRowsBuffer]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateHash [", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateKeySet [", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("If [TRUE]", StringComparison.Ordinal));
        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
    }

    private static QueryInspectionResult CompileDirectJoinForInspection(string query)
    {
        return InstanceCreator.CompileForInspection(
            query,
            "GeneratedSample_DirectJoinInspection",
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    { "#A", Array.Empty<BasicEntity>() },
                    { "#B", Array.Empty<BasicEntity>() }
                }),
            new TestsLoggerResolver());
    }
}
