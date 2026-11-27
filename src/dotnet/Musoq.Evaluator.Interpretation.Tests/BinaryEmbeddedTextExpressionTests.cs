using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryEmbeddedTextExpressionTests : BasicEntityTestBase
{
    [TestMethod]
    public void ComputedField_ShouldReadATypedEmbeddedTextMember()
    {
        const string sql = """
            text Header { Code: rest };
            binary Rec { Body: string[1] ascii as Header, Value: = Body.Code };
            let b: byte[] = array { 65ub };
            select r.Value from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, ["A"]);
    }

    [TestMethod]
    public void MissingOptionalTextMember_ShouldLiftItsLengthAndSkipTheGuardedByte()
    {
        const string sql = """
            text Header { Code: optional between '[' ']', Tail: rest };
            binary Rec { Body: string[1] ascii as Header, N: = Body.Code.Length, Mark: byte when N = 1, Tail: byte };
            let b: byte[] = array { 65ub, 126ub };
            select r.N, r.Mark, r.Tail from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [null, null, (byte)126]);
    }

    [TestMethod]
    public void EnclosingComputedField_ShouldReadTheEmbeddedCapture()
    {
        const string sql = """
            text Header { Code: pattern '(?<V>.)' capture (V) };
            binary Child { V: = parent.Body.Code.V };
            binary Rec { Body: string[1] ascii as Header, C: Child };
            let b: byte[] = array { 65ub };
            select r.C.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, ["A"]);
    }
}
