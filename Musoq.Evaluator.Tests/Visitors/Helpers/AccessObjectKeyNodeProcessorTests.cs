using System;
using System.Collections.Generic;
using System.Linq;
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
    private AccessObjectKeyNode _accessObjectKeyNode = null!;
    private Stack<SyntaxNode> _nodes = null!;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        _nodes = new Stack<SyntaxNode>();
        var token = new KeyAccessToken("TestProperty", "TestKey", TextSpan.Empty);
        _accessObjectKeyNode = new AccessObjectKeyNode(token);
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_ValidInput_ReturnsCorrectExpression()
    {
        // Arrange
        var expression = SyntaxFactory.IdentifierName("testObject");
        _nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(_accessObjectKeyNode, _nodes);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Expression);
        Assert.AreEqual(typeof(SafeArrayAccess).Namespace, result.RequiredNamespace);


        var invocation = result.Expression as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);


        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        Assert.IsNotNull(memberAccess);
        Assert.AreEqual("SafeArrayAccess", ((IdentifierNameSyntax)memberAccess.Expression).Identifier.Text);
        Assert.AreEqual("GetIndexedElement", memberAccess.Name.Identifier.Text);
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_ValidInput_GeneratesCorrectArguments()
    {
        // Arrange
        var expression = SyntaxFactory.IdentifierName("testObject");
        _nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(_accessObjectKeyNode, _nodes);

        // Assert
        var invocation = result.Expression as InvocationExpressionSyntax;
        var arguments = invocation?.ArgumentList.Arguments;

        Assert.IsNotNull(arguments);
        Assert.AreEqual(3, arguments.Value.Count);


        var firstArg = arguments.Value[0].Expression as MemberAccessExpressionSyntax;
        Assert.IsNotNull(firstArg);
        Assert.AreEqual("TestProperty", firstArg.Name.Identifier.Text);


        var secondArg = arguments.Value[1].Expression as LiteralExpressionSyntax;
        Assert.IsNotNull(secondArg);
        Assert.AreEqual("\"TestKey\"", secondArg.Token.ToString());


        var thirdArg = arguments.Value[2].Expression as TypeOfExpressionSyntax;
        Assert.IsNotNull(thirdArg);
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_WithComplexExpression_HandlesCorrectly()
    {
        var complexExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("parent"),
            SyntaxFactory.IdentifierName("child"));
        _nodes.Push(complexExpression);


        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(_accessObjectKeyNode, _nodes);


        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Expression);

        var invocation = result.Expression as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);


        var firstArg = invocation.ArgumentList.Arguments[0].Expression as MemberAccessExpressionSyntax;
        Assert.IsNotNull(firstArg);

        var parenthesizedExp = firstArg.Expression as ParenthesizedExpressionSyntax;
        Assert.IsNotNull(parenthesizedExp);
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_WithSpecialCharacterKey_HandlesCorrectly()
    {
        // Arrange
        var token = new KeyAccessToken("Property", "Special@Key#123", TextSpan.Empty);
        var specialKeyNode = new AccessObjectKeyNode(token);
        var expression = SyntaxFactory.IdentifierName("obj");
        _nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(specialKeyNode, _nodes);

        // Assert
        var invocation = result.Expression as InvocationExpressionSyntax;
        var secondArg = invocation?.ArgumentList.Arguments[1].Expression as LiteralExpressionSyntax;

        Assert.IsNotNull(secondArg);
        Assert.AreEqual("\"Special@Key#123\"", secondArg.Token.ToString());
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_NullNode_ThrowsArgumentNullException()
    {
        var expression = SyntaxFactory.IdentifierName("test");
        _nodes.Push(expression);


        Assert.Throws<ArgumentNullException>(() =>
            AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(null!, _nodes));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_NullNodes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(_accessObjectKeyNode, null!));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_EmptyNodesStack_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(_accessObjectKeyNode, _nodes));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_RequiredNamespace_ReturnsCorrectNamespace()
    {
        var expression = SyntaxFactory.IdentifierName("test");
        _nodes.Push(expression);


        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(_accessObjectKeyNode, _nodes);


        Assert.AreEqual(typeof(SafeArrayAccess).Namespace, result.RequiredNamespace);
        Assert.IsFalse(string.IsNullOrEmpty(result.RequiredNamespace));
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_NodesStackPop_RemovesOneElement()
    {
        var expression1 = SyntaxFactory.IdentifierName("test1");
        var expression2 = SyntaxFactory.IdentifierName("test2");
        _nodes.Push(expression1);
        _nodes.Push(expression2);
        var initialCount = _nodes.Count;


        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(_accessObjectKeyNode, _nodes);


        Assert.HasCount(initialCount - 1, _nodes);
        Assert.AreEqual(expression1, _nodes.Peek());
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_EmptyPropertyName_HandlesCorrectly()
    {
        // Arrange
        var token = new KeyAccessToken("", "TestKey", TextSpan.Empty);
        var nodeWithEmptyProperty = new AccessObjectKeyNode(token);
        var expression = SyntaxFactory.IdentifierName("test");
        _nodes.Push(expression);

        // Act
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(nodeWithEmptyProperty, _nodes);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Expression);

        var invocation = result.Expression as InvocationExpressionSyntax;
        var firstArg = invocation?.ArgumentList.Arguments[0].Expression as MemberAccessExpressionSyntax;

        Assert.IsNotNull(firstArg);
        Assert.AreEqual("", firstArg.Name.Identifier.Text);
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_LongPropertyName_HandlesCorrectly()
    {
        var longPropertyName = "VeryLongPropertyNameThatExceedsNormalLengthLimits";
        var token = new KeyAccessToken(longPropertyName, "TestKey", TextSpan.Empty);
        var nodeWithLongProperty = new AccessObjectKeyNode(token);
        var expression = SyntaxFactory.IdentifierName("test");
        _nodes.Push(expression);


        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(nodeWithLongProperty, _nodes);


        var invocation = result.Expression as InvocationExpressionSyntax;
        var firstArg = invocation?.ArgumentList.Arguments[0].Expression as MemberAccessExpressionSyntax;

        Assert.IsNotNull(firstArg);
        Assert.AreEqual(longPropertyName, firstArg.Name.Identifier.Text);
    }

    [TestMethod]
    public void ProcessAccessObjectKeyNode_GeneratedCode_IsValidSyntax()
    {
        var expression = SyntaxFactory.IdentifierName("myObject");
        _nodes.Push(expression);


        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(_accessObjectKeyNode, _nodes);


        var syntaxTree = SyntaxFactory.SyntaxTree(result.Expression);
        var diagnostics = syntaxTree.GetDiagnostics(TestContext.CancellationToken);

        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));
    }
}
