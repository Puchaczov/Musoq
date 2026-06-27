using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using ExecutionCSharpRenderer = Musoq.Evaluator.IR.Execution.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void RenderMethod_WhenPlanContainsPlainScanFilterProject_ShouldReturnStableCSharp()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreatePlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var pRows = pRowsSource.Chunks;", code);
        Assert.Contains("foreach (var pChunk in pRows)", code);
        Assert.IsFalse(code.Contains("pChunk is Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.Person[]", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("pChunk is List<Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.Person>", StringComparison.Ordinal));
        Assert.Contains("if (pChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.Person> pChunkView)", code);
        Assert.Contains("if (pChunkView.Source is Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.Person[] pChunkViewArray)", code);
        Assert.Contains("if (pChunkView.Source is List<Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.Person> pChunkViewList)", code);
        Assert.Contains("for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)", code);
        Assert.Contains("if ((pIndex & 1023) == 0)", code);
        Assert.Contains("token.ThrowIfCancellationRequested();", code);
        Assert.Contains("result.Add(new ResultRow0(p.Name));", code);
    }

    [TestMethod]
    public void RenderMethod_WhenExecutionRowsItemIsTableRow_ShouldIterateRowsDirectly()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateTableRowLoopPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("foreach (var t in source.Rows)", code);
        Assert.Contains("result.Add(new ResultRow0((string)t[0]));", code);
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("__tResolver.Contexts[0]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenStoredTableRowsAreReadMultipleTimes_ShouldCacheRowsLocal()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateRepeatedStoredRowsPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", code);
        Assert.AreEqual(1, CountOccurrences(code, "var __storedTable0Rows = _cteRowResults.Slot0;"));
        Assert.IsFalse(code.Contains("_tableResults[0].Rows", StringComparison.Ordinal));
        Assert.AreEqual(2, CountOccurrences(code, "foreach (var "));
        Assert.Contains("foreach (var l in __storedTable0Rows)", code);
        Assert.Contains("foreach (var r in __storedTable0Rows)", code);
    }

    [TestMethod]
    public void RenderMethod_WhenTypedMaterializedRowsBufferIsLoopSource_ShouldUseGeneratedRowIndexedLoop()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateTypedMaterializedRowsBufferPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var sourceRowsBuffer = EvaluationHelper.MaterializeGeneratedRows<SourceRow0>(source);", code);
        Assert.Contains("for (int sourceRowsBufferIndex = 0; sourceRowsBufferIndex < sourceRowsBuffer.Count; ++sourceRowsBufferIndex)", code);
        Assert.Contains("SourceRow0 s = (SourceRow0)sourceRowsBuffer[sourceRowsBufferIndex];", code);
        Assert.IsFalse(code.Contains("foreach (var s in sourceRowsBuffer)", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanHasParallelBlock_ShouldEmitParallelInvokeAndMerge()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateParallelBlockPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("Parallel.Invoke(new ParallelOptions", code);
        Assert.Contains("CancellationToken = token", code);
        Assert.Contains("MaxDegreeOfParallelism = 2", code);
        Assert.Contains("Table __parallelCteLevel0Task0Result = null;", code);
        Assert.Contains("Table __parallelCteLevel0Task1Result = null;", code);
        Assert.Contains("var cteLevel0Runner = new CteLevel0Runner", code);
        Assert.Contains("cteLevel0Runner.RunCteLevel0Task0", code);
        Assert.Contains("cteLevel0Runner.RunCteLevel0Task1", code);
        Assert.Contains("__parallelCteLevel0Task0Result = cteLevel0Runner.Task0Result;", code);
        Assert.Contains("__parallelCteLevel0Task1Result = cteLevel0Runner.Task1Result;", code);
        Assert.Contains("_tableResults[0] = __parallelCteLevel0Task0Result;", code);
        Assert.Contains("_tableResults[1] = __parallelCteLevel0Task1Result;", code);
    }

    [TestMethod]
    public void RenderMethod_WhenParallelBlockStoresTypedCteRows_ShouldEmitListTaskResults()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateTypedParallelBlockPlan();
        var method = renderer.RenderMethod(plan, "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();
        var memberCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("List<CteRow0> __parallelCteLevel0Task0Result = null;", code);
        Assert.Contains("List<CteRow0> __parallelCteLevel0Task1Result = null;", code);
        Assert.Contains("var cte0 = new List<CteRow0>();", memberCode);
        Assert.Contains("var cte1 = new List<CteRow0>();", memberCode);
        Assert.Contains("private static List<CteRow0> BuildCteLevel0Task0", memberCode);
        Assert.Contains("private static List<CteRow0> BuildCteLevel0Task1", memberCode);
        Assert.Contains("public List<CteRow0> Task0Result", memberCode);
        Assert.Contains("public List<CteRow0> Task1Result", memberCode);
        Assert.Contains("_cteRowResults.Slot0 = __parallelCteLevel0Task0Result;", code);
        Assert.Contains("_cteRowResults.Slot1 = __parallelCteLevel0Task1Result;", code);
        Assert.IsFalse(memberCode.Contains("new Table(\"cte0\"", StringComparison.Ordinal), memberCode);
        Assert.IsFalse(memberCode.Contains("new Table(\"cte1\"", StringComparison.Ordinal), memberCode);
        Assert.IsFalse(code.Contains("_tableResults[0]", StringComparison.Ordinal), code);
        Assert.IsFalse(code.Contains("_tableResults[1]", StringComparison.Ordinal), code);
    }

    [TestMethod]
    public void RenderMethod_WhenCreateTableHasCapacityHint_ShouldEmitEnsureCapacity()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateCapacityHintPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("result.EnsureCapacity(16);", code);
    }

    [TestMethod]
    public void RenderMethod_WhenCreateHashHasCapacityHint_ShouldEmitDictionaryCapacity()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateHashCapacityHintPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var hash = new Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tables.Row>>(32);", code);
    }

    [TestMethod]
    public void RenderMethod_WhenCreateHashHasEnumerableCapacityHint_ShouldEmitTryGetNonEnumeratedCount()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateHashEnumerableCapacityHintPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "var hash = new Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tables.Row>>(rows.TryGetNonEnumeratedCount(out var hashCapacity) ? hashCapacity : 0);",
            code);
    }

    [TestMethod]
    public void RenderMethod_WhenHashJoinKeyIsValueTuple_ShouldEmitTypedTupleKey()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateValueTupleHashPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var hash = new Dictionary<ValueTuple<int, int>, HashJoinBucket<Musoq.Evaluator.Tables.Row>>();", code);
        Assert.Contains("var key = (1, 2);", code);
        Assert.IsFalse(code.Contains("CreateNullableHashJoinKey", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenAppendRowUsesDirectMode_ShouldEmitAddDirect()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateDirectAppendPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("result.AddDirect(new ResultRow0(\"Ada\"));", code);
        Assert.IsFalse(code.Contains("result.Add(new ResultRow0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenAppendRowHasContextLayoutWithoutConsumers_ShouldPruneSourceContexts()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateContextLayoutAppendPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "result.AddDirect(new ResultRow0(p.Name, q.Name));",
            code);
        Assert.IsFalse(code.Contains("(object)p", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("(object)q", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("new object[] { p, q }", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenEnumerableRowsItemIsDirectScalar_ShouldIterateTypedValuesDirectly()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateScalarEnumerableLoopPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var nRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(numbers);", code);
        Assert.Contains("foreach (var nChunk in nRows)", code);
        Assert.Contains("for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)", code);
        Assert.Contains("var n = nChunk[nIndex];", code);
        Assert.Contains("result.Add(new ResultRow0(n));", code);
        Assert.IsFalse(code.Contains("n.Value", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("nRows.Rows", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("__nResolver.Contexts[0]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenEnumerableSourceIsSameTypedMethodCall_ShouldAvoidDuplicateCast()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateScalarMethodCallEnumerableLoopPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "var sRows = EvaluationHelper.ConvertEnumerableOutputToChunks<string>((string[])library0.JustReturnArrayOfString());",
            code);
        Assert.Contains("foreach (var sChunk in sRows)", code);
        Assert.Contains("for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)", code);
        Assert.Contains("var library0 = new Musoq.Evaluator.Tests.Schema.Basic.Library();", code);
        Assert.IsFalse(code.Contains("(string[])(string[])", StringComparison.Ordinal));
    }
}
