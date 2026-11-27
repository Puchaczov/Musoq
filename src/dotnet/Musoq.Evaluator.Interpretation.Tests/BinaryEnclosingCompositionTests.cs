using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryEnclosingCompositionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("Child", "B.V")]
    [DataRow("switch Tag { 1 => Arm: Child }", "B.Arm.V")]
    [DataRow("switch Tag { 1 => Arm: { V: = parent.Tag } }", "B.Arm.V")]
    public void SelectedChild_ShouldReadTheContainingRecord(string valueType, string member)
    {
        var sql = $$"""
            binary Child { V: = parent.Tag };
            binary Rec { Tag: byte, B: {{valueType}} };
            let b: byte[] = array { 1ub };
            select r.{{member}} from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)1]);
    }

    [TestMethod]
    public void SwitchBranchWithGenericArgument_ShouldForwardTheEnclosingInstance()
    {
        const string sql = """
            binary Leaf { V: = parent.parent.Tag };
            binary Box<T> { Inner: T };
            binary Rec { Tag: byte, B: switch Tag { 1 => Arm: Box<Leaf> } };
            let b: byte[] = array { 1ub };
            select r.B.Arm.Inner.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)1]);
    }

    [TestMethod]
    public void ParentPath_ShouldResolveAnInheritedMemberThroughANamedRecord()
    {
        const string sql = """
            binary BaseRec { Count: byte };
            binary Sub extends BaseRec { Tail: byte };
            binary Child { V: = parent.Info.Count };
            binary Rec { Info: Sub, C: Child };
            let b: byte[] = array { 2ub, 33ub };
            select r.C.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)2]);
    }
}
