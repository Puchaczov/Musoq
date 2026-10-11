using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExecutionCSharpRenderer = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void RenderMethod_WhenPlanContainsWindowRenderNodes_ShouldEmitWindowHelpers()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateWindowRenderNodePlan();
        var members = renderer.RenderClassMembers(plan);
        var method = renderer.RenderMethod(plan, "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();
        var memberCode = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.Contains("var resultRowNumbers = new long[resultWindowRows.Count];", code);
        Assert.Contains("resultRowNumbers[resultRowNumbersCurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbersContext);", code);
        Assert.Contains("var resultLagsValues = new string[resultWindowRows.Count];", code);
        Assert.Contains("WindowFunctionHelpers.SortStructPartitionSetInPlace(resultLagsPartitions, resultSharedOrderKeys, false);", code);
        Assert.Contains("var resultLags = new string[resultWindowRows.Count];", code);
        Assert.Contains("resultLags[resultLagsCurrentIndex] = resultLagsSourcePartitionIndex >= 0", code);
        Assert.Contains("resultWindowSumsIntOrderBuilder.ToSortedPartitionSet(false)", code);
        Assert.Contains("RunningAgeKernel.Reset(ref ", code);
        Assert.Contains("RunningAgeKernel.Set(ref resultWindowSumsState, (int)(p.Age));", code);
        Assert.Contains("AppendResultWindowRows(resultWindowRows, result, resultRowNumbers, resultLags, resultWindowSums);", code);
        Assert.Contains("resultWindowSums[windowIndex]", memberCode);
        Assert.IsFalse(code.Contains("WindowFunctionHelpers.ComputeRowNumber", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("WindowFunctionHelpers.ComputeLag", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("WindowFunctionHelpers.ComputeOrderedFramedPluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("resultWindowSumsFunction.SetArguments(Array.Empty<object?>())", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains(".AccumulateValue(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("resultWindowSumsValues", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenWindowNodesShareIntOrderKey_ShouldGenerateRankingKernelAndKeepOtherFusedBuilders()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateWindowRenderNodePlan();
        var members = renderer.RenderClassMembers(plan);
        var method = renderer.RenderMethod(plan, "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();
        var memberCode = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.Contains("var resultSharedOrderKeys = new WindowResultSharedOrderKeysKey[resultWindowRows.Count];", code);
        Assert.Contains("WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbersPartitions, resultSharedOrderKeys, false);", code);
        Assert.Contains("var resultLagsValues = new string[resultWindowRows.Count];", code);
        Assert.Contains("WindowFunctionHelpers.SortStructPartitionSetInPlace(resultLagsPartitions, resultSharedOrderKeys, false);", code);
        Assert.Contains("var resultWindowSumsIntOrderBuilder = new Musoq.Evaluator.Helpers.WindowIntOrderBuilder<string>(resultWindowRows.Count);", code);
        Assert.Contains("resultSharedOrderKeys[windowIndex] = new WindowResultSharedOrderKeysKey(p.Age);", memberCode);
        Assert.Contains("resultWindowSumsIntOrderBuilder.Add((string)p.Name, (int)p.Age, windowIndex);", code);
        Assert.IsFalse(code.Contains("resultRowNumbersIntOrderBuilder", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("resultLagsIntOrderBuilder", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("resultLagsOrderKeys", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("resultWindowSumsOrderKeys", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenPluginWindowResultIsTyped_ShouldEmitStreamingPluginLoop()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateTypedPluginWindowPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("RunningAgeKernel.State resultWindowSumsState", code);
        Assert.Contains("RunningAgeKernel.Reset(ref resultWindowSumsState)", code);
        Assert.Contains("RunningAgeKernel.Set(ref resultWindowSumsState, (int)(p.Age))", code);
        Assert.Contains("RunningAgeKernel.Get(in resultWindowSumsState)", code);
        Assert.IsFalse(code.Contains("resultWindowSumsFunction.SetArguments(Array.Empty<object?>())", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains(".AccumulateValue(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains(".GetCurrentValue(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("resultWindowSumsValues", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("WindowFunctionHelpers.ComputeTypedPluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("resultWindowSums = WindowFunctionHelpers.ComputePluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenPlanContainsGeneratedRow_ShouldReturnRowClass()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreatePlan());
        var code = members.Single(member =>
            member.NormalizeWhitespace().ToFullString().Contains("class ResultRow0", StringComparison.Ordinal))
            .NormalizeWhitespace()
            .ToFullString();
        var shapeCode = members.Single(member =>
            member.NormalizeWhitespace().ToFullString().Contains("class ResultShape0", StringComparison.Ordinal))
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("private sealed class ResultRow0 : Row", code);
        Assert.Contains("public ResultRow0(string __value0)", code);
        Assert.AreEqual(1, code.Split("public ResultRow0(").Length - 1);
        Assert.IsFalse(code.Contains("GeneratedRow", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("RowLayout", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("private object[] __values", StringComparison.Ordinal));
        Assert.Contains("public override int Count => 1;", code);
        Assert.IsFalse(code.Contains("public override object[] Values", StringComparison.Ordinal));
        Assert.DoesNotContain("public override object this[int columnNumber]", code);
        Assert.Contains("public override T GetValue<T>(int columnIndex)", code);
        Assert.DoesNotContain("public override object this[string name]", code);
        Assert.Contains("public override bool HasColumn(string name)", code);
        Assert.DoesNotContain("(object)Name", code);
        Assert.IsFalse(code.Contains("GetValue(int columnNumber)", StringComparison.Ordinal));
        Assert.Contains("private sealed class ResultShape0", shapeCode);
        Assert.Contains("public ResultShape0(string Name)", shapeCode);
        Assert.Contains("public string Name { get; }", shapeCode);
        Assert.IsFalse(shapeCode.Contains(": Row", StringComparison.Ordinal));
        Assert.IsFalse(shapeCode.Contains("AssignValue", StringComparison.Ordinal));
        Assert.IsFalse(shapeCode.Contains("HasColumn", StringComparison.Ordinal));
        Assert.IsFalse(shapeCode.Contains("this[int", StringComparison.Ordinal));
        Assert.IsFalse(shapeCode.Contains("Contexts", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenAggregateUsesKernelDescriptor_ShouldEmitStaticKernelCalls()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateKernelAggregatePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var classCode = string.Join(
            Environment.NewLine,
            renderer.RenderClassMembers(plan).Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.Contains("KernelSumAggregate.Set(ref currentGroup.__agg0", methodCode);
        Assert.Contains("KernelSumAggregate.Get(in currentGroup.__agg0)", methodCode);
        Assert.Contains("KernelSumAggregate.State __agg0", classCode);
        Assert.IsFalse(methodCode.Contains(".Accumulate(", StringComparison.Ordinal));
        Assert.IsFalse(methodCode.Contains(".GetValue()", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenGeneratedRowContextLayoutHasNoConsumers_ShouldEmitLeanConstructor()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateContextLayoutAppendPlan());
        var code = members.Single(member =>
            member.NormalizeWhitespace().ToFullString().Contains("class ResultRow0", StringComparison.Ordinal))
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains(
            "public ResultRow0(string __value0, string __value1)",
            code);
        Assert.AreEqual(1, code.Split("public ResultRow0(").Length - 1);
        Assert.IsFalse(code.Contains("__leftContext", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("__rightContext", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("object[] __contexts", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("Row __contextsRow", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenStoredRowShapeIsInternalOnly_ShouldEmitLeanCarrier()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateInternalStoredRowCarrierPlan(useRowContext: false));
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));
        var statementCode = members.Single(member =>
            member.NormalizeWhitespace().ToFullString().Contains("class Statement0Row0", StringComparison.Ordinal))
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("private sealed class Statement0Row0", code);
        Assert.IsFalse(code.Contains("private sealed class Statement0Row0 : Row", StringComparison.Ordinal));
        Assert.IsFalse(statementCode.Contains("public override object this[int columnNumber]", StringComparison.Ordinal));
        Assert.Contains("private sealed class ResultRow0 : Row", code);
    }

    [TestMethod]
    public void RenderClassMembers_WhenInternalStoredRowIsUsedAsRowContext_ShouldKeepRowCarrier()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateInternalStoredRowCarrierPlan(useRowContext: true));
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.Contains("private sealed class Statement0Row0 : Row", code);
        Assert.IsFalse(code.Contains("object[] Contexts", StringComparison.Ordinal));
        Assert.Contains("private sealed class ResultRow0 : Row", code);
        Assert.IsFalse(code.Contains("public ResultRow0(string __value0, Row __contextsRow)", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenPlanContainsExpandoAdapter_ShouldNotEmitLegacyAdapterClass()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateDynamicPlan();
        var members = renderer.RenderClassMembers(plan);
        var code = string.Join(Environment.NewLine,
            members.Select(member => member.NormalizeWhitespace().ToFullString()));
        Assert.DoesNotContain("class pDynamicRow0", code);
        Assert.IsFalse(renderer.CanRender(plan));
        Assert.AreEqual("Execution IR C# backend cannot render row shape SourceEntityShape.",
            renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void CanRender_WhenPlanContainsRemovedExpandoAdapter_ShouldRejectErasedShape()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateDynamicPlan();
        Assert.IsFalse(renderer.CanRender(plan));
        Assert.AreEqual("Execution IR C# backend cannot render row shape SourceEntityShape.",
            renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void CanRender_WhenAsOfProbeUsesDynamicMatchType_ShouldReturnFalse()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateDynamicAsOfProbePlan();

        Assert.IsFalse(renderer.CanRender(plan));
        Assert.AreEqual(
            "Execution IR C# backend cannot render row shape SourceEntityShape.",
            renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void RenderMethod_WhenAsOfProbeUsesIndex_ShouldEmitIndexFindInsteadOfFullScan()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateIndexedAsOfProbePlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "var asOfIndex = EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.Person, int, string>(rRows, (rCandidate) =>",
            code);
        Assert.Contains("new NativeEqualityKey<string>", code);
        Assert.Contains("var r = asOfIndex.Find(rEquality, l.Age);", code);
        Assert.DoesNotContain("(object)", code);
        Assert.IsFalse(code.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal));
    }
}
