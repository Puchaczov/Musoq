using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void SpecificationJoinSnapshots_WhenInspected_ShouldPreserveOperationAndRowPresenceShapes()
    {
        var semi = ReadSample("Q278_SpecCoreDirectSemiJoin.cs").Content;
        Assert.Contains("[LeftSemi]", semi);
        Assert.Contains("KeySetProbe", ExecutionPlan("Q278_SpecCoreDirectSemiJoin.cs"));
        Assert.Contains("[LeftSemi]", ReadSample("Q279_SpecCoreDirectSemiJoinResidual.cs").Content);
        Assert.Contains("[residual:", ReadSample("Q279_SpecCoreDirectSemiJoinResidual.cs").Content);

        var anti = ReadSample("Q280_SpecCoreDirectAntiJoin.cs").Content;
        Assert.Contains("[LeftAntiSemi]", anti);
        Assert.Contains("[LeftAntiSemi]", ReadSample("Q281_SpecCoreDirectAntiJoinResidual.cs").Content);
        Assert.Contains("[residual:", ReadSample("Q281_SpecCoreDirectAntiJoinResidual.cs").Content);

        var cross = ReadSample("Q282_SpecCoreDirectCrossJoin.cs").Content;
        Assert.Contains("[Cross]", cross);
        Assert.Contains("PhysicalNestedLoopJoin [Cross]", cross);

        var full = ReadSample("Q283_SpecCoreFullOuterJoinRowPresence.cs").Content;
        Assert.Contains("[FullOuter]", full);
        Assert.Contains("NULL", full);
        Assert.Contains("PhysicalNestedLoopJoin [FullOuter]", ReadSample("Q284_SpecCoreFullOuterJoinNonEqui.cs").Content);

        var asOf = ReadSample("Q285_SpecCoreAsOfLeftJoinRowPresence.cs").Content;
        Assert.Contains("[AsofLeft]", asOf);
        Assert.Contains("AsOfProbeNoMatch", asOf);
        Assert.Contains("b.Name: NULL", asOf);
    }

    [TestMethod]
    public void SpecificationAggregateAndSetSnapshots_WhenInspected_ShouldExposeTypedKernelsAndScopes()
    {
        var parentAggregate = ReadSample("Q286_SpecCoreParentLevelAggregate.cs").Content;
        Assert.Contains("PhysicalValueTupleAggregate", parentAggregate);
        Assert.Contains("AggregateGroup [ResultAggregateGroupPrefix1", parentAggregate);
        Assert.Contains("TypedAggregateSet", ExecutionPlan("Q286_SpecCoreParentLevelAggregate.cs"));

        var filteredAggregate = ExecutionPlan("Q287_SpecCoreAggregateFilter.cs");
        Assert.Contains("filter (where", ReadSample("Q287_SpecCoreAggregateFilter.cs").Content);
        Assert.Contains("TypedAggregateSet [Set(group.__agg0) filter", filteredAggregate);
        Assert.Contains("typed aggs: 1", filteredAggregate);

        var windows = ExecutionPlan("Q290_SpecCoreNamedWindowAnalytics.cs");
        Assert.Contains("ComputeNtileWindow", windows);
        Assert.Contains("ComputeFirstValueWindow", windows);
        Assert.Contains("ComputeLastValueWindow", windows);
        Assert.Contains("ComputeNthValueWindow", windows);
        Assert.Contains("ComputeMinWindowKernel", windows);
        Assert.Contains("ComputeMaxWindowKernel", windows);

        var globalSet = ExecutionPlan("Q291_SpecCoreSetResultModifiers.cs");
        Assert.Contains("SetOperation [result = left UnionAll right", globalSet);
        Assert.Contains("TopOffsetRowBuffer", globalSet);
        Assert.Contains("skip 1, take 3", globalSet);

        var branchSet = ExecutionPlan("Q292_SpecCoreSetBranchLocalSlice.cs");
        Assert.Contains("TopNTable [cte0 -> cte0TopN", branchSet);
        Assert.Contains("SortShapeRows [result -> resultSorted", branchSet);
    }

    [TestMethod]
    public void SpecificationTableAndInterpretationSnapshots_WhenInspected_ShouldRouteMetadataAndSpecialOperations()
    {
        var typeMatrix = ReadSample("Q319_SpecTableTypeMatrix.cs").Content;
        Assert.Contains("new Column(\"NullableInt\"", typeMatrix);
        Assert.Contains("typeof(int?)", typeMatrix);
        Assert.Contains("SourceEntity [ko3iko: SpecificationTypeMatrixEntity]", typeMatrix);
        Assert.DoesNotContain("AdaptExpando", typeMatrix);

        var modifiers = ReadSample("Q320_SpecTableReadModifiers.cs").Content;
        Assert.Contains("\"encoding\", \"windows-1250\"", modifiers);
        Assert.Contains("\"culture\", \"pl-PL\"", modifiers);
        Assert.Contains("\"format\", \"#,##0.00\"", modifiers);
        Assert.Contains("\"source.codec\", \"base64\"", modifiers);

        var switchShape = ReadSample("Q307_SpecBinarySwitchTaggedUnion.cs").Content;
        Assert.Contains("InterpretSource [Packet.Interpret", switchShape);
        Assert.Contains("Switch_Payload", switchShape);

        var rawSubstream = ReadSample("Q308_SpecBinaryRawSubstream.cs").Content;
        Assert.Contains("InterpretSource [Packet.Interpret", rawSubstream);
        Assert.Contains("ReadBytes(data, (int)_length)", rawSubstream);

        var structuredSubstreams = ReadSample("Q309_SpecBinaryStructuredSubstreams.cs").Content;
        Assert.Contains("EnsureSubstreamFullyConsumed", structuredSubstreams);
        Assert.Contains("ReadSubstreamSlice", structuredSubstreams);

        var safeAndOffset = ReadSample("Q310_SpecBinaryTryInterpretAndInterpretAt.cs").Content;
        Assert.Contains("Header.TryInterpret", safeAndOffset);
        Assert.Contains("Payload.InterpretAt", safeAndOffset);

        var partial = ReadSample("Q311_SpecBinaryPartialInterpret.cs").Content;
        Assert.Contains("PartialInterpretResult", partial);
        Assert.Contains(".PartialInterpret", partial);

        var textPartial = ReadSample("Q318_SpecTextTryParseAndPartialParse.cs").Content;
        Assert.Contains("KeyValue.TryParse", textPartial);
        Assert.Contains("KeyValue.PartialParse", textPartial);
    }

    private static string ExecutionPlan(string fileName)
    {
        var sample = ReadSample(fileName);
        return ReadGeneratedSampleSection(sample.Content, "Execution Plan", "Generated C#");
    }
}
