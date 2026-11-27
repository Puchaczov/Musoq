using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinarySchemaIndexBoundaryTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("1", 53)]
    [DataRow("2", 0)]
    [DataRow("-3", 53)]
    public void ComputedPrimitiveIndex_ShouldApplyArrayBounds(string index, int expected)
    {
        var sql = $$"""
            binary Rec { A: byte[2], V: = A[{{index}}] };
            let b: byte[] = array { 49ub, 53ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)expected]);
    }

    [TestMethod]
    [DataRow("0ub", (byte)0, null)]
    [DataRow("1ub, 53ub, 50ub", (byte)53, (byte)50)]
    public void ConditionalCollections_ShouldSupplyElementDefaultsWhenAbsent(string bytes, byte primitive, byte? record)
    {
        var sql = $$"""
            binary Item { N: byte };
            binary Rec { Flag: byte, A: byte[1] when Flag = 1, Rows: Item[1] when Flag = 1, X: = A[0], Y: = Rows[0].N };
            let b: byte[] = array { {{bytes}} };
            select r.X, r.Y from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [primitive, record]);
    }
}
