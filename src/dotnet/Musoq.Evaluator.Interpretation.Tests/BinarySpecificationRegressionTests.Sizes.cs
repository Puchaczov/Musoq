using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("F.P")]
    [DataRow("(F.P)")]
    [DataRow("F.P + 0")]
    [DataRow("Size")]
    public void DottedAndComputedArraySize_ShouldReadExactBytes(string size)
    {
        var table = RunBinary($"binary Header {{ P: byte }}; binary Rec {{ F: Header, Size: F.P, Values: byte[{size}], Tail: byte }};",
            "r.Values[0], r.Tail", [2, 3, 4, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)3, (byte)99]);
    }

    [TestMethod]
    [DataRow("byte", "[0]")]
    [DataRow("Item", "[0].V")]
    [DataRow("short", "[0]")]
    public void DottedTypedArraySize_ShouldResolveElementType(string element, string projection)
    {
        var suffix = element == "short" ? " le" : string.Empty;
        var table = RunBinary($"binary Item {{ V: byte }}; binary Header {{ P: byte }}; binary Rec {{ F: Header, Values: {element}[F.P]{suffix} }};",
            $"r.Values{projection}", [1, 3, 0]);

        Assert.AreEqual(3, System.Convert.ToInt32(table[0].ReadValues()[0]));
    }

    [TestMethod]
    [DataRow("N: int le when Flag = 1", new byte[] { 0 })]
    [DataRow("N: long le", new byte[] { 0, 0, 0, 0, 128, 0, 0, 0, 0 })]
    [DataRow("N: 1.5", new byte[] { 0 })]
    public void InvalidDynamicSize_ShouldReportInvalidSize(string field, byte[] input)
    {
        var exception = Assert.Throws<ParseException>(() =>
            RunBinary($"binary Rec {{ Flag: byte, {field}, Values: byte[N] }};", "r.Flag", input));

        Assert.AreEqual(ParseErrorCode.InvalidSize, exception.ErrorCode);
    }

    [TestMethod]
    public void FalseGuard_ShouldNotEvaluateInvalidSize()
    {
        var table = RunBinary("binary Rec { Flag: byte, N: int le when Flag = 1, Values: byte[N] when Flag = 1, Tail: byte };",
            "r.Tail", [0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)99]);
    }
}
