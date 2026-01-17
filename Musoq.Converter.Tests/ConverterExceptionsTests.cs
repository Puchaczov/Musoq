using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;

namespace Musoq.Converter.Tests;

/// <summary>
///     Tests for Converter exception classes
/// </summary>
[TestClass]
public class ConverterExceptionsTests
{
    #region AstValidationException Tests

    [TestMethod]
    public void AstValidationException_Constructor_WithNodeTypeContextMessage_ShouldSetProperties()
    {
        // Arrange
        var nodeType = "SelectNode";
        var context = "query parsing";
        var message = "Invalid structure detected";

        // Act
        var exception = new AstValidationException(nodeType, context, message);

        // Assert
        Assert.AreEqual(nodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void AstValidationException_Constructor_WithInnerException_ShouldSetAllProperties()
    {
        // Arrange
        var nodeType = "JoinNode";
        var context = "join processing";
        var message = "Failed to process join";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new AstValidationException(nodeType, context, message, innerException);

        // Assert
        Assert.AreEqual(nodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.AreEqual(message, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void AstValidationException_IsInvalidOperationException()
    {
        // Arrange & Act
        var exception = new AstValidationException("Node", "Context", "Message");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(InvalidOperationException));
    }

    [TestMethod]
    public void AstValidationException_ForNullNode_ShouldCreateProperException()
    {
        // Arrange
        var expectedNodeType = "WhereClauseNode";
        var context = "filter processing";

        // Act
        var exception = AstValidationException.ForNullNode(expectedNodeType, context);

        // Assert
        Assert.AreEqual(expectedNodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.Contains(expectedNodeType, exception.Message);
        Assert.Contains(context, exception.Message);
        Assert.Contains("null", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForNullNode_ShouldContainHelpfulMessage()
    {
        // Arrange & Act
        var exception = AstValidationException.ForNullNode("SelectNode", "column selection");

        // Assert
        Assert.Contains("SQL query structure", exception.Message);
        Assert.Contains("check your query syntax", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForInvalidNodeStructure_ShouldCreateProperException()
    {
        // Arrange
        var nodeType = "GroupByNode";
        var context = "aggregation processing";
        var issue = "missing grouping columns";

        // Act
        var exception = AstValidationException.ForInvalidNodeStructure(nodeType, context, issue);

        // Assert
        Assert.AreEqual(nodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.Contains(nodeType, exception.Message);
        Assert.Contains(context, exception.Message);
        Assert.Contains(issue, exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForInvalidNodeStructure_ShouldContainHelpfulMessage()
    {
        // Arrange & Act
        var exception = AstValidationException.ForInvalidNodeStructure("JoinNode", "join", "missing ON clause");

        // Assert
        Assert.Contains("invalid structure", exception.Message);
        Assert.Contains("SQL query", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForUnsupportedNode_ShouldCreateProperException()
    {
        // Arrange
        var nodeType = "PivotNode";
        var context = "query transformation";

        // Act
        var exception = AstValidationException.ForUnsupportedNode(nodeType, context);

        // Assert
        Assert.AreEqual(nodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.Contains(nodeType, exception.Message);
        Assert.Contains(context, exception.Message);
        Assert.Contains("not supported", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForUnsupportedNode_ShouldContainHelpfulMessage()
    {
        // Arrange & Act
        var exception = AstValidationException.ForUnsupportedNode("RecursiveCTE", "CTE processing");

        // Assert
        Assert.Contains("not supported", exception.Message);
        Assert.Contains("documentation", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_CanBeThrown_AndCaught()
    {
        // Arrange & Act
        AstValidationException caughtException = null;
        try
        {
            throw AstValidationException.ForNullNode("TestNode", "testing");
        }
        catch (AstValidationException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
        Assert.AreEqual("TestNode", caughtException.NodeType);
    }

    [TestMethod]
    public void AstValidationException_CanBeCaughtAsInvalidOperationException()
    {
        // Arrange & Act
        InvalidOperationException caughtException = null;
        try
        {
            throw new AstValidationException("Node", "Context", "Message");
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
    }

    #endregion

    #region CompilationException Tests

    [TestMethod]
    public void CompilationException_Constructor_ShouldSetMessage()
    {
        // Arrange
        var message = "Failed to compile the query";

        // Act
        var exception = new CompilationException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void CompilationException_IsException()
    {
        // Arrange & Act
        var exception = new CompilationException("Test message");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void CompilationException_CanBeThrown_AndCaught()
    {
        // Arrange
        var message = "Compilation failed due to syntax error";
        CompilationException caughtException = null;

        // Act
        try
        {
            throw new CompilationException(message);
        }
        catch (CompilationException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
        Assert.AreEqual(message, caughtException.Message);
    }

    [TestMethod]
    public void CompilationException_WithEmptyMessage_ShouldWork()
    {
        // Arrange & Act
        var exception = new CompilationException(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, exception.Message);
    }

    [TestMethod]
    public void CompilationException_WithDetailedMessage_ShouldPreserveMessage()
    {
        // Arrange
        var message = "Failed to compile query:\n" +
                      "  Error at line 3, column 15\n" +
                      "  Unexpected token 'INVALID'\n" +
                      "  Expected: identifier, keyword, or expression";

        // Act
        var exception = new CompilationException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.Contains("line 3", exception.Message);
        Assert.Contains("INVALID", exception.Message);
    }

    #endregion
}