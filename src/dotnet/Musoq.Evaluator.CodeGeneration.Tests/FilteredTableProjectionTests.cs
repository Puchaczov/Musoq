using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generated;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class FilteredTableProjectionTests
{
    private static readonly CompilationOptions Options = new CompilationOptions()
        .WithTableResultMaterialization()
        .WithStabilityAwareScalarReuse();

    [TestMethod]
    [DataRow(EvaluatorPerformanceQueries.Q230, "Name", "City", "Population")]
    [DataRow(
        "select Name, City as Town, Population from #A.entities() where Population > 0",
        "Name",
        "Town",
        "Population"
    )]
    [DataRow("select Month, Country, Money from #A.entities() where Money > 0", "Month", "Country", "Money")]
    [DataRow("select Population, City, Name from #A.entities() where Population > 0", "Population", "City", "Name")]
    public void FilteredProjection_StreamsDeclaredColumnsInOrder(string sql, string first, string second, string third)
    {
        var rows = new List<BasicEntity>
        {
            new() { Population = -1, Money = -1 },
            new()
            {
                Name = "A",
                Month = "A",
                City = null,
                Country = null,
                Population = 7,
                Money = 7,
            },
            new()
            {
                Name = "B",
                Month = "B",
                City = "Lodz",
                Country = "Lodz",
                Population = 2,
                Money = 2,
            },
        };
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = rows }
        );
        var logger = new TestsLoggerResolver();
        var inspection = InstanceCreator.CompileForInspection(sql, "inspect", provider, logger, Options);
        Assert.IsNotNull(inspection.ExecutionPlan);
        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(inspection.ExecutionPlan, out var plan));
        Assert.IsTrue(plan.FinalSinkPlans.TableDirectProjection.IsAccepted);
        Assert.IsNotNull(plan.FinalSinkPlans.TableDirectProjection.ProjectionLoop?.OptionalProjectionBody);

        using var query = InstanceCreator.CompileForExecution(sql, "query", provider, logger, Options);
        using var table = query.Run();
        var numberFirst = first == "Population";
        TableMaterializationTestHelper.AssertColumns(
            table,
            (first, numberFirst ? typeof(decimal) : typeof(string)),
            (second, typeof(string)),
            (third, numberFirst ? typeof(string) : typeof(decimal))
        );
        if (numberFirst)
            TableMaterializationTestHelper.AssertRowsInOrder(table, [7m, null, "A"], [2m, "Lodz", "B"]);
        else
            TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", null, 7m], ["B", "Lodz", 2m]);

        rows.RemoveRange(1, 2);
        using var empty = query.Run();
        Assert.AreEqual(0, empty.Count);
    }

    [TestMethod]
    [DoNotParallelize]
    public void FilteredProjection_EvaluatesSharedFieldOncePerInput()
    {
        var provider = EvaluatorSampleFixtures.CreateLoopInvariantSampleSchemaProvider();
        using var query = InstanceCreator.CompileForExecution(
            "select a.Value as Amount from #licm.outers() a where a.Value >= 20",
            "shared",
            provider,
            new TestsLoggerResolver(),
            Options
        );
        LoopInvariantSampleCounters.Reset();
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [20]);
        Assert.AreEqual(2, LoopInvariantSampleCounters.OuterStableValueReads);
    }
}
