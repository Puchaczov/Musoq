using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryComputedSwitchMemberTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("B.Case", "R")]
    [DataRow("B.R.N", (byte)66)]
    public void ComputedField_ShouldUseTheSelectedSwitchMember(string expression, object expected)
    {
        var sql = $$"""
            binary Item { N: byte };
            binary Rec { K: byte, B: switch K { 1 => R: Item, _ => S: byte }, V: = {{expression}} };
            let b: byte[] = array { 1ub, 66ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected]);
    }

    [TestMethod]
    [DataRow("1ub, 65ub, 66ub, 49ub, 50ub, 126ub", 2)]
    [DataRow("0ub, 66ub, 126ub", 0)]
    public void ArrayBranchLength_ShouldBeNullableForAFollowingSize(string bytes, int expected)
    {
        var sql = $$"""
            binary Rec { K: byte, B: switch K { 1 => Raw: byte[2], _ => S: byte }, V: byte[B.Raw.Length ?? 0], Tail: byte };
            let b: byte[] = array { {{bytes}} };
            select r.V.Length, r.Tail from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected, (byte)126]);
    }

    [TestMethod]
    [DataRow("1ub", "R: Item", "B.R", (byte)66)]
    [DataRow("0ub", "R: Item", "B.R", null)]
    [DataRow("1ub", "R: Item[1]", "B.R[0]", (byte)66)]
    [DataRow("0ub", "R: Item[1]", "B.R[0]", null)]
    public void SwitchBranchAlias_ShouldRetainTheSelectedRecordType(
        string tag,
        string branch,
        string expression,
        byte? expected
    )
    {
        var sql = $$"""
            binary Item { N: byte };
            binary Rec { K: byte, B: switch K { 1 => {{branch}}, _ => S: byte }, A: = {{expression}}, V: = A.N };
            let b: byte[] = array { {{tag}}, 66ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected]);
    }
}
