using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Validation;
using System.Linq;

namespace Musoq.Parser.Tests;

[TestClass]
public class QueryValidatorTests
{
    private QueryValidator _validator;

    [TestInitialize]
    public void Setup()
    {
        _validator = new QueryValidator();
    }

    [TestMethod]
    public void ValidateQuery_WithValidQuery_ShouldReturnNoIssues()
    {
        // Arrange
        var query = "SELECT Name, Age FROM #users.data()";

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        Assert.AreEqual(0, issues.Count);
    }

    [TestMethod]
    public void ValidateQuery_WithEmptyQuery_ShouldReturnEmptyQueryIssue()
    {
        // Arrange
        var query = "";

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        Assert.AreEqual(1, issues.Count);
        Assert.AreEqual(ValidationIssueType.EmptyQuery, issues[0].Type);
        Assert.IsTrue(issues[0].Message.Contains("cannot be empty"));
    }

    [TestMethod]
    public void ValidateQuery_WithNullQuery_ShouldReturnEmptyQueryIssue()
    {
        // Arrange
        string query = null;

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        Assert.AreEqual(1, issues.Count);
        Assert.AreEqual(ValidationIssueType.EmptyQuery, issues[0].Type);
    }

    [TestMethod]
    public void ValidateQuery_WithUnbalancedParentheses_ShouldReturnUnbalancedIssue()
    {
        // Arrange
        var query = "SELECT Name FROM #test.data( WHERE Age > 25";

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var parenthesesIssue = issues.FirstOrDefault(i => i.Type == ValidationIssueType.UnbalancedParentheses);
        Assert.IsNotNull(parenthesesIssue);
        Assert.IsTrue(parenthesesIssue.Message.Contains("Missing") && parenthesesIssue.Message.Contains("closing"));
    }

    [TestMethod]
    public void ValidateQuery_WithUnbalancedQuotes_ShouldReturnUnbalancedIssue()
    {
        // Arrange
        var query = "SELECT Name FROM #test.data() WHERE Name = 'John";

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var quotesIssue = issues.FirstOrDefault(i => i.Type == ValidationIssueType.UnbalancedQuotes);
        Assert.IsNotNull(quotesIssue);
        Assert.IsTrue(quotesIssue.Message.Contains("Unbalanced"));
    }

    [TestMethod]
    public void ValidateQuery_WithInvalidCharacters_ShouldReturnInvalidCharactersIssue()
    {
        // Arrange
        var query = "SELECT `Name` FROM #test.data()";

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var charactersIssue = issues.FirstOrDefault(i => i.Type == ValidationIssueType.InvalidCharacters);
        Assert.IsNotNull(charactersIssue);
        Assert.IsTrue(charactersIssue.Message.Contains("problematic characters"));
        Assert.IsTrue(charactersIssue.Message.Contains("'`'"));
        Assert.IsTrue(charactersIssue.Suggestion.Contains("double quotes"));
    }

    [TestMethod]
    public void ValidateQuery_WithSuspiciousPatterns_ShouldReturnSuspiciousPatternIssue()
    {
        // Arrange
        var query = "SELECT * FROM #test.data(); DROP TABLE users;";

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var suspiciousIssue = issues.FirstOrDefault(i => i.Type == ValidationIssueType.SuspiciousPattern);
        Assert.IsNotNull(suspiciousIssue);
        Assert.IsTrue(suspiciousIssue.Message.Contains("data modification"));
        Assert.IsTrue(suspiciousIssue.Message.Contains("DROP TABLE"));
    }

    [TestMethod]
    public void ValidateQuery_WithMissingKeywords_ShouldReturnMissingKeywordsIssue()
    {
        // Arrange
        var query = "Name, Age WHERE Age > 25"; // Missing SELECT and FROM

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var keywordsIssue = issues.FirstOrDefault(i => i.Type == ValidationIssueType.MissingKeywords);
        Assert.IsNotNull(keywordsIssue);
        Assert.IsTrue(keywordsIssue.Message.Contains("missing required keywords"));
        Assert.IsTrue(keywordsIssue.Message.Contains("SELECT") && keywordsIssue.Message.Contains("FROM"));
    }

    [TestMethod]
    public void ValidateQuery_WithTooLongQuery_ShouldReturnTooLongIssue()
    {
        // Arrange
        var query = "SELECT Name FROM #test.data() " + new string('X', 10000);

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var longIssue = issues.FirstOrDefault(i => i.Type == ValidationIssueType.TooLong);
        Assert.IsNotNull(longIssue);
        Assert.IsTrue(longIssue.Message.Contains("too long"));
    }

    [TestMethod]
    public void ValidateQuery_WithDeeplyNestedQuery_ShouldReturnTooComplexIssue()
    {
        // Arrange
        var query = "SELECT Name FROM #test.data() WHERE Age > " + 
                   new string('(', 15) + "25" + new string(')', 15);

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var complexIssue = issues.FirstOrDefault(i => i.Type == ValidationIssueType.TooComplex);
        Assert.IsNotNull(complexIssue);
        Assert.IsTrue(complexIssue.Message.Contains("deep nesting"));
    }

