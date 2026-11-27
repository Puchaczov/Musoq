using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinarySwitchFieldReferenceTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("H: Header", "H.Tag", "012A7E", true)]
    [DataRow("H: Header", "H.Tag", "007E", false)]
    [DataRow("K: byte, H: Header when K = 1", "H.Tag", "007E", false)]
    [DataRow("H: Header[2]", "H[1].Tag", "00012A7E", true)]
    [DataRow("I: byte, H: Header[2]", "H[I].Tag", "0100012A7E", true)]
    [DataRow("H: byte[2]", "H[-1]", "00012A7E", true)]
    [DataRow("H: Header, Dis: = H.Tag", "Dis", "012A7E", true)]
    [DataRow("H: Header, Dis: = H.Tag::Byte", "Dis", "012A7E", true)]
    [DataRow("Dis: byte", "Dis", "007E", false)]
    public void Switch_ShouldMatchTheValueAtTheSelectedField(string fields, string selector, string hex, bool match)
    {
        var sql = $$"""
            binary Header { Tag: byte };
            binary Rec { {{fields}}, B: switch {{selector}} { 1 => A: byte, _ => Z: byte[0] }, T: byte };
            select r.B.Case, r.B.A, r.T from values { (H: '{{hex}}') } s cross apply Interpret<Rec>(FromHexToBytes(s.H)) r
            """;

        AssertQueryRows(sql, [match ? "A" : "Z", match ? (byte?)42 : null, (byte)126]);
    }

    [TestMethod]
    [DataRow("C: { B: switch Tag { 1 => A: byte, _ => Z: byte[0] } }")]
    [DataRow("C: Child")]
    public void EnclosingDiscriminator_ShouldSelectTheMatchingChildAndAdvanceTheCursor(string child)
    {
        var decl =
            child == "C: Child"
                ? "binary Child { B: switch parent.Tag { 1 => A: byte, _ => Z: byte[0] } };"
                : string.Empty;
        var sql = $$"""
            {{decl}}
            binary Rec { Tag: byte, {{child}}, T: byte };
            let b: byte[] = array { 1ub, 42ub, 126ub };
            select r.C.B.Case, r.C.B.A, r.T from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, ["A", (byte)42, (byte)126]);
    }

    [TestMethod]
    public void MissingMemberSelectorMatch_ShouldFailAtTheSwitchWithOnlyTheHeaderCompleted()
    {
        const string sql = """
            binary Header { Tag: byte };
            binary Rec { H: Header, B: switch H.Tag { 1 => A: byte } };
            let b: byte[] = array { 0ub };
            select r.IsSuccess, r.ErrorField, r.BytesConsumed, r.BitOffset, ToJson(r.ParsedFields)
            from values { (N: 0) } s cross apply PartialInterpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [false, "B", 1, 0, """{"H":{"Tag":0}}"""]);
    }
}
