using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryRadixExpressionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("0x100000000", 4294967296L)]
    [DataRow("0b100000000000000000000000000000000", 4294967296L)]
    [DataRow("0o40000000000", 4294967296L)]
    [DataRow("0xFFFFFFFF", 4294967295L)]
    [DataRow("0xFFFFFFFF + N", 4294967297L)]
    [DataRow("0xFFFFFFFF + 0xFFFFFFFF", 8589934590L)]
    [DataRow("4294967296l", 4294967296L)]
    public void ComputedRadixField_ShouldRetainTheDeclaredNumber(string expression, long expected)
    {
        var sql = $$"""
            binary Rec { N: byte, K: = {{expression}} };
            let b: byte[] = array { 2ub };
            select r.K from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected]);
    }

    [TestMethod]
    [DataRow("int le", "K = 0xDEADBEEF")]
    [DataRow("int le", "0xDEADBEEF = K")]
    [DataRow("uint le", "K = 0xDEADBEEF")]
    public void HexadecimalSignature_ShouldCompareInTheParsedFieldsDomain(string type, string check)
    {
        var sql = $$"""
            binary Rec { K: {{type}} check {{check}} };
            let b: byte[] = array { 239ub, 190ub, 173ub, 222ub };
            select r.K is not null from values { (N: 0) } s outer apply TryInterpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [true]);
    }

    [TestMethod]
    [DataRow("1ul << 40", "1099511627776ul", "1099511627776")]
    [DataRow("1l << K", "1099511627776l", "1099511627776")]
    [DataRow("1ui << 31", "2147483648ui", "2147483648")]
    [DataRow("2147483647l + 1l", "2147483648l", "2147483648")]
    [DataRow("2000000000l * 3l", "6000000000l", "6000000000")]
    [DataRow("First << K", "1099511627776ul", "1099511627776")]
    public void SuffixedLiteral_ShouldKeepItsWidthInsideTheOperation(
        string expression,
        string expectedLiteral,
        string expected
    )
    {
        var sql = $$"""
            binary Rec { K: byte, First: = 1ul, V: = {{expression}}, Matched: byte when V = {{expectedLiteral}} };
            let b: byte[] = array { 40ub, 42ub };
            select r.V::String as V, r.Matched as M from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected, (byte)42]);
    }
}
