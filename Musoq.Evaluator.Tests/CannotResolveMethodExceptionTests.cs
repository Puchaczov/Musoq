using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser;
using System;
using System.Linq;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CannotResolveMethodExceptionTests
{
    private class MockNode : Node
    {
        private readonly Type _returnType;

        public MockNode(Type returnType) : base()
        {
            _returnType = returnType;
        }

        public override Type ReturnType => _returnType;
        public override string Id => $"mock_{_returnType?.Name ?? "null"}";
        public override void Accept(IExpressionVisitor visitor) { /* Mock implementation */ }
        public override string ToString() => $"MockNode({_returnType?.Name})";
    }

    [TestMethod]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var message = "Method resolution failed";
        var methodName = "TestMethod";
        var argumentTypes = new[] { "String", "Int32" };
        var availableSignatures = new[] { "TestMethod(String)", "TestMethod(Int32, String)" };

        // Act
        var exception = new CannotResolveMethodException(message, methodName, argumentTypes, availableSignatures);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.AreEqual(2, exception.ArgumentTypes.Length);
        Assert.IsTrue(exception.ArgumentTypes.Contains("String"));
        Assert.IsTrue(exception.ArgumentTypes.Contains("Int32"));
        Assert.AreEqual(2, exception.AvailableSignatures.Length);
    }

    [TestMethod]
    public void Constructor_WithMinimalParameters_ShouldUseDefaults()
    {
        // Arrange
        var message = "Simple error";

        // Act
        var exception = new CannotResolveMethodException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(string.Empty, exception.MethodName);
        Assert.AreEqual(0, exception.ArgumentTypes.Length);
        Assert.AreEqual(0, exception.AvailableSignatures.Length);
    }

    [TestMethod]
    public void CreateForNullArguments_ShouldCreateAppropriateException()
    {
        // Arrange
        var methodName = "TestMethod";

        // Act
        var exception = CannotResolveMethodException.CreateForNullArguments(methodName);

        // Assert
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.IsTrue(exception.Message.Contains($"Cannot resolve method '{methodName}'"));
        Assert.IsTrue(exception.Message.Contains("one or more arguments are null"));
        Assert.IsTrue(exception.Message.Contains("column references"));
        Assert.IsTrue(exception.Message.Contains("expressions are valid"));
        Assert.IsTrue(exception.Message.Contains("referenced columns exist"));
    }

    [TestMethod]
    public void CreateForCannotMatchMethodNameOrArguments_WithValidArgs_ShouldCreateAppropriateException()
    {
        // Arrange
        var methodName = "Sum";
        var args = new Node[]
        {
            new MockNode(typeof(int)),
            new MockNode(typeof(string))
        };
        var availableSignatures = new[] { "Sum(Int32)", "Sum(Decimal)" };

        // Act
        var exception = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(methodName, args, availableSignatures);

        // Assert
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.IsTrue(exception.Message.Contains($"Cannot resolve method '{methodName}'"));
        Assert.IsTrue(exception.Message.Contains("with arguments (System.Int32, System.String)"));
        Assert.IsTrue(exception.Message.Contains("Available method signatures:"));
        Assert.IsTrue(exception.Message.Contains("- Sum(Int32)"));
        Assert.IsTrue(exception.Message.Contains("- Sum(Decimal)"));
        Assert.IsTrue(exception.Message.Contains("Method name spelling"));
        Assert.IsTrue(exception.Message.Contains("Number and types of arguments"));
        Assert.AreEqual(2, exception.ArgumentTypes.Length);
        Assert.AreEqual(2, exception.AvailableSignatures.Length);
    }

    [TestMethod]
    public void CreateForCannotMatchMethodNameOrArguments_WithNullArgs_ShouldCreateAppropriateException()
    {
        // Arrange
        var methodName = "TestMethod";
        Node[] args = null;

        // Act
        var exception = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(methodName, args);

        // Assert
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.IsTrue(exception.Message.Contains("with arguments (no arguments)"));
        Assert.IsTrue(exception.Message.Contains("No matching methods found"));
        Assert.AreEqual(0, exception.ArgumentTypes.Length);
    }

    [TestMethod]
    public void CreateForCannotMatchMethodNameOrArguments_WithEmptyArgs_ShouldCreateAppropriateException()
    {
        // Arrange
        var methodName = "TestMethod";
        var args = new Node[0];

        // Act
        var exception = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(methodName, args);

        // Assert
        Assert.IsTrue(exception.Message.Contains("with arguments (no arguments)"));
        Assert.AreEqual(0, exception.ArgumentTypes.Length);
    }

    [TestMethod]
    public void CreateForCannotMatchMethodNameOrArguments_WithArgsWithNullReturnType_ShouldFilterNulls()
    {
        // Arrange
        var methodName = "TestMethod";
        var args = new Node[]
        {
            new MockNode(typeof(int)),
            new MockNode(null), // This should be filtered out
            new MockNode(typeof(string))
        };

        // Act
        var exception = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(methodName, args);

        // Assert
        Assert.AreEqual(2, exception.ArgumentTypes.Length); // Only non-null types
        Assert.IsTrue(exception.ArgumentTypes.Contains("System.Int32"));
        Assert.IsTrue(exception.ArgumentTypes.Contains("System.String"));
    }

    [TestMethod]
    public void CreateForCannotMatchMethodNameOrArguments_WithNoAvailableSignatures_ShouldShowNoMatchingMethods()
    {
        // Arrange
        var methodName = "UnknownMethod";
        var args = new Node[] { new MockNode(typeof(int)) };

        // Act
        var exception = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(methodName, args);

        // Assert
        Assert.IsTrue(exception.Message.Contains("No matching methods found"));
        Assert.IsFalse(exception.Message.Contains("Available method signatures:"));
    }

    [TestMethod]
    public void CreateForAmbiguousMatch_ShouldCreateAppropriateException()
    {
        // Arrange
        var methodName = "Convert";
        var args = new Node[] { new MockNode(typeof(object)) };
        var matchingSignatures = new[] { "Convert(Object) -> String", "Convert(Object) -> Int32" };

        // Act
        var exception = CannotResolveMethodException.CreateForAmbiguousMatch(methodName, args, matchingSignatures);

        // Assert
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.IsTrue(exception.Message.Contains($"method call '{methodName}(System.Object)' is ambiguous"));
        Assert.IsTrue(exception.Message.Contains("Ambiguous matches:"));
        Assert.IsTrue(exception.Message.Contains("- Convert(Object) -> String"));
        Assert.IsTrue(exception.Message.Contains("- Convert(Object) -> Int32"));
        Assert.IsTrue(exception.Message.Contains("more specific argument types"));
        Assert.IsTrue(exception.Message.Contains("explicit casting"));
        Assert.AreEqual(1, exception.ArgumentTypes.Length);
        Assert.AreEqual(2, exception.AvailableSignatures.Length);
    }

    [TestMethod]
    public void CreateForAmbiguousMatch_WithNullArgs_ShouldHandleGracefully()
    {
        // Arrange
        var methodName = "TestMethod";
        Node[] args = null;
        var matchingSignatures = new[] { "TestMethod()", "TestMethod() -> String" };

        // Act
        var exception = CannotResolveMethodException.CreateForAmbiguousMatch(methodName, args, matchingSignatures);

        // Assert
        Assert.IsTrue(exception.Message.Contains("TestMethod(no arguments)"));
        Assert.AreEqual(0, exception.ArgumentTypes.Length);
    }

    [TestMethod]
    public void CreateForUnsupportedOperation_ShouldCreateAppropriateException()
    {
        // Arrange
        var methodName = "FileWrite";
        var context = "read-only query context";

        // Act
        var exception = CannotResolveMethodException.CreateForUnsupportedOperation(methodName, context);

        // Assert
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.IsTrue(exception.Message.Contains($"method '{methodName}' is not supported"));
        Assert.IsTrue(exception.Message.Contains($"in {context}"));
        Assert.IsTrue(exception.Message.Contains("not be available in certain query contexts"));
        Assert.IsTrue(exception.Message.Contains("specific data types"));
        Assert.IsTrue(exception.Message.Contains("documentation for supported operations"));
    }

    [TestMethod]
    public void Constructor_WithNullParameters_ShouldUseDefaults()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new CannotResolveMethodException(message, null, null, null);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(string.Empty, exception.MethodName);
        Assert.AreEqual(0, exception.ArgumentTypes.Length);
        Assert.AreEqual(0, exception.AvailableSignatures.Length);
    }
}