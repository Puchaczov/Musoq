using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ComputedMemberAccessTests
{
    [TestMethod]
    public void IndexedMemberDependencies_ShouldIncludeBothReceiverAndIndexSources()
    {
        var items = new ColumnRef("items", "Records", typeof(object[]), "Musoq.Generated.Interpreters.Item[]");
        var position = new ColumnRef("positions", "Index", typeof(int));
        var member = new MemberAccess(
            new ArrayAccess(items, position, typeof(object), typeof(object)),
            "Value",
            typeof(byte)
        );

        CollectionAssert.AreEquivalent(new[] { items, position }, ColumnRefExtractor.Extract(member).ToArray());
        CollectionAssert.AreEquivalent(new[] { "items", "positions" }, AliasRefExtractor.Extract(member).ToArray());
        Assert.Contains("[positions.Index].Value", IrExpressionPrinter.Print(member));
    }

    [TestMethod]
    public void IndexedMemberLowering_ShouldCarryTheGeneratedElementTypeAndIndexExpression()
    {
        var member = new MemberAccess(
            new ArrayAccess(
                new ColumnRef("r", "Items", typeof(object[]), "Musoq.Generated.Interpreters.Item[]"),
                new ColumnRef("r", "Index", typeof(int)),
                typeof(object),
                typeof(object)
            ),
            "Value",
            typeof(byte),
            IsNullSafe: true
        );

        var result = (ExecutionMemberRead)
            ExecutionExpressionConverter.Convert(member, new Dictionary<string, RowShape>());
        var indexed = (ExecutionMethodCall)result.Receiver;
        Assert.AreEqual("Musoq.Generated.Interpreters.Item", indexed.GeneratedGenericArguments[0]);
        Assert.AreEqual("Musoq.Generated.Interpreters.Item", indexed.GeneratedResultTypeName);
        Assert.AreEqual("Index", ((ExecutionFieldRead)indexed.Arguments[1]).FieldName);
        Assert.IsTrue(result.IsNullSafe);
    }

    [TestMethod]
    public void MemberRewriting_ShouldRetainItsTypeContractWhenRebindingTheIndex()
    {
        var member = new MemberAccess(
            new ArrayAccess(
                new Literal(null, typeof(object[])),
                new ColumnRef("r", "Index", typeof(int)),
                typeof(object),
                typeof(object)
            ),
            "Nested",
            typeof(object),
            "Musoq.Generated.Interpreters.Nested",
            true
        );
        var rewritten = (MemberAccess)new ReplaceIndex().Visit(member);
        Assert.AreEqual(member.GeneratedTypeName, rewritten.GeneratedTypeName);
        Assert.IsTrue(rewritten.IsNullSafe);
        Assert.AreEqual(1, ((Literal)((ArrayAccess)rewritten.Receiver).Index).Value);
    }

    private sealed class ReplaceIndex : IrExpressionRewriter
    {
        protected override IrExpression VisitColumnRef(ColumnRef node) => new Literal(1, typeof(int));
    }
}
