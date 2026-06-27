using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests : BasicEntityTestBase
{
    #region §11 Set Operations

    [TestMethod]
    public void Spec_SetOp_UnionAll()
    {
        var query = @"
            select Name from #A.Entities()
            union all (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] },
            { "#B", [new BasicEntity("Bob"), new BasicEntity("Charlie")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(4, table.Count, "UNION ALL should preserve all rows including duplicates");
    }

    [TestMethod]
    public void Spec_SetOp_Union_ShouldDedup()
    {
        var query = @"
            select Name from #A.Entities()
            union (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] },
            { "#B", [new BasicEntity("Bob"), new BasicEntity("Charlie")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count, "UNION should deduplicate: Alice, Bob, Charlie");
    }

    [TestMethod]
    public void Spec_SetOp_Except()
    {
        var query = @"
            select Name from #A.Entities()
            except (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] },
            { "#B", [new BasicEntity("Bob"), new BasicEntity("Charlie")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count, "EXCEPT: Alice is in A but not in B");
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_SetOp_Intersect()
    {
        var query = @"
            select Name from #A.Entities()
            intersect (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] },
            { "#B", [new BasicEntity("Bob"), new BasicEntity("Charlie")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count, "INTERSECT: Only Bob appears in both");
        Assert.AreEqual("Bob", table[0][0]);
    }

    #endregion

    #region §12 ORDER BY, SKIP, TAKE

    [TestMethod]
    public void Spec_OrderBy_Ascending()
    {
        var query = "select Name from #A.Entities() order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Charlie"),
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual("Bob", table[1][0]);
        Assert.AreEqual("Charlie", table[2][0]);
    }

    [TestMethod]
    public void Spec_OrderBy_Descending()
    {
        var query = "select Name from #A.Entities() order by Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Charlie", table[0][0]);
        Assert.AreEqual("Bob", table[1][0]);
        Assert.AreEqual("Alice", table[2][0]);
    }

    [TestMethod]
    public void Spec_Skip()
    {
        var query = "select Name from #A.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Spec_Take()
    {
        var query = "select Name from #A.Entities() take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Skip_ExceedsRowCount_ShouldReturnZeroRows()
    {
        var query = "select Name from #A.Entities() skip 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count, "SKIP exceeding row count should return 0 rows (no error per spec)");
    }

    [TestMethod]
    public void Spec_SkipTake_Pagination()
    {
        var query = "select Name from #A.Entities() order by Name skip 1 take 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Bob", table[0][0]);
    }

    #endregion

    #region §13 Common Table Expressions (CTEs)

    [TestMethod]
    public void Spec_CTE_SimpleQuery()
    {
        var query = @"
            with p as (
                select City, Country from #A.Entities()
            )
            select Country, City from p";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("a") { City = "WARSAW", Country = "POLAND" }]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("POLAND", table[0][0]);
        Assert.AreEqual("WARSAW", table[0][1]);
    }

    [TestMethod]
    public void Spec_CTE_StarExpansion()
    {
        var query = @"
            with p as (
                select City, Country from #A.Entities()
            )
            select * from p";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a") { City = "WARSAW", Country = "POLAND" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count(), "Star expansion from CTE with 2 columns");
    }

    [TestMethod]
    public void Spec_CTE_WithJoin()
    {
        var query = @"
            with p as (select City, Country from #A.Entities())
            select p.City, b.Population
            from p
            inner join #B.Entities() b on p.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a") { City = "NYC", Country = "USA" }] },
            { "#B", [new BasicEntity("b") { City = "NYC", Population = 8000000m }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NYC", table[0][0]);
        Assert.AreEqual(8000000m, table[0][1]);
    }

    [TestMethod]
    public void Spec_CTE_WithSetOperation()
    {
        var query = @"
            with combined as (
                select Name from #A.Entities()
                union all (Name)
                select Name from #B.Entities()
            )
            select * from combined";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] },
            { "#B", [new BasicEntity("Bob")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region §16 Reordered Query Syntax (FROM-first)

    [TestMethod]
    public void Spec_Reordered_SimpleFromSelect()
    {
        var query = "from #A.Entities() select City, Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a") { City = "WARSAW", Country = "POLAND" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0][0]);
        Assert.AreEqual("POLAND", table[0][1]);
    }

    [TestMethod]
    public void Spec_Reordered_WithWhere()
    {
        var query = "from #A.Entities() where Country = 'POLAND' select City, Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { City = "WARSAW", Country = "POLAND" },
                    new BasicEntity("b") { City = "BERLIN", Country = "GERMANY" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0][0]);
    }

    [TestMethod]
    public void Spec_Reordered_WithGroupBy()
    {
        var query = "from #A.Entities() group by Country select Country, Count(Country)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND" },
                    new BasicEntity("b") { Country = "POLAND" },
                    new BasicEntity("c") { Country = "GERMANY" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region §18 NULL Semantics

    [TestMethod]
    public void Spec_Null_Propagation_AdditionWithNull()
    {
        var query = "select null + 1 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_SubtractionWithNull()
    {
        var query = "select 10 - null from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_MultiplicationWithNull()
    {
        var query = "select null * 5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_DivisionWithNull()
    {
        var query = "select null / 2 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_ModuloWithNull()
    {
        var query = "select null % 3 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_NullPlusNull()
    {
        var query = "select null + null from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    #endregion

    #region §20 Array and Property Access

    [TestMethod]
    public void Spec_ArrayIndexing_Basic()
    {
        var query = "select Array[0] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, table[0][0]);
    }

    [TestMethod]
    public void Spec_ArrayIndexing_Negative_ShouldWrap()
    {
        var query = "select Array[-1] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0], "Array[-1] should return the last element");
    }

    [TestMethod]
    public void Spec_PropertyNavigation_SingleLevel()
    {
        var query = "select Self.Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_PropertyNavigation_TwoLevels()
    {
        var query = "select Self.Self.Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    #endregion

    #region §Appendix F: CASE WHEN

    [TestMethod]
    public void Spec_CaseWhen_MultiBranch_StringEquality()
    {
        var query = @"
            select
                City,
                case
                    when City = 'Warsaw' then 'capital'
                    when City = 'Katowice' then 'silesia'
                    else 'other'
                end
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 1000),
                    new BasicEntity("Katowice", "Poland", 200),
                    new BasicEntity("Radom", "Poland", 50)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);


        var results = table.ToDictionary(r => (string)r.Values[0], r => (string)r.Values[1]);
        Assert.AreEqual("capital", results["Warsaw"]);
        Assert.AreEqual("silesia", results["Katowice"]);
        Assert.AreEqual("other", results["Radom"]);
    }

    [TestMethod]
    public void Spec_CaseWhen_SingleBranch_NumericComparison()
    {
        var query = @"
            select
                Name,
                case
                    when Id > 100 then 'large'
                    else 'small'
                end
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw") { Id = 1000 },
                    new BasicEntity("Radom") { Id = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);


        var results = table.ToDictionary(r => (string)r.Values[0], r => (string)r.Values[1]);
        Assert.AreEqual("large", results["Warsaw"]);
        Assert.AreEqual("small", results["Radom"]);
    }

    [TestMethod]
    public void Spec_CaseWhen_MultiBranch_DecimalComparison()
    {
        var query = @"
            select
                City,
                case
                    when Population >= 500d then 'large'
                    when Population >= 100d then 'medium'
                    else 'small'
                end
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 1000),
                    new BasicEntity("Katowice", "Poland", 200),
                    new BasicEntity("Radom", "Poland", 50)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);


        var results = table.ToDictionary(r => (string)r.Values[0], r => (string)r.Values[1]);
        Assert.AreEqual("large", results["Warsaw"]);
        Assert.AreEqual("medium", results["Katowice"]);
        Assert.AreEqual("small", results["Radom"]);
    }

    [TestMethod]
    public void Spec_CaseWhen_InArithmetic()
    {
        TestMethodTemplate("1 + (case when 2 > 1 then 1 else 0 end) - 1", 1);
    }

    #endregion
}
