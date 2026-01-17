using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class AccessObjectArrayNodeProcessorTests
{
    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithNullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var nodes = new Stack<SyntaxNode>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(null, nodes));
    }

    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithNullNodes_ThrowsArgumentNullException()
    {
        // Arrange
        var node = CreateAccessObjectArrayNode("test", 0, typeof(string), true);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, null));
    }

    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithColumnAccess_StringType_GeneratesStringCharacterAccess()
    {
        // Arrange
        var node = CreateAccessObjectArrayNode("testColumn", 2, typeof(string), true);
        var nodes = new Stack<SyntaxNode>();

        // Act
        var result = AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, nodes);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Expression);
        Assert.AreEqual("Musoq.Evaluator.Helpers", result.RequiredNamespace);

        var expressionString = result.Expression.ToString();
        Console.WriteLine($"Expression: {expressionString}");
        Assert.Contains("SafeArrayAccess.GetStringCharacter", expressionString);
        Assert.Contains("2", expressionString);
    }

    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithColumnAccess_IntArrayType_GeneratesArrayElementAccess()
    {
        // Arrange
        var node = CreateAccessObjectArrayNode("intArray", 1, typeof(int[]), true);
        var nodes = new Stack<SyntaxNode>();

        // Act
        var result = AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, nodes);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Expression);

        var expressionString = result.Expression.ToString();
        Console.WriteLine($"Expression: {expressionString}");
        // Update assertions based on actual output
        Assert.Contains("SafeArrayAccess.GetArrayElement", expressionString);
        Assert.Contains("1", expressionString);
    }

    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithColumnAccess_DoubleArrayType_GeneratesArrayElementAccess()
    {
        // Arrange
        var node = CreateAccessObjectArrayNode("doubleArray", 3, typeof(double[]), true);
        var nodes = new Stack<SyntaxNode>();

        // Act
        var result = AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, nodes);

        // Assert
        Assert.IsNotNull(result);
        var expressionString = result.Expression.ToString();
        Console.WriteLine($"Expression: {expressionString}");
        Assert.Contains("SafeArrayAccess.GetArrayElement", expressionString);
        Assert.Contains("3", expressionString);
    }

    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithColumnAccess_ObjectArrayType_GeneratesArrayElementAccess()
    {
        // Arrange
        var node = CreateAccessObjectArrayNode("objectArray", 0, typeof(object[]), true);
        var nodes = new Stack<SyntaxNode>();

        // Act
        var result = AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, nodes);

        // Assert
        Assert.IsNotNull(result);
        var expressionString = result.Expression.ToString();
        Console.WriteLine($"Expression: {expressionString}");
        Assert.Contains("SafeArrayAccess.GetArrayElement", expressionString);
        Assert.Contains("0", expressionString);
    }

    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithColumnAccess_OtherIndexableType_GeneratesDirectElementAccess()
    {
        // Arrange
        var node = CreateAccessObjectArrayNode("listColumn", 5, typeof(List<int>), true);
        var nodes = new Stack<SyntaxNode>();

        // Act
        var result = AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, nodes);

        // Assert
        Assert.IsNotNull(result);
        var expressionString = result.Expression.ToString();
        Console.WriteLine($"Expression: {expressionString}");
        // For non-array types, we expect direct element access
        Assert.Contains("[5]", expressionString);
    }

    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithPropertyAccess_ValidExpression_GeneratesElementAccess()
    {
        // Arrange
        var node = CreateAccessObjectArrayNode("Property", 1, typeof(string), false);
        var nodes = new Stack<SyntaxNode>();
        nodes.Push(SyntaxFactory.IdentifierName("parentObject"));

        // Act
        var result = AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, nodes);

        // Assert
        Assert.IsNotNull(result);
        var expressionString = result.Expression.ToString();
        Assert.Contains("(parentObject).Property[1]", expressionString);
        Assert.IsEmpty(nodes); // Should have popped the expression
    }

    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithPropertyAccess_NoParentExpression_ThrowsInvalidOperationException()
    {
        // Arrange
        var node = CreateAccessObjectArrayNode("Property", 1, typeof(string), false);
        var nodes = new Stack<SyntaxNode>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, nodes));

        Assert.Contains("Cannot generate code for array access", exception.Message);
        Assert.Contains("no parent expression available", exception.Message);
    }

    [TestMethod]
    public void ProcessAccessObjectArrayNode_WithPropertyAccess_NonExpressionOnStack_ThrowsInvalidOperationException()
    {
        // Arrange
        var node = CreateAccessObjectArrayNode("Property", 1, typeof(string), false);
        var nodes = new Stack<SyntaxNode>();
        nodes.Push(SyntaxFactory.Block()); // Non-expression syntax node

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, nodes));

        Assert.Contains("Cannot generate code for array access", exception.Message);
    }

    [TestMethod]
    [DataRow(typeof(string), "string")]
    [DataRow(typeof(int), "int")]
    [DataRow(typeof(double), "double")]
    [DataRow(typeof(bool), "bool")]
    [DataRow(typeof(decimal), "decimal")]
    [DataRow(typeof(long), "long")]
    [DataRow(typeof(object), "object")]
    public void GetCSharpType_WithPrimitiveTypes_ReturnsCorrectSyntax(Type inputType, string expectedKeyword)
    {
        // Act
        var result = AccessObjectArrayNodeProcessor.GetCSharpType(inputType);

        // Assert
        Assert.IsNotNull(result);
        if (result is PredefinedTypeSyntax predefinedType)
            Assert.AreEqual(expectedKeyword, predefinedType.Keyword.ValueText);
        else
            Assert.Fail($"Expected PredefinedTypeSyntax for {inputType.Name}, got {result.GetType().Name}");
    }

    [TestMethod]
    public void GetCSharpType_WithComplexType_ReturnsIdentifierName()
    {
        // Arrange
        var complexType = typeof(DateTime);

        // Act
        var result = AccessObjectArrayNodeProcessor.GetCSharpType(complexType);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IdentifierNameSyntax));
        var identifierName = (IdentifierNameSyntax)result;
        Assert.AreEqual("DateTime", identifierName.Identifier.ValueText);
    }

    [TestMethod]
    public void GetCSharpType_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccessObjectArrayNodeProcessor.GetCSharpType(null));
    }

    [TestMethod]
    public void AccessObjectArrayProcessingResult_Constructor_WithValidArguments_SetsProperties()
    {
        // Arrange
        var expression = SyntaxFactory.IdentifierName("test");
        var namespaceName = "TestNamespace";

        // Act
        var result = new AccessObjectArrayNodeProcessor.AccessObjectArrayProcessingResult(expression, namespaceName);

        // Assert
        Assert.AreEqual(expression, result.Expression);
        Assert.AreEqual(namespaceName, result.RequiredNamespace);
    }

    [TestMethod]
    public void AccessObjectArrayProcessingResult_Constructor_WithNullExpression_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AccessObjectArrayNodeProcessor.AccessObjectArrayProcessingResult(null, "namespace"));
    }

    [TestMethod]
    public void AccessObjectArrayProcessingResult_Constructor_WithNullNamespace_ThrowsArgumentNullException()
    {
        // Arrange
        var expression = SyntaxFactory.IdentifierName("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AccessObjectArrayNodeProcessor.AccessObjectArrayProcessingResult(expression, null));
    }

    /// <summary>
    ///     Helper method to create AccessObjectArrayNode for testing.
    /// </summary>
    private static AccessObjectArrayNode CreateAccessObjectArrayNode(string objectName, int index, Type columnType,
        bool isColumnAccess)
    {
        var token = new NumericAccessToken(objectName, index.ToString(), new TextSpan(0, index.ToString().Length));
        if (isColumnAccess) return new AccessObjectArrayNode(token, columnType);

        return new AccessObjectArrayNode(token);
    }
}