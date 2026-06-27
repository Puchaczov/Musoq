using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class UnpivotExecutionTests : BasicEntityTestBase
{
    [TestMethod]
    public void Run_WhenUnpivotIsTopLevel_ShouldExpandRowsInSourceAndEntryOrder()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Population as Population, s.Money as Money) using Amount keep s.Country as Country";
        var sources = CreateSingleSource(
            new BasicEntity { Country = "PL", Population = 10m, Money = 1.5m },
            new BasicEntity { Country = "US", Population = 20m, Money = 2.5m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Count);
        AssertColumn(table, 0, "Country", typeof(string));
        AssertColumn(table, 1, "Metric", typeof(string));
        AssertColumn(table, 2, "Amount", typeof(decimal));

        AssertRow(table[0], "PL", "Population", 10m);
        AssertRow(table[1], "PL", "Money", 1.5m);
        AssertRow(table[2], "US", "Population", 20m);
        AssertRow(table[3], "US", "Money", 2.5m);
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

        Assert.AreEqual(4, table.Count);
        AssertColumn(table, 0, "Name", typeof(string));
        AssertColumn(table, 1, "Metric", typeof(string));
        AssertColumn(table, 2, "Value", typeof(int?));

        AssertRow(table[0], "A", "NullableValue", 7);
        AssertRow(table[1], "A", "ExplicitNull", null);
        AssertRow(table[2], "B", "NullableValue", null);
        AssertRow(table[3], "B", "ExplicitNull", null);
    }

    [TestMethod]
    public void Run_WhenKeepUsesSimpleImplicitAlias_ShouldProjectKeepField()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Population as Population) using Amount keep s.Country";
        var sources = CreateSingleSource(
            new BasicEntity { Country = "PL", Population = 10m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        AssertColumn(table, 0, "Country", typeof(string));
        AssertColumn(table, 1, "Metric", typeof(string));
        AssertColumn(table, 2, "Amount", typeof(decimal));
        AssertRow(table[0], "PL", "Population", 10m);
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

        Assert.AreEqual(2, table.Count);
        AssertRow(table[0], "A", "Population", 10m);
        AssertRow(table[1], "B", "Money", 2m);
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

    private static void AssertRow(Row row, object? first, object? second, object? third)
    {
        Assert.AreEqual(first, row[0]);
        Assert.AreEqual(second, row[1]);
        Assert.AreEqual(third, row[2]);
    }
}
