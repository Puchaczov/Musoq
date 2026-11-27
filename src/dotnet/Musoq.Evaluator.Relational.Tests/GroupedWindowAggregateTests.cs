using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class GroupedWindowAggregateTests : BasicEntityTestBase
{
    [TestMethod]
    public void RankingDistinguishesIntegralAndDecimalDivision()
    {
        const string sql =
            "select s.K,Rank() over (order by s.K/2i) as L,Rank() over (order by s.K/2d) as R"
            + " from values { (K:2),(K:3) } s";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [2, 1L, 1L], [3, 1L, 2L]);
    }

    private const string Groups = " from values { (G:1,C:'a'),(G:1,C:'a'),(G:2,C:null) } s group by s.G";

    [TestMethod]
    public void RankingDistinguishesAQuotedListSeparatorFromTwoMembers()
    {
        const string sql =
            "select s.S,Rank() over (order by s.S in (r'a'', ''b') desc) as A,"
            + "Rank() over (order by s.S in ('a','b') desc) as B from values { (S:'a'),(S:'z') } s";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, ["a", 1L, 1L], ["z", 1L, 2L]);
    }

    [TestMethod]
    public void RankingKeyUsesItsBoundMembershipSet()
    {
        const string sql =
            "select s.S,Rank() over (order by s.S in ('a','c') desc nulls last) as R"
            + " from values { (G:1,S:'a'),(G:1,S:'b'),(G:2,S:null) } s";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, ["a", 1L], ["b", 2L], [null, 3L]);
    }

    [TestMethod]
    [DataRow("select s.G,Rank() over (order by Count(*)) as R", "")]
    [DataRow("select s.G,Rank() over w as R", " window w as (order by Count(*))")]
    public void RankingReadsTheCountOfEachGroup(string select, string window)
    {
        using var query = CreateAndRunVirtualMachine(select + Groups + window, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, 2L], [2, 1L]);
    }

    [TestMethod]
    public void OffsetReadsThePreviousGroupsNonNullCount()
    {
        const string select = "select s.G,Lag(Count(s.C)) over (order by s.G) as Prev";
        using var query = CreateAndRunVirtualMachine(select + Groups, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, null], [2, 2L]);
    }

    [TestMethod]
    public void QualifyOnlyRankingReadsTheGroupedCount()
    {
        const string sql = "select s.G" + Groups + " qualify Rank() over (order by Count(*))=1";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [2]);
    }

    [TestMethod]
    public void OppositeCaseFiltersProduceIndependentWindowRanks()
    {
        const string sql =
            "select s.G,Rank() over (order by Count(*) filter (where s.C='a')) as L,"
            + "Rank() over (order by Count(*) filter (where s.C='A')) as U"
            + " from values { (G:1,C:'a'),(G:2,C:'A') } s group by s.G";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, 2L, 1L], [2, 1L, 2L]);
    }

    [TestMethod]
    [DataRow("Rank() over (order by 2-Count(*))", 1L, 2L)]
    [DataRow("Count(*) filter (where Count(*)>1) over ()", 1L, 1L)]
    public void WindowExpressionReadsItsNestedGroupAggregate(string expression, long first, long second)
    {
        var sql = $"select s.G,{expression} as R from values {{ (G:1),(G:1),(G:2) }} s group by s.G";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, first], [2, second]);
    }
}
