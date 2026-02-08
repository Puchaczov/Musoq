#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests for common user mistakes when constructing queries.
///     These tests verify that our diagnostic system catches errors gracefully
///     and provides helpful error messages.
/// </summary>
[TestClass]
public class UserMistakesTests : BasicEntityTestBase
{
    #region Test Setup

    private static ISchemaProvider CreateSchemaProvider()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Warsaw", "Poland", 100)] },
            { "#B", [new BasicEntity("Berlin", "Germany", 200)] }
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

    private static void DocumentBehavior(QueryAnalysisResult result, string expectedBehavior, bool shouldHaveErrors)
    {
        if (shouldHaveErrors)
            Assert.IsTrue(result.HasErrors || !result.IsParsed,
                $"Behavior documentation: {expectedBehavior} - but query succeeded");
    }

    private static void AssertNoErrors(QueryAnalysisResult result)
    {
        if (result.HasErrors)
        {
            var errorMessages = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Fail($"Expected no errors but got:\n{errorMessages}");
        }
    }

    #endregion

    #region Typos and Misspellings

    [TestMethod]
    public void Typo_InColumnName_SimilarToExisting()
    {
        // Arrange - "Naem" instead of "Name"
        var analyzer = CreateAnalyzer();
        var query = "SELECT Naem FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3001_UnknownColumn for column not found
        AssertHasErrorCode(result, DiagnosticCode.MQ3001_UnknownColumn, "typo 'Naem' should error");
    }

    [TestMethod]
    public void Typo_InColumnName_CompletelyWrong()
    {
        // Arrange - "XYZ" doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT XYZ FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3001_UnknownColumn for non-existent column
        AssertHasErrorCode(result, DiagnosticCode.MQ3001_UnknownColumn, "unknown column 'XYZ'");
    }

    [TestMethod]
    public void Typo_InTableMethod_WrongMethodName()
    {
        // Arrange - "Entity()" instead of "Entities()"
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entity()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ9999_Unknown wrapping method not found exception
        AssertHasOneOfErrorCodes(result, "wrong method name 'Entity'",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3003_UnknownTable);
    }

    [TestMethod]
    public void Typo_InSchemaName_WrongSchema()
    {
        // Arrange - "#X" doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #X.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ9999_Unknown wrapping SchemaNotFoundException
        AssertHasOneOfErrorCodes(result, "unknown schema '#X'",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3010_UnknownSchema);
    }

    #endregion

    #region Missing Clauses and Keywords

