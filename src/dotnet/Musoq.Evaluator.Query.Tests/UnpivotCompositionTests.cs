using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class UnpivotCompositionTests : BasicEntityTestBase
{
    [TestMethod]
    public void Run_WhenUnpivotIsInCte_ShouldBeSelectable()
    {
        const string query = (
            "with u as (\n"
            + "    unpivot #A.Entities() s\n"
            + "    on Metric in (s.Population as Population, s.Money as Money)\n"
            + "    using Amount\n"
            + "    keep s.Name as Name\n"
            + ")\n"
            + "select Name, Metric, Amount from u order by Name, Metric"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity
                    {
                        Name = "A",
                        Population = 10m,
                        Money = 1m,
                    }
                )
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", "Money", 1m], ["A", "Population", 10m]);
    }

    [TestMethod]
    public void Run_WhenUnpivotIsInDerivedTable_ShouldBeSelectable()
    {
        const string query = (
            "select u.Name, u.Metric, u.Amount\n"
            + "from (\n"
            + "    unpivot #A.Entities() s\n"
            + "    on Metric in (s.Population as Population, s.Money as Money)\n"
            + "    using Amount\n"
            + "    keep s.Name as Name\n"
            + ") u\n"
            + "order by u.Amount"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity
                    {
                        Name = "A",
                        Population = 10m,
                        Money = 1m,
                    }
                )
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", "Money", 1m], ["A", "Population", 10m]);
    }

    [TestMethod]
    public void Run_WhenUnpivotCteIsJoined_ShouldComposeWithJoin()
    {
        const string query = (
            "with u as (\n"
            + "    unpivot #A.Entities() s\n"
            + "    on Metric in (s.Population as Population, s.Money as Money)\n"
            + "    using Amount\n"
            + "    keep s.City as City\n"
            + ")\n"
            + "select u.City, u.Metric, l.Country\n"
            + "from u\n"
            + "inner join #B.Entities() l on u.City = l.City\n"
            + "order by u.City, u.Metric"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity
                {
                    City = "GDA",
                    Population = 10m,
                    Money = 1m,
                },
            ],
            ["#B"] = [new BasicEntity { City = "GDA", Country = "PL" }],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Metric", typeof(string)),
            ("Country", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["GDA", "Money", "PL"], ["GDA", "Population", "PL"]);
    }

    [TestMethod]
    public void Run_WhenUnpivotSourceIsDirectJoin_ShouldExpandJoinedRows()
    {
        const string query = (
            "unpivot #A.Entities() a\n"
            + "inner join #B.Entities() b on a.City = b.City\n"
            + "on Metric in (a.Population as Population, b.Money as LookupMoney)\n"
            + "using Amount\n"
            + "keep a.City as City, b.Country as Country\n"
            + "order by City, Metric"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { City = "GDA", Population = 10m }],
            ["#B"] =
            [
                new BasicEntity
                {
                    City = "GDA",
                    Country = "PL",
                    Money = 2m,
                },
            ],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Country", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["GDA", "PL", "LookupMoney", 2m],
            ["GDA", "PL", "Population", 10m]
        );
    }

    [TestMethod]
    public void Compile_WhenUnpivotIsInsideCte_ShouldStreamExpansionWithoutIntermediateRowList()
    {
        const string query = (
            "with u as (\n"
            + "    unpivot #A.Entities() s\n"
            + "    on Metric in (s.Population as Population, s.Money as Money)\n"
            + "    using Amount\n"
            + "    keep s.Name as Name\n"
            + ")\n"
            + "select Name, Metric, Amount from u order by Name, Metric"
        );
        var sources = CreateSingleSource(
            new BasicEntity
            {
                Name = "A",
                Population = 10m,
                Money = 1m,
            }
        );

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            TestCompilationOptions
        );

        Assert.IsFalse(
            inspection.ExecutionPlanText.Contains("CreateUnpivotRows", StringComparison.Ordinal),
            inspection.ExecutionPlanText
        );
        Assert.IsFalse(
            inspection.GeneratedCSharpCode.Contains("__unpivotRows", StringComparison.Ordinal),
            inspection.GeneratedCSharpCode
        );
        Assert.Contains("CreateGeneratedRow [__unpivot <-", inspection.ExecutionPlanText);
        Assert.Contains("StoreTable [cte0 ->", inspection.ExecutionPlanText);
    }

    [TestMethod]
    public void Run_WhenUnpivotArmsUseSetOperator_ShouldCompose()
    {
        const string query = (
            "unpivot #A.Entities() s\n"
            + "on Metric in (s.Population as Population)\n"
            + "using Amount\n"
            + "keep s.Name as Name\n"
            + "union all (Name, Metric, Amount)\n"
            + "unpivot #B.Entities() s\n"
            + "on Metric in (s.Money as Money)\n"
            + "using Amount\n"
            + "keep s.Name as Name\n"
            + "order by Name"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Name = "A", Population = 10m }],
            ["#B"] = [new BasicEntity { Name = "B", Money = 2m }],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", "Population", 10m], ["B", "Money", 2m]);
    }

    [TestMethod]
    public void Run_WhenKeepUsesExpressionAlias_ShouldProjectExpression()
    {
        const string query = (
            "unpivot #A.Entities() s\n"
            + "on Metric in (s.Population as Population)\n"
            + "using Amount\n"
            + "keep s.Name + ':' + s.Country as Label"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity
                    {
                        Name = "A",
                        Country = "PL",
                        Population = 10m,
                    }
                )
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Label", typeof(string)),
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A:PL", "Population", 10m]);
    }

    [TestMethod]
    public void Run_WhenPivotCteIsUnpivotedAndWindowed_ShouldPreserveMissingMeasureRows()
    {
        const string query = (
            "with p as (\n"
            + "    pivot #A.Entities()\n"
            + "    on Month in ('Jan' as Jan, 'Feb' as Feb)\n"
            + "    using Sum(Money) as Sales\n"
            + "    group by City\n"
            + "), longRows as (\n"
            + "    unpivot p s\n"
            + "    on Month in (s.Jan as Jan, s.Feb as Feb)\n"
            + "    using Sales\n"
            + "    keep s.City as City\n"
            + ")\n"
            + "select City, Month, Sales,\n"
            + "       RowNumber() over (partition by City order by Month) as Rank\n"
            + "from longRows\n"
            + "order by City, Month"
        );
        var sources = CreateSingleSource(
            new BasicEntity("NY", "Jan", 10m),
            new BasicEntity("NY", "Feb", 20m),
            new BasicEntity("LA", "Jan", 5m)
        );

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Month", typeof(string)),
            ("Sales", typeof(decimal?)),
            ("Rank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", "Feb", null, 1L],
            ["LA", "Jan", 5m, 2L],
            ["NY", "Feb", 20m, 1L],
            ["NY", "Jan", 10m, 2L]
        );
    }

    [TestMethod]
    public void Run_WhenPivotAndUnpivotAreQualified_ShouldKeepNullableMissingMeasure()
    {
        const string query = (
            "with p as (\n"
            + "    pivot #A.Entities()\n"
            + "    on Month in ('Jan' as Jan, 'Feb' as Feb)\n"
            + "    using Sum(Money) as Sales\n"
            + "    group by City\n"
            + "), longRows as (\n"
            + "    unpivot p s\n"
            + "    on Month in (s.Jan as Jan, s.Feb as Feb)\n"
            + "    using Sales\n"
            + "    keep s.City as City\n"
            + ")\n"
            + "select City, Month, Sales,\n"
            + "       RowNumber() over (partition by City order by Month) as Rank\n"
            + "from longRows\n"
            + "qualify RowNumber() over (partition by City order by Month) <= 1\n"
            + "order by City, Month"
        );
        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity("NY", "Jan", 10m),
                    new BasicEntity("NY", "Feb", 20m),
                    new BasicEntity("LA", "Jan", 5m)
                )
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Month", typeof(string)),
            ("Sales", typeof(decimal?)),
            ("Rank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["LA", "Feb", null, 1L], ["NY", "Feb", 20m, 1L]);
    }
}
