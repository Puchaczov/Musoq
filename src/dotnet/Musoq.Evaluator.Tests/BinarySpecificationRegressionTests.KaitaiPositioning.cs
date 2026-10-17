using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void RepeatedRecordAndFinalArrayIndices_ShouldBothBeRetained()
    {
        var table = RunBinary("binary Body { Values: byte[2] }; binary Item { Body: Body }; binary Rec { Items: Item[2] };",
            "r.Items[0].Body.Values[1], r.Items[1].Body.Values[0]", [10, 20, 30, 40]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)20, (byte)30]);
    }

    [TestMethod]
    public void RepeatedRecordAndFinalArrayIndices_ShouldPreserveAbsentParentNull()
    {
        var table = RunBinary("binary Body { Values: byte[2] }; binary Item { Flag: byte, Body: Body when Flag = 1 }; binary Rec { Items: Item[2] };",
            "r.Items[0].Body.Values[1], r.Items[1].Body.Values[0]", [0, 1, 30, 40]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, (byte)30]);
    }

    [TestMethod]
    [DataRow("byte", new byte[] { 1, 42 })]
    [DataRow("sbyte", new byte[] { 1, 42 })]
    [DataRow("short le", new byte[] { 2, 0, 42 })]
    [DataRow("ushort le", new byte[] { 2, 0, 42 })]
    public void InterpretAt_NarrowIntegerOffset_ShouldPreserveImplicitWidening(string annotation, byte[] bytes)
    {
        var table = RunBinaryQuery($"binary Header {{ Offset: {annotation} }}; binary Rec {{ Value: byte }}; select r.Value from #test.files() f cross apply Interpret<Header>(f.Content) h cross apply InterpretAt<Rec>(f.Content, h.Offset) r", bytes);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)42]);
    }

    [TestMethod]
    [DataRow("1ui")]
    [DataRow("1::int")]
    [DataRow("'wrong'")]
    public void InterpretAt_NonIntOffset_ShouldReportBindingTypeMismatch(string offset)
    {
        var exception = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            RunBinaryQuery($"binary Rec {{ Value: byte }}; select r.Value from #test.files() f cross apply InterpretAt<Rec>(f.Content, {offset}) r", [10, 20]));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3005_TypeMismatch, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void ConditionalNestedArrayIndex_ShouldReturnNullThroughAnAbsentRecord()
    {
        var table = RunBinary("binary Item { Value: byte }; binary Body { Items: Item[2], Values: byte[2] }; binary Rec { Flag: byte, Body: Body when Flag = 1 };",
            "r.Body.Items[1].Value, r.Body.Values[0]", [0], [1, 10, 20, 30, 40]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, null], [(byte)20, (byte)30]);
        CollectionAssert.AreEqual(new[] { typeof(byte?), typeof(byte?) }, table.Columns.Select(column => column.ColumnType).ToArray());
    }

    [TestMethod]
    public void NestedArrayIndex_ShouldBindElementMembersAndPrimitiveValues()
    {
        var table = RunBinary("binary Item { Value: byte }; binary Body { Items: Item[2], Values: byte[2] }; binary Rec { Body: Body };",
            "r.Body.Items[1].Value, r.Body.Values[0]", [10, 20, 30, 40]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)20, (byte)30]);
    }
}
