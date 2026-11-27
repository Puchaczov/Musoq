using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretedCollectionCteTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("r.Items as Val, r.Tail as T", "q.Val[1].X, q.T")]
    [DataRow("r.Tail as T, r.Items as Val", "q.Val[1].X, q.T")]
    [DataRow("r.Items[1].X as V, r.Tail as T", "q.V, q.T")]
    public void StoredCollectionMember_ShouldRemainDistinctFromItsSibling(string fields, string projection)
    {
        var sql = $$"""
            text Cell { X: chars[1] };
            text Rec { Items: repeat Cell until '#', Tail: rest };
            with Q as (select {{fields}} from values { (S: 'ab#T') } s cross apply Parse<Rec>(s.S) r)
            select {{projection}} from Q q
            """;

        AssertQueryRows(sql, ["b", "T"]);
    }

    [TestMethod]
    public void FilterOnAnIndexedCteMember_ShouldRetainTheCollection()
    {
        const string sql = """
            text Cell { X: chars[1] };
            text Rec { Items: repeat Cell until '#', Tail: rest };
            with Q as (select r.Items as Val, r.Tail as T from values { (S: 'ab#T'), (S: 'cd#F') } s cross apply Parse<Rec>(s.S) r)
            select q.T from Q q where q.Val[1].X = 'b'
            """;

        AssertQueryRows(sql, ["T"]);
    }

    [TestMethod]
    [DataRow("0ub, 126ub", "0", null)]
    [DataRow("0ub, 126ub", "-5", null)]
    [DataRow("1ub, 65ub, 126ub", "1", null)]
    [DataRow("1ub, 0ub, 126ub", "0", (byte)0)]
    public void MissingCteRecordElement_ShouldPreserveNullThroughItsPrimitiveLeaf(
        string bytes,
        string index,
        byte? expected
    )
    {
        var sql = $$"""
            binary Cell { X: byte };
            binary Rec { N: byte, Items: Cell[N], Tail: byte };
            let b: byte[] = array { {{bytes}} };
            with Q as (select r.Items as Val, r.Tail as T from values { (N: 0) } s cross apply Interpret<Rec>($b) r)
            select q.Val[{{index}}].X, q.T from Q q
            """;

        AssertQueryRows(sql, [expected, (byte)126]);
    }

    [TestMethod]
    public void BinaryIndexedCteMember_ShouldReadTheStoredArray()
    {
        const string sql = """
            binary Cell { X: byte };
            binary Rec { Items: Cell[2], Tail: byte };
            let b: byte[] = array { 65ub, 66ub, 126ub };
            with Q as (select r.Items as Val, r.Tail as T from values { (N: 0) } s cross apply Interpret<Rec>($b) r)
            select q.Val[1].X, q.T from Q q
            """;

        AssertQueryRows(sql, [(byte)66, (byte)126]);
    }
}
