using System.Collections.Generic;
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Alice" }, new object?[] { "Bob" },
            new object?[] { "Bob" }, new object?[] { "Charlie" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Alice" }, new object?[] { "Bob" }, new object?[] { "Charlie" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "Alice" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "Bob" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Alice" }, new object?[] { "Bob" }, new object?[] { "Charlie" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Charlie" }, new object?[] { "Bob" }, new object?[] { "Alice" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "Charlie" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Alice" }, new object?[] { "Bob" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "Bob" });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("Country", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "POLAND", "WARSAW" });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "WARSAW", "POLAND" });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("p.City", typeof(string)), ("b.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "NYC", 8000000m });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Alice" }, new object?[] { "Bob" });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "WARSAW", "POLAND" });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "WARSAW", "POLAND" });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("Country", typeof(string)), ("Count(Country)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "POLAND", 2L }, new object?[] { "GERMANY", 1L });
    }

    #endregion

    #region §18 NULL Semantics

    [TestMethod]
    public void Spec_Null_Propagation_AdditionWithNull()
    {
        var query = "select null + 1 as Value from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }

    [TestMethod]
    public void Spec_Null_Propagation_SubtractionWithNull()
    {
        var query = "select 10 - null as Value from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }

    [TestMethod]
    public void Spec_Null_Propagation_MultiplicationWithNull()
    {
        var query = "select null * 5 as Value from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }

    [TestMethod]
    public void Spec_Null_Propagation_DivisionWithNull()
    {
        var query = "select null / 2 as Value from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }

    [TestMethod]
    public void Spec_Null_Propagation_ModuloWithNull()
    {
        var query = "select null % 3 as Value from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }

    [TestMethod]
    public void Spec_Null_Propagation_NullPlusNull()
    {
        var query = "select null + null as Value from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(object)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Array[0]", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 0 });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Array[-1]", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 2 });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Self.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "Alice" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Self.Self.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "Alice" });
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
                end as Category
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Category", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Warsaw", "capital" },
            new object?[] { "Katowice", "silesia" },
            new object?[] { "Radom", "other" });
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
                end as Category
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("Name", typeof(string)), ("Category", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Warsaw", "large" },
            new object?[] { "Radom", "small" });
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
                end as Category
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Category", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Warsaw", "large" },
            new object?[] { "Katowice", "medium" },
            new object?[] { "Radom", "small" });
    }

    [TestMethod]
    public void Spec_CaseWhen_InArithmetic()
    {
        TestMethodTemplate("1 + (case when 2 > 1 then 1 else 0 end) - 1", 1);
    }

    #endregion
}
