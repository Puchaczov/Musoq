using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class JoinSourcesTableProcessingHelperTests
{
    [TestMethod]
    public void ProcessJoinSourcesTable_WithNullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var scope = new Scope(null, 0, "test");
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression), SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
                null,
                generator,
                scope,
                "alias",
                ifStatement,
                emptyBlock,
                _ => SyntaxFactory.EmptyStatement(),
                statements => SyntaxFactory.Block(statements),
                () => SyntaxFactory.EmptyStatement()));
    }

    [TestMethod]
    public void ProcessJoinSourcesTable_WithNullGenerator_ThrowsArgumentNullException()
    {
        // Arrange
        var node = CreateMockJoinNode(JoinType.Inner);
        var scope = new Scope(null, 0, "test");
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression), SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
                node,
                null,
                scope,
                "alias",
                ifStatement,
                emptyBlock,
                _ => SyntaxFactory.EmptyStatement(),
                statements => SyntaxFactory.Block(statements),
                () => SyntaxFactory.EmptyStatement()));
    }

    [TestMethod]
    public void ProcessJoinSourcesTable_WithNullScope_ThrowsArgumentNullException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var node = CreateMockJoinNode(JoinType.Inner);
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression), SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
                node,
                generator,
                null,
                "alias",
                ifStatement,
                emptyBlock,
                _ => SyntaxFactory.EmptyStatement(),
                statements => SyntaxFactory.Block(statements),
                () => SyntaxFactory.EmptyStatement()));
    }

    [TestMethod]
    public void ProcessJoinSourcesTable_WithUnsupportedJoinType_ThrowsArgumentException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var node = CreateMockJoinNode((JoinType)999); // Invalid join type
        var scope = new Scope(null, 0, "test");
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression), SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
                node,
                generator,
                scope,
                "alias",
                ifStatement,
                emptyBlock,
                _ => SyntaxFactory.EmptyStatement(),
                statements => SyntaxFactory.Block(statements),
                () => SyntaxFactory.EmptyStatement()));

        Assert.IsTrue(exception.Message.Contains("Unsupported join type: 999"));
    }

    [TestMethod]
    public void ProcessJoinSourcesTable_WithInnerJoin_ReturnsValidBlockSyntax()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var node = CreateMockJoinNode(JoinType.Inner);
        var scope = new Scope(null, 0, "test");
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression), SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();

        // Act
        var result = JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
            node,
            generator,
            scope,
            "alias",
            ifStatement,
            emptyBlock,
            _ => SyntaxFactory.EmptyStatement(),
            statements => SyntaxFactory.Block(statements),
            () => SyntaxFactory.EmptyStatement());

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(BlockSyntax));
        Assert.IsTrue(result.Statements.Count > 0);
    }

    private static JoinSourcesTableFromNode CreateMockJoinNode(JoinType joinType)
    {
        // Create mock aliases
        var firstAlias = new AliasedFromNode(
            "testId",
            null, // args
            "FirstAlias",
            typeof(object),
            0); // inSourcePosition
        
        var secondAlias = new AliasedFromNode(
            "testId2",
            null, // args
            "SecondAlias",
            typeof(object),
            0); // inSourcePosition

        // Create a simple expression node (we'll use an integer literal)
        var expression = new IntegerNode("1", "");  // empty abbreviation for int

        return new JoinSourcesTableFromNode(
            firstAlias,
            secondAlias,
            expression,
            joinType,
            typeof(object)); // return type
    }
}