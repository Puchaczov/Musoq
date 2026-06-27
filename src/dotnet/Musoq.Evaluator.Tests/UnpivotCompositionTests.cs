using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class UnpivotCompositionTests : BasicEntityTestBase
{
    [TestMethod]
    public void Run_WhenUnpivotIsInCte_ShouldBeSelectable()
    {
        const string query = """
                             with u as (
                                 unpivot #A.Entities() s
                                 on Metric in (s.Population as Population, s.Money as Money)
                                 using Amount
                                 keep s.Name as Name
                             )
                             select Name, Metric, Amount from u order by Name, Metric
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Name = "A", Population = 10m, Money = 1m })).Run();

        Assert.AreEqual(2, table.Count);
        AssertRow(table[0], "A", "Money", 1m);
        AssertRow(table[1], "A", "Population", 10m);
    }

    [TestMethod]
    public void Run_WhenUnpivotIsInDerivedTable_ShouldBeSelectable()
    {
        const string query = """
                             select u.Name, u.Metric, u.Amount
                             from (
                                 unpivot #A.Entities() s
                                 on Metric in (s.Population as Population, s.Money as Money)
                                 using Amount
                                 keep s.Name as Name
                             ) u
                             order by u.Amount
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Name = "A", Population = 10m, Money = 1m })).Run();

        Assert.AreEqual(2, table.Count);
        AssertRow(table[0], "A", "Money", 1m);
        AssertRow(table[1], "A", "Population", 10m);
    }

    [TestMethod]
    public void Run_WhenUnpivotCteIsJoined_ShouldComposeWithJoin()
    {
        const string query = """
                             with u as (
                                 unpivot #A.Entities() s
                                 on Metric in (s.Population as Population, s.Money as Money)
                                 using Amount
                                 keep s.City as City
                             )
                             select u.City, u.Metric, l.Country
                             from u
                             inner join #B.Entities() l on u.City = l.City
                             order by u.City, u.Metric
                             """;

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { City = "GDA", Population = 10m, Money = 1m }],
            ["#B"] = [new BasicEntity { City = "GDA", Country = "PL" }]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(2, table.Count);
        AssertRow(table[0], "GDA", "Money", "PL");
        AssertRow(table[1], "GDA", "Population", "PL");
    }

    [TestMethod]
    public void Run_WhenUnpivotSourceIsDirectJoin_ShouldExpandJoinedRows()
    {
        const string query = """
                             unpivot #A.Entities() a
                             inner join #B.Entities() b on a.City = b.City
                             on Metric in (a.Population as Population, b.Money as LookupMoney)
                             using Amount
                             keep a.City as City, b.Country as Country
                             order by City, Metric
                             """;

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { City = "GDA", Population = 10m }],
            ["#B"] = [new BasicEntity { City = "GDA", Country = "PL", Money = 2m }]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(2, table.Count);
        AssertColumn(table, 0, "City", typeof(string));
        AssertColumn(table, 1, "Country", typeof(string));
        AssertColumn(table, 2, "Metric", typeof(string));
        AssertColumn(table, 3, "Amount", typeof(decimal));
        Assert.AreEqual("GDA", table[0][0]);
        Assert.AreEqual("PL", table[0][1]);
        Assert.AreEqual("LookupMoney", table[0][2]);
        Assert.AreEqual(2m, table[0][3]);
        Assert.AreEqual("GDA", table[1][0]);
        Assert.AreEqual("PL", table[1][1]);
        Assert.AreEqual("Population", table[1][2]);
        Assert.AreEqual(10m, table[1][3]);
    }

    [TestMethod]
    public void Compile_WhenUnpivotIsInsideCte_ShouldStreamExpansionWithoutIntermediateRowList()
    {
        const string query = """
                             with u as (
                                 unpivot #A.Entities() s
                                 on Metric in (s.Population as Population, s.Money as Money)
                                 using Amount
                                 keep s.Name as Name
                             )
                             select Name, Metric, Amount from u order by Name, Metric
                             """;
        var sources = CreateSingleSource(
            new BasicEntity { Name = "A", Population = 10m, Money = 1m });

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
        Assert.Contains("CreateGeneratedRow [__unpivot <-", inspection.ExecutionPlanText);
        Assert.Contains("StoreTable [cte0 ->", inspection.ExecutionPlanText);
    }

    [TestMethod]
    public void Run_WhenUnpivotArmsUseSetOperator_ShouldCompose()
    {
        const string query = """
                             unpivot #A.Entities() s
                             on Metric in (s.Population as Population)
                             using Amount
                             keep s.Name as Name
                             union all (Name, Metric, Amount)
                             unpivot #B.Entities() s
                             on Metric in (s.Money as Money)
                             using Amount
                             keep s.Name as Name
                             order by Name
                             """;

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Name = "A", Population = 10m }],
            ["#B"] = [new BasicEntity { Name = "B", Money = 2m }]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(2, table.Count);
        AssertRow(table[0], "A", "Population", 10m);
        AssertRow(table[1], "B", "Money", 2m);
    }

    [TestMethod]
    public void Run_WhenKeepUsesExpressionAlias_ShouldProjectExpression()
    {
        const string query = """
                             unpivot #A.Entities() s
                             on Metric in (s.Population as Population)
                             using Amount
                             keep s.Name + ':' + s.Country as Label
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Name = "A", Country = "PL", Population = 10m })).Run();

        Assert.AreEqual(1, table.Count);
        AssertRow(table[0], "A:PL", "Population", 10m);
    }

    private static void AssertRow(Tables.Row row, object? first, object? second, object? third)
    {
        Assert.AreEqual(first, row[0]);
        Assert.AreEqual(second, row[1]);
        Assert.AreEqual(third, row[2]);
    }
}
