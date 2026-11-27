using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    [DataRow("a.K in", "take 1", true)]
    [DataRow("exists", "take 1", true)]
    [DataRow("exists", "skip 1", false)]
    public void CorrelatedPredicatePaging_AppliesToTheMatchingOuterBinding(string predicate, string page, bool matches)
    {
        var sql = $$"""
            select a.K from values { (G: 2, K: 20) } a
            where {{predicate}} (
                select b.K from values { (G: 1, K: 10), (G: 2, K: 20) } b
                where b.G = a.G order by b.K {{page}}
            )
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            matches
                ?
                [
                    [20],
                ]
                : []
        );
    }

    [TestMethod]
    public void ProjectedIn_WithNoMatchAndNullMember_ReturnsUnknown()
    {
        const string sql = """
            select a.K in (select b.K from values { (K: 2), (K: null) } b) as V
            from values { (K: 1) } a
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("V", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }

    [TestMethod]
    public void ProjectedNotIn_WithUncorrelatedMatch_ReturnsFalse()
    {
        const string sql = """
            select a.K not in (select b.K from values { (K: 1) } b) as V
            from values { (K: 1) } a
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { false });
    }

    [TestMethod]
    public void NotInEmptySubquery_PreservesNullOuterKey()
    {
        const string sql = """
            select a.K from values { (K: 1), (K: null) } a
            where a.K not in (select b.K from values { (K: 1) } b where b.K = 0)
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { 1 }, new object?[] { null });
    }

    [TestMethod]
    public void NotInPagedSubquery_OnlyExaminesTheCompletedInnerRows()
    {
        const string sql = """
            select a.K from values { (K: 2), (K: null) } a
            where a.K not in (
                select b.K from values { (K: 1, N: 1), (K: null, N: 2) } b order by b.N take 1
            )
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { 2 });
    }
}
