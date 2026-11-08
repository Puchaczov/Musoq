using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void StringOrdering_ShouldUseOrdinalComparisonAndPreserveUnknown()
    {
        var table = RunBinary("binary Rec { Flag: byte, Text: string[1] ascii when Flag = 1, Before: = Text < 'z', Equal: = Text = 'A' };",
            "r.Before, r.Equal", [0], [1, 65], [1, 122]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, null], [true, true], [false, false]);
    }

    [TestMethod]
    public void StringLength_ShouldKeepIntTypeDespiteAnEnclosingLengthField()
    {
        var table = RunBinary("binary Rec { Length: byte, Text: string[Length] ascii, N: = Text.Length };",
            "r.N", [2, 65, 66]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2]);
    }

    [TestMethod]
    public void IntrinsicResults_ShouldExposeTheirDeclaredLogicalTypes()
    {
        var table = RunBinary("binary Rec { Data: byte[3], Text: = ToText(Data, 'ascii'), Hex: = ToHex(Data), N: = Length(Data), Prefix: = Substring(Text, 0, 1), Position: = IndexOf(Data, FromHex('4243')) };",
            "r.Text, r.Hex, r.N, r.Prefix, r.Position", [65, 66, 67]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["ABC", "414243", 3, "A", 1]);
    }

    [TestMethod]
    public void FloatingDecimalComparison_ShouldPromoteOperandsAndPreserveUnknown()
    {
        var table = RunBinary("binary Rec { Flag: byte, F: float le when Flag = 1, Same: = F = 0.5, Before: = F < 0.75 };",
            "r.Same, r.Before", [0], [1, 0, 0, 0, 0x3F]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, null], [true, true]);
    }

    [TestMethod]
    public void ChecksumAndDigestIntrinsics_ShouldMatchIndependentCheckVectors()
    {
        var table = RunBinary("binary Rec { Data: byte[9], C32: = Crc32(Data), C16: = Crc16(Data), Adler: = Adler32(Data), M: = ToHex(Md5(Data)), S: = ToHex(Sha256(Data)) };",
            "r.C32, r.C16, r.Adler, r.M, r.S", System.Text.Encoding.ASCII.GetBytes("123456789"));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [-873187034, (short)-17603, 152961502,
            "25F9E794323B453885F5181F1B624D0B", "15E2B0D3C33891EBB0F1EF609EC419420C20E320CE94C65FBC8C3312448EB225"]);
    }

    [TestMethod]
    public void IntrinsicFailures_ShouldReportTypedExpressionAndEncodingDiagnostics()
    {
        var hex = Assert.Throws<Musoq.Schema.Interpreters.ParseException>(() =>
            RunBinaryCollections("binary Rec { Bad: = FromHex('0G') };", "r.Bad", System.Array.Empty<byte>()));
        Assert.AreEqual(Musoq.Schema.Interpreters.ParseErrorCode.FieldReferenceError, hex.ErrorCode);
        var encoding = Assert.Throws<Musoq.Schema.Interpreters.ParseException>(() =>
            RunBinary("binary Rec { Data: byte[1], Bad: = ToText(Data, 'ascii') };", "r.Bad", [255]));
        Assert.AreEqual(Musoq.Schema.Interpreters.ParseErrorCode.EncodingError, encoding.ErrorCode);
    }

    [TestMethod]
    public void IntrinsicInCheck_ShouldReportItsFieldAndTypedFailure()
    {
        var exception = Assert.Throws<Musoq.Schema.Interpreters.ParseException>(() =>
            RunBinary("binary Rec { Data: byte[1] check Length(FromHex('0G')) = 0 };", "r.Data.Length", [7]));
        Assert.AreEqual(Musoq.Schema.Interpreters.ParseErrorCode.FieldReferenceError, exception.ErrorCode);
        Assert.AreEqual("Data", exception.FieldName);
    }
}
