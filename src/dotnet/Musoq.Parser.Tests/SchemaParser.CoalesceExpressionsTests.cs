using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SchemaCoalesceExpressionsTests : SchemaParserTestsBase
{
    [TestMethod]
    public void Coalesce_ShouldAssociateToTheRightBelowArithmetic()
    {
        var schema = ParseBinarySchema("binary Rec { Value: 1 + 2 ?? 3 ?? 4 }");
        var expression = ((ComputedFieldNode)schema.Fields[0]).Expression;
        var coalesce = Assert.IsInstanceOfType<CoalesceNode>(expression);
        Assert.IsInstanceOfType<AddNode>(coalesce.Left);
        Assert.IsInstanceOfType<CoalesceNode>(coalesce.Right);
    }

    [TestMethod]
    public void Coalesce_ShouldBindWithinRelationalOperands()
    {
        var schema = ParseBinarySchema("binary Rec { Value: 1 ?? 2 = 3 }");
        var expression = ((ComputedFieldNode)schema.Fields[0]).Expression;
        var comparison = Assert.IsInstanceOfType<EqualityNode>(expression);
        Assert.IsInstanceOfType<CoalesceNode>(comparison.Left);
    }
}
