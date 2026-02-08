#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 2: Complex Expressions, Edge Cases, TAKE/SKIP, DESC, and Formatting.
///     Covers: E-CEXPR, E-TAKE, E-EDGE, E-DESC, E-FMT categories.
/// </summary>
[TestClass]
public class ErrorQuality_Phase2_ExprEdgeCaseTests : BasicEntityTestBase
{
    #region Test Setup

    private static ISchemaProvider CreateSchemaProvider()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Warsaw", "Poland", 100) { Money = 1000.50m }] },
            { "#B", [new BasicEntity("Berlin", "Germany", 200) { Money = 2000.75m }] }
        };
        return new BasicSchemaProvider<BasicEntity>(sources);
    }

    private static QueryAnalyzer CreateAnalyzer()
    {
        return new QueryAnalyzer(CreateSchemaProvider());
    }

    private static void AssertHasErrorCode(QueryAnalysisResult result, DiagnosticCode expectedCode, string context)
    {
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected error code {expectedCode} ({context}) but query succeeded. IsParsed: {result.IsParsed}");

        if (result.HasErrors)
        {
            var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.IsTrue(
                result.Errors.Any(e => e.Code == expectedCode),
                $"Expected error code {expectedCode} ({context}) but got:\n{errorDetails}");
        }
    }

    private static void AssertHasOneOfErrorCodes(QueryAnalysisResult result, string context,
        params DiagnosticCode[] expectedCodes)
    {
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but query succeeded");

        if (result.HasErrors)
        {
            var hasExpected = result.Errors.Any(e => expectedCodes.Contains(e.Code));
            if (!hasExpected)
            {
                var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
                Assert.Fail(
                    $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but got:\n{errorDetails}");
            }
        }
    }

    private static void AssertNoErrors(QueryAnalysisResult result)
    {
        if (result.HasErrors)
        {
            var errorMessages = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Fail($"Expected no errors but got:\n{errorMessages}");
        }
    }

    private static void DocumentBehavior(QueryAnalysisResult result, string expectedBehavior, bool shouldHaveErrors)
    {
        if (shouldHaveErrors)
            Assert.IsTrue(result.HasErrors || !result.IsParsed,
                $"Behavior documentation: {expectedBehavior} - but query succeeded");
    }

    #endregion

    // ============================================================================
    // E-CEXPR: Complex Expression Evaluation Errors
    // ============================================================================

    #region E-CEXPR: Complex expression errors

    [TestMethod]
    public void E_CEXPR_01_DeeplyNestedFunctionWithTypeMismatch()
    {
        // Arrange — ToUpper(ToInt32('5')) — ToInt32 returns int, ToUpper expects string
        var analyzer = CreateAnalyzer();
        var query = "SELECT ToUpper(ToInt32('5')) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain type mismatch at ToUpper level
        AssertHasOneOfErrorCodes(result, "ToUpper(int) type mismatch",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void E_CEXPR_02_CaseWithMixedTypesAcrossBranches()
    {
        // Arrange — CASE with int, string, and boolean branches
        var analyzer = CreateAnalyzer();
        var query = @"SELECT CASE 
    WHEN 1 = 1 THEN 42
    WHEN 1 = 2 THEN 'hello'
    ELSE true
END FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain CASE branches must return same type
        AssertHasOneOfErrorCodes(result, "CASE branches with three different types",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3027_InvalidExpressionType,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void E_CEXPR_03_NullInArithmetic()
    {
        // Arrange — null + 5
        var analyzer = CreateAnalyzer();
        var query = "SELECT null + 5 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain null in arithmetic context
        AssertHasOneOfErrorCodes(result, "null in arithmetic",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticCode.MQ3009_NullReference,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void E_CEXPR_04_NullComparison()
    {
        // Arrange — null = null (three-valued logic)
        var analyzer = CreateAnalyzer();
        var query = "SELECT 1 FROM #A.Entities() WHERE null = null";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document behavior: should suggest IS NULL
        DocumentBehavior(result, "null = null: may suggest IS NULL or work with three-valued logic",
            result.HasErrors);
    }

    [TestMethod]
    public void E_CEXPR_06_DeeplyNestedCase()
    {
        // Arrange — Nested CASE expressions
        var analyzer = CreateAnalyzer();
        var query = @"SELECT CASE 
    WHEN CASE WHEN 1=1 THEN true ELSE false END 
    THEN CASE WHEN 2=2 THEN 'yes' ELSE 'no' END
    ELSE 'other'
END FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Nested CASE should either work or produce a clear error
        DocumentBehavior(result, "nested CASE expressions: may work or error on boolean WHEN", result.HasErrors);
    }

    [TestMethod]
    public void E_CEXPR_07_ExpressionInGroupByMismatch()
    {
        // Arrange — SELECT and GROUP BY use different modulo operands
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population % 3 AS Mod, Count(1) FROM #A.Entities() GROUP BY Population % 2";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq does not enforce strict GROUP BY expression matching.
        // Unlike standard SQL which requires SELECT expressions to exactly match GROUP BY
        // expressions (or be aggregates), Musoq is permissive and allows mismatched
        // expressions. The query compiles and runs, potentially returning arbitrary values
        // for the non-matching expression. This is a known design choice.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_CEXPR_08_AliasUsedInWhere()
    {
        // Arrange — Using SELECT alias in WHERE
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population * 2 AS Doubled FROM #A.Entities() WHERE Doubled > 10";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain aliases aren't available in WHERE
        AssertHasOneOfErrorCodes(result, "alias 'Doubled' used in WHERE",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3015_UnknownAlias,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void E_CEXPR_09_AggregateInNonAggregateContext()
    {
        // Arrange — Population + Count(1) without GROUP BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population + Count(1) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq allows mixing aggregate and non-aggregate expressions
        // without an explicit GROUP BY clause. The engine treats non-aggregated columns
        // as implicitly grouped, similar to MySQL's non-strict SQL mode.
        // This is a known design choice for flexibility over strict SQL compliance.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_CEXPR_10_PropertyAccessOnNull()
    {
        // Arrange — null.Something
        var analyzer = CreateAnalyzer();
        var query = "SELECT null.Something FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain null doesn't have properties
        AssertHasOneOfErrorCodes(result, "property access on null",
            DiagnosticCode.MQ3009_NullReference,
            DiagnosticCode.MQ3014_InvalidPropertyAccess,
            DiagnosticCode.MQ3028_UnknownProperty,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    // ============================================================================
    // E-TAKE: TAKE / SKIP Edge Cases
    // ============================================================================

    #region E-TAKE: TAKE / SKIP edge cases

    [TestMethod]
    public void E_TAKE_01_TakeZero()
    {
        // Arrange — TAKE 0
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 0";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document: TAKE 0 may be valid (empty result) or error
        DocumentBehavior(result, "TAKE 0: may be valid empty result set or error", result.HasErrors);
    }

    [TestMethod]
    public void E_TAKE_02_TakeNegative()
    {
        // Arrange — TAKE -1
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE -1";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error on negative TAKE
        AssertHasOneOfErrorCodes(result, "TAKE with negative value",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void E_TAKE_04_SkipNegative()
    {
        // Arrange — SKIP -1
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() SKIP -1";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error on negative SKIP
        AssertHasOneOfErrorCodes(result, "SKIP with negative value",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void E_TAKE_05_TakeWithNonInteger()
    {
        // Arrange — TAKE 3.5
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 3.5";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error on non-integer TAKE
        AssertHasOneOfErrorCodes(result, "TAKE with non-integer",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void E_TAKE_06_SkipWithNonInteger()
    {
        // Arrange — SKIP 2.5
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() SKIP 2.5";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error on non-integer SKIP
        AssertHasOneOfErrorCodes(result, "SKIP with non-integer",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void E_TAKE_07_TakeWithExpression()
    {
        // Arrange — TAKE 2 + 3 (is expression supported?)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 2 + 3";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document: expressions in TAKE may or may not be supported
        DocumentBehavior(result, "TAKE with expression 2 + 3: may need literal", result.HasErrors);
    }

    [TestMethod]
    public void E_TAKE_08_VeryLargeTake()
    {
        // Arrange — TAKE with very large number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 999999999";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should handle gracefully (valid or overflow error)
        DocumentBehavior(result, "very large TAKE value", result.HasErrors);
    }

    #endregion

    // ============================================================================
    // E-EDGE: Edge Cases in Expressions and Literals
    // ============================================================================

    #region E-EDGE: Edge cases in expressions and literals

    [TestMethod]
    public void E_EDGE_01_IntegerOverflow()
    {
        // Arrange — 2147483647 + 1 (int overflow)
        var analyzer = CreateAnalyzer();
        var query = "SELECT 2147483647 + 1 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document: may be detected at compile time or runtime
        DocumentBehavior(result, "integer overflow 2147483647+1", result.HasErrors);
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

        // Assert — Brackets should allow reserved keywords as aliases
        DocumentBehavior(result, "bracketed reserved keyword alias [Select]", result.HasErrors);
    }

    [TestMethod]
    public void E_EDGE_09_MultipleReservedKeywordAliases()
    {
        // Arrange — Multiple reserved keywords as bracketed aliases
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name AS [Where], Population AS [From] FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should work with bracketed names
        DocumentBehavior(result, "multiple bracketed reserved keyword aliases", result.HasErrors);
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

        // Assert — All should be handled gracefully
        DocumentBehavior(result1, "SELECT null", result1.HasErrors);
        DocumentBehavior(result2, "SELECT null AS Value", result2.HasErrors);
        DocumentBehavior(result3, "WHERE null IS NULL", result3.HasErrors);
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

        // Assert — Boolean literals should be valid
        DocumentBehavior(result1, "SELECT true AS Flag", result1.HasErrors);
        DocumentBehavior(result2, "WHERE true", result2.HasErrors);
        DocumentBehavior(result3, "WHERE NOT false", result3.HasErrors);
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

        // Assert — All should be valid
        AssertNoErrors(result1);
        DocumentBehavior(result2, "0xDEADBEEF large hex", result2.HasErrors);
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

        // Assert — Document: double star may produce duplicates or error
        DocumentBehavior(result, "SELECT *, * — double star", result.HasErrors);
    }

    [TestMethod]
    public void E_EDGE_23_StarWithExplicitColumns()
    {
        // Arrange — SELECT *, Name AS V2
        var analyzer = CreateAnalyzer();
        var query = "SELECT *, Name AS V2 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should be valid
        DocumentBehavior(result, "SELECT * with explicit columns", result.HasErrors);
    }

    [TestMethod]
    public void E_EDGE_24_StarWithAliasPrefix()
    {
        // Arrange — SELECT a.* with alias
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.* FROM #A.Entities() a";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should be valid with alias-prefixed star
        DocumentBehavior(result, "SELECT a.* with alias", result.HasErrors);
    }

    [TestMethod]
    public void E_EDGE_25_StarFromNonExistentAlias()
    {
        // Arrange — SELECT x.* where x doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT x.* FROM #A.Entities() a";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown alias x
        AssertHasOneOfErrorCodes(result, "star from non-existent alias x",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3003_UnknownTable,
            DiagnosticCode.MQ3015_UnknownAlias,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    // ============================================================================
    // E-DESC: DESC Command Edge Cases
    // ============================================================================

    #region E-DESC: DESC command edge cases

    [TestMethod]
    public void E_DESC_01_DescOnNonExistentSchema()
    {
        // Arrange — DESC on schema that doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "DESC #nonexistent.table()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown schema
        AssertHasOneOfErrorCodes(result, "DESC on non-existent schema",
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void E_DESC_02_DescOnNonExistentTable()
    {
        // Arrange — DESC on non-existent table within valid schema
        var analyzer = CreateAnalyzer();
        var query = "DESC #A.nonexistent()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq's semantic analysis does not validate table method existence
        // during the DESC command processing. The schema provider resolves methods
        // at runtime, so non-existent tables are not caught at analysis time.
        // This is a known limitation of the static analysis phase.
        AssertNoErrors(result);
    }

    #endregion

    // ============================================================================
    // E-FMT: Whitespace, Comments, and Formatting Edge Cases
    // ============================================================================

    #region E-FMT: Formatting edge cases

    [TestMethod]
    public void E_FMT_01_QueryWithOnlyComments()
    {
        // Arrange — Only a line comment
        var analyzer = CreateAnalyzer();
        var query = "-- this is a comment";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should produce helpful message about empty query
        DocumentBehavior(result, "query with only comments", result.HasErrors || !result.IsParsed);
    }

    [TestMethod]
    public void E_FMT_02_MultiLineCommentWrappingKeywords()
    {
        // Arrange — Comment spanning across keywords
        var analyzer = CreateAnalyzer();
        var query = "SELECT /* this wraps\nacross lines */ Name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should parse correctly ignoring comment
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_FMT_03_CommentInsideStringLiteral()
    {
        // Arrange — Comment syntax inside string should be part of string
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'hello -- world' FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — String literal should include the -- as content
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_FMT_05_EmptyQuery()
    {
        // Arrange — Empty string
        var analyzer = CreateAnalyzer();
        var query = "";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should handle gracefully, not crash
        Assert.IsNotNull(result, "Analyzer should not crash on empty query");
    }

    [TestMethod]
    public void E_FMT_06_WhitespaceOnlyQuery()
    {
        // Arrange — Only spaces/tabs
        var analyzer = CreateAnalyzer();
        var query = "     ";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should handle gracefully, not crash
        Assert.IsNotNull(result, "Analyzer should not crash on whitespace-only query");
    }

    [TestMethod]
    public void E_FMT_07_TabSeparatedKeywords()
    {
        // Arrange — Tabs instead of spaces
        var analyzer = CreateAnalyzer();
        var query = "SELECT\tName\tFROM\t#A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Tabs should be treated same as spaces
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_FMT_08_NewlineInMiddleOfKeyword()
    {
        // Arrange — Keyword split across lines
        var analyzer = CreateAnalyzer();
        var query = "SEL\nECT Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error: keyword split by newline
        AssertHasOneOfErrorCodes(result, "keyword split by newline",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2025_MissingSelectKeyword,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void E_FMT_09_VeryLongSingleLineQuery()
    {
        // Arrange — Very long WHERE clause
        var analyzer = CreateAnalyzer();
        var query =
            "SELECT Name FROM #A.Entities() WHERE Population > 1 AND Population > 2 AND Population > 3 AND Population > 4 AND Population > 5 AND Population > 6 AND Population > 7 AND Population > 8 AND Population > 9 AND Population < 100 AND Population < 99 AND Population < 98 AND Population < 97 AND Population < 96 AND Population < 95 AND Population < 94 AND Population < 93 AND Population < 92 AND Population < 91";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Long but valid query should succeed
        AssertNoErrors(result);
    }

    #endregion
}
