using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class QualifyTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenQualifyFiltersRowNumber_ShouldReturnOnlyMatchingRows()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (order by Name) <= 2";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Diana"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Alice"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Bob"));
    }

    [TestMethod]
    public void WhenQualifyWithPartitionedRowNumber_ShouldFilterPerPartition()
    {
        var query = @"
            select City, Name, RowNumber() over (partition by City order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (partition by City order by Name) = 1";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "LA" && (string)r.Values[1] == "Alice"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "NYC" && (string)r.Values[1] == "Bob"));
    }

    [TestMethod]
    public void WhenQualifyFiltersDenseRank_ShouldReturnTopRanked()
    {
        var query = @"
            select Name, DenseRank() over (order by City) as dr
            from #A.Entities()
            qualify DenseRank() over (order by City) <= 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "LA" },
            new BasicEntity("Diana") { City = "SF" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Alice"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Charlie"));
    }

    [TestMethod]
    public void WhenQualifyMatchesNoRows_ShouldReturnEmptyResult()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (order by Name) > 100";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenQualifyMatchesAllRows_ShouldReturnAllRows()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (order by Name) >= 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void WhenQualifyWithSkipAndTake_ShouldApplyAfterQualify()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (order by Name) <= 3
            skip 1
            take 1";

        var sources = CreateSingleSource(
            new BasicEntity("Diana"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenQualifyWithWhereClause_ShouldApplyWhereThenQualify()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            where Name != 'Diana'
            qualify RowNumber() over (order by Name) <= 2";

        var sources = CreateSingleSource(
            new BasicEntity("Diana"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Alice"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Bob"));
    }
}
