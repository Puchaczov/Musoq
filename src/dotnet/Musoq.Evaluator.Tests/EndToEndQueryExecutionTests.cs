using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Integration tests designed to hit uncovered branches in the evaluator.
///     These tests execute complete SQL queries through the entire pipeline
///     to exercise code paths in visitors, emitters, and code generation.
/// </summary>
[TestClass]
public partial class EndToEndQueryExecutionTests : BasicEntityTestBase
{

    [TestMethod]
    public void Query_IsNullInWhere_ShouldWork()
    {
        var query = "select Name from #A.Entities() where NullableValue is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("HasValue") { NullableValue = 5 },
                    new BasicEntity("NoValue") { NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NoValue", table[0][0]);
    }



    [TestMethod]
    public void Query_ChainedExcept_ShouldWork()
    {
        var query = @"
            select Name from #A.Entities()
            except (Name)
            select Name from #B.Entities()
            except (Name)
            select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] },
            { "#B", [new BasicEntity("B")] },
            { "#C", [new BasicEntity("C")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Query_NestedCaseWhen_ShouldWork()
    {
        var query = @"
            select 
                case 
                    when Population > 100 then 
                        case 
                            when City = 'NYC' then 'Big NYC' 
                            else 'Big Other' 
                        end
                    else 'Small'
                end as Size
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "NYC", Population = 200 },
                    new BasicEntity { Name = "B", City = "LA", Population = 200 },
                    new BasicEntity { Name = "C", City = "NYC", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }



    [TestMethod]
    public void Query_ComplexWhereWithAndOr_ShouldWork()
    {
        var query = @"
            select Name 
            from #A.Entities() 
            where (Name = 'A' or Name = 'B') and Population > 50";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 100 },
                    new BasicEntity { Name = "B", Population = 30 },
                    new BasicEntity { Name = "C", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithNotEquals_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name <> 'A'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A" }, new BasicEntity { Name = "B" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithNotIn_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name not in ('A', 'B')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("C", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithNotLike_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name not like 'Test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Other"), new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Other", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithBetween_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Population >= 100 and Population <= 200";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 150 },
                    new BasicEntity { Name = "C", Population = 250 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithIsNotNull_ShouldWork()
    {
        var query = "select Name from #A.Entities() where NullableValue is not null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("HasValue") { NullableValue = 5 },
                    new BasicEntity("NoValue") { NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("HasValue", table[0][0]);
    }



    [TestMethod]
    public void Query_CaseWhenSimple_ShouldWork()
    {
        var query = @"
            select Name,
                case when Population > 100 then 'High' else 'Low' end as Category
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_CaseWhenMultipleConditions_ShouldWork()
    {
        var query = @"
            select Name,
                case 
                    when Population > 200 then 'High'
                    when Population > 100 then 'Medium'
                    else 'Low'
                end as Category
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 150 },
                    new BasicEntity { Name = "C", Population = 300 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Query_CaseWhenInWhere_ShouldWork()
    {
        var query = @"
            select Name
            from #A.Entities()
            where case when Population > 100 then 1 else 0 end = 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", table[0][0]);
    }

    [TestMethod]
    public void Query_CaseWhenWithNullHandling_ShouldWork()
    {
        var query = @"
            select Name,
                case 
                    when NullableValue is null then 'No Value'
                    when NullableValue > 5 then 'High'
                    else 'Low'
                end as Category
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A") { NullableValue = null },
                    new BasicEntity("B") { NullableValue = 10 },
                    new BasicEntity("C") { NullableValue = 3 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }



    [TestMethod]
    public void Query_GroupByWithMultipleAggregates_ShouldWork()
    {
        var query = @"
            select City, 
                Count(City) as Cnt,
                Sum(Population) as TotalPop,
                Avg(Population) as AvgPop,
                Min(Population) as MinPop,
                Max(Population) as MaxPop
            from #A.Entities()
            group by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "NYC", Population = 100 },
                    new BasicEntity { Name = "B", City = "NYC", Population = 200 },
                    new BasicEntity { Name = "C", City = "LA", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

}
