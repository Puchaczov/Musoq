using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForUnionAll_ShouldStreamIntoFinalShapes()
    {
        var result = Inspect("select d.Dummy as Dummy from #system.dual() d union all (Dummy) select e.Dummy as Dummy from #system.dual() e",
            new CompilationOptions());

        Assert.Contains("CreateShapeRows [result: ResultShape0 from ResultRow0]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: d.Dummy)]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: e.Dummy)]", result.ExecutionPlanText);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(d.Dummy));", result.GeneratedCSharpCode);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(e.Dummy));", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("result.AddDirect(new ResultRow0", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("SetOperation [result = left UnionAll right]", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("UnionAll(left, right", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class LeftRow0", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class RightRow0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenUnionAllUsesFilteredDirectSources_ShouldStreamDirectlyIntoResult()
    {
        var result = Inspect(CreateFilteredUnionAllQuery(),
            new CompilationOptions());

        Assert.Contains("CreateShapeRows [result: ResultShape0 from ResultRow0]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: dummy)]", result.ExecutionPlanText);
        Assert.Contains("If [", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("SetOperation [result = left UnionAll right]", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("UnionAll(left, right", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class LeftRow0", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class RightRow0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderUnionAll_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy as Dummy from #system.dual() d union all (Dummy) select e.Dummy as Dummy from #system.dual() e");

        AssertUsesExecutionBackend(result);
        Assert.Contains("CreateShapeRows [result: ResultShape0 from ResultRow0]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: d.Dummy)]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: e.Dummy)]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("SetOperation [result = left UnionAll right]", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("UnionAll(left, right", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForUnionAll_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select d.Dummy as Dummy from #system.dual() d union all (Dummy) select e.Dummy as Dummy from #system.dual() e",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("single", table[1][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenUnionAllUsesFilteredDirectSources_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateFilteredUnionAllQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForCteUnionAll_ShouldStreamStoredCtesIntoResult()
    {
        var result = Inspect(CreateCteUnionAllQuery(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ParallelBlock [cte-level-0, tasks 2, maxDegree 2]", result.ExecutionPlanText);
        Assert.Contains("StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("StoreTable [__parallelCteLevel0Task1Result -> _cteRowResults.Slot1", result.ExecutionPlanText);
        Assert.Contains("CreateShapeRows [result: ResultShape0 from ResultRow0]", result.ExecutionPlanText);
        Assert.Contains("ForEach [l in _cteRowResults.Slot0]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: l.Dummy)]", result.ExecutionPlanText);
        Assert.Contains("ForEach [r in _cteRowResults.Slot1]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: r.Dummy)]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("SetOperation [result = left UnionAll right]", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("UnionAll(left, right", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForCteUnionAll_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateCteUnionAllQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("single", table[1][0]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForNestedUnionAll_ShouldStreamNestedLeftArm()
    {
        var result = Inspect(CreateNestedUnionAllQuery(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("CreateRowBuffer [left: List<LeftRow0>]", result.ExecutionPlanText);
        Assert.Contains("AppendRowBuffer [left <- LeftRow0(Dummy: d.Dummy)]", result.ExecutionPlanText);
        Assert.Contains("AppendRowBuffer [left <- LeftRow0(Dummy: e.Dummy)]", result.ExecutionPlanText);
        Assert.Contains("SetOperation [result = left UnionAll right, AppendLoop]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("SetOperation [left = leftLeft UnionAll leftRight]", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("UnionAll(", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForNestedUnionAll_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateNestedUnionAllQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("single", table[1][0]);
        Assert.AreEqual("single", table[2][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForComputedUnionAll_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateComputedUnionAllQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("single!", table[0][0]);
        Assert.AreEqual("single?", table[1][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenComputedEntityUnionAllIsUsed_ShouldStreamWithoutSetHelper()
    {
        const string query = """
            SELECT a.City + ':' + a.Country AS Label FROM #A.entities() a
            UNION ALL
            SELECT b.City + ':' + b.Country AS Label FROM #B.entities() b
            """;
        var schemaProvider = CreateEntitySetSchemaProvider();
        var result = Inspect(query, schemaProvider, new CompilationOptions());

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> StreamingUnionAll", result.PlanningText);
        AssertNoSetOperationFallbackPath(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("SetOperation [result = left UnionAll right", StringComparison.Ordinal), result.ExecutionPlanText);

        var table = CompileForExecution(query, schemaProvider, new CompilationOptions()).Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Warsaw:PL", table[0][0]);
        Assert.AreEqual("Berlin:DE", table[1][0]);
        Assert.AreEqual("Krakow:PL", table[2][0]);
        Assert.AreEqual("Munich:DE", table[3][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenJoinedArmUnionAllIsUsed_ShouldAppendGeneratedArmRows()
    {
        const string query = """
            SELECT a.City AS City FROM #A.entities() a
            INNER JOIN #B.entities() b ON a.Country = b.Country
            UNION ALL
            SELECT c.City AS City FROM #C.entities() c
            """;
        var schemaProvider = CreateEntitySetSchemaProvider();
        var result = Inspect(query, schemaProvider, new CompilationOptions());

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> AppendLoop", result.PlanningText);
        Assert.Contains("SetOperation [result = left UnionAll right, AppendLoop]", result.ExecutionPlanText);
        AssertNoSetOperationFallbackPath(result);

        var table = CompileForExecution(query, schemaProvider, new CompilationOptions()).Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Warsaw", table[0][0]);
        Assert.AreEqual("Berlin", table[1][0]);
        Assert.AreEqual("Prague", table[2][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenNestedSortedAggregateUnionAllIsUsed_ShouldAppendGeneratedArmRows()
    {
        const string query = """
            SELECT a.Country, Count(a.City) AS Cnt FROM #A.entities() a GROUP BY a.Country
            UNION ALL (Country)
            SELECT b.Country, Count(b.City) AS Cnt FROM #B.entities() b GROUP BY b.Country ORDER BY Cnt DESC
            """;
        var schemaProvider = CreateEntitySetSchemaProvider();
        var result = Inspect(query, schemaProvider, new CompilationOptions());

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> AppendLoop", result.PlanningText);
        AssertNoSetOperationFallbackPath(result);

        var table = CompileForExecution(query, schemaProvider, new CompilationOptions()).Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("PL", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual("DE", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
        Assert.AreEqual("PL", table[2][0]);
        Assert.AreEqual(1L, table[2][1]);
        Assert.AreEqual("DE", table[3][0]);
        Assert.AreEqual(1L, table[3][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForComputedUnionAll_ShouldExplainStreamingStrategy()
    {
        var result = Inspect(CreateComputedUnionAllQuery(),
            new CompilationOptions());

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> StreamingUnionAll", result.PlanningText);
        Assert.Contains("UnionAll arms use directly streamable row sources with optional filters, projected expressions, and no post-operations", result.PlanningText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("SetOperation [result = left UnionAll right", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("UnionAll(left, right", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenUnionAllArmUsesJoinSource_ShouldExplainAppendLoopStrategy()
    {
        var result = CreateApplyInspection(
            "select l.Name as Name from #apply.items() l inner join #apply.items() r on l.Line = r.Line union all (Name) select i.Name as Name from #apply.items() i");

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> AppendLoop", result.PlanningText);
        Assert.Contains("UnionAll uses generated append lowering because at least one arm is not a directly streamable row-source pipeline with optional filters, projected expressions, and no post-operations", result.PlanningText);
        Assert.IsFalse(result.PlanningText.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> StreamingUnionAll", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenUnionAllArmUsesApplySource_ShouldExplainAppendLoopStrategy()
    {
        var result = CreateApplyInspection(
            "select r.Line as Line from #apply.items() i cross apply #apply.related(i.Name) r union all (Line) select j.Line as Line from #apply.items() j");

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> AppendLoop", result.PlanningText);
        Assert.Contains("UnionAll uses generated append lowering because at least one arm is not a directly streamable row-source pipeline with optional filters, projected expressions, and no post-operations", result.PlanningText);
        Assert.IsFalse(result.PlanningText.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> StreamingUnionAll", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForAggregateUnionAllArms_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateAggregateUnionAllQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual("single", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderAggregateUnionAllArms_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateAggregateUnionAllQuery());

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("SetOperation [result = left UnionAll right, AppendLoop]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateAggregateLibrary [leftLibraryBase0: LibraryBase]", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateAggregateLibrary [rightLibraryBase0: LibraryBase]", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("Statement0Row0", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.GetColumnValue", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".Contexts", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenUnionAllHasTrailingOrderBy_ShouldSortTheCombinedResult()
    {
        var result = Inspect(
            CreateGloballySortedUnionAllQuery(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: 'b')]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(Dummy: 'a')]", result.ExecutionPlanText);
        Assert.Contains("SortShapeRows [result -> resultSorted by Dummy ASC]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenUnionAllHasTrailingOrderBy_ShouldReturnGloballySortedRows()
    {
        var compiled = CompileForExecution(CreateGloballySortedUnionAllQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual("b", table[1][0]);
    }

    [TestMethod]
    [DataRow("union", 1)]
    [DataRow("except", 0)]
    [DataRow("intersect", 1)]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForSetOperationKind_ShouldRunExecutableQuery(
        string operation,
        int expectedRows)
    {
        var compiled = CompileForExecution($"select d.Dummy as Dummy from #system.dual() d {operation} (Dummy) select e.Dummy as Dummy from #system.dual() e",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(expectedRows, table.Count);
    }

    [TestMethod]
    [DataRow("union", "Union", "Union(")]
    [DataRow("except", "Except", "Except(")]
    [DataRow("intersect", "Intersect", "Intersect(")]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForSetOperationKind_ShouldUseHashSetStrategy(
        string operation,
        string planKind,
        string helperCall)
    {
        var result = Inspect($"select d.Dummy as Dummy from #system.dual() d {operation} (Dummy) select e.Dummy as Dummy from #system.dual() e",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains($"SetOperationStrategy [SetOperationStrategy] {planKind} -> HashSet", result.PlanningText);
        Assert.Contains($"SetOperation [result = left {planKind} right, HashSet]", result.ExecutionPlanText);
        Assert.Contains("new HashSet<string>(", result.GeneratedCSharpCode);
        Assert.Contains("__musoqFinalShapeRows.Add(new LeftShape0(", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("result.AddDirect(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".AddUnchecked(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(helperCall, StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("union", 8, false, 1)]
    [DataRow("union", 8, true, 2)]
    [DataRow("except", 8, false, 0)]
    [DataRow("except", 8, true, 1)]
    [DataRow("intersect", 8, false, 1)]
    [DataRow("intersect", 8, true, 0)]
    [DataRow("union", 12, false, 1)]
    [DataRow("union", 12, true, 2)]
    [DataRow("except", 12, false, 0)]
    [DataRow("except", 12, true, 1)]
    [DataRow("intersect", 12, false, 1)]
    [DataRow("intersect", 12, true, 0)]
    public void CompileForExecution_WhenSetOperationUsesWideExplicitKeys_ShouldRunExecutableQuery(
        string operation,
        int keyCount,
        bool makeRightDistinct,
        int expectedRows)
    {
        var compiled = CompileForExecution(
            CreateWideSetOperationQuery(operation, keyCount, makeRightDistinct),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(expectedRows, table.Count);
    }

    [TestMethod]
    [DataRow("union", "Union", "Union(", 8)]
    [DataRow("except", "Except", "Except(", 8)]
    [DataRow("intersect", "Intersect", "Intersect(", 8)]
    [DataRow("union", "Union", "Union(", 12)]
    [DataRow("except", "Except", "Except(", 12)]
    [DataRow("intersect", "Intersect", "Intersect(", 12)]
    public void CompileForInspection_WhenSetOperationUsesWideExplicitKeys_ShouldUseHashSetStrategy(
        string operation,
        string planKind,
        string helperCall,
        int keyCount)
    {
        var result = Inspect(
            CreateWideSetOperationQuery(operation, keyCount, makeRightDistinct: false),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains($"SetOperationStrategy [SetOperationStrategy] {planKind} -> HashSet", result.PlanningText);
        Assert.Contains($"SetOperation [result = left {planKind} right, HashSet]", result.ExecutionPlanText);
        Assert.Contains(CreateWideSetOperationHashSetText(keyCount), result.GeneratedCSharpCode);
        Assert.IsFalse(result.PlanningText.Contains("-> RowComparer", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(helperCall, StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("union", "Union", "DoubleValue", "double")]
    [DataRow("except", "Except", "DoubleValue", "double")]
    [DataRow("intersect", "Intersect", "DoubleValue", "double")]
    [DataRow("union", "Union", "FloatValue", "float")]
    [DataRow("except", "Except", "FloatValue", "float")]
    [DataRow("intersect", "Intersect", "FloatValue", "float")]
    [DataRow("union", "Union", "NullableDoubleValue", "double?")]
    [DataRow("except", "Except", "NullableDoubleValue", "double?")]
    [DataRow("intersect", "Intersect", "NullableDoubleValue", "double?")]
    [DataRow("union", "Union", "NullableFloatValue", "float?")]
    [DataRow("except", "Except", "NullableFloatValue", "float?")]
    [DataRow("intersect", "Intersect", "NullableFloatValue", "float?")]
    public void ExecutionSetOperations_WhenNanSensitiveKeyIsUsed_ShouldUseGeneratedEqualityLoopStrategy(
        string operation,
        string planKind,
        string columnName,
        string hashSetTypeName)
    {
        var result = Inspect(
            CreateNanSetOperationQuery(operation, columnName),
            CreateNanSetOperationSchemaProvider(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains($"SetOperationStrategy [SetOperationStrategy] {planKind} -> GeneratedEqualityLoop", result.PlanningText);
        Assert.Contains("uses NaN-sensitive equality semantics, so Execution IR emits an explicit generated equality loop instead of HashSet lowering", result.PlanningText);
        Assert.Contains($"SetOperation [result = left {planKind} right]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("HashSet", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains($"new HashSet<{hashSetTypeName}>", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains($"{planKind}(left, right", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    [TestMethod]
    [DataRow("union", "DoubleValue", true, 2)]
    [DataRow("except", "DoubleValue", true, 1)]
    [DataRow("intersect", "DoubleValue", true, 0)]
    [DataRow("union", "FloatValue", false, 2)]
    [DataRow("except", "FloatValue", false, 1)]
    [DataRow("intersect", "FloatValue", false, 0)]
    [DataRow("union", "NullableDoubleValue", true, 2)]
    [DataRow("except", "NullableDoubleValue", true, 1)]
    [DataRow("intersect", "NullableDoubleValue", true, 0)]
    [DataRow("union", "NullableFloatValue", false, 2)]
    [DataRow("except", "NullableFloatValue", false, 1)]
    [DataRow("intersect", "NullableFloatValue", false, 0)]
    public void ExecutionSetOperations_WhenNanSensitiveKeyIsUsed_ShouldKeepGeneratedEqualityNanSemantics(
        string operation,
        string columnName,
        bool expectDouble,
        int expectedRows)
    {
        var compiled = CompileForExecution(
            CreateNanSetOperationQuery(operation, columnName),
            CreateNanSetOperationSchemaProvider(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(expectedRows, table.Count);
        for (var rowIndex = 0; rowIndex < table.Count; rowIndex++)
        {
            if (expectDouble)
                Assert.IsTrue(double.IsNaN((double)table[rowIndex][0]));
            else
                Assert.IsTrue(float.IsNaN((float)table[rowIndex][0]));
        }
    }

    private static DynamicRowsSchemaProvider CreateNanSetOperationSchemaProvider()
    {
        var columns = new Dictionary<string, Type>
        {
            ["Side"] = typeof(string),
            ["DoubleValue"] = typeof(double),
            ["FloatValue"] = typeof(float),
            ["NullableDoubleValue"] = typeof(double?),
            ["NullableFloatValue"] = typeof(float?)
        };
        var rows = new List<IReadOnlyDictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                ["Side"] = "L",
                ["DoubleValue"] = double.NaN,
                ["FloatValue"] = float.NaN,
                ["NullableDoubleValue"] = double.NaN,
                ["NullableFloatValue"] = float.NaN
            },
            new Dictionary<string, object>
            {
                ["Side"] = "R",
                ["DoubleValue"] = double.NaN,
                ["FloatValue"] = float.NaN,
                ["NullableDoubleValue"] = double.NaN,
                ["NullableFloatValue"] = float.NaN
            }
        };

        return new DynamicRowsSchemaProvider(columns, rows);
    }

    private static string CreateNanSetOperationQuery(string operation, string columnName)
    {
        return $"select d.{columnName} as Value from #dynamic.all() d where d.Side = 'L' {operation} (Value) select r.{columnName} as Value from #dynamic.all() r where r.Side = 'R'";
    }

    private static string CreateWideSetOperationQuery(
        string operation,
        int keyCount,
        bool makeRightDistinct)
    {
        var keys = Enumerable.Range(0, keyCount)
            .Select(index => $"K{index}")
            .ToArray();
        var leftFields = CreateWideSetOperationFields(keyCount, distinctLastValue: false);
        var rightFields = CreateWideSetOperationFields(keyCount, makeRightDistinct);

        return
            $"select {leftFields} from #system.dual() d {operation} ({string.Join(", ", keys)}) select {rightFields} from #system.dual() e";
    }

    private static string CreateWideSetOperationFields(int keyCount, bool distinctLastValue)
    {
        return string.Join(", ", Enumerable.Range(0, keyCount)
            .Select(index =>
            {
                var value = distinctLastValue && index == keyCount - 1
                    ? index + 101
                    : index + 1;
                return $"{value} as K{index}";
            }));
    }

    private static string CreateWideSetOperationHashSetText(int keyCount)
    {
        return $"new HashSet<({string.Join(", ", Enumerable.Repeat("int", keyCount))})>(";
    }

    private static void AssertNoSetOperationFallbackPath(QueryInspectionResult result)
    {
        Assert.IsFalse(result.PlanningText.Contains("RowComparer", StringComparison.Ordinal), result.PlanningText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("UnionAll(left, right", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Union(left, right", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Except(left, right", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Intersect(left, right", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

}
