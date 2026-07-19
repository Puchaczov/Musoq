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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A", "Money", 1m],
            ["A", "Population", 10m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("u.Name", typeof(string)),
            ("u.Metric", typeof(string)),
            ("u.Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A", "Money", 1m],
            ["A", "Population", 10m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("u.City", typeof(string)),
            ("u.Metric", typeof(string)),
            ("l.Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["GDA", "Money", "PL"],
            ["GDA", "Population", "PL"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Country", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["GDA", "PL", "LookupMoney", 2m],
            ["GDA", "PL", "Population", 10m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Label", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A:PL", "Population", 10m]);
    }

    [TestMethod]
    public void Run_WhenPivotCteIsUnpivotedAndWindowed_ShouldPreserveMissingMeasureRows()
    {
        const string query = """
                             with p as (
                                 pivot #A.Entities()
                                 on Month in ('Jan' as Jan, 'Feb' as Feb)
                                 using Sum(Money) as Sales
                                 group by City
                             ), longRows as (
                                 unpivot p s
                                 on Month in (s.Jan as Jan, s.Feb as Feb)
                                 using Sales
                                 keep s.City as City
                             )
                             select City, Month, Sales,
                                    RowNumber() over (partition by City order by Month) as Rank
                             from longRows
                             order by City, Month
                             """;
        var sources = CreateSingleSource(
            new BasicEntity("NY", "Jan", 10m),
            new BasicEntity("NY", "Feb", 20m),
            new BasicEntity("LA", "Jan", 5m));

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Month", typeof(string)),
            ("Sales", typeof(decimal?)),
            ("Rank", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", "Feb", null, 1L],
            ["LA", "Jan", 5m, 2L],
            ["NY", "Feb", 20m, 1L],
            ["NY", "Jan", 10m, 2L]);
    }

    [TestMethod]
    public void Run_WhenPivotAndUnpivotAreQualified_ShouldKeepNullableMissingMeasure()
    {
        const string query = """
                             with p as (
                                 pivot #A.Entities()
                                 on Month in ('Jan' as Jan, 'Feb' as Feb)
                                 using Sum(Money) as Sales
                                 group by City
                             ), longRows as (
                                 unpivot p s
                                 on Month in (s.Jan as Jan, s.Feb as Feb)
                                 using Sales
                                 keep s.City as City
                             )
                             select City, Month, Sales,
                                    RowNumber() over (partition by City order by Month) as Rank
                             from longRows
                             qualify RowNumber() over (partition by City order by Month) <= 1
                             order by City, Month
                             """;
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity("NY", "Jan", 10m),
            new BasicEntity("NY", "Feb", 20m),
            new BasicEntity("LA", "Jan", 5m))).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Month", typeof(string)),
            ("Sales", typeof(decimal?)),
            ("Rank", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", "Feb", null, 1L],
            ["NY", "Feb", 20m, 1L]);
    }

}
