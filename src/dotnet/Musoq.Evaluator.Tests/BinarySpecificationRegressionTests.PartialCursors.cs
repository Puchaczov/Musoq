using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("binary Rec { Value: bits[3] check Value = 0 }", 0, 3, false, "{}")]
    [DataRow("binary Rec { Value: bits[3] }", 0, 3, true, "{\"Value\":7}")]
    [DataRow("binary Rec { Value: string[2] utf8 }", 0, 0, false, "{}")]
    [DataRow("binary Rec { Prefix: byte[2], Value: byte at 0 check Value = 0 }", 1, 0, false, "{\"Prefix\":\"/wA=\"}")]
    [DataRow("binary Child { Value: byte at 0 check Value = 0 }; binary Rec { Prefix: byte[2], Child: Child }", 1, 0, false, "{\"Prefix\":\"/wA=\"}")]
    [DataRow("binary Child { Value: bits[3] }; binary Rec { Prefix: byte, Child: substream[1] as Child exact }", 1, 3, false, "{\"Prefix\":255}")]
    [DataRow("binary Child { Value: byte }; binary Rec { Prefix: byte, Child: substream[2] as Child exact }", 2, 0, false, "{\"Prefix\":255}")]
    [DataRow("binary Child { Value: bits[3] check Value = 1 }; binary Rec { Prefix: byte, Child: substream[1] as Child lax }", 1, 3, false, "{\"Prefix\":255}")]
    public void PartialCursor_ShouldRetainTerminalPositionAndOnlyCompletedFields(
        string schema, int bytes, int bits, bool success, string json)
    {
        using var table = RunBinaryQuery($"{schema}; select p.BytesConsumed, p.BitOffset, p.IsSuccess, ToJson(p.ParsedFields) as Json " +
            "from #test.files() f cross apply PartialInterpret<Rec>(f.Content) p", [255, 0, 1]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [bytes, bits, success, json]);
    }

    [TestMethod]
    public void LaxSubstream_ShouldDiscardPartialByteAndContinueAfterFullSlice()
    {
        using var table = RunBinary("binary Child { Value: bits[3] }; binary Rec { Prefix: byte, Child: substream[1] as Child lax, Tail: byte };",
            "r.Child.Value, r.Tail", [255, 7, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)99]);
    }

    [TestMethod]
    public void FailedArray_ShouldNotPublishSuccessfulPrefix()
    {
        using var table = RunBinaryQuery("binary Rec { Prefix: byte, Values: int le[2] }; " +
            "select p.BytesConsumed, p.BitOffset, ToJson(p.ParsedFields) as Json " +
            "from #test.files() f cross apply PartialInterpret<Rec>(f.Content) p", [7, 1, 0, 0, 0]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [5, 0, "{\"Prefix\":7}"]);
    }

    [TestMethod]
    public void PartialMetadata_ShouldAppendCompletionColumnsAndCompleteAbsentFields()
    {
        using var table = RunBinaryQuery("binary Rec { Flag: byte, Absent: byte when Flag = 1 }; " +
            "select p.ParsedFields, p.ErrorField, p.ErrorMessage, p.BytesConsumed, p.CompletedFields, p.IsSuccess, p.BitOffset from #test.files() f cross apply PartialInterpret<Rec>(f.Content) p",
            new CompilationOptions(usePrimitiveTypeValidation: false), [0]);
        CollectionAssert.AreEqual(new[] { "ParsedFields", "ErrorField", "ErrorMessage", "BytesConsumed", "CompletedFields", "IsSuccess", "BitOffset" },
            table.Columns.Select(column => column.ColumnName).ToArray(), string.Join(",", table.Columns.Select(column => column.ColumnName)));
        var completed = (IReadOnlySet<string>)table[0].ReadValue(4)!;
        Assert.IsTrue(completed.SetEquals(["Flag", "Absent"]));
        Assert.AreEqual(true, table[0].ReadValue(5));
        Assert.AreEqual(0, table[0].ReadValue(6));
    }
}
