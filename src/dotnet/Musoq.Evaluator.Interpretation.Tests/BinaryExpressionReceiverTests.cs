using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryExpressionReceiverTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("Substring('ab', 1, 1).Length")]
    [DataRow("S.Length")]
    public void StringResultLength_ShouldBeAUsableByteCount(string length)
    {
        var sql = $$"""
            binary Rec { S: = Substring('ab', 1, 1), Data: byte[{{length}}], Tail: byte };
            let b: byte[] = array { 65ub, 126ub };
            select r.Data[0], r.Tail from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)65, (byte)126]);
    }

    [TestMethod]
    [DataRow("0ub", true, null, 2)]
    [DataRow("1ub", false, "Data", 1)]
    public void GuardedInvalidReceiver_ShouldFailOnlyWhenEvaluated(
        string flag,
        bool success,
        string? error,
        int position
    )
    {
        var sql = $$"""
            binary Rec { Flag: byte, Data: byte[Substring('abc', -1, 0).Length] when Flag = 1, Tail: byte };
            let b: byte[] = array { {{flag}}, 126ub };
            select r.IsSuccess, r.ErrorField, r.BytesConsumed from values { (N: 0) } s cross apply PartialInterpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [success, error, position]);
    }

    [TestMethod]
    [DataRow("ToText(FromHex('F09F8CB1'), 'utf8')", 2)]
    [DataRow("(CASE WHEN Flag = 0 THEN 'abc' ELSE NULL END)", 3)]
    [DataRow("(CASE WHEN Flag = 1 THEN 'abc' ELSE NULL END)", null)]
    public void ComputedMember_ShouldReadItsReceiverAndPreserveAbsence(string receiver, int? expected)
    {
        var sql = $$"""
            binary Rec { Flag: byte, V: = {{receiver}}.Length };
            let b: byte[] = array { 0ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected]);
    }
}
