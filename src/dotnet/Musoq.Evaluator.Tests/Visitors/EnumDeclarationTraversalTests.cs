using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors;

[TestClass]
public sealed class EnumDeclarationTraversalTests
{
    [TestMethod]
    public void EnumDeclaration_ShouldParticipateInCanonicalTraversalAndCloning()
    {
        const string query = "enum State : byte { Ready = 1ub, Done = 2ub }; select 1 from #schema.rows() r";
        var root = new Musoq.Parser.Parser(new Lexer(query, true)).ComposeAll();
        var visitor = new CollectingVisitor();

        root.Accept(new CollectingTraverser(visitor));

        CollectionAssert.AreEqual(new[] { "Ready", "Done" }, visitor.MemberNames.ToArray());
        CollectionAssert.AreEqual(new[] { "State" }, visitor.EnumNames.ToArray());

        var cloneVisitor = new CloneQueryVisitor();
        root.Accept(new CloneTraverseVisitor(cloneVisitor));
        var clonedStatements = (StatementsArrayNode)cloneVisitor.Root.Expression;
        var clonedDeclaration = (EnumDeclarationNode)clonedStatements.Statements[0].Node;

        Assert.AreNotSame(((StatementsArrayNode)root.Expression).Statements[0].Node, clonedDeclaration);
        Assert.AreEqual("State", clonedDeclaration.Name);
        CollectionAssert.AreEqual(
            new ulong[] { 1, 2 },
            clonedDeclaration.Members.Select(static member => member.RawValue).ToArray());
    }

    private sealed class CollectingVisitor : NoOpExpressionVisitor
    {
        public List<string> EnumNames { get; } = [];

        public List<string> MemberNames { get; } = [];

        public override void Visit(EnumDeclarationNode node)
        {
            EnumNames.Add(node.Name);
        }

        public override void Visit(EnumMemberNode node)
        {
            MemberNames.Add(node.Name);
        }
    }

    private sealed class CollectingTraverser(CollectingVisitor visitor)
        : RawTraverseVisitor<CollectingVisitor>(visitor);
}
