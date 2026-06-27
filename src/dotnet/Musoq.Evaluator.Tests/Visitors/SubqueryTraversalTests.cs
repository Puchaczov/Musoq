using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors;

[TestClass]
public sealed class SubqueryTraversalTests
{
    [TestMethod]
    public void RawTraverseVisitor_ShouldVisitInQuerySubqueryBody()
    {
        var root = Parse("select a.City from #A.entities() a where a.City in (select b.City from #B.entities() b)");
        var visitor = new RecordingVisitor();
        var traverser = new RecordingTraverseVisitor(visitor);

        root.Accept(traverser);

        CollectionAssert.AreEquivalent(new[] { "a", "b" }, visitor.SchemaAliases.ToArray());
        Assert.AreEqual(1, visitor.InQueryVisits);
    }

    [TestMethod]
    public void CloneQueryVisitor_ShouldCloneInQuerySubqueryBody()
    {
        var root = Parse("select a.City from #A.entities() a where a.City in (select b.City from #B.entities() b)");
        var originalInQuery = FindSingleInQuery(root);
        var cloneVisitor = new CloneQueryVisitor();
        var cloneTraverser = new CloneTraverseVisitor(cloneVisitor);

        root.Accept(cloneTraverser);

        var clonedInQuery = FindSingleInQuery(cloneVisitor.Root);
        Assert.AreNotSame(originalInQuery, clonedInQuery);
        Assert.AreNotSame(originalInQuery.Subquery, clonedInQuery.Subquery);
        Assert.AreEqual(originalInQuery.Subquery.ToString(), clonedInQuery.Subquery.ToString());
    }

    private static InQueryNode FindSingleInQuery(Node root)
    {
        var visitor = new InQueryFindingVisitor();
        var traverser = new InQueryFindingTraverseVisitor(visitor);

        root.Accept(traverser);

        Assert.HasCount(1, visitor.Nodes);
        return visitor.Nodes[0];
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        return parser.ComposeAll();
    }

    private sealed class RecordingTraverseVisitor(RecordingVisitor visitor)
        : RawTraverseVisitor<RecordingVisitor>(visitor);

    private sealed class RecordingVisitor : NoOpExpressionVisitor
    {
        public List<string> SchemaAliases { get; } = [];

        public int InQueryVisits { get; private set; }

        public override void Visit(SchemaFromNode node)
        {
            SchemaAliases.Add(node.Alias);
        }

        public override void Visit(InQueryNode node)
        {
            InQueryVisits += 1;
        }
    }

    private sealed class InQueryFindingTraverseVisitor(InQueryFindingVisitor visitor)
        : RawTraverseVisitor<InQueryFindingVisitor>(visitor);

    private sealed class InQueryFindingVisitor : NoOpExpressionVisitor
    {
        public List<InQueryNode> Nodes { get; } = [];

        public override void Visit(InQueryNode node)
        {
            Nodes.Add(node);
        }
    }
}
