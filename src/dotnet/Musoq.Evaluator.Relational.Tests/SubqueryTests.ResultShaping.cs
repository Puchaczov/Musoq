using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    [DataRow(" take 1", 0L)]
    [DataRow(" skip 1", null)]
    [DataRow("", 0L)]
    public void CorrelatedScalarCount_PagesTheAggregateResult(string page, long? expected)
    {
        var sql = $$"""
            select (select Count(*) from values { (G:2) } b where b.G=a.G{{page}}) as N
            from values { (G:1) } a
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected]);
    }

    [TestMethod]
    [DataRow("b.V")]
    [DataRow("b.G")]
    public void CorrelatedScalarWindow_BindsOrderingColumnsInsideTheSubquery(string order)
    {
        var sql = $$"""
            select (select RowNumber() over (order by {{order}}) as N
                    from values { (G:2,V:20) } b where b.G=a.G) as N
            from values { (G:2) } a
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1L]);
    }

    [TestMethod]
    [DataRow("with r as (select x.G from values { (G:2) } x) ", "r b")]
    [DataRow("", "values { (G:2) } b")]
    public void GuardedScalarWindow_ReadsTheCapturedRelation(string definitions, string source)
    {
        var sql = $$"""
            {{definitions}}select case when a.G=2 then (
                select RowNumber() over (order by b.G) as N from {{source}} where b.G=a.G
            ) else null end as N from values { (G:2) } a
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1L]);
    }

    [TestMethod]
    public void ProjectedIn_MixedIntegerWidths_MarksEachLeftRowOnce()
    {
        const string sql = """
            select a.K in (select b.K from values { (K: 1L), (K: 1L) } b) as V
            from values { (K: 1), (K: 2) } a
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [true], [false]);
    }

    [TestMethod]
    [DataRow("in")]
    [DataRow("= any")]
    public void MaterializedFloatingMembership_MixedWidths_MatchesNaN(string comparison)
    {
        var sql = $$"""
            with v as (select s.T::float as X, s.T::double as Y from values { (T: 'NaN') } s)
            select 1 from v a where a.X {{comparison}} (select b.Y from v b)
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1]);
    }

    [TestMethod]
    public void ExistsOverUnionAll_AfterDistinctAndSkip_PreservesTheCombinedRowShape()
    {
        const string sql = """
            select case when exists (
                select distinct y.K as W from values { (K: 2), (K: 2) } y
                union all select x.K as V from values { (K: 1) } x skip 1
            ) then 1 else 0 end as V from values { (N: 3) } a
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1]);
    }

    [TestMethod]
    [DataRow(
        "select x.K as V from values { (K: 1), (K: null) } x",
        "select distinct y.K as W from values { (K: 2), (K: 2) } y"
    )]
    [DataRow("select x.K as V from values { (K: 1) } x", "select Count(*)::int as W from values { (K: 1), (K: 2) } y")]
    public void QuantifiedUnionAll_AfterSkip_ReadsBothBranchBuffers(string first, string second)
    {
        var sql = $"select a.N > any ({first} union all {second} skip 1) as V from values {{ (N: 3) }} a";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [true]);
    }

    [TestMethod]
    public void ExistsOverUnionAll_SkipsUnusedCastsInBothBranches()
    {
        const string sql = """
            select case when exists (
                select b.T::int as V from values { (T: 'bad') } b
                union all select c.T::int as V from values { (T: 'worse') } c
            ) then 1 else 0 end as V from values { (K: 1) } a
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [1]);
    }

    [TestMethod]
    [DataRow("1")]
    [DataRow("b.G+1")]
    [DataRow("b.T::int")]
    public void ExistsOverUnionAll_WithUnmatchedJoin_KeepsRequiredValueTypes(string projection)
    {
        var sql = $$"""
            select case when exists (
                select {{projection}} as V from values { (G:1,T:'bad') } b
                left join values { (G:2) } c on b.G=c.G
                union all select 2 from values { (N:0) } x where x.N=1
            ) then 1 else 0 end as V from values { (N:1) } a
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1]);
    }

    [TestMethod]
    [DataRow("select Count(*) from values { (K: 1) } b where b.K = 0", true)]
    [DataRow("select x.K from values { (K: 1) } x except select y.K from values { (K: 2) } y", true)]
    [DataRow("select x.K from values { (K: 1) } x intersect select y.K from values { (K: 2) } y", false)]
    public void Exists_ReducesTheCompletedAggregateOrSetOperation(string body, bool expected)
    {
        var sql = $"select exists ({body}) as V from values {{ (N: 1) }} a";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected]);
    }

    [TestMethod]
    public void CorrelatedIn_WithGlobalCountOnEmptyBinding_MatchesZero()
    {
        const string sql = """
            select a.K from values { (G: 2, K: 0) } a
            where a.K in (select Count(*) from values { (G: 1) } b where b.G = a.G)
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [0]);
    }
}
