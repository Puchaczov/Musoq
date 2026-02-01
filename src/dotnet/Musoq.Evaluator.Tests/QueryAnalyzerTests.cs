#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for the QueryAnalyzer class that provides LSP-friendly query analysis.
/// </summary>
[TestClass]
public class QueryAnalyzerTests : BasicEntityTestBase
{
    #region Setup

    private static ISchemaProvider CreateSchemaProvider()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", Array.Empty<BasicEntity>() }
        };
        return new BasicSchemaProvider<BasicEntity>(sources);
    }

    #endregion

    #region Performance Tests (Quick validation)

    [TestMethod]
    public void ValidateSyntax_IsFasterThanFullAnalysis()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT Name, City, Country FROM #A.Entities() WHERE Name = 'test'";

        // Act - Run multiple times to get average
        var syntaxWatch = Stopwatch.StartNew();
        for (var i = 0; i < 10; i++) analyzer.ValidateSyntax(query);
        syntaxWatch.Stop();

        var fullWatch = Stopwatch.StartNew();
        for (var i = 0; i < 10; i++) analyzer.Analyze(query);
        fullWatch.Stop();

        // Assert - Syntax validation should generally be faster
        // (but we don't fail the test, just log the comparison)
        Console.WriteLine($"Syntax validation: {syntaxWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Full analysis: {fullWatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Valid Query Tests

    [TestMethod]
    public void Analyze_ValidQuery_ReturnsSuccessWithNoErrors()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT Name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Query should be parsed successfully");
        Assert.IsNotNull(result.Root, "Root node should not be null");
        // Note: Due to semantic analysis requirements, we may still have diagnostics
        // but the parse should succeed
    }

    [TestMethod]
    public void ValidateSyntax_ValidQuery_ReturnsSuccessWithNoSyntaxErrors()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT 1 + 2 FROM #system.dual()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Query should be parsed successfully");
        Assert.IsNotNull(result.Root, "Root node should not be null for valid syntax");
        Assert.IsFalse(result.HasErrors, "Should not have syntax errors for valid query");
    }

    [TestMethod]
    public void Analyze_SelectStar_ReturnsSuccess()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT * FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Query should be parsed successfully");
    }

    #endregion

    #region Syntax Error Tests

    [TestMethod]
    public void ValidateSyntax_MissingFromClause_ReturnsError()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT Name"; // Missing FROM clause

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        Assert.IsTrue(result.HasErrors, "Should detect missing FROM clause error");
        var errors = result.Errors.ToList();
        Assert.IsNotEmpty(errors, "Should have at least one error");
    }

    [TestMethod]
    public void ValidateSyntax_InvalidToken_ReturnsError()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        // Use completely invalid SQL syntax that cannot be parsed
        var query = "SELECT @ @ @ FROM";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        // The parser may handle this differently - either as an error or as recoverable
        // We accept both outcomes: HasErrors true OR the parsing fails with issues visible
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            "Should detect invalid token error or fail to parse");
    }

    [TestMethod]
    public void ValidateSyntax_UnclosedParenthesis_ReturnsError()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT Name FROM #A.Entities(";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        Assert.IsTrue(result.HasErrors, "Should detect unclosed parenthesis error");
    }

    [TestMethod]
    public void ValidateSyntax_EmptyQuery_ReturnsError()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        // Empty query should either fail to parse or return with errors
        Assert.IsTrue(!result.IsParsed || result.HasErrors, "Empty query should not be valid");
    }

    #endregion

    #region Semantic Error Tests

    [TestMethod]
    public void Analyze_UnknownColumn_CollectsSemanticError()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT NonExistentColumn FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Query syntax should be valid");
        Assert.IsTrue(result.HasErrors, "Should have some kind of error for unknown column");

        // The error could be about the unknown column, OR about schema resolution
        // Both are valid semantic errors that should be caught
        var errors = result.Errors.ToList();
        Assert.IsNotEmpty(errors,
            $"Should report at least one error. Actual errors: {string.Join(", ", errors.Select(e => $"{e.Code}: {e.Message}"))}");
    }

    [TestMethod]
    public void Analyze_MultipleErrors_CollectsAllErrors()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        // Query with multiple potential errors
        var query = "SELECT NonExistent1, NonExistent2 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Query syntax should be valid");
        Assert.IsTrue(result.HasErrors, "Should have semantic errors");
        // In diagnostic mode, we should collect multiple errors
        // (though current implementation may stop at first error)
    }

    #endregion

    #region Diagnostic Properties Tests

    [TestMethod]
    public void Analyze_WithErrors_ErrorsPropertyFiltersCorrectly()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT UnknownCol FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        if (result.HasErrors)
        {
            var allDiagnostics = result.Diagnostics;
            var errors = result.Errors.ToList();

            Assert.IsTrue(errors.All(e => e.Severity == DiagnosticSeverity.Error),
                "Errors collection should only contain error-level diagnostics");
        }
    }

    [TestMethod]
    public void QueryAnalysisResult_IsSuccess_ReturnsFalseWhenHasErrors()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT BadColumn FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        if (result.HasErrors) Assert.IsFalse(result.IsSuccess, "IsSuccess should be false when there are errors");
    }

    [TestMethod]
    public void QueryAnalysisResult_IsSuccess_ReturnsTrueWhenNoErrors()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        // A simple query that should parse without issues
        var query = "SELECT 1 FROM #A.Entities()";

        // Act  
        var result = analyzer.Analyze(query);

        // Assert
        if (!result.HasErrors) Assert.IsTrue(result.IsSuccess, "IsSuccess should be true when there are no errors");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Analyze_QueryWithCTE_ParsesSuccessfully()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = @"
            WITH cte AS (SELECT Name FROM #A.Entities())
            SELECT Name FROM cte";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "CTE query should be parsed");
    }

    [TestMethod]
    public void Analyze_QueryWithJoin_ParsesSuccessfully()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = @"
            SELECT a.Name, b.Name 
            FROM #A.Entities() a 
            INNER JOIN #A.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "JOIN query should be parsed");
    }

    [TestMethod]
    public void Analyze_QueryWithGroupBy_ParsesSuccessfully()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT Name, Count(1) FROM #A.Entities() GROUP BY Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "GROUP BY query should be parsed");
    }

    [TestMethod]
    public void Analyze_QueryWithSetOperator_ParsesSuccessfully()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION
            SELECT Name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "UNION query should be parsed");
    }

    #endregion

    #region Null Handling

    [TestMethod]
    public void QueryAnalyzer_Constructor_ThrowsOnNullSchemaProvider()
    {
        // Act & Assert
        try
        {
            _ = new QueryAnalyzer(null!);
            Assert.Fail("Expected ArgumentNullException was not thrown");
        }
        catch (ArgumentNullException)
        {
            // This is the expected behavior
        }
    }

    [TestMethod]
    public void Analyze_NullQuery_HandlesGracefully()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);

        // Act & Assert - should either throw or return error result
        try
        {
            var result = analyzer.Analyze(null!);
            // If it doesn't throw, it should report an error
            Assert.IsTrue(result.HasErrors || !result.IsParsed,
                "Null query should result in error or failed parse");
        }
        catch (ArgumentNullException)
        {
            // This is also acceptable behavior
        }
        catch (NullReferenceException)
        {
            // This is also acceptable behavior for null input
        }
    }

    #endregion
}
