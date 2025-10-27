using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Musoq.Converter.Exceptions;
using System;
using System.Linq;

namespace Musoq.Converter.Tests;

[TestClass]
public class CompilationExceptionTests
{
    [TestMethod]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var message = "Compilation failed";
        var generatedCode = "public class Test { }";
        var diagnostics = new Diagnostic[0];
        var queryContext = "SELECT * FROM test";

        // Act
        var exception = new CompilationException(message, generatedCode, diagnostics, queryContext);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(generatedCode, exception.GeneratedCode);
        Assert.AreEqual(queryContext, exception.QueryContext);
        Assert.IsNotNull(exception.CompilationErrors);
    }

    [TestMethod]
    public void Constructor_WithMinimalParameters_ShouldUseDefaults()
    {
        // Arrange
        var message = "Simple error";

        // Act
        var exception = new CompilationException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(string.Empty, exception.GeneratedCode);
        Assert.AreEqual(string.Empty, exception.QueryContext);
        Assert.IsNotNull(exception.CompilationErrors);
        Assert.AreEqual(0, exception.CompilationErrors.Count());
    }

    [TestMethod]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var message = "Compilation error";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new CompilationException(message, innerException);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(innerException, exception.InnerException);
    }

    [TestMethod]
    public void ForAssemblyLoadFailure_ShouldCreateAppropriateException()
    {
        // Arrange
        var innerException = new System.IO.FileLoadException("Could not load assembly");
        var queryContext = "SELECT id FROM users";

        // Act
        var exception = CompilationException.ForAssemblyLoadFailure(innerException, queryContext);

        // Assert
        Assert.AreEqual(innerException, exception.InnerException);
        Assert.AreEqual(queryContext, exception.QueryContext);
        Assert.IsTrue(exception.Message.Contains("compiled query assembly could not be loaded"));
        Assert.IsTrue(exception.Message.Contains("missing dependencies"));
        Assert.IsTrue(exception.Message.Contains("incompatible assembly references"));
        Assert.IsTrue(exception.Message.Contains("data source plugins are properly installed"));
    }

    [TestMethod]
    public void ForTypeResolutionFailure_ShouldCreateAppropriateException()
    {
        // Arrange
        var typeName = "Query_12345";
        var queryContext = "SELECT name FROM products";

        // Act
        var exception = CompilationException.ForTypeResolutionFailure(typeName, queryContext);

        // Assert
        Assert.AreEqual(queryContext, exception.QueryContext);
        Assert.IsTrue(exception.Message.Contains($"Could not resolve type '{typeName}'"));
        Assert.IsTrue(exception.Message.Contains("problem with code generation"));
        Assert.IsTrue(exception.Message.Contains("missing references"));
        Assert.IsTrue(exception.Message.Contains("query syntax"));
        Assert.IsTrue(exception.Message.Contains("schemas are registered"));
    }

    [TestMethod]
    public void ForAssemblyLoadFailure_WithDefaultContext_ShouldUseUnknown()
    {
        // Arrange
        var innerException = new Exception("Load error");

        // Act
        var exception = CompilationException.ForAssemblyLoadFailure(innerException);

        // Assert
        Assert.AreEqual("Unknown", exception.QueryContext);
    }

    [TestMethod]
    public void ForTypeResolutionFailure_WithDefaultContext_ShouldUseUnknown()
    {
        // Arrange
        var typeName = "TestType";

        // Act
        var exception = CompilationException.ForTypeResolutionFailure(typeName);

        // Assert
        Assert.AreEqual("Unknown", exception.QueryContext);
    }

    [TestMethod]
    public void Constructor_WithNullParameters_ShouldHandleGracefully()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new CompilationException(message, null, null, null);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(string.Empty, exception.GeneratedCode);
        Assert.AreEqual(string.Empty, exception.QueryContext);
        Assert.IsNotNull(exception.CompilationErrors);
        Assert.AreEqual(0, exception.CompilationErrors.Count());
    }

    [TestMethod]
    public void Constructor_WithNullParametersAndInnerException_ShouldHandleGracefully()
    {
        // Arrange
        var message = "Test message";
        var innerException = new Exception("Inner");

        // Act
        var exception = new CompilationException(message, innerException, null, null, null);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(innerException, exception.InnerException);
        Assert.AreEqual(string.Empty, exception.GeneratedCode);
        Assert.AreEqual(string.Empty, exception.QueryContext);
        Assert.IsNotNull(exception.CompilationErrors);
        Assert.AreEqual(0, exception.CompilationErrors.Count());
    }
}