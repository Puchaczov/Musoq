using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SchemaParserBinaryExpressionPostfixTests : SchemaParserTestsBase
{
    [TestMethod]
    public void CompletedArrayFieldIndexWithMember_ShouldParseAsComputedExpression()
    {
        var schema = ParseBinarySchema("binary Rec { Entries: Item[1], Next: Entries[0].Value + 1 }");
        var computed = (ComputedFieldNode)schema.Fields[1];
        var addition = (AddNode)computed.Expression;
        var member = (DotNode)addition.Left;
        Assert.IsInstanceOfType<ArrayIndexNode>(member.Root);
        Assert.AreEqual("Value", member.Name);
    }

    [TestMethod]
    public void StrictCast_ShouldBindBeforeArithmeticAndAllowChaining()
    {
        var schema = ParseBinarySchema("binary Rec { F: float le, Sum: F::Decimal + 0.5, Chain: (F + F)::Decimal::Double }");
        var addition = (AddNode)((ComputedFieldNode)schema.Fields[1]).Expression;
        Assert.AreEqual("Decimal", ((CastNode)addition.Left).TargetTypeName);
        var outer = (CastNode)((ComputedFieldNode)schema.Fields[2]).Expression;
        Assert.AreEqual("Double", outer.TargetTypeName);
        Assert.AreEqual("Decimal", ((CastNode)outer.Expression).TargetTypeName);
        Assert.IsInstanceOfType<AddNode>(((CastNode)outer.Expression).Expression);
    }

    [TestMethod]
    [DataRow("1::Decimal")]
    [DataRow("null::Decimal")]
    [DataRow("'1.5'::Decimal")]
    [DataRow("(1 + 2)::Decimal")]
    public void StrictCast_ShouldApplyToEveryPrimaryExpression(string expression)
    {
        var schema = ParseBinarySchema($"binary Rec {{ Value: {expression} }}");
        Assert.IsInstanceOfType<CastNode>(((ComputedFieldNode)schema.Fields[0]).Expression);
    }
}
