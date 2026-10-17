using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void RepeatedRecordComputedBoolean_ShouldBindAsAStopCondition()
    {
        var table = RunBinary("binary Item { Value: byte, Stop: = Value = 9 }; binary Rec { Items: Item repeat until Items[-1].Stop };",
            "r.Items.Length, r.Items[1].Value", [1, 9, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, (byte)9]);
    }

    [TestMethod]
    public void RepeatedRecordNumericMember_ShouldNotBindAsAStopCondition()
    {
        var exception = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            RunBinary("binary Item { Value: byte }; binary Rec { Items: Item repeat until Items[-1].Value };",
                "r.Items.Length", [1, 9]));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void RepeatedPrimitiveElement_ShouldAllowBitwiseSchemaExpressions()
    {
        var table = RunBinary("binary Rec { Raw: byte repeat until eof, High: = (Raw[0] >> 4) & 15 };",
            "r.High", [0xAB]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [10]);
    }

    [TestMethod]
    public void ArrayLengthMember_ShouldNotReenterASiblingLengthComputation()
    {
        var table = RunBinary("binary Rec { Raw: byte[2], Marker: = 1, Length: = CASE WHEN Marker < 0 THEN Raw.Length ELSE Marker END };",
            "r.Length", [10, 20]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1]);
    }

    [TestMethod]
    public void ConditionalComputedString_ShouldLiftLengthAndGuardSubstring()
    {
        var table = RunBinary("binary Rec { Flag: byte, Raw: byte[2] when Flag = 1, Text: = CASE WHEN Flag = 1 THEN ToText(Raw, 'ascii') ELSE NULL END, Length: = Text.Length, Chopped: = CASE WHEN Flag = 1 THEN Substring(Text, 0, Text.Length - 1) ELSE NULL END };",
            "r.Length, r.Chopped", [0], [1, 65, 0]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, null], [2, "A"]);
    }

    [TestMethod]
    public void NullSubstringLength_ShouldReportExpressionDiagnostic()
    {
        var exception = Assert.Throws<Musoq.Schema.Interpreters.ParseException>(() =>
            RunBinary("binary Rec { Flag: byte, Length: byte when Flag = 1, Invalid: = Substring('A', 0, Length) };", "r.Invalid", [0]));
        Assert.AreEqual(Musoq.Schema.Interpreters.ParseErrorCode.FieldReferenceError, exception.ErrorCode);
        Assert.AreEqual("Invalid", exception.FieldName);
    }

    [TestMethod]
    public void ComputedBoundedMember_ShouldRetainItsPrimitiveType()
    {
        var table = RunBinary("binary Item { Value: byte }; binary Rec { Item: substream[1] as Item, Copy: = Item.Value, Bytes: byte[Copy] };",
            "r.Copy, r.Bytes.Length", [2, 10, 20]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)2, 2]);
    }
}
