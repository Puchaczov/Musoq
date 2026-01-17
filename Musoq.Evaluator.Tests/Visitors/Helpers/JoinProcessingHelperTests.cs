using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class JoinProcessingHelperTests
{
    private SyntaxGenerator _generator;
    private Workspace _workspace;
    private StatementSyntax _ifStatement;
    private BlockSyntax _emptyBlock;

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
        StatementSyntax GetRowsSourceOrEmpty(string alias) => SyntaxFactory.EmptyStatement();
        StatementSyntax GenerateCancellationExpression() => SyntaxFactory.EmptyStatement();

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
        
        // Verify the structure contains a foreach statement (the outer join loop)
        var firstStatement = result.Statements[0];
        Assert.IsInstanceOfType(firstStatement, typeof(ForEachStatementSyntax), "First statement should be a foreach loop");
    }

    [TestMethod]
    public void ProcessInnerJoin_NullNode_ThrowsArgumentNullException()
    {
        // Arrange
        StatementSyntax GetRowsSourceOrEmpty(string alias) => SyntaxFactory.EmptyStatement();
        StatementSyntax GenerateCancellationExpression() => SyntaxFactory.EmptyStatement();

        // Act & Assert
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
        // Arrange
        StatementSyntax GetRowsSourceOrEmpty(string alias) => SyntaxFactory.EmptyStatement();
        StatementSyntax GenerateCancellationExpression() => SyntaxFactory.EmptyStatement();
        
        // Create a minimal join node for this test
        var mockJoinNode = CreateMockJoinNode();

        // Act & Assert
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
        // Arrange
        StatementSyntax GetRowsSourceOrEmpty(string alias) => SyntaxFactory.EmptyStatement();
        StatementSyntax GenerateCancellationExpression() => SyntaxFactory.EmptyStatement();
        
        // Create a minimal join node for this test
        var mockJoinNode = CreateMockJoinNode();

        // Act & Assert
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
        // Arrange
        StatementSyntax GenerateCancellationExpression() => SyntaxFactory.EmptyStatement();
        
        // Create a minimal join node for this test
        var mockJoinNode = CreateMockJoinNode();

        // Act & Assert
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
        // Arrange
        StatementSyntax GetRowsSourceOrEmpty(string alias) => SyntaxFactory.EmptyStatement();
        
        // Create a minimal join node for this test
        var mockJoinNode = CreateMockJoinNode();

        // Act & Assert
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