    [TestMethod]
    public void ValidateQuery_WithManyJoins_ShouldReturnTooComplexIssue()
    {
        // Arrange
        var query = "SELECT * FROM #a.data() a " +
                   "JOIN #b.data() b ON a.Id = b.Id " +
                   "JOIN #c.data() c ON b.Id = c.Id " +
                   "JOIN #d.data() d ON c.Id = d.Id " +
                   "JOIN #e.data() e ON d.Id = e.Id " +
                   "JOIN #f.data() f ON e.Id = f.Id " +
                   "JOIN #g.data() g ON f.Id = g.Id";

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var complexIssue = issues.FirstOrDefault(i => i.Type == ValidationIssueType.TooComplex);
        Assert.IsNotNull(complexIssue);
        Assert.IsTrue(complexIssue.Message.Contains("many JOIN operations"));
    }

    [TestMethod]
    public void ValidateAndThrow_WithValidQuery_ShouldNotThrow()
    {
        // Arrange
        var query = "SELECT Name FROM #test.data()";

        // Act & Assert
        _validator.ValidateAndThrow(query); // Should not throw
    }

    [TestMethod]
    public void ValidateAndThrow_WithInvalidQuery_ShouldThrowQueryValidationException()
    {
        // Arrange
        var query = "SELECT `Name` FROM #test.data( WHERE condition"; // Multiple issues

        // Act & Assert
        var exception = Assert.ThrowsException<QueryValidationException>(() => 
            _validator.ValidateAndThrow(query));
        
        Assert.IsTrue(exception.Message.Contains("validation failed"));
        Assert.IsTrue(exception.ValidationIssues.Any());
    }

    [TestMethod]
    public void GetQuerySuggestions_WithSelectStar_ShouldSuggestSpecificColumns()
    {
        // Arrange
        var query = "SELECT * FROM #test.data()";

        // Act
        var suggestions = _validator.GetQuerySuggestions(query);

        // Assert
        Assert.IsTrue(suggestions.Any(s => s.Contains("specific columns")));
    }

    [TestMethod]
    public void GetQuerySuggestions_WithoutSchemaReference_ShouldSuggestSchemaReference()
    {
        // Arrange
        var query = "SELECT Name FROM users";

        // Act
        var suggestions = _validator.GetQuerySuggestions(query);

        // Assert
        Assert.IsTrue(suggestions.Any(s => s.Contains("schema references")));
    }

    [TestMethod]
    public void GetQuerySuggestions_WithoutWhereOrLimit_ShouldSuggestFiltering()
    {
        // Arrange
        var query = "SELECT Name FROM #test.data()";

        // Act
        var suggestions = _validator.GetQuerySuggestions(query);

        // Assert
        Assert.IsTrue(suggestions.Any(s => s.Contains("WHERE clause") || s.Contains("TAKE/LIMIT")));
    }

    [TestMethod]
    public void GetQuerySuggestions_WithEmptyQuery_ShouldReturnEmptyList()
    {
        // Arrange
        var query = "";

        // Act
        var suggestions = _validator.GetQuerySuggestions(query);

        // Assert
        Assert.AreEqual(0, suggestions.Count);
    }

    [TestMethod]
    public void ValidateQuery_WithMultipleIssues_ShouldReturnAllIssues()
    {
        // Arrange
        var query = "SELECT `Name` FROM #test.data( WHERE Name = 'John"; // Backtick + unbalanced parens + quotes

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        Assert.IsTrue(issues.Count >= 2); // Should have multiple issues
        Assert.IsTrue(issues.Any(i => i.Type == ValidationIssueType.InvalidCharacters));
        Assert.IsTrue(issues.Any(i => i.Type == ValidationIssueType.UnbalancedParentheses));
    }

    [TestMethod]
    public void ValidateQuery_WithQuotesInStrings_ShouldNotReportFalsePositives()
    {
        // Arrange
        var query = "SELECT Name FROM #test.data() WHERE Description = 'He said \"Hello\" to me'";

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var quoteIssues = issues.Where(i => i.Type == ValidationIssueType.UnbalancedQuotes).ToList();
        Assert.AreEqual(0, quoteIssues.Count, "Should not report unbalanced quotes when quotes are within strings");
    }

    [TestMethod]
    public void ValidateQuery_WithParenthesesInStrings_ShouldNotReportFalsePositives()
    {
        // Arrange
        var query = "SELECT Name FROM #test.data() WHERE Description = 'Function(param)'";

        // Act
        var issues = _validator.ValidateQuery(query);

        // Assert
        var parenIssues = issues.Where(i => i.Type == ValidationIssueType.UnbalancedParentheses).ToList();
        Assert.AreEqual(0, parenIssues.Count, "Should not report unbalanced parentheses when they are within strings");
    }
}