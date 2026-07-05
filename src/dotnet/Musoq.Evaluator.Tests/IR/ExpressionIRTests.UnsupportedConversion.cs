using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{
    [TestMethod]
    public void Converter_WhenUnsupportedNodeType_ShouldThrowDedicatedUnsupportedShapeException()
    {
        var node = new SkipNode(new IntegerNode("5", string.Empty, default));

        var exception = Assert.Throws<UnsupportedIrShapeException>(() => _converter.Convert(node));

        StringAssert.Contains(exception.Message, nameof(SkipNode));
    }

    [TestMethod]
    public void Converter_TryConvert_WhenUnsupportedNodeType_ShouldReturnUnsupportedResult()
    {
        var node = new SkipNode(new IntegerNode("5", string.Empty, default));

        var result = _converter.TryConvert(node);

        Assert.IsFalse(result.IsSupported);
        StringAssert.Contains(result.UnsupportedReason, nameof(SkipNode));
    }

    [TestMethod]
    public void Converter_TryConvert_WhenConverterInvariantFails_ShouldPropagateException()
    {
        var node = new AccessMethodNode(
            new FunctionToken("Unbound", TextSpan.Empty),
            ArgsListNode.Empty,
            null,
            false);

        var exception = Assert.Throws<InvalidOperationException>(() => _converter.TryConvert(node));

        StringAssert.Contains(exception.Message, "missing Method");
    }
}
