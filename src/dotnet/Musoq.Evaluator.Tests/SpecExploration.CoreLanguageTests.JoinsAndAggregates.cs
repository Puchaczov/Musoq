using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    #region §8 JOIN Clause

    [TestMethod]
    public void Spec_Join_InnerJoin()
    {
        var query = @"
            select a.City, b.City
            from #A.Entities() a
            inner join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { City = "NYC" },
                    new BasicEntity("b") { City = "LA" }
                ]
            },
            {
                "#B", [
                    new BasicEntity("c") { City = "NYC" },
                    new BasicEntity("d") { City = "SF" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.City", typeof(string)), ("b.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "NYC", "NYC" });
    }

    [TestMethod]
    public void Spec_Join_LeftOuterJoin()
    {
        var query = @"
            select a.Name, b.Name
            from #A.Entities() a
            left outer join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("PersonA") { City = "NYC" },
                    new BasicEntity("PersonC") { City = "LA" }
                ]
            },
            {
                "#B", [
                    new BasicEntity("PersonB") { City = "NYC" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "PersonA", "PersonB" },
            new object?[] { "PersonC", null });
    }

    [TestMethod]
    public void Spec_Join_CrossJoin()
    {
        var query = @"
            select a.Name, b.Name
            from #A.Entities() a
            cross join #B.Entities() b";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A1"),
                    new BasicEntity("A2")
                ]
            },
            {
                "#B", [
                    new BasicEntity("B1"),
                    new BasicEntity("B2")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "A1", "B1" }, new object?[] { "A1", "B2" },
            new object?[] { "A2", "B1" }, new object?[] { "A2", "B2" });
    }

    #endregion

    #region §10 GROUP BY and Aggregation

    [TestMethod]
    public void Spec_GroupBy_Count()
    {
        var query = "select Country, Count(Country) from #A.Entities() group by Country";
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

    [TestMethod]
    public void Spec_GroupBy_Sum()
    {
        var query = "select Country, Sum(Population) from #A.Entities() group by Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND", Population = 100m },
                    new BasicEntity("b") { Country = "POLAND", Population = 200m },
                    new BasicEntity("c") { Country = "GERMANY", Population = 500m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Country", typeof(string)), ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "POLAND", 300m }, new object?[] { "GERMANY", 500m });
    }

    [TestMethod]
    public void Spec_GroupBy_Having()
    {
        var query = @"
            select Name, Count(Name)
            from #A.Entities()
            group by Name
            having Count(Name) >= 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Name", typeof(string)), ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Alice", 2L });
    }

    [TestMethod]
    public void Spec_GroupBy_WithConstant_ShouldTreatAllRowsAsSingleGroup()
    {
        var query = "select Count(Country) from #A.Entities() group by 'fake'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND" },
                    new BasicEntity("b") { Country = "GERMANY" },
                    new BasicEntity("c") { Country = "FRANCE" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Count(Country)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 3L });
    }

    #endregion
}
