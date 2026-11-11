using System.Linq;
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
    public void Stage1_UnterminatedString_SingleQuote()
    {
        // Arrange - Missing closing quote: 'hello world (no closing ')
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'hello world FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - lexer reports unterminated string literal
        AssertHasErrorCode(result, DiagnosticCode.MQ1002_UnterminatedString,
            "unterminated single-quoted string");

        // Verify the error message mentions the quote character
        Assert.IsTrue(result.Errors.Any(e => e.Message.Contains('\'')),
            "Error should mention the quote character");
    }

    [TestMethod]
    public void Stage1_UnterminatedString_DoubleQuote()
    {


        var analyzer = CreateAnalyzer();
        var query = "SELECT \"hello world FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);


        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result, DiagnosticCode.MQ1001_UnknownToken, nameof(Stage1_UnterminatedString_DoubleQuote));
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.IndexOf("\"", System.StringComparison.Ordinal), 1), diagnostic.Span);
    }

    [TestMethod]
    public void Stage1_InvalidNumericLiteral_MultipleDecimals()
    {


        var analyzer = CreateAnalyzer();
        var query = "SELECT 1.2.3 FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);


        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Stage1_InvalidHexNumber_InvalidDigits()
    {


        var analyzer = CreateAnalyzer();
        var query = "SELECT 0xZZZ FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result, DiagnosticCode.MQ1006_InvalidHexNumber, nameof(Stage1_InvalidHexNumber_InvalidDigits));
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.IndexOf("0xZZZ", System.StringComparison.Ordinal), 5), diagnostic.Span);
    }

    [TestMethod]
    public void Stage1_InvalidBinaryNumber_InvalidDigits()
    {


        var analyzer = CreateAnalyzer();
        var query = "SELECT 0b123 FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result, DiagnosticCode.MQ1007_InvalidBinaryNumber, nameof(Stage1_InvalidBinaryNumber_InvalidDigits));
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.IndexOf("0b123", System.StringComparison.Ordinal), 5), diagnostic.Span);
    }

    [TestMethod]
    public void Stage1_InvalidOctalNumber_InvalidDigits()
    {

        var analyzer = CreateAnalyzer();
        var query = "SELECT 0o89 FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result, DiagnosticCode.MQ1008_InvalidOctalNumber, nameof(Stage1_InvalidOctalNumber_InvalidDigits));
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.IndexOf("0o89", System.StringComparison.Ordinal), 4), diagnostic.Span);
    }

    [TestMethod]
    public void Stage1_TrueInvalidHexNumber_NoDigits()
    {

        var analyzer = CreateAnalyzer();
        var query = "SELECT 0x FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result, DiagnosticCode.MQ1006_InvalidHexNumber, nameof(Stage1_TrueInvalidHexNumber_NoDigits));
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.IndexOf("0x", System.StringComparison.Ordinal), 2), diagnostic.Span);
    }

    [TestMethod]
    public void Stage1_TrueInvalidBinaryNumber_NoDigits()
    {

        var analyzer = CreateAnalyzer();
        var query = "SELECT 0b FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result, DiagnosticCode.MQ1007_InvalidBinaryNumber, nameof(Stage1_TrueInvalidBinaryNumber_NoDigits));
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.IndexOf("0b", System.StringComparison.Ordinal), 2), diagnostic.Span);
    }

    [TestMethod]
    public void Stage1_UnterminatedBlockComment()
    {
        // Arrange - /* comment without close
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name /* this comment is not closed FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Parser sees content after /* as comment body, then fails on structure
        // Returns MQ2001_UnexpectedToken because the SELECT is incomplete
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ1005_UnterminatedBlockComment, "block comment never closed with */");
    }

    [TestMethod]
    public void Stage1_UnrecognizedCharacter_At()
    {

        var analyzer = CreateAnalyzer();
        var query = "SELECT @ FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result, DiagnosticCode.MQ1001_UnknownToken, nameof(Stage1_UnrecognizedCharacter_At));
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.IndexOf("@", System.StringComparison.Ordinal), 1), diagnostic.Span);
    }

    [TestMethod]
    public void Stage1_UnrecognizedCharacter_Backtick()
    {

        var analyzer = CreateAnalyzer();
        var query = "SELECT `Name` FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result, DiagnosticCode.MQ1001_UnknownToken, nameof(Stage1_UnrecognizedCharacter_Backtick));
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.IndexOf("`", System.StringComparison.Ordinal), 1), diagnostic.Span);
    }

    [TestMethod]
    public void Stage1_TrueUnrecognizedCharacter_Caret()
    {

        var analyzer = CreateAnalyzer();
        var query = "SELECT ^ FROM #A.Entities()";

        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result, DiagnosticCode.MQ2020_MissingOperand, nameof(Stage1_TrueUnrecognizedCharacter_Caret));
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.IndexOf("^", System.StringComparison.Ordinal), 1), diagnostic.Span);
    }

    [TestMethod]
    public void Stage1_ValidNumericLiterals()
    {
        // Arrange - Valid hex, binary, octal
        var analyzer = CreateAnalyzer();
        var query = "SELECT 0xFF + 0b1010 + 0o77 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must parse without errors
        Assert.IsTrue(result.IsParsed, "Valid numeric literals should parse");
        AssertNoErrors(result);
    }

}
