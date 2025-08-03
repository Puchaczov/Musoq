using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using System.Linq;

namespace Musoq.Parser.Tests;

[TestClass]
public class QueryValidationExceptionTests
{
    [TestMethod]
    public void ForEmptyQuery_ShouldCreateAppropriateException()
    {
        // Act
        var exception = QueryValidationException.ForEmptyQuery();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("Query cannot be empty"));
        Assert.AreEqual(string.Empty, exception.Query);
        Assert.AreEqual(1, exception.ValidationIssues.Count());
        Assert.AreEqual(ValidationIssueType.EmptyQuery, exception.ValidationIssues.First().Type);
    }

    [TestMethod]
    public void ForInvalidCharacters_ShouldCreateAppropriateException()
    {
        // Arrange
        var query = "SELECT * FROM table WHERE column = 'value'`";
        var invalidChars = new char[] { '`' };

        // Act
        var exception = QueryValidationException.ForInvalidCharacters(query, invalidChars);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("invalid characters"));
        Assert.IsTrue(exception.Message.Contains("'`'"));
        Assert.AreEqual(query, exception.Query);
        Assert.AreEqual(1, exception.ValidationIssues.Count());
        Assert.AreEqual(ValidationIssueType.InvalidCharacters, exception.ValidationIssues.First().Type);
    }

    [TestMethod]
    public void ForUnbalancedParentheses_ShouldCreateAppropriateException()
    {
        // Arrange
        var query = "SELECT * FROM table WHERE (column = 'value'";

        // Act
        var exception = QueryValidationException.ForUnbalancedParentheses(query);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("unbalanced parentheses"));
        Assert.AreEqual(query, exception.Query);
        Assert.AreEqual(1, exception.ValidationIssues.Count());
        Assert.AreEqual(ValidationIssueType.UnbalancedParentheses, exception.ValidationIssues.First().Type);
    }

    [TestMethod]
    public void ForUnbalancedQuotes_ShouldCreateAppropriateException()
    {
        // Arrange
        var query = "SELECT * FROM table WHERE column = 'value";

        // Act
        var exception = QueryValidationException.ForUnbalancedQuotes(query);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("unbalanced quotes"));
        Assert.AreEqual(query, exception.Query);
        Assert.AreEqual(1, exception.ValidationIssues.Count());
        Assert.AreEqual(ValidationIssueType.UnbalancedQuotes, exception.ValidationIssues.First().Type);
    }

    [TestMethod]
    public void ForSuspiciousPatterns_ShouldCreateAppropriateException()
    {
        // Arrange
        var query = "SELECT * FROM table; DROP TABLE users;";
        var patterns = new string[] { "DROP TABLE", "DELETE FROM" };

        // Act
        var exception = QueryValidationException.ForSuspiciousPatterns(query, patterns);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("potentially problematic patterns"));
        Assert.IsTrue(exception.Message.Contains("DROP TABLE"));
        Assert.AreEqual(query, exception.Query);
        Assert.AreEqual(1, exception.ValidationIssues.Count());
        Assert.AreEqual(ValidationIssueType.SuspiciousPattern, exception.ValidationIssues.First().Type);
    }

    [TestMethod]
    public void ForMultipleIssues_ShouldCreateAppropriateException()
    {
        // Arrange
        var query = "invalid query";
        var issues = new[]
        {
            new ValidationIssue(ValidationIssueType.InvalidStructure, "Invalid structure"),
            new ValidationIssue(ValidationIssueType.MissingKeywords, "Missing SELECT keyword")
        };

        // Act
        var exception = QueryValidationException.ForMultipleIssues(query, issues);

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("2 issue(s)"));
        Assert.IsTrue(exception.Message.Contains("Invalid structure"));
        Assert.IsTrue(exception.Message.Contains("Missing SELECT"));
        Assert.AreEqual(query, exception.Query);
        Assert.AreEqual(2, exception.ValidationIssues.Count());
    }

    [TestMethod]
    public void ValidationIssue_ConstructorWithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var type = ValidationIssueType.InvalidCharacters;
        var message = "Test message";
        var position = 10;
        var suggestion = "Test suggestion";

        // Act
        var issue = new ValidationIssue(type, message, position, suggestion);

        // Assert
        Assert.AreEqual(type, issue.Type);
        Assert.AreEqual(message, issue.Message);
        Assert.AreEqual(position, issue.Position);
        Assert.AreEqual(suggestion, issue.Suggestion);
    }

    [TestMethod]
    public void ValidationIssue_ConstructorWithMinimalParameters_ShouldSetDefaultValues()
    {
        // Arrange
        var type = ValidationIssueType.EmptyQuery;
        var message = "Test message";

        // Act
        var issue = new ValidationIssue(type, message);

        // Assert
        Assert.AreEqual(type, issue.Type);
        Assert.AreEqual(message, issue.Message);
        Assert.IsNull(issue.Position);
        Assert.AreEqual(string.Empty, issue.Suggestion);
    }
}