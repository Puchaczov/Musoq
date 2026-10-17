using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void IndexedPrimitiveArrayLength_ShouldPreserveMissingAndNegativeIndexSemantics()
    {
        var table = RunBinary("binary Rec { Rows: byte[2][2], Trailer: byte };",
            "r.Rows[0].Length as FirstWidth, r.Rows[2].Length as MissingWidth, r.Rows[-1].Length as LastWidth, r.Trailer",
            [0x11, 0x22, 0x33, 0x44, 0xEE]);

        TableMaterializationTestHelper.AssertColumns(table,
            ("FirstWidth", typeof(int)), ("MissingWidth", typeof(int?)),
            ("LastWidth", typeof(int)), ("Trailer", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, null, 2, (byte)0xEE]);
    }

    [TestMethod]
    public void IndexedPrimitiveArrayLength_ShouldReturnNullForEmptyOuterArray()
    {
        var table = RunBinary("binary Rec { Rows: byte[2][0], Trailer: byte };",
            "r.Rows[0].Length as FirstWidth, r.Rows[-1].Length as LastWidth, r.Trailer", [0xEE]);

        TableMaterializationTestHelper.AssertColumns(table,
            ("FirstWidth", typeof(int?)), ("LastWidth", typeof(int?)), ("Trailer", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, null, (byte)0xEE]);
    }

    [TestMethod]
    public void IndexedPrimitiveArrayLength_ShouldRemainNullableForDynamicBounds()
    {
        var table = RunBinary("binary Rec { Count: byte, Rows: byte[2][Count], Trailer: byte };",
            "r.Rows[0].Length as FirstWidth, r.Rows[-1].Length as LastWidth, r.Trailer",
            [2, 0x11, 0x22, 0x33, 0x44, 0xEE], [0, 0xEE]);

        TableMaterializationTestHelper.AssertColumns(table,
            ("FirstWidth", typeof(int?)), ("LastWidth", typeof(int?)), ("Trailer", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            [2, 2, (byte)0xEE], [null, null, (byte)0xEE]);
    }
}
