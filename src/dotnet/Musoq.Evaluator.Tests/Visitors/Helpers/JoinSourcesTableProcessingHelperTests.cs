using System;
using System.Linq;
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
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();


        Assert.Throws<ArgumentNullException>(() =>
            JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
                null,
                generator,
                scope,
                "alias",
                ifStatement,
                emptyBlock,
                _ => SyntaxFactory.EmptyStatement(),
                statements => SyntaxFactory.Block(statements),
                () => SyntaxFactory.EmptyStatement(),
                _ => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
    }

    [TestMethod]
    public void ProcessJoinSourcesTable_WithNullGenerator_ThrowsArgumentNullException()
    {
        // Arrange
        var node = CreateMockJoinNode(JoinType.Inner);
        var scope = new Scope(null, 0, "test");
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();


        Assert.Throws<ArgumentNullException>(() =>
            JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
                node,
                null,
                scope,
                "alias",
                ifStatement,
                emptyBlock,
                _ => SyntaxFactory.EmptyStatement(),
                statements => SyntaxFactory.Block(statements),
                () => SyntaxFactory.EmptyStatement(),
                _ => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
    }

    [TestMethod]
    public void ProcessJoinSourcesTable_WithNullScope_ThrowsArgumentNullException()
    {
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var node = CreateMockJoinNode(JoinType.Inner);
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();


        Assert.Throws<ArgumentNullException>(() =>
            JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
                node,
                generator,
                null,
                "alias",
                ifStatement,
                emptyBlock,
                _ => SyntaxFactory.EmptyStatement(),
                statements => SyntaxFactory.Block(statements),
                () => SyntaxFactory.EmptyStatement(),
                _ => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
    }

    [TestMethod]
    public void ProcessJoinSourcesTable_WithUnsupportedJoinType_ThrowsArgumentException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var node = CreateMockJoinNode((JoinType)999);
        var scope = new Scope(null, 0, "test");
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();


        var exception = Assert.Throws<ArgumentException>(() =>
            JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
                node,
                generator,
                scope,
                "alias",
                ifStatement,
                emptyBlock,
                _ => SyntaxFactory.EmptyStatement(),
                statements => SyntaxFactory.Block(statements),
                () => SyntaxFactory.EmptyStatement(),
                _ => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));

        Assert.Contains("Unsupported join type: 999", exception.Message);
    }

    [TestMethod]
    public void ProcessJoinSourcesTable_WithInnerJoin_ReturnsValidBlockSyntax()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var node = CreateMockJoinNode(JoinType.Inner);
        var scope = new Scope(null, 0, "test");
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block());
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
            () => SyntaxFactory.EmptyStatement(),
            _ => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(BlockSyntax));
        Assert.IsGreaterThan(0, result.Statements.Count);
    }

    [TestMethod]
    public void ProcessJoinSourcesTable_WithInnerJoin_LoadsSecondRowsOutsideOuterLoop()
    {
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var node = CreateMockJoinNode(JoinType.Inner);
        var scope = new Scope(null, 0, "test");
        var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block());
        var emptyBlock = SyntaxFactory.Block();

        var result = JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
            node,
            generator,
            scope,
            "alias",
            ifStatement,
            emptyBlock,
            alias => SyntaxFactory.ParseStatement($"{alias}Loaded();"),
            statements => SyntaxFactory.Block(statements),
            () => SyntaxFactory.EmptyStatement(),
            _ => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        Assert.AreEqual("FirstAliasLoaded();", result.Statements[0].ToString());
        Assert.AreEqual("SecondAliasLoaded();", result.Statements[1].ToString());
        Assert.AreEqual("var SecondAliasRowsEnumerable = SecondAliasRows.Rows;", result.Statements[2].ToString());
        Assert.AreEqual(
            "var SecondAliasRowsCached = SecondAliasRowsEnumerable as Musoq.Schema.DataSources.IObjectResolver[] ?? System.Linq.Enumerable.ToArray(SecondAliasRowsEnumerable);",
            result.Statements[3].ToString());

        var outerLoop = result.Statements.OfType<ForEachStatementSyntax>().Single();
        Assert.IsFalse(outerLoop.Statement.DescendantNodes().OfType<ExpressionStatementSyntax>()
            .Any(statement => statement.ToString() == "SecondAliasLoaded();"));
        Assert.IsTrue(outerLoop.Statement.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Any(identifier => identifier.Identifier.Text == "SecondAliasRowsCached"));
    }

    private static JoinSourcesTableFromNode CreateMockJoinNode(JoinType joinType)
    {
        var firstAlias = new AliasedFromNode(
            "testId",
            null,
            "FirstAlias",
            typeof(object),
            0);

        var secondAlias = new AliasedFromNode(
            "testId2",
            null,
            "SecondAlias",
            typeof(object),
            0);


        var expression = new IntegerNode("1", "");

        return new JoinSourcesTableFromNode(
            firstAlias,
            secondAlias,
            expression,
            joinType,
            typeof(object));
    }
}
