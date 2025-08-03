using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;

namespace Musoq.Parser.Tests;

[TestClass]
public class SyntaxExceptionTests
{
    [TestMethod]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var message = "Test error message";
        var queryPart = "SELECT * FROM";
        var position = 5;
        var expectedTokens = "table name";
        var actualToken = "WHERE";

        // Act
        var exception = new SyntaxException(message, queryPart, position, expectedTokens, actualToken);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(queryPart, exception.QueryPart);
        Assert.AreEqual(position, exception.Position);
        Assert.AreEqual(expectedTokens, exception.ExpectedTokens);
        Assert.AreEqual(actualToken, exception.ActualToken);
    }

    [TestMethod]
    public void ForUnexpectedToken_ShouldCreateAppropriateException()
    {
        // Arrange
        var actualToken = "WHERE";
        var expectedTokens = new[] { "FROM", "table name" };
        var queryPart = "SELECT * WHERE";
        var position = 9;

        // Act
        var exception = SyntaxException.ForUnexpectedToken(actualToken, expectedTokens, queryPart, position);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains($"Unexpected token '{actualToken}'"));
        Assert.IsTrue(exception.Message.Contains("at position 9"));
        Assert.IsTrue(exception.Message.Contains("Expected: FROM, table name"));
        Assert.IsTrue(exception.Message.Contains("Query context"));
        Assert.AreEqual(queryPart, exception.QueryPart);
        Assert.AreEqual(position, exception.Position);
        Assert.AreEqual("FROM, table name", exception.ExpectedTokens);
        Assert.AreEqual(actualToken, exception.ActualToken);
    }

    [TestMethod]
    public void ForUnexpectedToken_WithoutPosition_ShouldNotIncludePosition()
    {
        // Arrange
        var actualToken = "WHERE";
        var expectedTokens = new[] { "FROM" };
        var queryPart = "SELECT * WHERE";

        // Act
        var exception = SyntaxException.ForUnexpectedToken(actualToken, expectedTokens, queryPart);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains($"Unexpected token '{actualToken}'"));
        Assert.IsFalse(exception.Message.Contains("at position"));
        Assert.IsTrue(exception.Message.Contains("Expected: FROM"));
    }

    [TestMethod]
    public void ForUnexpectedToken_WithEmptyExpectedTokens_ShouldUseDefaultMessage()
    {
        // Arrange
        var actualToken = "INVALID";
        var expectedTokens = new string[0];
        var queryPart = "SELECT * INVALID";

        // Act
        var exception = SyntaxException.ForUnexpectedToken(actualToken, expectedTokens, queryPart);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("Expected: valid SQL token"));
    }

    [TestMethod]
    public void ForMissingToken_ShouldCreateAppropriateException()
    {
        // Arrange
        var expectedToken = "FROM";
        var queryPart = "SELECT *";
        var position = 8;

        // Act
        var exception = SyntaxException.ForMissingToken(expectedToken, queryPart, position);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains($"Missing required token '{expectedToken}'"));
        Assert.IsTrue(exception.Message.Contains("at position 8"));
        Assert.IsTrue(exception.Message.Contains("Query context"));
        Assert.IsTrue(exception.Message.Contains($"add the missing '{expectedToken}'"));
        Assert.AreEqual(queryPart, exception.QueryPart);
        Assert.AreEqual(position, exception.Position);
        Assert.AreEqual(expectedToken, exception.ExpectedTokens);
    }

    [TestMethod]
    public void ForInvalidStructure_ShouldCreateAppropriateException()
    {
        // Arrange
        var issue = "Nested queries are not supported";
        var queryPart = "SELECT * FROM (SELECT";
        var position = 15;

        // Act
        var exception = SyntaxException.ForInvalidStructure(issue, queryPart, position);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("Invalid SQL structure"));
        Assert.IsTrue(exception.Message.Contains("at position 15"));
        Assert.IsTrue(exception.Message.Contains(issue));
        Assert.IsTrue(exception.Message.Contains("Query context"));
        Assert.AreEqual(queryPart, exception.QueryPart);
        Assert.AreEqual(position, exception.Position);
    }

    [TestMethod]
    public void ForUnsupportedSyntax_ShouldCreateAppropriateException()
    {
        // Arrange
        var feature = "Common Table Expressions (CTE)";
        var queryPart = "WITH cte AS (";
        var position = 0;

        // Act
        var exception = SyntaxException.ForUnsupportedSyntax(feature, queryPart, position);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("Unsupported SQL syntax"));
        Assert.IsTrue(exception.Message.Contains("at position 0"));
        Assert.IsTrue(exception.Message.Contains(feature));
        Assert.IsTrue(exception.Message.Contains("refer to the documentation"));
        Assert.AreEqual(queryPart, exception.QueryPart);
        Assert.AreEqual(position, exception.Position);
    }

    [TestMethod]
    public void WithSuggestions_ShouldCreateAppropriateException()
    {
        // Arrange
        var baseMessage = "Invalid table reference";
        var queryPart = "SELECT * FROM unknwon_table";
        var suggestions = new[] 
        { 
            "Check table name spelling", 
            "Ensure table exists in schema",
            "Use #schema.table syntax for data sources"
        };
        var position = 14;

        // Act
        var exception = SyntaxException.WithSuggestions(baseMessage, queryPart, suggestions, position);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains(baseMessage));
        Assert.IsTrue(exception.Message.Contains("Suggestions:"));
        Assert.IsTrue(exception.Message.Contains("- Check table name spelling"));
        Assert.IsTrue(exception.Message.Contains("- Ensure table exists"));
        Assert.IsTrue(exception.Message.Contains("- Use #schema.table syntax"));
        Assert.AreEqual(queryPart, exception.QueryPart);
        Assert.AreEqual(position, exception.Position);
    }

    [TestMethod]
    public void WithSuggestions_WithEmptySuggestions_ShouldNotIncludeSuggestionsSection()
    {
        // Arrange
        var baseMessage = "Invalid syntax";
        var queryPart = "SELECT *";
        var suggestions = new string[0];

        // Act
        var exception = SyntaxException.WithSuggestions(baseMessage, queryPart, suggestions);

        // Assert
        Assert.IsNotNull(exception);
        Assert.AreEqual(baseMessage, exception.Message);
        Assert.IsFalse(exception.Message.Contains("Suggestions:"));
    }

    [TestMethod]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var message = "Parse error";
        var queryPart = "SELECT";
        var innerException = new System.ArgumentException("Inner error");

        // Act
        var exception = new SyntaxException(message, queryPart, innerException);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(queryPart, exception.QueryPart);
        Assert.AreEqual(innerException, exception.InnerException);
    }
}