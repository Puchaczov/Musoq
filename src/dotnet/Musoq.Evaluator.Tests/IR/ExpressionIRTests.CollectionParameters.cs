using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;
using Musoq.Parser.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{
    [TestMethod]
    public void Converter_WhenCollectionInNode_ShouldReturnCollectionInCheck()
    {
        var node = new CollectionInNode(
            new AccessColumnNode("Id", "t", typeof(int), default),
            new ParameterReferenceNode("ids", typeof(int[]), default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<CollectionInCheck>(result);
        var check = (CollectionInCheck)result;
        Assert.IsInstanceOfType<ColumnRef>(check.Expression);
        Assert.IsInstanceOfType<ScriptParameterRef>(check.Collection);
        Assert.AreEqual(typeof(int), check.ElementType);
        Assert.AreEqual(typeof(bool), check.ReturnType);
    }

    [TestMethod]
    public void Converter_WhenCollectionInExpression_ShouldPrintCorrectly()
    {
        var node = new CollectionInNode(
            new AccessColumnNode("Id", "t", typeof(int), default),
            new ParameterReferenceNode("ids", typeof(int[]), default));

        var result = _converter.Convert(node);

        Assert.AreEqual("t.Id IN $ids", IrExpressionPrinter.Print(result));
    }
}
