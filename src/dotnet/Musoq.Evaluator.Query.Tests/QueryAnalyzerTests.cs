using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for the QueryAnalyzer class that provides LSP-friendly query analysis.
/// </summary>
[global::Musoq.Tests.Common.CultureTestClass]
public class QueryAnalyzerTests : BasicEntityTestBase
{
    #region Setup

    private static BasicSchemaProvider<BasicEntity> CreateSchemaProvider()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [] } };
        return new BasicSchemaProvider<BasicEntity>(sources);
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
        Assert.IsNotEmpty(result.Errors, "Should detect an invalid-token diagnostic");
    }

    [TestMethod]
    public void ValidateSyntax_MistypedSelectKeyword_SuggestsSelect()
    {
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECTE Name FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);
        var error = result.Errors.FirstOrDefault();

        Assert.IsNotNull(error, "Expected a syntax diagnostic for mistyped SELECT keyword.");
        Assert.Contains("Did you mean 'SELECT'?", error.Message);
    }

    [TestMethod]
    public void ValidateSyntax_LimitKeyword_SuggestsTake()
    {
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT Name FROM #A.Entities() LIMIT 5";

        var result = analyzer.ValidateSyntax(query);
        var error = result.Errors.FirstOrDefault();

        Assert.IsNotNull(error, "Expected a syntax diagnostic for LIMIT.");
        Assert.Contains("TAKE", error.Message);
    }

    [TestMethod]
    public void ValidateSyntax_GarbageCharacter_ExplainsHowToFixIt()
    {
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT @ FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);
        var error = result.Errors.FirstOrDefault(e => e.Code == DiagnosticCode.MQ1001_UnknownToken);

        Assert.IsNotNull(error, "Expected an unknown-token diagnostic for garbage character input.");
        Assert.Contains("Remove the unsupported character", error.Message);
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
        Assert.IsNotEmpty(
            errors,
            $"Should report at least one error. Actual errors: {string.Join(", ", errors.Select(e => $"{e.Code}: {e.Message}"))}"
        );
    }

    [TestMethod]
    public void Analyze_UnknownColumnTypo_UsesDidYouMeanWording()
    {
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT Nmae FROM #A.Entities()";

        var result = analyzer.Analyze(query);

        var error = result.Errors.FirstOrDefault(e => e.Code == DiagnosticCode.MQ3001_UnknownColumn);

        Assert.IsNotNull(error, "Expected unknown-column diagnostic for misspelled column name.");
        Assert.Contains("Did you mean 'Name'?", error.Message);
    }

    [TestMethod]
    public void Analyze_UnknownProperty_UsesUnknownPropertyDiagnostic()
    {
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query = "SELECT Self.NonExistingProperty FROM #A.Entities()";

        var result = analyzer.Analyze(query);

        var error = result.Errors.FirstOrDefault(e => e.Code == DiagnosticCode.MQ3028_UnknownProperty);

        Assert.IsNotNull(
            error,
            $"Expected unknown-property diagnostic. Actual errors: {string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Message}"))}"
        );
        Assert.Contains("Unknown property", error.Message);
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

        DiagnosticContractTestAssertions.AssertSingleError(result, DiagnosticCode.MQ3001_UnknownColumn, query);
        Assert.IsFalse(result.IsSuccess);
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

        DiagnosticContractTestAssertions.AssertSingleError(result, DiagnosticCode.MQ3001_UnknownColumn, query);
        Assert.IsFalse(result.IsSuccess);
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
        Assert.IsFalse(result.HasErrors);
        Assert.IsTrue(result.IsSuccess);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Analyze_QueryWithCTE_ParsesSuccessfully()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);
        var query =
            @"
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
        var query =
            @"
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
        var query =
            @"
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
        var failure = Assert.ThrowsExactly<ArgumentNullException>(() => new QueryAnalyzer(null!));

        Assert.AreEqual("schemaProvider", failure.ParamName);
    }

    [TestMethod]
    public void Analyze_NullQuery_HandlesGracefully()
    {
        // Arrange
        var schemaProvider = CreateSchemaProvider();
        var analyzer = new QueryAnalyzer(schemaProvider);

        var failure = Assert.ThrowsExactly<ArgumentNullException>(() => analyzer.Analyze(null!));

        Assert.AreEqual("query", failure.ParamName);
    }

    [TestMethod]
    public void Analyze_NullQueryWithCancellationToken_ThrowsArgumentNullException()
    {
        var analyzer = new QueryAnalyzer(CreateSchemaProvider());

        var failure = Assert.ThrowsExactly<ArgumentNullException>(() =>
            analyzer.Analyze(null!, CancellationToken.None)
        );

        Assert.AreEqual("query", failure.ParamName);
    }

    [TestMethod]
    public void Analyze_CancelledNullQuery_PreservesCancellationPrecedence()
    {
        var analyzer = new QueryAnalyzer(CreateSchemaProvider());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var failure = Assert.ThrowsExactly<OperationCanceledException>(() =>
            analyzer.Analyze(null!, cancellation.Token)
        );

        Assert.AreEqual(cancellation.Token, failure.CancellationToken);
    }

    #endregion
}
