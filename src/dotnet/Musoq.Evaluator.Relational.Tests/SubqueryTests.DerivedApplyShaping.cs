using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void CorrelatedCrossApply_TakeSelectsTheFirstMatchingRightRow()
    {
        const string sql = """
            select d.V from values { (G: 2) } a cross apply (
                select b.G, b.V from values { (G: 1, V: 10), (G: 2, V: 20) } b
                where b.G=a.G order by b.V take 1
            ) d
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [20]);
    }

    [TestMethod]
    public void CorrelatedOuterApply_RangePagePreservesUnmatchedRowsAndRestartsOrdinality()
    {
        const string sql = """
            select a.G, d.V, d.Ordinal from values { (G: 1), (G: 2), (G: 4) } a outer apply (
                select b.G, b.V from values { (G: 1, V: 10), (G: 2, V: 20), (G: 3, V: 30) } b
                where b.G >= a.G order by b.V skip 1 take 1
            ) d with ordinality
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, 20, 0], [2, 30, 0], [4, null, null]);
    }

    [TestMethod]
    public void CorrelatedApply_UnionKeyDeduplicatesWithinEachBinding()
    {
        const string sql = """
            select a.G,d.V from values { (G: 1), (G: 2) } a cross apply (
                select b.G, b.V from values { (G: 1,V: 9), (G: 2,V: 9) } b where b.G=a.G
                union (V)
                select c.G, c.V from values { (G: 2,V: 10) } c where c.G=a.G
            ) d
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, 9], [2, 9], [2, 10]);
    }

    [TestMethod]
    [DataRow("union", true)]
    [DataRow("except", true)]
    [DataRow("intersect", false)]
    [DataRow("union all", true)]
    public void CorrelatedApply_RangeFiltersBothArmsBeforeSetMembership(string operation, bool containsNine)
    {
        var sql = $$"""
            select d.V from values { (G: 2) } a cross apply (
                select b.G,b.V from values { (G: 1,V: 9), (G: 2,V: 9) } b where b.G>=a.G
                {{operation}} (V)
                select c.G,c.V from values { (G: 1,V: 9) } c where c.G>=a.G
            ) d
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            containsNine
                ?
                [
                    [9],
                ]
                : []
        );
    }

    [TestMethod]
    [DataRow("=", "")]
    [DataRow(">=", "")]
    [DataRow("=", "partition by b.G ")]
    public void CorrelatedApply_WindowRanksOnlyMatchingRows(string comparison, string partition)
    {
        var sql = $$"""
            select d.N from values { (G: 2) } a cross apply (
                select b.G,RowNumber() over ({{partition}}order by b.G) as N
                from values { (G: 1), (G: 2) } b where b.G{{comparison}}a.G
            ) d
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1L]);
    }

    [TestMethod]
    [DataRow("bad")]
    [DataRow("1")]
    public void CorrelatedApply_ConvertsOnlyTheMatchingRow(string discardedValue)
    {
        var sql = $$"""
            select d.V from values { (G:2) } a outer apply (
                select b.G,b.T::int as V from values { (G:1,T:'{{discardedValue}}'),(G:2,T:'42') } b
                where b.G=a.G
            ) d
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [42]);
    }

    [TestMethod]
    [DataRow("order by b.T::int", "bad")]
    [DataRow("and b.T::int>0", "bad")]
    [DataRow("order by b.T::int", "1")]
    public void CorrelatedApply_EvaluatesLocalFilterAndSortKeysWithinTheBinding(string clause, string discarded)
    {
        var sql = $$"""
            select a.G,d.T from values { (G:2) } a cross apply (
                select b.G,b.T from values { (G:1,T:'{{discarded}}'),(G:2,T:'42') } b
                where b.G=a.G {{clause}}
            ) d
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2, "42"]);
    }

    [TestMethod]
    [DataRow("union", 1)]
    [DataRow("except", 0)]
    [DataRow("intersect", 2)]
    [DataRow("union all", 3)]
    public void CorrelatedApply_ComputedEqualityDoesNotChangeTheDeclaredSetKey(string operation, int copies)
    {
        var sql = $$"""
            select d.V from values { (G:1) } a cross apply (
                select b.G,b.V from values { (G:2,V:9),(G:1,V:9),(G:3,V:9) } b where b.G % 2=a.G
                {{operation}} (V)
                select c.G,c.V from values { (G:5,V:9) } c where c.G % 2=a.G
            ) d
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, Enumerable.Repeat<object?[]>([9], copies).ToArray());
    }
}
