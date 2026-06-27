using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Logical.Rewriting;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class LogicalPlanRewriterTests
{
    [TestMethod]
    public void RewriteChildren_WhenUnaryChildUnchanged_ShouldReturnOriginalNode()
    {
        var scan = CreateScan();
        var filter = new FilterNode(CreatePredicate(), scan);

        var rewritten = LogicalPlanRewriter.RewriteChildren(filter, static node => node);

        Assert.AreSame(filter, rewritten);
    }

    [TestMethod]
    public void RewriteChildren_WhenNestedUnaryChildChanges_ShouldRebuildAffectedAncestors()
    {
        var scan = CreateScan("source");
        var replacement = CreateScan("replacement");
        var predicate = CreatePredicate();
        var filter = new FilterNode(predicate, scan);
        var fields = new[] { new ProjectedField("Id", new ColumnRef("source", "Id", typeof(int)), 0) };
        var project = new ProjectNode(fields, filter) { IsDistinct = true };

        var rewritten = RewriteReplacing(project, scan, replacement);

        var rewrittenProject = (ProjectNode)rewritten;
        var rewrittenFilter = (FilterNode)rewrittenProject.Input;
        Assert.AreNotSame(project, rewrittenProject);
        Assert.AreNotSame(filter, rewrittenFilter);
        Assert.AreSame(fields, rewrittenProject.Fields);
        Assert.IsTrue(rewrittenProject.IsDistinct);
        Assert.AreSame(predicate, rewrittenFilter.Predicate);
        Assert.AreSame(replacement, rewrittenFilter.Input);
    }

    [TestMethod]
    public void RewriteChildren_WhenBinaryDescendantChanges_ShouldKeepUnchangedSibling()
    {
        var left = CreateScan("left");
        var rightScan = CreateScan("right");
        var replacement = CreateScan("replacement");
        var rightFilter = new FilterNode(CreatePredicate("right"), rightScan);
        var join = new JoinNode(JoinKind.Inner, CreatePredicate("left"), left, rightFilter);

        var rewritten = RewriteReplacing(join, rightScan, replacement);

        var rewrittenJoin = (JoinNode)rewritten;
        var rewrittenRightFilter = (FilterNode)rewrittenJoin.Right;
        Assert.AreNotSame(join, rewrittenJoin);
        Assert.AreSame(left, rewrittenJoin.Left);
        Assert.AreNotSame(rightFilter, rewrittenRightFilter);
        Assert.AreSame(replacement, rewrittenRightFilter.Input);
    }

    [TestMethod]
    public void RewriteChildren_WhenCteDefinitionChanges_ShouldRebuildCteAndKeepQuery()
    {
        var definitionPlan = CreateScan("definition");
        var replacement = CreateScan("replacement");
        var query = new CteRefNode("items", "i", CreateSchema(("Id", typeof(int))));
        var cte = new CteNode(
            [new CteDefinition("items", definitionPlan)],
            query);

        var rewritten = RewriteReplacing(cte, definitionPlan, replacement);

        var rewrittenCte = (CteNode)rewritten;
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
        var multiStatement = new MultiStatementNode([first, second]);

        var rewritten = RewriteReplacing(multiStatement, second, replacement);

        var rewrittenMultiStatement = (MultiStatementNode)rewritten;
        Assert.AreNotSame(multiStatement, rewritten);
        Assert.AreSame(first, rewrittenMultiStatement.Statements[0]);
        Assert.AreSame(replacement, rewrittenMultiStatement.Statements[1]);
    }

    [TestMethod]
    public void RewriteProjectedFields_WhenExpressionChanges_ShouldReplaceOnlyChangedField()
    {
        var literal = new Literal(1, typeof(int));
        var replacement = new Literal(2, typeof(int));
        var column = new ColumnRef("t", "Id", typeof(int));
        var fields = new[]
        {
            new ProjectedField("One", literal, 0),
            new ProjectedField("Id", column, 1)
        };

        var rewritten = LogicalPlanRewriter.RewriteProjectedFields(
            fields,
            expression => ReferenceEquals(expression, literal) ? replacement : expression,
            out var changed);

        Assert.IsTrue(changed);
        Assert.AreSame(replacement, rewritten[0].Expression);
        Assert.AreSame(fields[1], rewritten[1]);
    }

    private static LogicalNode RewriteReplacing(
        LogicalNode node,
        LogicalNode target,
        LogicalNode replacement)
    {
        return ReferenceEquals(node, target)
            ? replacement
            : LogicalPlanRewriter.RewriteChildren(
                node,
                child => RewriteReplacing(child, target, replacement));
    }

    private static OutputSchema CreateSchema(params (string Name, Type Type)[] columns)
    {
        var schemas = new ColumnSchema[columns.Length];

        for (var index = 0; index < columns.Length; index++)
            schemas[index] = new ColumnSchema(columns[index].Name, columns[index].Type, index);

        return new OutputSchema(schemas);
    }

    private static SchemaScanNode CreateScan(
        string alias = "t",
        params (string Name, Type Type)[] columns)
    {
        if (columns.Length == 0)
            columns = [("Id", typeof(int))];

        return new SchemaScanNode("test", "data", [], alias, CreateSchema(columns));
    }

    private static IrExpression CreatePredicate(string alias = "t")
    {
        return new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef(alias, "Id", typeof(int)),
            new Literal(0, typeof(int)),
            typeof(bool));
    }
}
