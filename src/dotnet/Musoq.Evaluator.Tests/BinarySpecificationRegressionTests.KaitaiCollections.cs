using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    private Table RunBinaryCollections(string schema, string projection, params byte[][] records) =>
        RunBinaryQuery($"{schema} select {projection} from #test.files() f cross apply Interpret<Rec>(f.Content) r",
            new CompilationOptions(usePrimitiveTypeValidation: false), records);

    [TestMethod]
    public void FixedByteArrayElements_ShouldPreserveBytesAndAdvanceToTrailer()
    {
        var table = RunBinaryCollections("binary Rec { Blocks: byte[2][2], Tail: byte };",
            "r.Blocks[0], r.Blocks[1], r.Tail", [1, 2, 3, 4, 99]);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, (byte[])table[0].ReadValue(0));
        CollectionAssert.AreEqual(new byte[] { 3, 4 }, (byte[])table[0].ReadValue(1));
        Assert.AreEqual((byte)99, table[0].ReadValue(2));
    }

    [TestMethod]
    public void NestedPrimitiveArrayProjection_ShouldKeepLeafArrayType()
    {
        var table = RunBinaryCollections("binary Item { Count: byte, Values: byte[Count] }; binary Rec { Items: Item[2] };",
            "r.Items[0].Values, r.Items[1].Values", [1, 7, 2, 8, 9]);
        CollectionAssert.AreEqual(new byte[] { 7 }, (byte[])table[0].ReadValue(0));
        CollectionAssert.AreEqual(new byte[] { 8, 9 }, (byte[])table[0].ReadValue(1));
        Assert.AreEqual(typeof(byte[]), table.Columns.First().ColumnType);
    }

    [TestMethod]
    public void RepeatedBoundedRecords_ShouldUseEachSliceAndResumeParent()
    {
        var table = RunBinary("binary Item { Value: byte }; binary Rec { Items: substream[2] as Item lax repeat until Items[-1].Value = 0, Tail: byte };",
            "r.Items[0].Value, r.Items[1].Value, r.Tail", [7, 55, 0, 66, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)0, (byte)99]);
    }

    [TestMethod]
    public void ConditionalByteArrayElements_ShouldHonorGuardAndZeroCount()
    {
        var table = RunBinaryCollections("binary Rec { Flag: byte, Count: byte, Blocks: byte[2][Count] when Flag = 1, Tail: byte };",
            "r.Blocks, r.Tail", [0, 2, 99], [1, 0, 99], [1, 2, 1, 2, 3, 4, 99]);
        Assert.IsNull(table[0].ReadValue(0));
        Assert.AreEqual(0, ((byte[][])table[1].ReadValue(0)).Length);
        var blocks = (byte[][])table[2].ReadValue(0);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, blocks[0]);
        CollectionAssert.AreEqual(new byte[] { 3, 4 }, blocks[1]);
        foreach (var row in table.Rows)
            Assert.AreEqual((byte)99, row.ReadValue(1));
    }
}
