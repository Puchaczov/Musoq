using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class QualifyTests : BasicEntityTestBase
{

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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 1L],
            ["Bob", 2L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Name", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["LA", "Alice", 1L],
            ["NYC", "Bob", 1L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("dr", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 1L],
            ["Charlie", 1L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 1L],
            ["Bob", 2L],
            ["Charlie", 3L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Bob", 2L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 1L],
            ["Bob", 2L]);
    }
}
