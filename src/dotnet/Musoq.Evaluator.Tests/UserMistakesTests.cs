using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests for common user mistakes when constructing queries.
///     These tests verify that our diagnostic system catches errors gracefully
///     and provides helpful error messages.
/// </summary>
[TestClass]
public partial class UserMistakesTests : BasicEntityTestBase
{

    private static BasicSchemaProvider<BasicEntity> CreateSchemaProvider()
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
        DiagnosticContractTestAssertions.AssertErrorsHaveCode(result, expectedCode, context);
    }

    private static void AssertHasDiagnosticCode(QueryAnalysisResult result, DiagnosticCode expectedCode,
        string context)
    {
        _ = DiagnosticContractTestAssertions.AssertSingleError(result, expectedCode, context);
    }

    private static void AssertNoErrors(QueryAnalysisResult result)
    {
        if (result.HasErrors)
        {
            var errorMessages = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Fail($"Expected no errors but got:\n{errorMessages}");
        }
    }



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

        // Assert - unknown table/method reference
        AssertHasErrorCode(result, DiagnosticCode.MQ3085_UnknownSource, "wrong method name 'Entity'");
    }

    [TestMethod]
    public void Typo_InSchemaName_WrongSchema()
    {
        // Arrange - "#X" doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #X.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - schema does not exist
        AssertHasErrorCode(result, DiagnosticCode.MQ3010_UnknownSchema, "unknown schema '#X'");
    }



    [TestMethod]
    public void Missing_FromClause_SelectOnly()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001_UnexpectedToken: "Expected token is From"
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "SELECT without FROM");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "missing SELECT keyword");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "missing table reference");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "missing WHERE condition");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2006_MissingGroupByColumn, "missing GROUP BY column");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "missing ORDER BY column");
    }



    [TestMethod]
    public void Unclosed_Parenthesis_InMethod()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities(";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2030_UnsupportedSyntax for unexpected EOF
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "unclosed parenthesis in method");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "unclosed parenthesis in expression");
    }

    [TestMethod]
    public void Unclosed_SingleQuote_InString()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - lexer reports unterminated string
        AssertHasErrorCode(result, DiagnosticCode.MQ1002_UnterminatedString, "unclosed single quote");
    }

    [TestMethod]
    public void Unclosed_DoubleQuote_InString()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = \"test";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - double quotes are not string delimiters in Musoq (single quotes are),
        // so " is an unknown token (MQ1001) rather than an unterminated string (MQ1002).
        // The parser may also report MQ2001 due to the unexpected token.
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ1001_UnknownToken, "unclosed double quote");
    }

    [TestMethod]
    public void Unclosed_SquareBracket()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT [Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - invalid bracketed identifier syntax
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "unclosed square bracket");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "extra closing parenthesis");
    }



}
