using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenQueryIsValid_ShouldReturnExecutionPlanText()
    {
        var result = CreateInspection();

        AssertTextEquals(
            string.Join("\n",
                "ExecutionPlan [compiled]",
                "  Shapes",
                "    SourceEntity [d: DualEntity]",
                "      Dummy: string <- property Dummy",
                "    Generated [ResultRow0]",
                "      d.Dummy: string <- field d_Dummy",
                string.Empty,
                "  Body",
                "    SourceScan [d: DualEntity] -> dRows",
                "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
                "    ChunkedForEach [d in dRows]",
                "      AppendShape [result <- ResultShape0(d.Dummy: d.Dummy)]",
                "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]"),
            result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenQueryIsValid_ShouldReturnGeneratedCSharpCode()
    {
        var result = CreateInspection();

        Assert.Contains("public sealed class CompiledQuery", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenQueryShapeIsSupported_ShouldEmitExecutionBackendCodeByDefault()
    {
        var result = CreateInspection();

        Assert.Contains("__dSchema.GetRowSource", result.GeneratedCSharpCode);
        Assert.Contains("private sealed class ResultRow0", result.GeneratedCSharpCode);
        Assert.Contains("private IEnumerable<ResultRow0> ComputeRows_compiled_0(", result.GeneratedCSharpCode);
        Assert.Contains("QueryTableEnumerable<ResultRow0>", result.GeneratedCSharpCode);
        Assert.Contains("TableProjectionRows.ProjectRowsSerial<", result.GeneratedCSharpCode);
        Assert.Contains("new ResultRow0(d.Dummy", result.GeneratedCSharpCode);
        Assert.Contains("d_Dummy = __value0;", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenCommonSubexpressionEliminationIsEnabled_ShouldHoistRepeatedFieldReads()
    {
        var result = Inspect(
            "select d.Dummy, d.Dummy from #system.dual() d where d.Dummy = d.Dummy",
            new CompilationOptions(useCommonSubexpressionElimination: true));

        Assert.Contains("Let [dummy:", result.ExecutionPlanText);
        Assert.Contains("If [(dummy = dummy)]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenCommonSubexpressionEliminationIsDisabled_ShouldNotHoistRepeatedFieldReads()
    {
        var result = Inspect(
            "select d.Dummy, d.Dummy from #system.dual() d where d.Dummy = d.Dummy",
            new CompilationOptions(useCommonSubexpressionElimination: false));

        Assert.IsFalse(result.ExecutionPlanText.Contains("Let [dummy:", StringComparison.Ordinal));
        Assert.Contains("If [(d.Dummy = d.Dummy)]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenRepeatedValueTypedMethodIsHoisted_ShouldUseRowLocalTempWithoutSharedCache()
    {
        var result = Inspect(
            "select ExpensiveMethod(Score), ExpensiveMethod(Score) + 10, Name from #dynamic.all() where ExpensiveMethod(Score) > 0",
            CreateDynamicRowsSchemaProvider(),
            new CompilationOptions(useCommonSubexpressionElimination: true));

        Assert.Contains("Let [expensiveMethod: int = ExpensiveMethod(score)]", result.ExecutionPlanText);
        Assert.Contains("If [(expensiveMethod > 0)]", result.ExecutionPlanText);
        Assert.Contains("int expensiveMethod = (int)__resultEmptyLibrary0.ExpensiveMethod(score);", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ConcurrentDictionary", StringComparison.Ordinal) || result.GeneratedCSharpCode.Contains("GetOrAddCachedMethod<Musoq.Converter.Tests.Schema.EmptyLibrary, int, int>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderOuterNestedLoopJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d left outer join #system.dual() e on d.Dummy <> e.Dummy");

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("If [(dummy <> dummy1)]", result.ExecutionPlanText);
        Assert.Contains("e.Dummy: NULL", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderAsOfJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d asof join #system.dual() e on d.Dummy >= e.Dummy");

        Assert.Contains("ExecutionPlan [compiled]", result.ExecutionPlanText);
        AssertExecutionPlanContains("AsOfProbe [e <- eRows", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.CreateAsOfIndex<", result.GeneratedCSharpCode);
        Assert.Contains("resultAsOfIndex.Find", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.GetColumnValue", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderAsOfLeftJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d asof left join #system.dual() e on d.Dummy >= e.Dummy");

        Assert.Contains("ExecutionPlan [compiled]", result.ExecutionPlanText);
        Assert.Contains("AsOfProbeNoMatch", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.CreateAsOfIndex<", result.GeneratedCSharpCode);
        Assert.Contains("resultAsOfIndex.Find", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.GetColumnValue", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCteBackedAsOfJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateCteBackedAsOfJoinQuery());

        AssertUsesExecutionBackend(result);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("AsOfProbe [r <- _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.CreateAsOfIndex<", result.GeneratedCSharpCode);
        Assert.Contains("resultAsOfIndex.Find", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.GetColumnValue", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderCteBackedAsOfJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateCteBackedAsOfJoinQuery());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("single", table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingSeesDynamicAsOfRightSource_ShouldFailWithoutOldRenderer()
    {
        var exception = Assert.Throws<NotSupportedException>(() =>
            Inspect(CreateDynamicAsOfJoinQuery(),
                CreateDynamicRowsSchemaProvider()));

        Assert.Contains(
            "Execution IR ASOF join lowering requires a non-dynamic source-entity or table-row right source. Found ExpandoAdapterShape with row type IReadOnlyDictionary`2.",
            exception.Message);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingSeesDynamicAsOfRightSource_ShouldFailWithoutOldRenderer()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileForExecution(CreateDynamicAsOfJoinQuery(),
                CreateDynamicRowsSchemaProvider()));

        Assert.Contains(
            "Execution IR ASOF join lowering requires a non-dynamic source-entity or table-row right source. Found ExpandoAdapterShape with row type IReadOnlyDictionary`2.",
            exception.Message);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderDynamicCteBackedAsOfJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateDynamicCteBackedAsOfJoinQuery(), CreateDynamicRowsSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.Contains("ExpandoAdapter [d: dDynamicRow0]", result.ExecutionPlanText);
        Assert.Contains("ExpandoAdapter [l: lDynamicRow0]", result.ExecutionPlanText);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("AsOfProbe [r <- _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.CreateAsOfIndex<Cte0Row0, int>", result.GeneratedCSharpCode);
        Assert.Contains("resultAsOfIndex.Find", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.GetColumnValue", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.ConvertTableToSource", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderDynamicCteBackedAsOfJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateDynamicCteBackedAsOfJoinQuery(), CreateDynamicRowsSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("ada", table[0][0]);
        Assert.AreEqual("ada", table[0][1]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual("bea", table[1][1]);
        Assert.AreEqual("cid", table[2][0]);
        Assert.AreEqual("cid", table[2][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderDistinctProjection_ShouldUseLeanDistinctSet()
    {
        var result = Inspect("select distinct d.Dummy from #system.dual() d");

        Assert.Contains("ExecutionPlan [compiled]", result.ExecutionPlanText);
        Assert.Contains("CreateKeySet [distinctKeys: string]", result.ExecutionPlanText);
        Assert.Contains("If [Add(dummy)]", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("var distinctKeys = new HashSet<string>();", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("TryGetNonEnumeratedCount(out var distinctKeysCapacity)", StringComparison.Ordinal));
        Assert.Contains("if ((bool)distinctKeys.Add(dummy))", result.GeneratedCSharpCode);
        Assert.IsFalse(result.ExecutionPlanText.Contains("DistinctTable", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.ToDistinctTable", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderScalarMethodCall_ShouldUseExecutionBackend()
    {
        var result = Inspect("select ToUpper(d.Dummy) from #system.dual() d");

        AssertUsesExecutionBackend(result);
        Assert.Contains("var __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();", result.GeneratedCSharpCode);
        Assert.Contains("(string)__resultLibraryBase0.ToUpper(d.Dummy)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMethodCallInsideStringConcatenation_ShouldUseExecutionBackend()
    {
        var result = Inspect("select ToUpper(d.Dummy) + '!' from #system.dual() d");

        AssertUsesExecutionBackend(result);
        Assert.Contains("var __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();", result.GeneratedCSharpCode);
        Assert.Contains("(string)__resultLibraryBase0.ToUpper(d.Dummy)", result.GeneratedCSharpCode);
        Assert.Contains("+ \"!\"", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMethodCallInsideArithmeticBinary_ShouldUseExecutionBackend()
    {
        var result = Inspect("select Rand() + 1 from #system.dual() d");

        AssertUsesExecutionBackend(result);
        Assert.Contains("(int)new Musoq.Plugins.LibraryBase().Rand()", result.GeneratedCSharpCode);
        Assert.Contains("+ 1", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMethodCallInsideUnary_ShouldUseExecutionBackend()
    {
        var result = Inspect("select -Rand() from #system.dual() d");

        AssertUsesExecutionBackend(result);
        Assert.Contains("new Musoq.Plugins.LibraryBase().Rand()", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderNestedMethodCallArgument_ShouldUseExecutionBackend()
    {
        var result = Inspect("select ToString(Rand() + 1) from #system.dual() d");

        AssertUsesExecutionBackend(result);
        Assert.Contains("var __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();", result.GeneratedCSharpCode);
        Assert.Contains("__resultLibraryBase0.ToString", result.GeneratedCSharpCode);
        Assert.Contains("new Musoq.Plugins.LibraryBase().Rand()", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderArrayAccessExpression_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy[0] from #system.dual() d");

        AssertUsesExecutionBackend(result);
        Assert.Contains("d.Dummy[0]", result.ExecutionPlanText);
        Assert.Contains("d.Dummy[0]", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderNullableMethodCallInsideBinary_ShouldUseExecutionBackend()
    {
        var result = Inspect("select 1 + ToFloat(1) from #system.dual() d");

        AssertUsesExecutionBackend(result);
        Assert.Contains("var __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();", result.GeneratedCSharpCode);
        Assert.Contains("__resultLibraryBase0.ToFloat", result.GeneratedCSharpCode);
        Assert.Contains("+", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderNullableTemporalSubtraction_ShouldUseExecutionBackend()
    {
        var result = Inspect("select ToDateTime('2012/01/14') - ToDateTime('2012/01/13') from #system.dual() d");

        AssertUsesExecutionBackend(result);
        Assert.Contains("var __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();", result.GeneratedCSharpCode);
        Assert.Contains("__resultLibraryBase0.ToDateTime", result.GeneratedCSharpCode);
        Assert.Contains("new ResultRow0((TimeSpan?)((DateTime?)", result.GeneratedCSharpCode);
        Assert.Contains("public TimeSpan?", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".GetValueOrDefault()", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenInterpretationSchemasAreUnused_ShouldNotEmitInterpreterClasses()
    {
        var result = Inspect(@"
                binary UnusedBinary {
                    Value: byte
                };
                text UnusedText {
                    Value: rest
                };
                select 1 from #system.dual()");

        Assert.IsFalse(result.GeneratedCSharpCode.Contains("class UnusedBinary", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("class UnusedText", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("namespace Musoq.Generated.Interpreters", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GetGeneratedCSharpCode_WhenQueryIsValid_ShouldReturnRunnableClassCode()
    {
        var code = GetGeneratedCSharpCode("select d.Dummy from #system.dual() d");

        Assert.Contains("ITableRunnable", code);
    }

    [TestMethod]
    public void CompileForExecution_WhenQueryIsValid_ShouldStillRunExecutableQuery()
    {
        var compiled = CompileForExecution("select 1 from #system.dual()");

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForPlainQuery_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select d.Dummy from #system.dual() d",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual("single", table[0][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForSort_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select d.Dummy from #system.dual() d order by d.Dummy",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual("single", table[0][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForPagination_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select d.Dummy from #system.dual() d order by d.Dummy skip 0 take 1",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual("single", table[0][0]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderSortedPagination_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy from #system.dual() d order by d.Dummy skip 0 take 1");

        AssertUsesExecutionBackend(result);
        Assert.Contains("CreateBoundedRecordList [resultOrderRecords: ResultRow0WithSortKeys by d.Dummy ASC, skip 0, take 1]", result.ExecutionPlanText);
        Assert.Contains("MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0]", result.ExecutionPlanText);
        Assert.Contains("new EvaluationHelper.BoundedTopRecordList<ResultRow0WithSortKeys>(0, 1, ResultRow0WithSortKeysComparer.Instance)", result.GeneratedCSharpCode);
        Assert.Contains("resultOrderRecords", result.GeneratedCSharpCode);
        Assert.IsFalse(result.ExecutionPlanText.Contains("OrderRecordList [resultOrderRecords", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.SelectTopOffsetRecords(resultOrderRecords", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("AppendTopOffsetRowsDirect", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("RowOrderKey", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("SortShapeRows [result -> resultSorted", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("SliceTable [resultSorted -> resultSortedSliced", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForCte_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "with p as (select d.Dummy as Dummy from #system.dual() d) select p.Dummy from p",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual("single", table[0][0]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCte_ShouldUseExecutionBackend()
    {
        var result = Inspect("with p as (select d.Dummy as Dummy from #system.dual() d) select p.Dummy from p");

        AssertUsesExecutionBackend(result);
        Assert.Contains("CtePhase [cte0]", result.ExecutionPlanText);
        Assert.Contains("CteStrategy [CteReuseStrategy] cte:p -> FuseReadOnce", result.PlanningText);
        Assert.Contains("Materialization [CteFusionBoundary] cte:p -> Candidate", result.PlanningText);
        Assert.Contains("ForEach [d in dRows]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [cte0 -> _tableResults[0]]", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("ForEach [p in _tableResults[0].Rows]", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_tableResults[0]", StringComparison.Ordinal));
        Assert.Contains("OnPhaseChanged(\"compiled:cte0\", QueryPhase.Begin);", result.GeneratedCSharpCode);
        Assert.Contains("new ResultRow0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderChainedCte_ShouldUseExecutionBackend()
    {
        var result = Inspect("with p as (select d.Dummy as Dummy from #system.dual() d), q as (select Dummy from p where Dummy is not null) select Dummy from q");

        AssertUsesExecutionBackend(result);
        Assert.Contains("CtePhase [cte0]", result.ExecutionPlanText);
        Assert.Contains("CtePhase [cte1]", result.ExecutionPlanText);
        Assert.Contains("CteStrategy [CteReuseStrategy] cte:p -> FuseReadOnce", result.PlanningText);
        Assert.Contains("CteStrategy [CteReuseStrategy] cte:q -> FuseReadOnce", result.PlanningText);
        Assert.Contains("ForEach [d in dRows]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: dummy)]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [cte0 -> _tableResults[0]]", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("ForEach [p in _tableResults[0].Rows]", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [cte1 -> _tableResults[1]]", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("ForEach [q in _tableResults[1].Rows]", StringComparison.Ordinal));
        Assert.Contains("OnPhaseChanged(\"compiled:cte0\", QueryPhase.Begin);", result.GeneratedCSharpCode);
        Assert.Contains("OnPhaseChanged(\"compiled:cte1\", QueryPhase.Begin);", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_tableResults[0]", StringComparison.Ordinal));
        Assert.Contains("new ResultRow0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderChainedCte_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("with p as (select d.Dummy as Dummy from #system.dual() d), q as (select Dummy from p where Dummy is not null) select Dummy from q");

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteParallelizationIsEnabledForIndependentCtes_ShouldEmitParallelExecutionIr()
    {
        var result = Inspect(
            CreateIndependentCteJoinQuery(),
            new CompilationOptions(useCteParallelization: true));

        AssertUsesExecutionBackend(result);
        Assert.Contains("ParallelBlock [cte-level-0, tasks 2, maxDegree 2]", result.ExecutionPlanText);
        Assert.Contains("ParallelMerge", result.ExecutionPlanText);
        Assert.Contains("Parallel.Invoke(new ParallelOptions", result.GeneratedCSharpCode);
        Assert.Contains("CancellationToken = token", result.GeneratedCSharpCode);
        Assert.Contains("MaxDegreeOfParallelism = 2", result.GeneratedCSharpCode);
        Assert.Contains("StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0: List<Cte0Row0>]", result.ExecutionPlanText);
        Assert.Contains("StoreCteIndex [cte1HashSidecar0Dummy -> _cteIndexResults.Slot0 Hash]", result.ExecutionPlanText);
        Assert.Contains("private static List<Cte0Row0> BuildCteLevel0Task0", result.GeneratedCSharpCode);
        Assert.Contains("_cteRowResults.Slot0 = __parallelCteLevel0Task0Result", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_tableResults[", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static Musoq.Evaluator.Tables.Table BuildCteLevel0Task", StringComparison.Ordinal));
        Assert.Contains("ParallelEligibility [ParallelCte] PhysicalCteNode -> Candidate", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenParallelizationModeIsNoneForIndependentCtes_ShouldKeepSerialCteStores()
    {
        var result = Inspect(
            CreateIndependentCteJoinQuery(),
            new CompilationOptions(
                parallelizationMode: ParallelizationMode.None,
                useCteParallelization: true));

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ParallelBlock", StringComparison.Ordinal));
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("StoreCteIndex [cte1HashSidecar0Dummy -> _cteIndexResults.Slot0 Hash]", result.ExecutionPlanText);
        Assert.Contains("LoadCteIndex [qHash <- _cteIndexResults.Slot0 Hash: string]", result.ExecutionPlanText);
        Assert.Contains("ParallelEligibility [ParallelCte] PhysicalCteNode -> Skipped", result.PlanningText);
        Assert.Contains("Compilation option disables parallel execution.", result.PlanningText);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteParallelizationIsEnabledForIndependentCtes_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            CreateIndependentCteJoinQuery(),
            new CompilationOptions(useCteParallelization: true));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("single", table[0][1]);
    }

}
