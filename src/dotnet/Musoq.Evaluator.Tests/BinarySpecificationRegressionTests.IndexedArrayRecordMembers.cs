using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void IndexedRecordArrayMember_ShouldUseSafeOutOfBoundsDefaults()
    {
        var table = RunBinary(
            "binary Cell { Value: byte }; binary Rec { Count: byte, Cells: Cell[Count], Trailer: byte at 2 check Trailer = 0xEE, After: byte };",
            "r.Cells[0].Value as FirstValue, r.Cells[1].Value as MissingValue, r.Cells[-1].Value as LastValue, r.Trailer as Trailer, r.After as After",
            [0x01, 0x2A, 0xEE, 0x5A],
            [0x00, 0x2A, 0xEE, 0x5A]);

        TableMaterializationTestHelper.AssertColumns(table,
            ("FirstValue", typeof(byte?)),
            ("MissingValue", typeof(byte?)),
            ("LastValue", typeof(byte?)),
            ("Trailer", typeof(byte)),
            ("After", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            [(byte)0x2A, null, (byte)0x2A, (byte)0xEE, (byte)0x5A],
            [null, null, null, (byte)0xEE, (byte)0x5A]);
    }
}
