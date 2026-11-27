using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class AnonymousEnclosingValueTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("float le", "0000C03F41427E", "N", false, 4)]
    [DataRow("float le", "0000C03F41427E", "parent.N", false, 4)]
    [DataRow("byte when false", "7E", "N", false, 0)]
    [DataRow("uint le", "000000807E", "N", false, 4)]
    [DataRow("float le", "0000004041427E", "N", true, 7)]
    [DataRow("byte", "0241427E", "N", true, 4)]
    public void AnonymousSize_ShouldUseTheEnclosingValuesExactType(
        string type,
        string hex,
        string length,
        bool success,
        int terminal
    )
    {
        var sql = $$"""
            binary Rec { N: {{type}}, Sub: { Items: byte[{{length}}] }, Tail: byte };
            select r.IsSuccess, r.BytesConsumed, r.BitOffset, r.ErrorField,
                   case when r.IsSuccess then r.ParsedFields.Tail else null end
            from values { (H: '{{hex}}') } s cross apply PartialInterpret<Rec>(FromHexToBytes(s.H)) r
            """;

        AssertQueryRows(sql, [success, terminal, 0, success ? null : "Sub.Items", success ? (byte?)126 : null]);
    }

    [TestMethod]
    [DataRow("string[1] ascii", "Kind = 'A'", "41557E", 85)]
    [DataRow("string[1] ascii", "Kind = 'A'", "427E", null)]
    [DataRow("byte when false", "Kind is null", "557E", 85)]
    [DataRow("byte when false", "parent.Kind is null", "557E", 85)]
    public void AnonymousPresenceGuard_ShouldObserveStringAndNullableEnclosingValues(
        string type,
        string guard,
        string hex,
        int? expected
    )
    {
        var sql = $$"""
            binary Rec { Kind: {{type}}, Sub: { V: byte when {{guard}} }, Tail: byte };
            select r.Sub.V, r.Tail from values { (H: '{{hex}}') } s cross apply Interpret<Rec>(FromHexToBytes(s.H)) r
            """;

        AssertQueryRows(sql, [(byte?)expected, (byte)126]);
    }

    [TestMethod]
    [DataRow("H: Header", "H.N", "0241427E")]
    [DataRow("H: { N: byte }", "H.N", "0241427E")]
    [DataRow("S: string[2] ascii", "S.Length", "414251527E")]
    [DataRow("Sizes: byte[2]", "Sizes[1]", "000241427E")]
    [DataRow("H: Header", "parent.H.N", "0241427E")]
    [DataRow("H: Header[2]", "H[1].N", "000241427E")]
    [DataRow("H: Header[1]", "H[-1].N", "0241427E")]
    [DataRow("I: byte, H: Header[2]", "H[I].N", "01000241427E")]
    [DataRow("H: Header[2]", "parent.H[1].N", "000241427E")]
    public void AnonymousSize_ShouldBindDeclaredMembersOfAnEnclosingRecord(string head, string size, string hex)
    {
        var sql = $$"""
            binary Header { N: byte };
            binary Rec { {{head}}, Sub: { Value: byte[{{size}}] }, Tail: byte };
            select r.Tail from values { (H: '{{hex}}') } s cross apply Interpret<Rec>(FromHexToBytes(s.H)) r
            """;

        AssertQueryRows(sql, [(byte)126]);
    }

    [TestMethod]
    public void DifferentlyCasedLocal_ShouldLeaveTheEnclosingLengthVisible()
    {
        const string sql = """
            binary Rec { N: byte, Sub: { n: byte, Body: byte[N] }, Tail: byte };
            let b: byte[] = array { 2ub, 1ub, 65ub, 66ub, 126ub };
            select r.Sub.n, r.Sub.Body[1], r.Tail from values { (K: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)1, (byte)66, (byte)126]);
    }
}