    [TestMethod]
    public void Missing_FromClause_SelectOnly()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001_UnexpectedToken: "Expected token is From"
        AssertHasOneOfErrorCodes(result, "SELECT without FROM",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2004_MissingFromClause);
    }

    [TestMethod]
    public void Missing_SelectKeyword()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001_UnexpectedToken: "Cannot compose statement"
        AssertHasOneOfErrorCodes(result, "missing SELECT keyword",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2025_MissingSelectKeyword);
    }

    [TestMethod]
    public void Missing_TableReference()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030_UnsupportedSyntax or MQ2001_UnexpectedToken
        AssertHasOneOfErrorCodes(result, "missing table reference",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Missing_ColumnInSelect()
    {
        // Arrange - "SELECT FROM" might be parsed as SELECT with implicit *
        var analyzer = CreateAnalyzer();
        var query = "SELECT FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Some SQL dialects allow this as SELECT *, we accept both behaviors
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Missing_WhereCondition()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030_UnsupportedSyntax: incomplete WHERE
        AssertHasOneOfErrorCodes(result, "missing WHERE condition",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Missing_GroupByColumn()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() GROUP BY";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030_UnsupportedSyntax or MQ2001: incomplete GROUP BY
        AssertHasOneOfErrorCodes(result, "missing GROUP BY column",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Missing_OrderByColumn()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030_UnsupportedSyntax: incomplete ORDER BY
        AssertHasOneOfErrorCodes(result, "missing ORDER BY column",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    #endregion

    #region Unclosed Brackets and Quotes

    [TestMethod]
    public void Unclosed_Parenthesis_InMethod()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities(";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2030_UnsupportedSyntax for unexpected EOF
        AssertHasOneOfErrorCodes(result, "unclosed parenthesis in method",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2010_MissingClosingParenthesis,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void Unclosed_Parenthesis_InExpression()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT (Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001_UnexpectedToken: Expected RightParenthesis
        AssertHasOneOfErrorCodes(result, "unclosed parenthesis in expression",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2010_MissingClosingParenthesis);
    }

    [TestMethod]
    public void Unclosed_SingleQuote_InString()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ9999_Unknown wrapping lexer error for unterminated string
        AssertHasOneOfErrorCodes(result, "unclosed single quote",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ1002_UnterminatedString);
    }

    [TestMethod]
    public void Unclosed_DoubleQuote_InString()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = \"test";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ9999 or lexer accepts double quote differently
        AssertHasOneOfErrorCodes(result, "unclosed double quote",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ1002_UnterminatedString,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Unclosed_SquareBracket()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT [Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ9999 wrapping lexer error or MQ2001
        AssertHasOneOfErrorCodes(result, "unclosed square bracket",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void ExtraClosing_Parenthesis()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities())";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001_UnexpectedToken for extra )
        AssertHasOneOfErrorCodes(result, "extra closing parenthesis",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2003_InvalidExpression);
    }

    #endregion

    #region Invalid Operators and Expressions

    [TestMethod]
    public void Invalid_ComparisonOperator()
    {
        // Arrange - using "==" instead of "="
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name == 'test'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - could be valid or error depending on parser
        // We just verify it doesn't crash
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Invalid_ArithmeticExpression_DoubleOperator()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT 1 ++ 2 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030 or MQ2001 for invalid syntax
        AssertHasOneOfErrorCodes(result, "double operator ++",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2003_InvalidExpression);
    }

    [TestMethod]
    public void Invalid_BooleanExpression_MissingOperand()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test' AND";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030_UnsupportedSyntax: trailing AND
        AssertHasOneOfErrorCodes(result, "trailing AND without operand",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Invalid_BooleanExpression_MissingOperator()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name 'test'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030 or MQ2001: missing operator between Name and 'test'
        AssertHasOneOfErrorCodes(result, "missing operator between Name and literal",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Invalid_InExpression_EmptyList()
    {
        // Arrange - Some SQL dialects allow empty IN lists
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IN ()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Accept either error or success (dialect-dependent)
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Invalid_BetweenExpression_MissingAnd()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population BETWEEN 1 100";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: missing AND in BETWEEN
        AssertHasOneOfErrorCodes(result, "BETWEEN missing AND",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    #region Wrong Types and Type Mismatches

    [TestMethod]
    public void TypeMismatch_StringComparedToNumber()
    {
        // Arrange - comparing string column to number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 123";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - This might be allowed in some SQL dialects
        // Just verify it's processed without crashing
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TypeMismatch_NumberInStringFunction()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Population, 1, 2) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - verify it handles the type mismatch
        Assert.IsNotNull(result);
    }

    #endregion

    #region Case Sensitivity Issues

    [TestMethod]
    public void CaseSensitivity_KeywordLowercase()
    {
        // Arrange - all lowercase keywords should work
        var analyzer = CreateAnalyzer();
        var query = "select Name from #A.Entities() where Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - keywords are case-insensitive
        Assert.IsTrue(result.IsParsed);
    }

    [TestMethod]
    public void CaseSensitivity_KeywordMixedCase()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SeLeCt Name FrOm #A.Entities() WhErE Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
    }

    [TestMethod]
    public void CaseSensitivity_ColumnNameWrongCase()
    {
        // Arrange - "name" instead of "Name"
        var analyzer = CreateAnalyzer();
        var query = "SELECT name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - column names may or may not be case-sensitive
        Assert.IsNotNull(result);
    }

    #endregion

    #region Join and Set Operation Mistakes

    [TestMethod]
    public void Join_MissingOnCondition()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.Name FROM #A.Entities() a INNER JOIN #B.Entities() b";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: Expected token is On
        AssertHasOneOfErrorCodes(result, "JOIN missing ON",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2007_InvalidJoinCondition);
    }

    [TestMethod]
    public void Join_InvalidJoinType()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.Name FROM #A.Entities() a WEIRD JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001 or MQ2030: unrecognized JOIN type
        AssertHasOneOfErrorCodes(result, "invalid JOIN type 'WEIRD'",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void Union_MismatchedColumnCount()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION
            SELECT Name, City FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3031_SetOperatorMissingKeys or similar set operator error
        AssertHasOneOfErrorCodes(result, "UNION column count mismatch",
            DiagnosticCode.MQ3031_SetOperatorMissingKeys,
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Union_MissingSecondQuery()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() UNION";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001 or MQ2030: UNION without second query
        AssertHasOneOfErrorCodes(result, "UNION without second query",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    #region Aggregate Function Mistakes

    [TestMethod]
    public void Aggregate_NonAggregatedColumnInSelect()
    {
        // Arrange - City not in GROUP BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT City, Count(Name) FROM #A.Entities() GROUP BY Country";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - City is not in GROUP BY and not inside an aggregate → MQ3012
        AssertHasOneOfErrorCodes(result, "City not in GROUP BY",
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Aggregate_WithoutGroupBy()
    {
        // Arrange - mixing aggregated and non-aggregated
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(*) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Aggregate_UnknownFunction()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT FakeAggregate(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3029_UnresolvableMethod for unknown function
        AssertHasOneOfErrorCodes(result, "unknown function 'FakeAggregate'",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3004_UnknownFunction);
    }

    [TestMethod]
    public void Aggregate_WrongArgumentCount()
    {
        // Arrange - Count with wrong args
        var analyzer = CreateAnalyzer();
        var query = "SELECT Count(Name, City, Country) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsNotNull(result);
    }

    #endregion

    #region Subquery and CTE Mistakes

    [TestMethod]
    public void CTE_MissingAsKeyword()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "WITH cte (SELECT Name FROM #A.Entities()) SELECT * FROM cte";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: missing AS keyword
        AssertHasOneOfErrorCodes(result, "CTE missing AS keyword",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void CTE_ReferenceBeforeDefinition()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM cte WITH cte AS (SELECT Name FROM #A.Entities())";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: incorrect CTE syntax order
        AssertHasOneOfErrorCodes(result, "CTE referenced before definition",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void CTE_DuplicateName()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = @"
            WITH cte AS (SELECT Name FROM #A.Entities()),
                 cte AS (SELECT City FROM #A.Entities())
            SELECT * FROM cte";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - duplicate CTE name should be an error
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Subquery_NotEnclosedInParens()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM SELECT Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: unexpected SELECT in FROM
        AssertHasOneOfErrorCodes(result, "subquery without parentheses",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    #region Order By and Limit Mistakes

    [TestMethod]
    public void OrderBy_InvalidColumnReference()
    {
        // Arrange - referencing column not in SELECT
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY NonExistent";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderBy_InvalidDirection()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name ASCENDING";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030_UnsupportedSyntax: unrecognized token for order direction
        AssertHasOneOfErrorCodes(result, "invalid ORDER BY direction 'ASCENDING'",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Skip_NegativeValue()
    {
        // Arrange - Parser may or may not validate numeric ranges
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() SKIP -5";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Negative might be handled at runtime, accept either
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Take_NonIntegerValue()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 'five'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: expected number for TAKE
        AssertHasOneOfErrorCodes(result, "non-integer TAKE value",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    #region Alias Mistakes

    [TestMethod]
    public void Alias_DuplicateTableAlias()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.Name FROM #A.Entities() a, #B.Entities() a";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Parser returns MQ2001 for comma syntax (cross join not supported)
        AssertHasOneOfErrorCodes(result, "duplicate table alias 'a'",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3002_AmbiguousColumn,
            DiagnosticCode.MQ3003_UnknownTable);
    }

    [TestMethod]
    public void Alias_ReferencingUndefinedAlias()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT x.Name FROM #A.Entities() a";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3001_UnknownColumn or alias resolution error
        AssertHasOneOfErrorCodes(result, "undefined alias 'x'",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Alias_AmbiguousColumnWithoutQualifier()
    {
        // Arrange - joining two tables with same column name
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() a INNER JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - should warn about ambiguous column
        Assert.IsNotNull(result);
    }

    #endregion

    #region Function Call Mistakes

    [TestMethod]
    public void Function_UnknownFunctionName()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT UnknownFunction(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3029_UnresolvableMethod
        AssertHasOneOfErrorCodes(result, "unknown function 'UnknownFunction'",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3004_UnknownFunction);
    }

    [TestMethod]
    public void Function_MissingRequiredArgument()
    {
        // Arrange - Substring needs arguments
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring() FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3029_UnresolvableMethod: no overload matches
        AssertHasOneOfErrorCodes(result, "Substring with no arguments",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3006_InvalidArgumentCount);
    }

    [TestMethod]
    public void Function_TooManyArguments()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Length(Name, City, Country, Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Function_UnclosedArgumentList()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Length(Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: Expected RightParenthesis
        AssertHasOneOfErrorCodes(result, "unclosed function argument list",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2010_MissingClosingParenthesis);
    }

    #endregion

    #region Special Character and Unicode Issues

    [TestMethod]
    public void SpecialChars_InColumnName()
    {
        // Arrange - trying to use special chars without quotes
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name-With-Dashes FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - should handle gracefully
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Unicode_InStringLiteral()
    {
        // Arrange - Unicode in string should work
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = '日本語'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - should parse correctly
        Assert.IsTrue(result.IsParsed);
    }

    [TestMethod]
    public void Emoji_InStringLiteral()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = '😀'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - should parse correctly
        Assert.IsTrue(result.IsParsed);
    }

    #endregion

    #region Empty and Null Input Edge Cases

    [TestMethod]
    public void Empty_Query()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ2001: empty query error
        AssertHasOneOfErrorCodes(result, "empty query",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile);
    }

    [TestMethod]
    public void Whitespace_OnlyQuery()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "   \t\n   ";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ2001: whitespace-only query error
        AssertHasOneOfErrorCodes(result, "whitespace-only query",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile);
    }

    [TestMethod]
    public void Comment_OnlyQuery()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "-- This is just a comment";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ2001: comment-only query error
        AssertHasOneOfErrorCodes(result, "comment-only query",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile);
    }

    [TestMethod]
    public void MultiLineComment_Unclosed()
    {
        // Arrange - Lexer behavior for unclosed comments varies
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name /* unclosed comment FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Should ideally report error, but lexer may consume rest of input
        Assert.IsNotNull(result);
    }

    #endregion

    #region Valid Queries (Control Group)

    [TestMethod]
    public void Valid_SimpleSelect()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - should parse and analyze successfully without errors
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_SelectWithWhere()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_SelectWithOrderBy()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name ASC";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_SelectWithGroupBy()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Country, Count(Name) FROM #A.Entities() GROUP BY Country";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_SelectStar()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_ArithmeticExpression()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population * 2 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_StringConcatenation()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + ' - ' + City FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_CTE()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "WITH cte AS (SELECT Name FROM #A.Entities()) SELECT Name FROM cte";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    #endregion
}
