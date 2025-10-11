using System;
using Musoq.Evaluator.Exceptions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class SyntaxBinaryOperationHelperTests
{
    private SyntaxGenerator _generator;
    private Stack<SyntaxNode> _nodes;

    [TestInitialize]
    public void Initialize()
    {
        var workspace = new AdhocWorkspace();
        _generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        _nodes = new Stack<SyntaxNode>();
    }

    [TestMethod]
    public void ProcessMultiplyOperation_ShouldCreateMultiplyExpression()
    {
        // Arrange
        var left = _generator.LiteralExpression(5);
        var right = _generator.LiteralExpression(3);
        _nodes.Push(left);
        _nodes.Push(right);

        // Act
        SyntaxBinaryOperationHelper.ProcessMultiplyOperation(_nodes, _generator);

        // Assert
        Assert.AreEqual(1, _nodes.Count);
        var result = _nodes.Pop();
        var code = result.ToString();
        Assert.IsTrue(code.Contains("5") && code.Contains("3") && code.Contains("*"));
    }

    [TestMethod]
    public void ProcessAddOperation_ShouldCreateAddExpression()
    {
        // Arrange
        var left = _generator.LiteralExpression(10);
        var right = _generator.LiteralExpression(20);
        _nodes.Push(left);
        _nodes.Push(right);

        // Act
        SyntaxBinaryOperationHelper.ProcessAddOperation(_nodes, _generator);

        // Assert
        Assert.AreEqual(1, _nodes.Count);
        var result = _nodes.Pop();
        var code = result.ToString();
        Assert.IsTrue(code.Contains("10") && code.Contains("20") && code.Contains("+"));
    }

    [TestMethod]
    public void ProcessLogicalAndOperation_ShouldCreateLogicalAndExpression()
    {
        // Arrange
        var left = _generator.LiteralExpression(true);
        var right = _generator.LiteralExpression(false);
        _nodes.Push(left);
        _nodes.Push(right);

        // Act
        SyntaxBinaryOperationHelper.ProcessLogicalAndOperation(_nodes, _generator);

        // Assert
        Assert.AreEqual(1, _nodes.Count);
        var result = _nodes.Pop();
        var code = result.ToString();
        Assert.IsTrue(code.Contains("true") && code.Contains("false") && code.Contains("&&"));
    }

    [TestMethod]
    public void ProcessValueEqualsOperation_ShouldCreateEqualityExpression()
    {
        // Arrange
        var left = _generator.LiteralExpression("test");
        var right = _generator.LiteralExpression("value");
        _nodes.Push(left);
        _nodes.Push(right);

        // Act
        SyntaxBinaryOperationHelper.ProcessValueEqualsOperation(_nodes, _generator);

        // Assert
        Assert.AreEqual(1, _nodes.Count);
        var result = _nodes.Pop();
        var code = result.ToString();
        Assert.IsTrue(code.Contains("test") && code.Contains("value") && code.Contains("=="));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ProcessMultiplyOperation_WithNullNodes_ShouldThrowArgumentNullException()
    {
        // Act
        SyntaxBinaryOperationHelper.ProcessMultiplyOperation(null, _generator);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ProcessAddOperation_WithNullGenerator_ShouldThrowArgumentNullException()
    {
        // Act
        SyntaxBinaryOperationHelper.ProcessAddOperation(_nodes, null);
    }

    [TestMethod]
    [ExpectedException(typeof(VisitorException))]
    public void ProcessDivideOperation_WithInsufficientNodes_ShouldThrowInvalidOperationException()
    {
        // Arrange - only one node in stack
        _nodes.Push(_generator.LiteralExpression(10));

        // Act
        SyntaxBinaryOperationHelper.ProcessDivideOperation(_nodes, _generator);
    }

    [TestMethod]
    [ExpectedException(typeof(VisitorException))]
    public void ProcessSubtractOperation_WithEmptyStack_ShouldThrowInvalidOperationException()
    {
        // Act
        SyntaxBinaryOperationHelper.ProcessSubtractOperation(_nodes, _generator);
    }

    [TestMethod]
    public void ProcessGreaterThanOperation_ShouldCreateGreaterThanExpression()
    {
        // Arrange
        var left = _generator.LiteralExpression(10);
        var right = _generator.LiteralExpression(5);
        _nodes.Push(left);
        _nodes.Push(right);

        // Act
        SyntaxBinaryOperationHelper.ProcessGreaterThanOperation(_nodes, _generator);

        // Assert
        Assert.AreEqual(1, _nodes.Count);
        var result = _nodes.Pop();
        var code = result.ToString();
        Assert.IsTrue(code.Contains("10") && code.Contains("5") && code.Contains(">"));
    }

    [TestMethod]
    public void ProcessLessThanOrEqualOperation_ShouldCreateLessThanOrEqualExpression()
    {
        // Arrange
        var left = _generator.LiteralExpression(5);
        var right = _generator.LiteralExpression(10);
        _nodes.Push(left);
        _nodes.Push(right);

        // Act
        SyntaxBinaryOperationHelper.ProcessLessThanOrEqualOperation(_nodes, _generator);

        // Assert
        Assert.AreEqual(1, _nodes.Count);
        var result = _nodes.Pop();
        var code = result.ToString();
        Assert.IsTrue(code.Contains("5") && code.Contains("10") && code.Contains("<="));
    }
}