using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryNumericValueComparisonTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("byte const 256", "00", false)]
    [DataRow("byte magic -1", "FF", false)]
    [DataRow("byte oneOf [1.5, 7]", "01", false)]
    [DataRow("byte oneOf [1.5, 7]", "07", true)]
    [DataRow("sbyte oneOf [128, 0]", "80", false)]
    [DataRow("sbyte magic -1", "FF", true)]
    [DataRow("uint le const 4294967296l", "00000000", false)]
    [DataRow("uint le const 0xFFFFFFFF", "FFFFFFFF", true)]
    [DataRow("ulong be magic 0xFFFFFFFFFFFFFFFF", "FFFFFFFFFFFFFFFF", true)]
    [DataRow("float le const 1.5", "0000C03F", true)]
    [DataRow("double le oneOf [1.5, 7]", "000000000000F83F", true)]
    [DataRow("float le const 1.00000001", "0000803F", false)]
    [DataRow("float le const 0.1", "CDCCCC3D", true)]
    [DataRow("float le const 1.5", "0000C07F", false)]
    [DataRow("float le const 1.5", "FFFF7F7F", false)]
    [DataRow("double le const 1.5", "000000000000F07F", false)]
    [DataRow("double le const 0.0", "0000000000000080", true)]
    public void NumericValueValidation_ShouldCompareTheAuthoredNumber(string field, string hex, bool passes)
    {
        var sql = $$"""
            binary Rec { K: {{field}} };
            select r.K is not null from values { (H: '{{hex}}') } s
            outer apply TryInterpret<Rec>(FromHexToBytes(s.H)) r
            """;

        AssertQueryRows(sql, [passes]);
    }

    [TestMethod]
    public void UnsatisfiedNumericConstraint_ShouldRetainTheCursorAndEarlierCompletedField()
    {
        const string sql = """
            binary Rec { Tag: byte, K: byte const 256 };
            let b: byte[] = array { 42ub, 0ub };
            select r.IsSuccess, r.ParsedFields.Tag, r.ErrorField, r.BytesConsumed, r.BitOffset
            from values { (N: 0) } s cross apply PartialInterpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [false, (byte)42, "K", 2, 0]);
    }
}
