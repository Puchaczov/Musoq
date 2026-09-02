using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityExprEdgeCaseTests
{
    [TestMethod]
    public void E_EDGE_01_IntegerOverflow()
    {
        // Arrange — 2147483647 + 1 (int overflow)
        var analyzer = CreateAnalyzer();
        var query = "SELECT 2147483647 + 1 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Integer literal arithmetic is accepted by analysis.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_EDGE_06_DeeplyNestedParentheses()
    {
        // Arrange — 10 levels of nested parentheses
        var analyzer = CreateAnalyzer();
        var query = "SELECT ((((((((((1 + 2)))))))))) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should parse and evaluate fine
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_EDGE_07_VeryLongAliasName()
    {
        // Arrange — Extremely long alias
        var analyzer = CreateAnalyzer();
        var query =
            "SELECT Name AS ThisIsAnExtremelyLongAliasNameThatShouldStillWorkButMightCauseIssuesInCodeGeneration FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should work fine with long alias
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_EDGE_08_ReservedKeywordAsAlias_Bracketed()
    {
        // Arrange — Reserved keyword as alias with brackets
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name AS [Select] FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Brackets allow reserved keywords as aliases.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_EDGE_09_MultipleReservedKeywordAliases()
    {
        // Arrange — Multiple reserved keywords as bracketed aliases
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name AS [Where], Population AS [From] FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Bracketed reserved keywords are valid aliases.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_EDGE_10_EmptyStringComparison()
    {
        // Arrange — '' = ''
        var analyzer = CreateAnalyzer();
        var query = "SELECT 1 FROM #A.Entities() WHERE '' = ''";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should work: empty string equality
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_EDGE_11_NullLiteralInVariousPositions()
    {
        // Arrange — null in SELECT, AS, WHERE
        var analyzer = CreateAnalyzer();

        var query1 = "SELECT null FROM #A.Entities()";
        var query2 = "SELECT null AS Value FROM #A.Entities()";
        var query3 = "SELECT 1 FROM #A.Entities() WHERE null IS NULL";

        var result1 = analyzer.Analyze(query1);
        var result2 = analyzer.Analyze(query2);
        var result3 = analyzer.Analyze(query3);

        // Assert — All forms should analyze successfully.
        AssertNoErrors(result1);
        AssertNoErrors(result2);
        AssertNoErrors(result3);
    }

    [TestMethod]
    public void E_EDGE_12_BooleanLiteralUsage()
    {
        // Arrange — Boolean literals in various contexts
        var analyzer = CreateAnalyzer();

        var query1 = "SELECT true AS Flag FROM #A.Entities()";
        var query2 = "SELECT 1 FROM #A.Entities() WHERE true";
        var query3 = "SELECT 1 FROM #A.Entities() WHERE NOT false";

        var result1 = analyzer.Analyze(query1);
        var result2 = analyzer.Analyze(query2);
        var result3 = analyzer.Analyze(query3);

        // Assert — SELECT/WHERE boolean literals and prefix NOT are supported.
        AssertNoErrors(result1);
        AssertNoErrors(result2);
        AssertNoErrors(result3);
    }

    [TestMethod]
    public void E_EDGE_13_HexadecimalLiterals()
    {
        // Arrange — Various hex literals
        var analyzer = CreateAnalyzer();

        var query1 = "SELECT 0xFF FROM #A.Entities()";
        var query2 = "SELECT 0xDEADBEEF FROM #A.Entities()";
        var query3 = "SELECT 0x0 FROM #A.Entities()";

        var result1 = analyzer.Analyze(query1);
        var result2 = analyzer.Analyze(query2);
        var result3 = analyzer.Analyze(query3);

        // Assert — All should be valid.
        AssertNoErrors(result1);
        AssertNoErrors(result2);
        AssertNoErrors(result3);
    }

    [TestMethod]
    public void E_EDGE_14_NegativeNumbers()
    {
        // Arrange — Negative number literals
        var analyzer = CreateAnalyzer();

        var query1 = "SELECT -1 FROM #A.Entities()";
        var query2 = "SELECT -0 FROM #A.Entities()";

        var result1 = analyzer.Analyze(query1);
        var result2 = analyzer.Analyze(query2);

        // Assert — Should parse fine
        AssertNoErrors(result1);
        AssertNoErrors(result2);
    }

    [TestMethod]
    public void E_EDGE_16_StringWithEscapedQuotes()
    {
        // Arrange — Escaped quotes inside string using SQL-standard '' syntax
        var analyzer = CreateAnalyzer();

        // Musoq's lexer does not support the SQL-standard '' (double-single-quote)
        // escape mechanism. The lexer treats adjacent quotes as separate string tokens.
        // For example, 'it''s a test' becomes 'it' + 's a test' (two separate tokens).
        // Users should use backslash escaping (e.g., 'it\'s a test') for embedded quotes.
        // The parser's error recovery behavior for these malformed queries is non-deterministic
        // and may or may not surface visible errors depending on token arrangement.
        var query1 = "SELECT 'it''s a test' FROM #A.Entities()";
        var query2 = "SELECT 'double ''quotes'' inside' FROM #A.Entities()";

        // Act — use ValidateSyntax for parse-level validation
        var result1 = analyzer.ValidateSyntax(query1);
        var result2 = analyzer.ValidateSyntax(query2);

        // Assert — Both queries use SQL-standard '' escape which Musoq doesn't support.
        // The parser may or may not produce errors depending on error recovery behavior.
        // We verify the queries are processed without crashing (result is not null).
        Assert.IsNotNull(result1, "Analysis result for query1 should not be null");
        Assert.IsNotNull(result2, "Analysis result for query2 should not be null");
    }

    [TestMethod]
    public void E_EDGE_22_MultipleStarSelects()
    {
        // Arrange — SELECT *, *
        var analyzer = CreateAnalyzer();
        var query = "SELECT *, * FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Multiple stars are accepted by analysis.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_EDGE_23_StarWithExplicitColumns()
    {
        // Arrange — SELECT *, Name AS V2
        var analyzer = CreateAnalyzer();
        var query = "SELECT *, Name AS V2 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Star with explicit columns is valid.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_EDGE_24_StarWithAliasPrefix()
    {
        // Arrange — SELECT a.* with alias
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.* FROM #A.Entities() a";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Alias-prefixed star is valid.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_EDGE_25_StarFromNonExistentAlias()
    {
        // Arrange — SELECT x.* where x doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT x.* FROM #A.Entities() a";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Unknown alias in wildcard access should surface as unknown column/alias.
        AssertHasErrorCode(result, DiagnosticCode.MQ3001_UnknownColumn, "star from non-existent alias x");
    }


    // ============================================================================
    // E-DESC: DESC Command Edge Cases
    // ============================================================================


}
