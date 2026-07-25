using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityStructuralSyntaxTests
{
    #region P-TC: TABLE/COUPLE syntax errors

    [TestMethod]
    public void P_TC_03_TableWithInvalidTypeNames()
    {
        // Arrange — TABLE with invalid type names
        var analyzer = CreateAnalyzer();
        var query = @"table MyType { Name: banana, Value: potato };
couple #A.Entities() with table MyType as Source;
select Name, Value from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate invalid type names
        AssertHasOneOfErrorCodes(result, "invalid type names 'banana', 'potato'",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_TC_05_TableWithNoColumns()
    {
        // Arrange — TABLE with empty definition
        var analyzer = CreateAnalyzer();
        var query = @"table Empty {};
couple #A.Entities() with table Empty as Source;
select * from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate empty table definition
        AssertHasOneOfErrorCodes(result, "TABLE with no columns",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2005_InvalidSelectList,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_TC_06_MalformedTableProbe_ShouldReportOneTypedParseDiagnostic()
    {
        // Arrange — this legacy duplicate-column probe currently fails during parsing.
        var analyzer = CreateAnalyzer();
        var query = @"table Dupes { Name: string, Name: int };
couple #A.Entities() with table Dupes as Source;
select Name from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — parser ownership is explicit and recovery emits one root diagnostic.
        AssertHasExactlyOneErrorCode(result, "malformed duplicate-column TABLE probe",
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void P_TC_08_TableColumnWithoutType()
    {
        // Arrange — TABLE column without type
        var analyzer = CreateAnalyzer();
        var query = @"table MyType { Name, Value: int };
couple #A.Entities() with table MyType as Source;
select Name from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing type
        AssertHasOneOfErrorCodes(result, "TABLE column without type",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_TC_09_TableColumnWithEmptyType()
    {
        // Arrange — TABLE column with missing type (second column has no type)
        var analyzer = CreateAnalyzer();
        var query = @"table MyType { Name: string, Value };
couple #A.Entities() with table MyType as Source;
select Name from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate invalid empty type
        AssertHasOneOfErrorCodes(result, "TABLE column with empty type",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    #region P-EXPR: Expression/Operator parse errors

    [TestMethod]
    public void P_EXPR_01_DanglingOperator()
    {
        // Arrange — Dangling operator: SELECT Name + FROM
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing operand
        AssertHasOneOfErrorCodes(result, "dangling + operator",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2020_MissingOperand,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_02_DoubleOperator()
    {
        // Arrange — Double operator: Name ++ 1
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population ++ 1 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate invalid operator
        AssertHasOneOfErrorCodes(result, "double ++ operator",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_03_UnclosedCaseExpression()
    {
        // Arrange — CASE without END
        var analyzer = CreateAnalyzer();
        var query = "SELECT CASE WHEN Population > 50 THEN 'high' FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing END
        AssertHasOneOfErrorCodes(result, "CASE without END",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            DiagnosticCode.MQ2029_MissingEndKeyword,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_04_CaseWithoutWhen()
    {
        // Arrange — CASE THEN (missing WHEN)
        var analyzer = CreateAnalyzer();
        var query = "SELECT CASE THEN 'value' END FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing WHEN
        AssertHasOneOfErrorCodes(result, "CASE without WHEN",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            DiagnosticCode.MQ2027_MissingWhenClause,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_05_CaseWhenWithoutThen()
    {
        // Arrange — CASE WHEN ... ELSE (missing THEN)
        var analyzer = CreateAnalyzer();
        var query = "SELECT CASE WHEN Population > 50 ELSE 'low' END FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing THEN
        AssertHasOneOfErrorCodes(result, "CASE WHEN without THEN",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            DiagnosticCode.MQ2028_MissingThenClause,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_06_MultipleElseInCase()
    {
        // Arrange — CASE with two ELSE branches
        var analyzer = CreateAnalyzer();
        var query = "SELECT CASE WHEN Population > 50 THEN 'high' ELSE 'medium' ELSE 'low' END FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate multiple ELSE not allowed
        AssertHasOneOfErrorCodes(result, "multiple ELSE in CASE",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_07_UnclosedStringLiteral()
    {
        // Arrange — Unclosed string literal
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'hello";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate unterminated string
        AssertHasOneOfErrorCodes(result, "unclosed string literal",
            DiagnosticCode.MQ1002_UnterminatedString,
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_08_UnclosedParenthesisInExpression()
    {
        // Arrange — (Population + 1 without closing paren
        var analyzer = CreateAnalyzer();
        var query = "SELECT (Population + 1 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate unclosed parenthesis
        AssertHasOneOfErrorCodes(result, "unclosed parenthesis in expression",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2010_MissingClosingParenthesis);
    }

    [TestMethod]
    public void P_EXPR_09_EmptyInList()
    {
        // Arrange — IN with empty list
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IN ()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, IN() with empty argument list is syntactically valid.
        // The parser treats () as an empty args list. At runtime, IN with no values
        // will simply never match (always false).
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_EXPR_10_InWithoutParentheses()
    {
        // Arrange — IN without parentheses
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IN 'Warsaw', 'Berlin'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing parentheses for IN
        AssertHasOneOfErrorCodes(result, "IN without parentheses",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_11_LikeWithoutPattern()
    {
        // Arrange — LIKE without pattern
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name LIKE";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing LIKE pattern
        AssertHasOneOfErrorCodes(result, "LIKE without pattern",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2020_MissingOperand,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_13_TernaryConditional_CSharpHabit()
    {
        // Arrange — Ternary-style conditional from C#/JS
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population > 50 ? 'high' : 'low' FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should suggest CASE WHEN expression
        AssertHasOneOfErrorCodes(result, "ternary ?: should suggest CASE WHEN",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_14_LambdaExpression_CSharpHabit()
    {
        // Arrange — Lambda expression from C#
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population => Population * 2 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error with clear message
        AssertHasOneOfErrorCodes(result, "lambda => expression not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion
}
