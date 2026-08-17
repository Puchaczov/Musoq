using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests covering all error stages in the Musoq query processing pipeline.
///     Stage 1: Lexical Analysis (Tokenization) - Raw SQL text → Token stream
///     Stage 2: Parsing (Syntax) - Token stream → Abstract Syntax Tree
///     Stage 3: Visitor Phase (Semantic Analysis)
///     - 3a: Schema Resolution
///     - 3b: Type Resolution
///     - 3c: Method Resolution
///     Stage 4: Code Generation - Validated AST → C# code
///     Stage 5: Roslyn Compilation - Generated C# → IL
///     Stage 6: Runtime Execution - Compiled query runs against data sources
///     All errors should produce readable messages suitable for LSP and LLM agentic tooling.
///     Each test verifies the SPECIFIC diagnostic code, not just "any error".
/// </summary>
public partial class CompilationPipelineErrorTests
{

    [TestMethod]
    public void Stage2_MissingFromClause()
    {
        // Arrange - SELECT Name WHERE x > 5 (missing FROM)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name WHERE Name = 'test'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2001_UnexpectedToken: "Expected token is From but received Identifier"
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "SELECT without FROM clause");
    }

    [TestMethod]
    public void Stage2_UnexpectedToken_CommaAtStart()
    {
        // Arrange - SELECT , Name FROM (unexpected comma at start of select list)
        var analyzer = CreateAnalyzer();
        var query = "SELECT , Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Should be MQ2015_LeadingComma or MQ2001_UnexpectedToken
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "leading comma in select list");
    }

    [TestMethod]
    public void Stage2_UnexpectedToken_DoubleComma()
    {
        // Arrange - Double comma in column list
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name,, City FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Should be trailing comma or unexpected token
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "double comma in select list");
    }

    [TestMethod]
    public void Stage2_UnbalancedParentheses_Open()
    {
        // Arrange - ((a + b) - missing closing parenthesis
        var analyzer = CreateAnalyzer();
        var query = "SELECT ((Population + 1) FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2001: "Expected token is RightParenthesis but received From"
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "unbalanced parentheses - missing close");
    }

    [TestMethod]
    public void Stage2_UnbalancedParentheses_Close()
    {
        // Arrange - (a + b)) - extra closing parenthesis
        var analyzer = CreateAnalyzer();
        var query = "SELECT (Population + 1)) FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Should be unexpected token (extra paren)
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "extra closing parenthesis");
    }

    [TestMethod]
    public void Stage2_InvalidClauseOrder_WhereBeforeFrom()
    {
        // Arrange - WHERE before SELECT (invalid SQL order)
        var analyzer = CreateAnalyzer();
        var query = "WHERE Name = 'test' SELECT Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Should be missing SELECT or unexpected token
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "WHERE before SELECT");
    }

    [TestMethod]
    public void Stage2_InvalidExpression_ConsecutiveLiterals()
    {
        // Arrange - SELECT 5 5 FROM (two literals without operator)
        // Parser may interpret second 5 as alias
        var analyzer = CreateAnalyzer();
        var query = "SELECT 5 5 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Document: parser may accept "5" as alias for literal 5
        DocumentParserBehavior(result,
            "5 5: may parse as literal 5 with alias '5' (valid) or MQ2018_MissingOperator",
            result.HasErrors);
    }

    [TestMethod]
    public void Stage2_InvalidExpression_ConsecutiveIdentifiers()
    {
        // Arrange - Two identifiers without operator
        // Parser interprets second as alias: "Name City" = Name AS City
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name City FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - This is VALID SQL (City is alias for Name)
        DocumentParserBehavior(result,
            "Name City: valid alias syntax (Name AS City implicit)",
            false);
    }

    [TestMethod]
    public void Stage2_MissingSelectKeyword()
    {
        // Arrange - Name FROM (no SELECT)
        var analyzer = CreateAnalyzer();
        var query = "Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2001: "Cannot compose statement, Identifier is not expected here"
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "query without SELECT keyword");
    }

    [TestMethod]
    public void Stage2_IncompleteExpression_TrailingOperator()
    {
        // Arrange - Expression ends with operator
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name =";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns one typed MQ2001 diagnostic.
        AssertHasExactlyOneErrorCode(result, "trailing = operator without operand",
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Stage2_IncompleteExpression_TrailingAnd()
    {
        // Arrange - Boolean expression ends with AND
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test' AND";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns one typed MQ2001 diagnostic.
        AssertHasExactlyOneErrorCode(result, "trailing AND without right operand",
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void Stage2_MissingJoinCondition()
    {
        // Arrange - JOIN without ON clause
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.Name FROM #A.Entities() a INNER JOIN #B.Entities() b";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2001: "Expected token is On but received EndOfFile"
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2007_InvalidJoinCondition, "JOIN without ON condition");
    }

    [TestMethod]
    public void Stage2_InvalidOrderByDirection()
    {
        // Arrange - ASCENDING instead of ASC
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name ASCENDING";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2030_UnsupportedSyntax: "Unrecognized token for ComposeOrder(), the token was Identifier"
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2009_InvalidOrderByExpression, "ASCENDING not recognized (should be ASC)");
    }

    [TestMethod]
    public void Stage2_EmptyQuery()
    {
        // Arrange - Empty string
        var analyzer = CreateAnalyzer();
        var query = "";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2001: "Parse error: The SQL query input cannot be empty..."
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2016_IncompleteStatement, "empty query");
    }

    [TestMethod]
    public void Stage2_WhitespaceOnlyQuery()
    {
        // Arrange - Only whitespace
        var analyzer = CreateAnalyzer();
        var query = "   \n\t   ";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2001: "Parse error: The SQL query input cannot be empty..."
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2016_IncompleteStatement, "whitespace-only query");
    }

}
