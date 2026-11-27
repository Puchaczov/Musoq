using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class JoinRowPresencePredicateTests
{
    private const string DuplicateLeftJoin =
        " from values { (K:1),(K:2) } a left join values { (K:1),(K:1) } b on a.K=b.K";

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void ASecondLeftJoinPreservesTheAbsenceOfAnEarlierSource(int secondKey)
    {
        var sql =
            "select b is present as P from values { (K:1) } a left join values { (K:2) } b on a.K=b.K"
            + $" left join values {{ (K:{secondKey}) }} c on a.K=c.K";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [false]);
    }

    [TestMethod]
    public void GroupedPresenceReadsTheJoinedRowCarrier()
    {
        const string sql = "select b is present as P,Count(*) as N" + DuplicateLeftJoin + " group by b is present";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [true, 2L], [false, 1L]);
    }

    [TestMethod]
    public void WindowPartitionReadsTheOptionalSideOfEachJoinRow()
    {
        const string sql = "select a.K,Count(*) over (partition by b is present) as N" + DuplicateLeftJoin;
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, 2L], [1, 2L], [2, 1L]);
    }

    [TestMethod]
    public void WindowFilterCountsOnlyPresentJoinRows()
    {
        const string sql = "select Count(*) filter (where b is present) over () as N" + DuplicateLeftJoin;
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [2L], [2L], [2L]);
    }

    [TestMethod]
    public void FullJoinProjectsBothStoredPresenceKeys()
    {
        const string sql =
            "select a is present as L,b is present as R,Count(*) as N from values { (K:1),(K:2) } a"
            + " full join values { (K:1),(K:3) } b on a.K=b.K group by a is present,b is present";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            result,
            [true, true, 1L],
            [true, false, 1L],
            [false, true, 1L]
        );
    }
}
