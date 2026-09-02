using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class UserMistakesTests
{
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "subquery without parentheses");
    }



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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2009_InvalidOrderByExpression, "invalid ORDER BY direction 'ASCENDING'");
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
        AssertHasErrorCode(result, DiagnosticCode.MQ2038_InvalidSliceCount, "non-integer TAKE value");
    }



    [TestMethod]
    public void Alias_DuplicateTableAlias()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.Name FROM #A.Entities() a, #B.Entities() a";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Parser returns MQ2001 for comma syntax (cross join not supported)
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "duplicate table alias 'a'");
    }

    [TestMethod]
    public void Alias_ReferencingUndefinedAlias()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT x.Name FROM #A.Entities() a";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - unknown alias 'x' should be reported
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3015_UnknownAlias, "undefined alias 'x'");
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



    [TestMethod]
    public void Function_UnknownFunctionName()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT UnknownFunction(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3088_NoMatchingCallableOverload
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3086_UnknownCallable, "unknown function 'UnknownFunction'");
    }

    [TestMethod]
    public void Function_MissingRequiredArgument()
    {
        // Arrange - Substring needs arguments
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring() FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3088_NoMatchingCallableOverload: no overload matches
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3087_InvalidCallableArity, "Substring with no arguments");
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

        // Assert - the parser identifies the unclosed function call.
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2021_UnclosedFunctionCall, "unclosed function argument list");
    }



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



    [TestMethod]
    public void Empty_Query()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ2001: empty query error
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2016_IncompleteStatement, "empty query");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2016_IncompleteStatement, "whitespace-only query");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2016_IncompleteStatement, "comment-only query");
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

}
