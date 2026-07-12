using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionIrRewriterTests
{
    [TestMethod]
    public void RewritePlan_WhenNoChanges_ShouldPreserveExistingReferences()
    {
        var literal = new ExecutionLiteral(1, typeof(int));
        var append = new ExecutionAppendRow(
            Var("result"),
            CreateRowShape(),
            [new ExecutionRowValue("Value", literal)]);
        var innerBlock = new ExecutionBlock([append]);
        var forEach = new ExecutionForEach(
            Var("item", typeof(int)),
            new ExecutionStoredTableRows(7),
            innerBlock);
        var block = new ExecutionBlock([forEach]);
        var plan = new ExecutionPlan("compiled", [], block);

        var rewritten = new NoOpRewriter().RewritePlan(plan);

        Assert.AreSame(plan, rewritten);
        Assert.AreSame(block, rewritten.Body);
        Assert.AreSame(block.Nodes, rewritten.Body.Nodes);
        Assert.AreSame(forEach, rewritten.Body.Nodes[0]);
        Assert.AreSame(innerBlock, ((ExecutionForEach)rewritten.Body.Nodes[0]).Body);
        Assert.AreSame(append, ((ExecutionForEach)rewritten.Body.Nodes[0]).Body.Nodes[0]);
    }

    [TestMethod]
    public void RewritePlan_WhenNestedExpressionChanges_ShouldReplaceAffectedAncestorsOnly()
    {
        var source = new ExecutionStoredTableRows(7);
        var context = new ExecutionLiteral(3, typeof(int));
        var append = new ExecutionAppendRow(
            Var("result"),
            CreateRowShape(),
            [
                new ExecutionRowValue(
                    "Value",
                    new ExecutionBinary(
                        BinaryOpKind.Add,
                        new ExecutionLiteral(1, typeof(int)),
                        new ExecutionLiteral(2, typeof(int)),
                        typeof(int)))
            ],
            [],
            ExecutionAppendMode.Checked,
            new ExecutionContextLayout(
            [
                new ExecutionContextSegment(ExecutionContextSegmentKind.Single, context, 1)
            ]));
        var innerBlock = new ExecutionBlock([append]);
        var forEach = new ExecutionForEach(Var("item", typeof(int)), source, innerBlock);
        var block = new ExecutionBlock([forEach]);
        var plan = new ExecutionPlan("compiled", [], block);

        var rewritten = new IncrementIntLiteralRewriter().RewritePlan(plan);

        Assert.AreNotSame(plan, rewritten);
        var rewrittenForEach = (ExecutionForEach)rewritten.Body.Nodes[0];
        var rewrittenAppend = (ExecutionAppendRow)rewrittenForEach.Body.Nodes[0];
        var rewrittenBinary = (ExecutionBinary)rewrittenAppend.Values[0].Value;

        Assert.AreSame(source, rewrittenForEach.Source);
        Assert.AreEqual(2, ((ExecutionLiteral)rewrittenBinary.Left).Value.ToClrValue());
        Assert.AreEqual(3, ((ExecutionLiteral)rewrittenBinary.Right).Value.ToClrValue());
        Assert.AreEqual(
            4,
            ((ExecutionLiteral)rewrittenAppend.ContextLayout!.Segments[0].Value).Value.ToClrValue());
    }

    [TestMethod]
    public void RewritePlan_WhenNestedBlockNodeChanges_ShouldPreserveUnaffectedSiblingBlocks()
    {
        var changedBody = new ExecutionBlock([new ExecutionReturnTable(Var("old"))]);
        var unchangedNoMatchBody = new ExecutionBlock([new ExecutionReturnTable(Var("unchanged"))]);
        var probe = new ExecutionHashProbe(
            Var("hash"),
            Var("matches"),
            new ExecutionLiteral(1, typeof(int)),
            typeof(int),
            typeof(object),
            changedBody,
            unchangedNoMatchBody);
        var block = new ExecutionBlock([probe]);
        var plan = new ExecutionPlan("compiled", [], block);

        var rewritten = new RenameReturnTableRewriter().RewritePlan(plan);

        var rewrittenProbe = (ExecutionHashProbe)rewritten.Body.Nodes[0];
        var returnTable = (ExecutionReturnTable)rewrittenProbe.Body.Nodes[0];

        Assert.AreNotSame(probe, rewrittenProbe);
        Assert.AreNotSame(changedBody, rewrittenProbe.Body);
        Assert.AreSame(unchangedNoMatchBody, rewrittenProbe.NoMatchBody);
        Assert.AreEqual("new", returnTable.Table.Name);
    }

    [TestMethod]
    public void RewritePlan_WhenCapacityHintCandidateExpressionChanges_ShouldReplaceOwningNode()
    {
        var hash = Var("hash");
        var createHash = new ExecutionCreateHash(
            hash,
            typeof(int),
            typeof(object),
            new ExecutionRowsCapacityHintCandidate(hash, new ExecutionStoredTableRows(3)));
        var block = new ExecutionBlock([createHash]);
        var plan = new ExecutionPlan("compiled", [], block);

        var rewritten = new StoredRowsIndexRewriter().RewritePlan(plan);

        Assert.AreNotSame(plan, rewritten);
        var rewrittenHash = (ExecutionCreateHash)rewritten.Body.Nodes[0];
        var candidate = (ExecutionRowsCapacityHintCandidate)rewrittenHash.CapacityHint!;
        var rows = (ExecutionStoredTableRows)candidate.Rows;

        Assert.AreEqual(4, rows.TableIndex);
    }

    [TestMethod]
    public void SubstitutionRewriter_WhenExpressionMatches_ShouldReplaceBeforeRewritingChildren()
    {
        var binary = new ExecutionBinary(
            BinaryOpKind.Add,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(2, typeof(int)),
            typeof(int));
        var append = new ExecutionAppendRow(
            Var("result"),
            CreateRowShape(),
            [new ExecutionRowValue("Value", binary)]);
        var plan = new ExecutionPlan("compiled", [], new ExecutionBlock([append]));
        var replacement = new ExecutionLiteral(10, typeof(int));
        var rewriter = new ExecutionExpressionSubstitutionRewriter(expression =>
            ReferenceEquals(expression, binary) ? replacement : null);

        var rewritten = rewriter.RewritePlan(plan);

        var rewrittenAppend = (ExecutionAppendRow)rewritten.Body.Nodes[0];
        Assert.AreSame(replacement, rewrittenAppend.Values[0].Value);
    }

    private static ExecutionVariable Var(string name, Type? type = null)
    {
        return new ExecutionVariable(name, type ?? typeof(object));
    }

    private static GeneratedRowShape CreateRowShape()
    {
        return new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Value", "Value", 0, typeof(int), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
    }

    private sealed class NoOpRewriter : ExecutionIrRewriter;

    private sealed class IncrementIntLiteralRewriter : ExecutionIrRewriter
    {
        protected override ExecutionExpression RewriteLiteral(ExecutionLiteral expression)
        {
            return expression.Value.TryGetInt32(out var value)
                ? new ExecutionLiteral(value + 1, expression.ReturnType)
                : expression;
        }
    }

    private sealed class RenameReturnTableRewriter : ExecutionIrRewriter
    {
        protected override ExecutionNode RewriteReturnTable(ExecutionReturnTable node)
        {
            return node.Table.Name == "old"
                ? node with { Table = new ExecutionVariable("new", node.Table.Type) }
                : node;
        }
    }

    private sealed class StoredRowsIndexRewriter : ExecutionIrRewriter
    {
        protected override ExecutionExpression RewriteStoredTableRows(ExecutionStoredTableRows expression)
        {
            return expression with { TableIndex = expression.TableIndex + 1 };
        }
    }
}
