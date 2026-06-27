using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{
    [TestMethod]
    public void Visitor_WhenCustomVisitor_ShouldDispatchCorrectly()
    {
        var visitor = new TypeNameVisitor();

        Assert.AreEqual("ColumnRef", visitor.Visit(new ColumnRef("t", "Name", typeof(string))));
        Assert.AreEqual("ScriptParameterRef", visitor.Visit(new ScriptParameterRef("author", typeof(string))));
        Assert.AreEqual("Literal", visitor.Visit(new Literal(42, typeof(int))));
        Assert.AreEqual("WildcardLiteral", visitor.Visit(new WildcardLiteral(typeof(void))));
        Assert.AreEqual("BinaryOp", visitor.Visit(new BinaryOp(BinaryOpKind.Add, new Literal(1, typeof(int)), new Literal(2, typeof(int)), typeof(int))));
        Assert.AreEqual("UnaryOp", visitor.Visit(new UnaryOp(UnaryOpKind.Not, new Literal(true, typeof(bool)), typeof(bool))));
        Assert.AreEqual("IsNullCheck", visitor.Visit(new IsNullCheck(new ColumnRef("t", "X", typeof(string)), false, typeof(bool))));
        Assert.AreEqual("AggregateRef", visitor.Visit(new AggregateRef("agg_0", typeof(int))));
        Assert.AreEqual("WindowFunctionRef", visitor.Visit(new WindowFunctionRef(0, typeof(int))));
        Assert.AreEqual("CteTableRef", visitor.Visit(new CteTableRef("cte")));
    }
}