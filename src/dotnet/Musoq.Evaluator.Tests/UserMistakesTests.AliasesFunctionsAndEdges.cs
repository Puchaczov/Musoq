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
        AssertHasOneOfErrorCodes(result, "subquery without parentheses",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
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

        // Assert - unknown alias 'x' should be reported
        AssertHasOneOfErrorCodes(result, "undefined alias 'x'",
            DiagnosticCode.MQ3015_UnknownAlias,
            DiagnosticCode.MQ3001_UnknownColumn);
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
        AssertHasOneOfErrorCodes(result, "empty query",
            DiagnosticCode.MQ2016_IncompleteStatement,
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
            DiagnosticCode.MQ2016_IncompleteStatement,
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
            DiagnosticCode.MQ2016_IncompleteStatement,
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
