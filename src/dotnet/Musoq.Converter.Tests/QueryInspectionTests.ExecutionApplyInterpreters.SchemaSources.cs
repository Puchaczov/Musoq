using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCrossApplySchemaSource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select l.Name, r.Line as RightLine from #apply.items() l cross apply #apply.related(l.Name) r", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("ForEach [l in lRows]", result.ExecutionPlanText);
        AssertExecutionPlanContains("SourceScan [r: ApplyCandidateEntity] -> rRows", result.ExecutionPlanText);
        AssertExecutionPlanContains("ForEach [r in rRows]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenCrossApplyUsesReorderedNamedSourceArguments_ShouldCanonicalizeEverySurface()
    {
        var result = Inspect(
            "select l.Name, r.Line as RightLine from #apply.items() l cross apply #apply.related(limit: 1, name: l.Name) r",
            CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.Contains("#apply.related", result.LogicalPlanText);
        Assert.Contains("#apply.related", result.PhysicalPlanText);
        AssertExecutionPlanContains("SourceScan [r: ApplyCandidateEntity] -> rRows", result.ExecutionPlanText);
        Assert.IsFalse(result.LogicalPlanText.Contains("limit:", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.PhysicalPlanText.Contains("name:", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("limit:", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("name:", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CompileForExecution_WhenCrossApplyUsesNamedSourceArguments_ShouldPreserveCorrelatedRows()
    {
        var compiled = CompileForExecution(
            "select l.Name, r.Line as RightLine from #apply.items() l cross apply #apply.related(limit: 1, name: l.Name) r",
            CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => string.Equals(row[0] as string, "left", StringComparison.Ordinal)));
        Assert.IsTrue(table.Any(row => string.Equals(row[0] as string, "right", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderCrossApplySchemaSource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select l.Name, r.Line as RightLine from #apply.items() l cross apply #apply.related(l.Name) r", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();
        var pairs = Enumerable.Range(0, table.Count)
            .Select(index => $"{table[index][0]}:{table[index][1]}")
            .OrderBy(pair => pair, StringComparer.Ordinal)
            .ToArray();

        Assert.HasCount(2, table);
        Assert.AreEqual(
            "left:WARN retry|right:INFO ready",
            string.Join("|", pairs));
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderOuterApplySchemaSource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select l.Name, r.Line as RightLine from #apply.items() l outer apply #apply.related(l.Numbers) r", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("ForEach [l in lRows]", result.ExecutionPlanText);
        AssertExecutionPlanContains("SourceScan [r: ApplyCandidateEntity] -> rRows", result.ExecutionPlanText);
        Assert.Contains("Assign [rHasMatch = TRUE]", result.ExecutionPlanText);
        Assert.Contains("If [NOT rHasMatch]", result.ExecutionPlanText);
        Assert.Contains("r.Line: NULL", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderOuterApplySchemaSource_ShouldPreserveUnmatchedRows()
    {
        var compiled = CompileForExecution("select l.Name, r.Line as RightLine from #apply.items() l outer apply #apply.related(l.Numbers) r", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.IsNull(table[1][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderOuterApplyWithLeftOnlyFilter_ShouldUseExecutionBackend()
    {
        var result = Inspect("select l.Name, r.Line as RightLine from #apply.items() l outer apply #apply.related(l.Numbers) r where l.Name = 'left'", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.Contains("Assign [rHasMatch = TRUE]", result.ExecutionPlanText);
        Assert.Contains("If [NOT rHasMatch]", result.ExecutionPlanText);
        Assert.Contains("r.Line: NULL", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderOuterApplyWithLeftOnlyFilter_ShouldPreserveFilteredUnmatchedRows()
    {
        var compiled = CompileForExecution("select l.Name, r.Line as RightLine from #apply.items() l outer apply #apply.related(l.Numbers) r where l.Name = 'left'", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderOuterApplyWithRightFilterThroughTransition_ShouldUseExecutionBackend()
    {
        var result = Inspect("select l.Name, r.Line as RightLine from #apply.items() l outer apply #apply.related(l.Numbers) r where r.Line = 'INFO ready'", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.Contains("StoreTable [statement0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("ForEach [lr in _cteRowResults.Slot0]", result.ExecutionPlanText);
        Assert.Contains("If [(r_Line = 'INFO ready')]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderOuterApplyPropertySource_ShouldNullExtendValueTypes()
    {
        var compiled = CompileForExecution("select i.Name, n.Value from #apply.items() i outer apply i.Numbers n", new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "empty",
                    Line = "INFO empty",
                    Numbers = []
                },
                new ApplyCandidateEntity
                {
                    Name = "filled",
                    Line = "INFO filled",
                    Numbers = [7]
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("empty", table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual("filled", table[1][0]);
        Assert.AreEqual(7, table[1][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderOuterApplyComputedRightProjection_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name, n.Value + 1 as NextValue from #apply.items() i outer apply i.Numbers n", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.Contains("Let [iName: string = i.Name]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(i.Name: iName, NextValue: NULL)]", result.ExecutionPlanText);
        Assert.Contains("NextValue: int? <- field NextValue", result.ExecutionPlanText);
        Assert.Contains("(n.Value + 1)", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderOuterApplyComputedRightProjection_ShouldNullExtendComputedValue()
    {
        var compiled = CompileForExecution("select i.Name, n.Value + 1 as NextValue from #apply.items() i outer apply i.Numbers n", new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "empty",
                    Line = "INFO empty",
                    Numbers = []
                },
                new ApplyCandidateEntity
                {
                    Name = "filled",
                    Line = "INFO filled",
                    Numbers = [7]
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("empty", table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual("filled", table[1][0]);
        Assert.AreEqual(8, table[1][1]);
    }

}
