using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{
    [TestMethod]
    public void Converter_WhenNestedCoalesceNode_ShouldReturnFlatCoalesce()
    {
        var node = new CoalesceNode(
            new AccessColumnNode("First", "t", typeof(string), default),
            new CoalesceNode(
                new AccessColumnNode("Second", "t", typeof(string), default),
                new AccessColumnNode("Third", "t", typeof(string), default),
                typeof(string)),
            typeof(string));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<Coalesce>(result);
        var coalesce = (Coalesce)result;
        Assert.HasCount(3, coalesce.Expressions);
        Assert.AreEqual("First", ((ColumnRef)coalesce.Expressions[0]).ColumnName);
        Assert.AreEqual("Second", ((ColumnRef)coalesce.Expressions[1]).ColumnName);
        Assert.AreEqual("Third", ((ColumnRef)coalesce.Expressions[2]).ColumnName);
    }
}