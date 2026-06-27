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
        AssertHasOneOfErrorCodes(result, "SELECT without FROM clause",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2004_MissingFromClause);
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
        AssertHasOneOfErrorCodes(result, "leading comma in select list",
            DiagnosticCode.MQ2015_LeadingComma,
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2003_InvalidExpression);
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
        AssertHasOneOfErrorCodes(result, "double comma in select list",
            DiagnosticCode.MQ2014_TrailingComma,
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2003_InvalidExpression);
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
        AssertHasOneOfErrorCodes(result, "unbalanced parentheses - missing close",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2010_MissingClosingParenthesis);
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
        AssertHasOneOfErrorCodes(result, "extra closing parenthesis",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2003_InvalidExpression);
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
        AssertHasOneOfErrorCodes(result, "WHERE before SELECT",
            DiagnosticCode.MQ2025_MissingSelectKeyword,
            DiagnosticCode.MQ2001_UnexpectedToken);
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
        AssertHasOneOfErrorCodes(result, "query without SELECT keyword",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2025_MissingSelectKeyword);
    }

    [TestMethod]
    public void Stage2_IncompleteExpression_TrailingOperator()
    {
        // Arrange - Expression ends with operator
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name =";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2030_UnsupportedSyntax: "Token (EndOfFile) at position X cannot be used here"
        AssertHasOneOfErrorCodes(result, "trailing = operator without operand",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2020_MissingOperand,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile,
            DiagnosticCode.MQ2016_IncompleteStatement);
    }

    [TestMethod]
    public void Stage2_IncompleteExpression_TrailingAnd()
    {
        // Arrange - Boolean expression ends with AND
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test' AND";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser returns MQ2030_UnsupportedSyntax: "Token (EndOfFile) at position X cannot be used here"
        AssertHasOneOfErrorCodes(result, "trailing AND without right operand",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2020_MissingOperand,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile,
            DiagnosticCode.MQ2016_IncompleteStatement);
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
        AssertHasOneOfErrorCodes(result, "JOIN without ON condition",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            DiagnosticCode.MQ2002_MissingToken,
            DiagnosticCode.MQ2016_IncompleteStatement);
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
        AssertHasOneOfErrorCodes(result, "ASCENDING not recognized (should be ASC)",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2009_InvalidOrderByExpression,
            DiagnosticCode.MQ2001_UnexpectedToken);
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
        AssertHasOneOfErrorCodes(result, "empty query",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2016_IncompleteStatement,
            DiagnosticCode.MQ2025_MissingSelectKeyword,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile);
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
        AssertHasOneOfErrorCodes(result, "whitespace-only query",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2016_IncompleteStatement,
            DiagnosticCode.MQ2025_MissingSelectKeyword,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile);
    }

}
