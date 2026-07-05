using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static readonly CompilationOptions SidecarGlobalOptions = new(useCteSidecarIndexes: true);

    [TestMethod]
    public void CompileForExecution_WhenCteSidecarIndexesAreEnabledForCrossJoinValues_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select d.Dummy, marker.Label from #system.dual() d cross join values { { Label: 'x' }, { Label: 'y' } } marker order by marker.Label",
            SidecarGlobalOptions);

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("x", table[0][1]);
        Assert.AreEqual("single", table[1][0]);
        Assert.AreEqual("y", table[1][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteSidecarIndexesAreEnabledForCrossApplySchemaSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(
            "select l.Name, r.Line as RightLine from #apply.items() l cross apply #apply.related(l.Name) r order by l.Name",
            CreateApplyCandidateSchemaProvider(),
            SidecarGlobalOptions);

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanDoesNotContain("Sidecar join pipeline failed", result.ExecutionPlanText);
        AssertExecutionPlanContains("ForEach [l in lRows]", result.ExecutionPlanText);
        AssertExecutionPlanContains("ForEach [r in rRows]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteSidecarIndexesAreEnabledForOuterApplyNoMatches_ShouldPreserveRows()
    {
        var compiled = CompileForExecution(
            "select l.Name, r.Line as RightLine from #apply.items() l outer apply #apply.related(l.Numbers) r order by l.Name",
            CreateApplyCandidateSchemaProvider(),
            SidecarGlobalOptions);

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.IsNull(table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteSidecarIndexesAreEnabledForApplyWithOrdinality_ShouldExposeOrdinal()
    {
        var compiled = CompileForExecution(
            "select i.Name, n.Value, n.Ordinal from #apply.items() i cross apply i.Numbers n with ordinality order by i.Name, n.Ordinal",
            CreateApplyCandidateSchemaProvider(),
            SidecarGlobalOptions);

        var table = compiled.Run();
        var rows = Enumerable.Range(0, table.Count)
            .Select(index => $"{table[index][0]}:{table[index][1]}:{table[index][2]}")
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "left:1:0", "left:2:1", "right:3:0" },
            rows);
    }
}
