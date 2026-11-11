using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Integration tests designed to hit uncovered branches in the evaluator.
///     These tests execute complete SQL queries through the entire pipeline
///     to exercise code paths in visitors, emitters, and code generation.
/// </summary>
[global::Musoq.Tests.Common.CultureTestClass]
public partial class EndToEndQueryExecutionTests : BasicEntityTestBase
{
    [TestMethod]
    public void Query_ChainedExcept_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name from #A.Entities()\n"
            + "            except (Name)\n"
            + "            select Name from #B.Entities()\n"
            + "            except (Name)\n"
            + "            select Name from #C.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] },
            { "#B", [new BasicEntity("B")] },
            { "#C", [new BasicEntity("C")] },
        };
        using var vm = CreateAndRunVirtualMachine(query, sources);
        using var table = vm.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A"], ["D"]);
    }

    [TestMethod]
    public void Query_NestedCaseWhen_ShouldWork()
    {
        var query = (
            "\n"
            + "            select \n"
            + "                case \n"
            + "                    when Population > 100 then \n"
            + "                        case \n"
            + "                            when City = 'NYC' then 'Big NYC' \n"
            + "                            else 'Big Other' \n"
            + "                        end\n"
            + "                    else 'Small'\n"
            + "                end as Size\n"
            + "            from #A.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity
                    {
                        Name = "A",
                        City = "NYC",
                        Population = 200,
                    },
                    new BasicEntity
                    {
                        Name = "B",
                        City = "LA",
                        Population = 200,
                    },
                    new BasicEntity
                    {
                        Name = "C",
                        City = "NYC",
                        Population = 50,
                    },
                ]
            },
        };
        using var vm = CreateAndRunVirtualMachine(query, sources);
        using var table = vm.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Big NYC"], ["Big Other"], ["Small"]);
    }

    [TestMethod]
    public void Query_ComplexWhereWithAndOr_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name \n"
            + "            from #A.Entities() \n"
            + "            where (Name = 'A' or Name = 'B') and Population > 50"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", Population = 100 },
                    new BasicEntity { Name = "B", Population = 30 },
                    new BasicEntity { Name = "C", Population = 200 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_WhereWithNotEquals_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name <> 'A'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A" }, new BasicEntity { Name = "B" }] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_WhereWithNotIn_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name not in ('A', 'B')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("C", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_WhereWithNotLike_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name not like 'Test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Other"), new BasicEntity("Test2")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Other", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_WhereWithBetween_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Population >= 100 and Population <= 200";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 150 },
                    new BasicEntity { Name = "C", Population = 250 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_WhereWithIsNotNull_ShouldWork()
    {
        var query = "select Name from #A.Entities() where NullableValue is not null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity("HasValue") { NullableValue = 5 }, new BasicEntity("NoValue") { NullableValue = null }]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("HasValue", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_CaseWhenSimple_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name,\n"
            + "                case when Population > 100 then 'High' else 'Low' end as Category\n"
            + "            from #A.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity { Name = "A", Population = 50 }, new BasicEntity { Name = "B", Population = 200 }]
            },
        };
        using var vm = CreateAndRunVirtualMachine(query, sources);
        using var table = vm.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A", "Low"], ["B", "High"]);
    }

    [TestMethod]
    public void Query_CaseWhenMultipleConditions_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name,\n"
            + "                case \n"
            + "                    when Population > 200 then 'High'\n"
            + "                    when Population > 100 then 'Medium'\n"
            + "                    else 'Low'\n"
            + "                end as Category\n"
            + "            from #A.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 150 },
                    new BasicEntity { Name = "C", Population = 300 },
                ]
            },
        };
        using var vm = CreateAndRunVirtualMachine(query, sources);
        using var table = vm.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A", "Low"], ["B", "Medium"], ["C", "High"]);
    }

    [TestMethod]
    public void Query_CaseWhenInWhere_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name\n"
            + "            from #A.Entities()\n"
            + "            where case when Population > 100 then 1 else 0 end = 1"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity { Name = "A", Population = 50 }, new BasicEntity { Name = "B", Population = 200 }]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_CaseWhenWithNullHandling_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name,\n"
            + "                case \n"
            + "                    when NullableValue is null then 'No Value'\n"
            + "                    when NullableValue > 5 then 'High'\n"
            + "                    else 'Low'\n"
            + "                end as Category\n"
            + "            from #A.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A") { NullableValue = null },
                    new BasicEntity("B") { NullableValue = 10 },
                    new BasicEntity("C") { NullableValue = 3 },
                ]
            },
        };
        using var vm = CreateAndRunVirtualMachine(query, sources);
        using var table = vm.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A", "No Value"], ["B", "High"], ["C", "Low"]);
    }

    [TestMethod]
    public void Query_GroupByWithMultipleAggregates_ShouldWork()
    {
        var query = (
            "\n"
            + "            select City, \n"
            + "                Count(City) as Cnt,\n"
            + "                Sum(Population) as TotalPop,\n"
            + "                Avg(Population) as AvgPop,\n"
            + "                Min(Population) as MinPop,\n"
            + "                Max(Population) as MaxPop\n"
            + "            from #A.Entities()\n"
            + "            group by City"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity
                    {
                        Name = "A",
                        City = "NYC",
                        Population = 100,
                    },
                    new BasicEntity
                    {
                        Name = "B",
                        City = "NYC",
                        Population = 200,
                    },
                    new BasicEntity
                    {
                        Name = "C",
                        City = "LA",
                        Population = 50,
                    },
                ]
            },
        };
        using var vm = CreateAndRunVirtualMachine(query, sources);
        using var table = vm.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["NYC", 2L, 300m, 150m, 100m, 200m],
            ["LA", 1L, 50m, 50m, 50m, 50m]
        );
    }
}
