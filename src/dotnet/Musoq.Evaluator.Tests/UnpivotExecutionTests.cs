using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class UnpivotExecutionTests : BasicEntityTestBase
{
    [TestMethod]
    public void Run_WhenUnpivotIsTopLevel_ShouldExpandRowsWithKeepOnUsingColumns()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Population as Population, s.Money as Money) using Amount keep s.Country as Country";
        var sources = CreateSingleSource(
            new BasicEntity { Country = "PL", Population = 10m, Money = 1.5m },
            new BasicEntity { Country = "US", Population = 20m, Money = 2.5m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["PL", "Population", 10m],
            ["PL", "Money", 1.5m],
            ["US", "Population", 20m],
            ["US", "Money", 2.5m]);
    }

    [TestMethod]
    public void Run_WhenUnpivotValuesAreNull_ShouldPreserveRows()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.NullableValue as NullableValue, null as ExplicitNull) using Value keep s.Name as Name";
        var sources = CreateSingleSource(
            new BasicEntity { Name = "A", NullableValue = 7 },
            new BasicEntity { Name = "B", NullableValue = null });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Metric", typeof(string)),
            ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", "NullableValue", 7],
            new object?[] { "A", "ExplicitNull", null },
            new object?[] { "B", "NullableValue", null },
            new object?[] { "B", "ExplicitNull", null });
    }

    [TestMethod]
    public void Run_WhenKeepUsesSimpleImplicitAlias_ShouldProjectKeepField()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Population as Population) using Amount keep s.Country";
        var sources = CreateSingleSource(
            new BasicEntity { Country = "PL", Population = 10m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["PL", "Population", 10m]);
    }

    [TestMethod]
    public void Run_WhenUnpivotHasOrderingAndSlice_ShouldApplyAfterExpansion()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Population as Population, s.Money as Money) using Amount keep s.Name as Name order by Amount desc skip 1 take 2";
        var sources = CreateSingleSource(
            new BasicEntity { Name = "A", Population = 10m, Money = 1m },
            new BasicEntity { Name = "B", Population = 20m, Money = 2m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A", "Population", 10m],
            ["B", "Money", 2m]);
    }

    [TestMethod]
    public void Compile_WhenUnpivotIsInspected_ShouldStreamExpansionWithoutIntermediateRowList()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Population as Population, s.Money as Money) using Amount keep s.Country as Country";
        var sources = CreateSingleSource(
            new BasicEntity { Country = "PL", Population = 10m, Money = 1.5m });

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            TestCompilationOptions);

        Assert.IsFalse(
            inspection.ExecutionPlanText.Contains("CreateUnpivotRows", StringComparison.Ordinal),
            inspection.ExecutionPlanText);
        Assert.IsFalse(
            inspection.GeneratedCSharpCode.Contains("__unpivotRows", StringComparison.Ordinal),
            inspection.GeneratedCSharpCode);
        Assert.Contains("ForEach [s in", inspection.ExecutionPlanText);
        Assert.Contains("ScopedBlock", inspection.ExecutionPlanText);
        Assert.Contains("CreateGeneratedRow [__unpivot <-", inspection.ExecutionPlanText);
    }

}
