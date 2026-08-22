using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class FusedSiblingAliasRewriterTests
{
    [TestMethod]
    public void RewriteBlock_ShouldSubstituteAliasesAcrossRegisteredNodesExpressionsContextsAndSidecars()
    {
        var source = new ExecutionVariable("siblingRow", typeof(object));
        var target = new ExecutionVariable("canonicalRow", typeof(object));
        var table = new ExecutionVariable("result", typeof(object));
        var row = new ExecutionVariable("outputRow", typeof(object));
        var hash = new ExecutionVariable("hash", typeof(object));
        var keySet = new ExecutionVariable("keySet", typeof(object));
        var rowShape = CreateRowShape();
        var complex = CreateComplexExpression(source);
        var append = new ExecutionAppendRow(
            table,
            rowShape,
            [new ExecutionRowValue("Value", complex)],
            [new ExecutionRowContextsRead(source)],
            ExecutionAppendMode.Checked,
            CreateContextLayout(source));
        var block = new ExecutionBlock(
        [
            new ExecutionLet(new ExecutionVariable("complex", typeof(object)), complex),
            new ExecutionIf(
                new ExecutionIsNullCheck(Read(source), false, typeof(bool)),
                new ExecutionBlock([new ExecutionContinueIf(new ExecutionInCheck(Read(source), [Literal(1)], typeof(bool)))])),
            new ExecutionCreateGeneratedRow(
                row,
                rowShape,
                [new ExecutionRowValue("Value", complex)],
                [new ExecutionVariableRead(source)],
                CreateContextLayout(source)),
            append,
            new ExecutionHashAdd(hash, complex, row, typeof(object), typeof(object), precomputedKey: source),
            new ExecutionKeySetAdd(keySet, complex, typeof(object), precomputedKey: source),
            new ExecutionCteSidecarAppendRewriteCandidate(
                append,
                [
                    new ExecutionCteSidecarAppendIndexSpec(
                        hash,
                        complex,
                        ExecutionCteSidecarIndexKind.Hash,
                        typeof(object),
                        null,
                        [new ExecutionRowValue("Payload", complex)])
                ])
        ]);
        var before = new AliasReferenceCounter(source.Name, target.Name);
        before.RewriteBlock(block);

        var rewritten = new FusedSiblingAliasRewriter(source, target).RewriteBlock(block);

        var after = new AliasReferenceCounter(source.Name, target.Name);
        after.RewriteBlock(rewritten);
        Assert.IsGreaterThan(0, before.SourceReferences);
        Assert.AreEqual(0, before.TargetReferences);
        Assert.AreEqual(0, after.SourceReferences);
        Assert.AreEqual(before.SourceReferences, after.TargetReferences);
        Assert.AreNotSame(block, rewritten);
    }

    [TestMethod]
    public void RewriterCompleteness_ShouldBeBackedByTheAuthoritativeNodeRegistryAndRejectUnknownExpressions()
    {
        var concreteNodes = typeof(ExecutionPlan).Assembly.GetTypes()
            .Where(static type => !type.IsAbstract && typeof(ExecutionNode).IsAssignableFrom(type))
            .ToArray();
        var registeredNodes = ExecutionNodeRegistry.Descriptors
            .Select(static descriptor => descriptor.NodeType)
            .ToArray();

        CollectionAssert.AreEquivalent(concreteNodes, registeredNodes);
        Assert.IsTrue(ExecutionNodeRegistry.Descriptors.All(static descriptor => descriptor.Behavior.Rewriter != null));

        var rewriter = new FusedSiblingAliasRewriter(
            new ExecutionVariable("from", typeof(object)),
            new ExecutionVariable("to", typeof(object)));
        var exception = Assert.ThrowsExactly<NotSupportedException>(
            () => rewriter.RewriteExpression(new UnregisteredExpression()));
        StringAssert.Contains(exception.Message, typeof(UnregisteredExpression).FullName!);
    }

    private static ExecutionExpression CreateComplexExpression(ExecutionVariable source)
    {
        return new ExecutionCompositeKey(
        [
            new ExecutionBinary(BinaryOpKind.Add, Read(source), Literal(1), typeof(int)),
            new ExecutionUnary(UnaryOpKind.Negate, Read(source), typeof(int)),
            new ExecutionStrictCast(Read(source), "System.Int32", typeof(int)),
            new ExecutionArrayAccess(new ExecutionVariableRead(source), Literal(0), typeof(object), typeof(object)),
            new ExecutionIsNullCheck(Read(source), false, typeof(bool)),
            new ExecutionInCheck(Read(source), [Literal(1), Literal(2)], typeof(bool)),
            new ExecutionPatternMatch(Read(source), new ExecutionLiteral("x", typeof(string)), PatternKind.Like, typeof(bool)),
            new ExecutionBetween(Read(source), Literal(0), Literal(10), typeof(bool)),
            new ExecutionCaseWhen(
                [new ExecutionCaseWhenBranch(new ExecutionIsNullCheck(Read(source), false, typeof(bool)), Read(source))],
                Read(source),
                typeof(object)),
            new ExecutionCoalesce([Read(source), new ExecutionLiteral(null, typeof(object))], typeof(object)),
            new ExecutionValueTupleKey([Read(source), new ExecutionVariableRead(source)], typeof(object)),
            new ExecutionContextArray(
            [
                new ExecutionContextSegment(ExecutionContextSegmentKind.Single, Read(source), 1),
                new ExecutionContextSegment(ExecutionContextSegmentKind.Array, new ExecutionRowContextsRead(source), 2)
            ])
        ]);
    }

    private static ExecutionContextLayout CreateContextLayout(ExecutionVariable source)
    {
        return new ExecutionContextLayout(
        [
            new ExecutionContextSegment(ExecutionContextSegmentKind.Single, Read(source), 1),
            new ExecutionContextSegment(ExecutionContextSegmentKind.Array, new ExecutionRowContextsRead(source), 2)
        ]);
    }

    private static ExecutionFieldRead Read(ExecutionVariable source)
    {
        return new ExecutionFieldRead(source.Name, "Value", typeof(int));
    }

    private static ExecutionLiteral Literal(int value)
    {
        return new ExecutionLiteral(value, typeof(int));
    }

    private static GeneratedRowShape CreateRowShape()
    {
        return new GeneratedRowShape(
            "FusedSiblingRow",
            [new FieldBinding("Value", "Value", 0, typeof(object), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
    }

    private sealed class AliasReferenceCounter(string sourceName, string targetName) : ExecutionIrRewriter
    {
        public int SourceReferences { get; private set; }

        public int TargetReferences { get; private set; }

        protected override ExecutionExpression RewriteFieldRead(ExecutionFieldRead expression)
        {
            Count(expression.Alias);
            return expression;
        }

        protected override ExecutionExpression RewriteVariableRead(ExecutionVariableRead expression)
        {
            Count(expression.Variable.Name);
            return expression;
        }

        protected override ExecutionExpression RewriteRowContextsRead(ExecutionRowContextsRead expression)
        {
            Count(expression.Row.Name);
            return expression;
        }

        protected override ExecutionNode RewriteHashAdd(ExecutionHashAdd node)
        {
            Count(node.PrecomputedKey?.Name);
            return base.RewriteHashAdd(node);
        }

        protected override ExecutionNode RewriteKeySetAdd(ExecutionKeySetAdd node)
        {
            Count(node.PrecomputedKey?.Name);
            return base.RewriteKeySetAdd(node);
        }

        private void Count(string? name)
        {
            if (string.Equals(name, sourceName, StringComparison.Ordinal))
                SourceReferences++;
            if (string.Equals(name, targetName, StringComparison.Ordinal))
                TargetReferences++;
        }
    }

    private sealed record UnregisteredExpression()
        : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(object)));
}
