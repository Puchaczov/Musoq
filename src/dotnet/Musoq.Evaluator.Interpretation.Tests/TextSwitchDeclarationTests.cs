using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class TextSwitchDeclarationTests : BasicEntityTestBase
{
    private const string OuterSwitch = "switch { pattern 'c' => pattern '(?<Z>c)' capture (Z), _ => chars[1] }";
    private const string RepeatedSwitch =
        "repeat switch { pattern 'a' => pattern '(?<X>a)' capture (X), _ => chars[1] } until '#'";

    [TestMethod]
    [DataRow("PartsElement")]
    [DataRow("Other")]
    public void AuthoredSwitch_ShouldExposeItsOwnArmMember(string field)
    {
        var sql = $$"""
            text Rec { Parts: {{RepeatedSwitch}}, {{field}}: {{OuterSwitch}} };
            select r.{{field}}.Z from values { (S: 'a#c') } s cross apply Parse<Rec>(s.S) r
            """;

        AssertQueryRows(sql, ["c"]);
    }

    [TestMethod]
    [DataRow("PartsElement")]
    [DataRow("Other")]
    public void RepeatedSwitch_ShouldExposeItsOwnArmMember(string field)
    {
        var sql = $$"""
            text Rec { {{field}}: {{OuterSwitch}}, Parts: {{RepeatedSwitch}} };
            select c.X from values { (S: 'ca#') } s cross apply Parse<Rec>(s.S) r cross apply r.Parts c
            """;

        AssertQueryRows(sql, ["a"]);
    }

    [TestMethod]
    [DataRow("TextSwitch_Item")]
    [DataRow("GoodItem")]
    public void ExpandedNamedTextSchema_ShouldRetainItsElementType(string name)
    {
        var sql = $$"""
            text {{name}} { X: chars[1] };
            text Rec { Items: repeat {{name}} until end };
            select i.X from values { (S: 'ab') } s cross apply Parse<Rec>(s.S) r cross apply r.Items i
            """;

        AssertQueryRows(sql, ["a"], ["b"]);
    }

    [TestMethod]
    [DataRow("TextSwitch_Item")]
    [DataRow("GoodItem")]
    public void ExpandedNamedBinarySchema_ShouldRetainItsElementType(string name)
    {
        var sql = $$"""
            binary {{name}} { N: byte };
            binary Rec { Rows: {{name}}[1] };
            let b: byte[] = array { 66ub };
            select i.N from values { (N: 0) } s cross apply Interpret<Rec>($b) r cross apply r.Rows i
            """;

        AssertQueryRows(sql, [(byte)66]);
    }
}
