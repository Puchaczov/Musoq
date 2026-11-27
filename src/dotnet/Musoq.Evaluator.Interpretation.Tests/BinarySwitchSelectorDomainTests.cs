using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinarySwitchSelectorDomainTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("K: float le", "1.00000001", "0000803F2A", false, "Other")]
    [DataRow("K: float le", "1.5", "0000C03F2A", false, "MatchArm")]
    [DataRow("K: float le", "1.5", "0000C03F2A", true, "MatchArm")]
    [DataRow("k: byte, K: float le", "1.5", "020000C03F2A", false, "MatchArm")]
    [DataRow("K: float le, k: byte", "1.5", "0000C03F022A", false, "MatchArm")]
    [DataRow("Val: float le, K: = Val", "1.5", "0000C03F2A", false, "MatchArm")]
    [DataRow("Val: float le, K: = Val", "1.5", "0000C03F2A", true, "MatchArm")]
    [DataRow("K: float le", "1.5", "0000807F2A", false, "Other")]
    [DataRow("Val: byte, K: = Val", "1", "012A", false, "MatchArm")]
    [DataRow("Val: string[1] ascii, K: = Val", "'A'", "412A", false, "MatchArm")]
    [DataRow("Val: byte, K: = Val = 1", "true", "012A", false, "MatchArm")]
    public void Switch_ShouldCompareTheBoundSelectorInItsRecordScope(
        string fields,
        string label,
        string hex,
        bool nested,
        string selected
    )
    {
        var body = $"{fields}, U: switch K {{ {label} => MatchArm: byte, _ => Other: byte }}";
        var schema = nested ? $"binary Rec {{ Sub: {{ {body} }} }}" : $"binary Rec {{ {body} }}";
        var record = nested ? "r.Sub.U" : "r.U";
        var sql = $$"""
            {{schema}};
            select {{record}}.Case, {{record}}.MatchArm, {{record}}.Other
            from values { (H: '{{hex}}') } s cross apply Interpret<Rec>(FromHexToBytes(s.H)) r
            """;
        byte? match = selected == "MatchArm" ? (byte)42 : null;
        byte? other = selected == "Other" ? (byte)42 : null;

        AssertQueryRows(sql, [selected, match, other]);
    }
}
