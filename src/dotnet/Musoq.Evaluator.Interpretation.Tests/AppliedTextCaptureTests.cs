using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class AppliedTextCaptureTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("repeat pattern '(?<X>[ab])' capture (X) until end")]
    [DataRow("switch { _ => repeat pattern '(?<X>[ab])' capture (X) until end }")]
    [DataRow(
        "repeat switch { pattern 'a' => pattern '(?<X>a)' capture (X), _ => pattern '(?<X>b)' capture (X) } until end"
    )]
    [DataRow("repeat Item until end")]
    public void ExpandedCaptures_ShouldExposeTheirMembersAndPreserveDuplicateMatches(string type)
    {
        var sql = $$"""
            text Item { X: chars[1] };
            text Rec { Items: {{type}} };
            select c.X from values { (S: 'aab') } s cross apply Parse<Rec>(s.S) r cross apply r.Items c
            """;

        AssertQueryRows(sql, ["a"], ["a"], ["b"]);
    }

    [TestMethod]
    public void EmptyCaptureArray_ShouldPreserveTheOuterRow()
    {
        const string sql = """
            text Rec { Items: repeat pattern '(?<X>[ab])' capture (X) until end };
            select c.X from values { (S: '') } s cross apply Parse<Rec>(s.S) r outer apply r.Items c
            """;

        AssertQueryRows(sql, [(object?)null]);
    }

    [TestMethod]
    public void ExpandedSwitchArms_ShouldReturnNullForAnUnselectedMember()
    {
        const string sql = """
            text Rec { Items: repeat switch { pattern 'a' => pattern '(?<X>a)' capture (X), _ => pattern '(?<Y>b)' capture (Y) } until end };
            select c.X, c.Y from values { (S: 'aab') } s cross apply Parse<Rec>(s.S) r cross apply r.Items c
            """;

        AssertQueryRows(sql, ["a", null], ["a", null], [null, "b"]);
    }
}
