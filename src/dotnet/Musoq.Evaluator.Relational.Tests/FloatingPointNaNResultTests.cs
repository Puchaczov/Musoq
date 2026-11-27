using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class FloatingPointNaNResultTests : BasicEntityTestBase
{
    private const string ValuesCte = "with v as (select t::double as x from values {(t:'NaN'),(t:'NaN'),(t:'1')} q)";

    [TestMethod]
    public void OrderedAll_WithNaN_DoesNotSelectRow()
    {
        const string sql = """
            with v as (select T::double as K from values { (T: 'NaN') } s)
            select 1 from v a where a.K <= all (select b.K from v b)
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ProjectedEqualityAny_WithNaN_UsesMaterializedMembershipEquality()
    {
        const string sql = """
            with v as (select T::double as K from values { (T: 'NaN') } s)
            select a.K = any (select b.K from v b) as V from v a
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { true });
    }

    [TestMethod]
    [DataRow("select 1 from v a inner join v b on a.K = b.K")]
    [DataRow("select 1 from v a semi join v b on a.K = b.K")]
    [DataRow("select 1 from v a where exists (select b.K from v b where a.K = b.K)")]
    [DataRow("select 1 from v a asof join v b on a.K >= b.K")]
    public void NaNJoinKeys_DoNotSatisfyComparison(string select)
    {
        var sql = "with v as (select T::double as K from values { (T: 'NaN') } s) " + select;

        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void FullJoinWithCompositeNaNKey_PreservesBothUnmatchedRows()
    {
        const string sql = """
            with v as (select T::float as K, 1 as P from values { (T: 'NaN') } s)
            select 1 from v a full join v b on a.P = b.P and a.K = b.K
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [1], [1]);
    }

    [TestMethod]
    public void AsOfJoinWithNaNPartition_DoesNotMatch()
    {
        const string sql = """
            with v as (select T::float as K, 1 as P from values { (T: 'NaN') } s)
            select 1 from v a asof join v b on a.K = b.K and a.P >= b.P
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ComparisonAndMembership_ShouldExposeNaNEqualityForEachForm()
    {
        const string sql =
            ValuesCte
            + """
                 select a.x::string as Label, a.x = a.x as Eq, a.x <> a.x as Ne,
                     a.x in (a.x) as InList, a.x in (select b.x from v b) as InSubquery
                 from v a order by a.x
                """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["NaN", false, true, false, true],
            ["NaN", false, true, false, true],
            ["1", true, false, true, true]
        );
    }

    [TestMethod]
    public void GroupBy_ShouldCombineNaNsAndSortThemBeforeFiniteValues()
    {
        const string sql = ValuesCte + " select x::string as Label, Count(*) as N from v group by x order by x";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Label", typeof(string)), ("N", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["NaN", 2L], ["1", 1L]);
    }

    [TestMethod]
    [DataRow("select distinct x from v", 1, 1)]
    [DataRow("select x from v union select x from v", 4, 1)]
    [DataRow("select x from v union (x) select x from v", 4, 1)]
    [DataRow("select x from v union all select x from v", 4, 2)]
    [DataRow("select x from v except select x from v", 2, 0)]
    [DataRow("select x from v intersect select x from v", 0, 1)]
    public void SetOperations_ShouldPreserveOperatorSpecificNaNMultiplicity(string operation, int nans, int ones)
    {
        var sql = $"{ValuesCte}, u as ({operation}) select x as Value from u order by x";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        var expected = new List<object?[]>();
        for (var i = 0; i < nans; i++)
            expected.Add([double.NaN]);
        for (var i = 0; i < ones; i++)
            expected.Add([1d]);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(double?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, expected.ToArray());
    }
}
