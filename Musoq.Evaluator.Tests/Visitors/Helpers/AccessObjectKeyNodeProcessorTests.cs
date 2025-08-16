using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class AccessObjectKeyNodeProcessorTests
{
    [TestMethod]
    public void ProcessAccessObjectKeyNode_WithValidNode_GeneratesCorrectSyntax()
    {
        // Arrange
        var keyToken = new KeyAccessToken("obj", "key", new TextSpan(0, 3));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();
        var expression = SyntaxFactory.IdentifierName("testObj");
        nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Expression);
        Assert.AreEqual(typeof(SafeArrayAccess).Namespace, result.RequiredNamespace);
        
        // Verify the generated syntax contains SafeArrayAccess.GetIndexedElement
        var syntaxText = result.Expression.ToString();
        Assert.IsTrue(syntaxText.Contains("SafeArrayAccess.GetIndexedElement"));
        Assert.IsTrue(syntaxText.Contains("\"key\""));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_WithDifferentKeyValue_GeneratesCorrectKey()
    {
        // Arrange
        var keyToken = new KeyAccessToken("data", "customKey", new TextSpan(0, 4));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();
        var expression = SyntaxFactory.IdentifierName("data");
        nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);

        // Assert
        var syntaxText = result.Expression.ToString();
        Assert.IsTrue(syntaxText.Contains("\"customKey\""));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_WithComplexExpression_HandlesCorrectly()
    {
        // Arrange
        var keyToken = new KeyAccessToken("obj", "prop", new TextSpan(0, 3));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();
        var complexExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("source"),
            SyntaxFactory.IdentifierName("data"));
        nodes.Push(complexExpression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);

        // Assert
        Assert.IsNotNull(result.Expression);
        var syntaxText = result.Expression.ToString();
        Assert.IsTrue(syntaxText.Contains("SafeArrayAccess.GetIndexedElement"));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_ConsumesNodeFromStack()
    {
        // Arrange
        var keyToken = new KeyAccessToken("obj", "key", new TextSpan(0, 3));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();
        var expression = SyntaxFactory.IdentifierName("testObj");
        nodes.Push(expression);
        var initialCount = nodes.Count;

        // Act
        AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);

        // Assert
        Assert.AreEqual(initialCount - 1, nodes.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ProcessAccessObjectKeyNode_WithNullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var nodes = new Stack<SyntaxNode>();
        nodes.Push(SyntaxFactory.IdentifierName("test"));

        // Act & Assert
        AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(null, nodes);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ProcessAccessObjectKeyNode_WithNullNodes_ThrowsArgumentNullException()
    {
        // Arrange
        var keyToken = new KeyAccessToken("obj", "key", new TextSpan(0, 3));
        var node = new AccessObjectKeyNode(keyToken);

        // Act & Assert
        AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, null);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ProcessAccessObjectKeyNode_WithEmptyStack_ThrowsInvalidOperationException()
    {
        // Arrange
        var keyToken = new KeyAccessToken("obj", "key", new TextSpan(0, 3));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();

        // Act & Assert
        AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_GeneratesObjectKeywordTypeOf()
    {
        // Arrange
        var keyToken = new KeyAccessToken("obj", "key", new TextSpan(0, 3));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();
        var expression = SyntaxFactory.IdentifierName("testObj");
        nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);

        // Assert
        var syntaxText = result.Expression.ToString();
        Assert.IsTrue(syntaxText.Contains("typeof(object)"));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_GeneratesMemberAccessWithNodeName()
    {
        // Arrange
        var keyToken = new KeyAccessToken("dataSource", "id", new TextSpan(0, 10));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();
        var expression = SyntaxFactory.IdentifierName("record");
        nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);

        // Assert
        var syntaxText = result.Expression.ToString();
        // Should contain the node name in the member access
        Assert.IsTrue(syntaxText.Contains("dataSource"));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_WithSpecialCharactersInKey_HandlesCorrectly()
    {
        // Arrange
        var keyToken = new KeyAccessToken("obj", "key-with-dash", new TextSpan(0, 3));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();
        var expression = SyntaxFactory.IdentifierName("testObj");
        nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);

        // Assert
        var syntaxText = result.Expression.ToString();
        Assert.IsTrue(syntaxText.Contains("\"key-with-dash\""));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_ReturnsNonNullRequiredNamespace()
    {
        // Arrange
        var keyToken = new KeyAccessToken("obj", "key", new TextSpan(0, 3));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();
        var expression = SyntaxFactory.IdentifierName("testObj");
        nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);

        // Assert
        Assert.IsNotNull(result.RequiredNamespace);
        Assert.IsTrue(result.RequiredNamespace.Length > 0);
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_GeneratesValidCSharpExpression()
    {
        // Arrange
        var keyToken = new KeyAccessToken("obj", "key", new TextSpan(0, 3));
        var node = new AccessObjectKeyNode(keyToken);
        var nodes = new Stack<SyntaxNode>();
        var expression = SyntaxFactory.IdentifierName("testObj");
        nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, nodes);

        // Assert
        // Verify the generated syntax is valid by checking it has proper structure
        Assert.IsInstanceOfType(result.Expression, typeof(InvocationExpressionSyntax));
        var invocation = (InvocationExpressionSyntax)result.Expression;
        Assert.IsNotNull(invocation.ArgumentList);
        Assert.AreEqual(3, invocation.ArgumentList.Arguments.Count);
    }
}