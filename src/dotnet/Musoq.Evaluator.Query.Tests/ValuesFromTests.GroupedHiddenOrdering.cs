using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ValuesFromTests
{
    [TestMethod]
    [DataRow("asc", "2", 2L, "10", 1L)]
    [DataRow("desc", "10", 1L, "2", 2L)]
    public void GroupedOrdering_WhenNumericKeyIsFormatted_ShouldPreserveNumericOrder(
        string direction,
        string firstKey,
        long firstCount,
        string secondKey,
        long secondCount
    )
    {
        var query = $$"""
            select ToString(p.Value) as Label, Count(1) as Books
            from values { (Value: 10l), (Value: 2l), (Value: 2l) } p
            group by p.Value order by p.Value {{direction}}
            """;
        var vm = CreateAndRunVirtualMachine(query, EmptySources());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Label", typeof(string)), ("Books", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [firstKey, firstCount], [secondKey, secondCount]);
    }

    [TestMethod]
    public void GroupedOrdering_WhenKeyIsNullable_ShouldHonorNullsLast()
    {
        const string query = """
            select ToString(p.Value) as Label, Count(1) as Books
            from values { (Value: null), (Value: 10l), (Value: 2l) } p
            group by p.Value order by p.Value nulls last
            """;
        var vm = CreateAndRunVirtualMachine(query, EmptySources());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["2", 1L], ["10", 1L], [null, 1L]);
    }

    [TestMethod]
    public void GroupedOrdering_WhenTupleKeysAreFormatted_ShouldKeepHiddenKeysOutOfOutput()
    {
        const string query = """
            select p.Category as Category, ToString(p.Value) as Label, Count(1) as Books
            from values {
                (Category: 'B', Value: 2l), (Category: 'A', Value: 10l),
                (Category: 'A', Value: 2l), (Category: 'A', Value: 2l)
            } p
            group by p.Category, p.Value order by p.Category, p.Value
            """;
        var vm = CreateAndRunVirtualMachine(query, EmptySources());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Category", typeof(string)),
            ("Label", typeof(string)),
            ("Books", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", "2", 2L], ["A", "10", 1L], ["B", "2", 1L]);
    }

    [TestMethod]
    public void GroupedOrdering_WhenHavingAndPagingAreApplied_ShouldPageNumericOrder()
    {
        const string query = """
            select ToString(p.Value) as Label, Count(1) as Books
            from values { (Value: 20l), (Value: 10l), (Value: 2l), (Value: 10l), (Value: 2l) } p
            group by p.Value having Count(1) > 1 order by p.Value desc skip 1 take 1
            """;
        var vm = CreateAndRunVirtualMachine(query, EmptySources());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["2", 2L]);
    }

    [TestMethod]
    public void GroupedOrdering_WhenHistogramUsesNestedCtes_ShouldOrderOriginalCount()
    {
        const string query = """
            with DistinctLinks as (
                select e.BookId, e.AuthorId from values {
                    (BookId: 1, AuthorId: 1), (BookId: 2, AuthorId: 2),
                    (BookId: 2, AuthorId: 3), (BookId: 2, AuthorId: 3)
                } e group by e.BookId, e.AuthorId
            ), PerBook as (
                select l.BookId, Count(1) as AuthorCount from DistinctLinks l group by l.BookId
            )
            select ToString(p.AuthorCount) as AuthorsPerBook, ToString(Count(1)) as Books
            from PerBook p group by p.AuthorCount order by p.AuthorCount
            """;
        var vm = CreateAndRunVirtualMachine(query, EmptySources());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("AuthorsPerBook", typeof(string)),
            ("Books", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["1", "1"], ["2", "1"]);
    }

    [TestMethod]
    public void GroupedOrdering_WhenProfiled_ShouldExposeOnlyPublicColumns()
    {
        const string query = """
            select ToString(p.Value) as Label, Count(1) as Books
            from values { (Value: 10l), (Value: 2l), (Value: 2l) } p
            group by p.Value order by p.Value
            """;
        var vm = CreateAndRunVirtualMachine(
            query,
            EmptySources(),
            TestCompilationOptions.WithInstrumentationMode(QueryInstrumentationMode.Full)
        );

        var result = vm.RunWithProfile(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result.Result, ("Label", typeof(string)), ("Books", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(result.Result, ["2", 2L], ["10", 1L]);
    }
}
