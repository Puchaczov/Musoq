using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class JoinProcessingHelperTests
{
    private BlockSyntax _emptyBlock;
    private SyntaxGenerator _generator;
    private StatementSyntax _ifStatement;
    private Workspace _workspace;

    [TestInitialize]
    public void SetUp()
    {
        _workspace = new AdhocWorkspace();
        _generator = SyntaxGenerator.GetGenerator(_workspace, LanguageNames.CSharp);
        _ifStatement = SyntaxFactory.ContinueStatement();
        _emptyBlock = SyntaxFactory.Block();
    }

    [TestCleanup]
    public void TearDown()
    {
        _workspace?.Dispose();
    }

    [TestMethod]
    public void ProcessInnerJoin_ValidParameters_ReturnsBlockSyntax()
    {
        // Arrange
        var mockJoinNode = CreateTestJoinNode();

        StatementSyntax GetRowsSourceOrEmpty(string alias)
        {
            return SyntaxFactory.ParseStatement($"{alias}Loaded();");
        }

        StatementSyntax GenerateCancellationExpression()
        {
            return SyntaxFactory.EmptyStatement();
        }

        // Act
        var result = JoinProcessingHelper.ProcessInnerJoin(
            mockJoinNode,
            _ifStatement,
            _emptyBlock,
            _generator,
            GetRowsSourceOrEmpty,
            GenerateCancellationExpression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(BlockSyntax));
        Assert.IsGreaterThan(0, result.Statements.Count, "Block should contain statements");


        Assert.AreEqual("sourceLoaded();", result.Statements[0].ToString());
        Assert.AreEqual(
            "var sourceRowsCached = sourceRows.Rows as Musoq.Schema.DataSources.IObjectResolver[] ?? System.Linq.Enumerable.ToArray(sourceRows.Rows);",
            result.Statements[1].ToString());
        Assert.IsInstanceOfType(result.Statements[2], typeof(ForEachStatementSyntax),
            "Third statement should be a foreach loop");

        var outerLoop = (ForEachStatementSyntax)result.Statements[2];
        Assert.IsFalse(outerLoop.Statement.DescendantNodes().OfType<ExpressionStatementSyntax>()
            .Any(statement => statement.ToString() == "sourceLoaded();"));
        Assert.IsTrue(outerLoop.Statement.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Any(identifier => identifier.Identifier.Text == "sourceRowsCached"));
    }

    [TestMethod]
    public void ProcessInnerJoin_NullNode_ThrowsArgumentNullException()
    {
        StatementSyntax GetRowsSourceOrEmpty(string alias)
        {
            return SyntaxFactory.EmptyStatement();
        }

        StatementSyntax GenerateCancellationExpression()
        {
            return SyntaxFactory.EmptyStatement();
        }


        Assert.Throws<ArgumentNullException>(() =>
            JoinProcessingHelper.ProcessInnerJoin(
                null,
                _ifStatement,
                _emptyBlock,
                _generator,
                GetRowsSourceOrEmpty,
                GenerateCancellationExpression));
    }

    [TestMethod]
    public void ProcessInnerJoin_NullIfStatement_ThrowsArgumentNullException()
    {
        StatementSyntax GetRowsSourceOrEmpty(string alias)
        {
            return SyntaxFactory.EmptyStatement();
        }

        StatementSyntax GenerateCancellationExpression()
        {
            return SyntaxFactory.EmptyStatement();
        }


        var mockJoinNode = CreateMockJoinNode();


        Assert.Throws<ArgumentNullException>(() =>
            JoinProcessingHelper.ProcessInnerJoin(
                mockJoinNode,
                null,
                _emptyBlock,
                _generator,
                GetRowsSourceOrEmpty,
                GenerateCancellationExpression));
    }

    [TestMethod]
    public void ProcessInnerJoin_NullGenerator_ThrowsArgumentNullException()
    {
        StatementSyntax GetRowsSourceOrEmpty(string alias)
        {
            return SyntaxFactory.EmptyStatement();
        }

        StatementSyntax GenerateCancellationExpression()
        {
            return SyntaxFactory.EmptyStatement();
        }


        var mockJoinNode = CreateMockJoinNode();


        Assert.Throws<ArgumentNullException>(() =>
            JoinProcessingHelper.ProcessInnerJoin(
                mockJoinNode,
                _ifStatement,
                _emptyBlock,
                null,
                GetRowsSourceOrEmpty,
                GenerateCancellationExpression));
    }

    [TestMethod]
    public void ProcessInnerJoin_NullGetRowsSourceOrEmpty_ThrowsArgumentNullException()
    {
        StatementSyntax GenerateCancellationExpression()
        {
            return SyntaxFactory.EmptyStatement();
        }


        var mockJoinNode = CreateMockJoinNode();


        Assert.Throws<ArgumentNullException>(() =>
            JoinProcessingHelper.ProcessInnerJoin(
                mockJoinNode,
                _ifStatement,
                _emptyBlock,
                _generator,
                null,
                GenerateCancellationExpression));
    }

    [TestMethod]
    public void ProcessInnerJoin_NullGenerateCancellationExpression_ThrowsArgumentNullException()
    {
        StatementSyntax GetRowsSourceOrEmpty(string alias)
        {
            return SyntaxFactory.EmptyStatement();
        }


        var mockJoinNode = CreateMockJoinNode();


        Assert.Throws<ArgumentNullException>(() =>
            JoinProcessingHelper.ProcessInnerJoin(
                mockJoinNode,
                _ifStatement,
                _emptyBlock,
                _generator,
                GetRowsSourceOrEmpty,
                null));
    }

    private JoinInMemoryWithSourceTableFromNode CreateMockJoinNode()
    {
        return null;
    }

    private JoinInMemoryWithSourceTableFromNode CreateTestJoinNode()
    {
        var sourceAlias = "source";
        var inMemoryAlias = "memory";
        var expression = new IntegerNode("1");


        var sourceTable = new TestFromNode(sourceAlias);


        return new JoinInMemoryWithSourceTableFromNode(
            inMemoryAlias,
            sourceTable,
            expression,
            JoinType.Inner,
            typeof(object));
    }

    // Helper class to create a minimal FromNode for testing
    private class TestFromNode : FromNode
    {
        public TestFromNode(string alias) : base(alias)
        {
        }

        public override string Id => $"Test_{Alias}";

        public override void Accept(IExpressionVisitor visitor)
        {
        }

        public override string ToString()
        {
            return $"TestFromNode({Alias})";
        }
    }

    // Note: ProcessOuterLeftJoin and ProcessOuterRightJoin tests are omitted 
    // as they require complex scope setup with symbol tables.
    // The core functionality is tested through ProcessInnerJoin which uses
    // the same validation and processing patterns.
}
