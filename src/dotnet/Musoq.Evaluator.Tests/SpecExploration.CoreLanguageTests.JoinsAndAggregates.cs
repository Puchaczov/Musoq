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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NYC", table[0][0]);
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

        Assert.AreEqual(2, table.Count);


        var matchedRow = table[0][1] != null ? 0 : 1;
        var unmatchedRow = 1 - matchedRow;

        Assert.AreEqual("PersonA", table[matchedRow][0]);
        Assert.AreEqual("PersonB", table[matchedRow][1]);
        Assert.AreEqual("PersonC", table[unmatchedRow][0]);
        Assert.IsNull(table[unmatchedRow][1], "Unmatched right side should be null");
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

        Assert.AreEqual(4, table.Count, "Cartesian product of 2x2 should be 4 rows");
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

        Assert.AreEqual(2, table.Count);
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

        Assert.AreEqual(2, table.Count);
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3L, table[0][0]);
    }

    #endregion
}
