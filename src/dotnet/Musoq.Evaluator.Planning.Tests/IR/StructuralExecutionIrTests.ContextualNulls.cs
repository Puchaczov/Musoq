using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class StructuralExecutionIrTests
{
    [TestMethod]
    [DataRow("Mode", typeof(string))]
    [DataRow("Count", typeof(int?))]
    public void ReceiverFieldNull_ShouldKeepContextualType(string field, Type expectedType)
    {
        var syntax = new RecordLiteralNode([new RecordFieldNode(field, new NullNode())]);
        var expression = (StructuralRecordLiteral)new ExpressionConverter().Convert(syntax, typeof(ContextualNullInput));
        var literal = (Literal)expression.Fields[0].Value;

        Assert.IsNull(literal.Value);
        Assert.AreEqual(expectedType, literal.ReturnType);
    }

    public readonly record struct ContextualNullInput(string? Mode = "default", int? Count = 7);
}
