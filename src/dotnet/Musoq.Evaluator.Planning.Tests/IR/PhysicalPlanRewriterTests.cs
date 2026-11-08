using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class PhysicalPlanRewriterTests
{
    [TestMethod]
    public void RewriteChildren_WhenUnaryChildUnchanged_ShouldReturnOriginalNode()
    {
        var scan = CreateScan();
        var filter = new PhysicalFilterNode(CreatePredicate(), scan);

        var rewritten = PhysicalPlanRewriter.RewriteChildren(filter, static node => node);

        Assert.AreSame(filter, rewritten);
    }

    [TestMethod]
    public void RewriteChildren_WhenUnaryChildChanges_ShouldRebuildOnlyParent()
    {
        var scan = CreateScan("source");
        var replacement = CreateScan("replacement");
        var predicate = CreatePredicate();
        var filter = new PhysicalFilterNode(predicate, scan);

        var rewritten = PhysicalPlanRewriter.RewriteChildren(
            filter,
            node => ReferenceEquals(node, scan) ? replacement : node);

        var rewrittenFilter = (PhysicalFilterNode)rewritten;
        Assert.AreNotSame(filter, rewritten);
        Assert.AreSame(replacement, rewrittenFilter.Input);
        Assert.AreSame(predicate, rewrittenFilter.Predicate);
    }

    [TestMethod]
    public void RewriteChildren_WhenBinaryChildChanges_ShouldRebuildJoinWithUnchangedSibling()
    {
        var left = CreateScan("left");
        var right = CreateScan("right", ("UserId", typeof(int)));
        var replacement = CreateScan("replacement", ("UserId", typeof(int)));
        var buildKey = new ColumnRef("left", "Id", typeof(int));
        var probeKey = new ColumnRef("right", "UserId", typeof(int));
        var join = new PhysicalHashJoinNode(JoinKind.Inner, [buildKey], [probeKey], null, left, right);

        var rewritten = PhysicalPlanRewriter.RewriteChildren(
            join,
            node => ReferenceEquals(node, right) ? replacement : node);

        var rewrittenJoin = (PhysicalHashJoinNode)rewritten;
        Assert.AreNotSame(join, rewritten);
        Assert.AreSame(left, rewrittenJoin.Left);
        Assert.AreSame(replacement, rewrittenJoin.Right);
        Assert.AreSame(buildKey, rewrittenJoin.BuildKeys[0]);
        Assert.AreSame(probeKey, rewrittenJoin.ProbeKeys[0]);
    }

    [TestMethod]
    public void RewriteChildren_WhenCteDefinitionChanges_ShouldRebuildCteAndKeepQuery()
    {
        var definitionPlan = CreateScan("definition");
        var replacement = CreateScan("replacement");
        var query = new PhysicalCteRefNode("items", "i", CreateSchema(("Id", typeof(int))));
        var cte = new PhysicalCteNode(
            [new PhysicalCteDefinition("items", definitionPlan)],
            query);

        var rewritten = PhysicalPlanRewriter.RewriteChildren(
            cte,
            node => ReferenceEquals(node, definitionPlan) ? replacement : node);

        var rewrittenCte = (PhysicalCteNode)rewritten;
        Assert.AreNotSame(cte, rewritten);
        Assert.AreEqual("items", rewrittenCte.Definitions[0].Name);
        Assert.AreSame(replacement, rewrittenCte.Definitions[0].Plan);
        Assert.AreSame(query, rewrittenCte.Query);
    }

    [TestMethod]
    public void RewriteChildren_WhenMultiStatementChanges_ShouldRebuildMultiStatement()
    {
        var first = CreateScan("first");
        var second = CreateScan("second");
        var replacement = CreateScan("replacement");
        var multiStatement = new PhysicalMultiStatementNode([first, second]);

        var rewritten = PhysicalPlanRewriter.RewriteChildren(
            multiStatement,
            node => ReferenceEquals(node, second) ? replacement : node);

        var rewrittenMultiStatement = (PhysicalMultiStatementNode)rewritten;
        Assert.AreNotSame(multiStatement, rewritten);
        Assert.AreSame(first, rewrittenMultiStatement.Statements[0]);
        Assert.AreSame(replacement, rewrittenMultiStatement.Statements[1]);
    }

    [TestMethod]
    public void TryResolveDirectSchemaScan_WhenInputIsNonDistinctProjectOverScan_ShouldReturnScan()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Id", new ColumnRef("t", "Id", typeof(int)), 0)],
            scan);

        var resolved = PhysicalPlanRewriter.TryResolveDirectSchemaScan(project, out var resolvedScan);

        Assert.IsTrue(resolved);
        Assert.AreSame(scan, resolvedScan);
    }

    private static OutputSchema CreateSchema(params (string Name, Type Type)[] columns)
    {
        var schemas = new ColumnSchema[columns.Length];

        for (var index = 0; index < columns.Length; index++)
            schemas[index] = new ColumnSchema(columns[index].Name, columns[index].Type, index);

        return new OutputSchema(schemas);
    }

    private static PhysicalSchemaScanNode CreateScan(
        string alias = "t",
        params (string Name, Type Type)[] columns)
    {
        if (columns.Length == 0)
            columns = [("Id", typeof(int))];

        return new PhysicalSchemaScanNode("test", "data", [], alias, [], [], CreateSchema(columns));
    }

    private static IrExpression CreatePredicate()
    {
        return new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("t", "Id", typeof(int)),
            new Literal(0, typeof(int)),
            typeof(bool));
    }
}
