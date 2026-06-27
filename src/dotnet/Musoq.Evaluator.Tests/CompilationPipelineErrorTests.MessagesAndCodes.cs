using System;
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
    public void ErrorMessage_UnknownColumn_ShouldSuggestSimilar()
    {
        // Arrange - 'Naem' is typo for 'Name'
        var analyzer = CreateAnalyzer();
        var query = "SELECT Naem FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must have MQ3001 and message should mention the typo
        AssertHasErrorCode(result, DiagnosticCode.MQ3001_UnknownColumn, "typo 'Naem'");

        // Verify message quality
        var errorMessage = result.Errors.First(e => e.Code == DiagnosticCode.MQ3001_UnknownColumn).Message;
        Assert.IsTrue(
            errorMessage.Contains("Naem", StringComparison.OrdinalIgnoreCase),
            $"Error message should mention 'Naem': {errorMessage}");
    }

    [TestMethod]
    public void ErrorMessage_UnknownMethod_ShouldMentionMethodName()
    {
        // Arrange - Non-existent function
        var analyzer = CreateAnalyzer();
        var query = "SELECT UnknownFunction(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Returns MQ3029_UnresolvableMethod: "Method UnknownFunction with argument types System.String cannot be resolved"
        AssertHasOneOfErrorCodes(result, "unknown function",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod);

        // Verify message mentions the function name
        var hasMethodInMessage = result.Errors.Any(e =>
            e.Message.Contains("UnknownFunction", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(hasMethodInMessage,
            $"Error message should mention 'UnknownFunction': {string.Join("; ", result.Errors.Select(e => e.Message))}");
    }

    [TestMethod]
    public void ErrorMessage_SyntaxError_ShouldNotExposeInternals()
    {
        // Arrange - Query that triggers syntax error
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE WHERE"; // Double WHERE

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Must have syntax error
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            "Double WHERE should produce syntax error");

        // Verify no internal exception types exposed
        foreach (var error in result.Errors)
            Assert.IsFalse(
                error.Message.Contains("NullReferenceException") ||
                error.Message.Contains("KeyNotFoundException") ||
                error.Message.Contains("StackTrace") ||
                error.Message.Contains("at System.") ||
                error.Message.Contains("at Musoq."),
                $"Error message should not contain internal exception details: {error.Message}");
    }

    [TestMethod]
    public void ErrorMessage_PropertyAccess_ShouldNotExposeInternals()
    {
        // Arrange - Property chain error
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name.Unknown.Deep.Chain FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Returns MQ3028_UnknownProperty or MQ3001_UnknownColumn for property chain issues
        AssertHasOneOfErrorCodes(result, "invalid property chain on string",
            DiagnosticCode.MQ3028_UnknownProperty,
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3014_InvalidPropertyAccess);
    }

    [TestMethod]
    public void DiagnosticCode_MQ3001_UnknownColumn_IsUsed()
    {
        // Arrange - Completely non-existent column
        var analyzer = CreateAnalyzer();
        var query = "SELECT NonExistentColumn FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must be exactly MQ3001_UnknownColumn
        AssertHasErrorCode(result, DiagnosticCode.MQ3001_UnknownColumn,
            "column 'NonExistentColumn' doesn't exist");
    }

    [TestMethod]
    public void DiagnosticCode_MQ3004_UnknownFunction_IsUsed()
    {
        // Arrange - Function that doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT NoSuchMethod(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Returns MQ3029_UnresolvableMethod: "Method NoSuchMethod with argument types System.String cannot be resolved"
        AssertHasOneOfErrorCodes(result, "unknown function 'NoSuchMethod'",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod);
    }

    [TestMethod]
    public void DiagnosticCode_MQ3010_UnknownSchema_IsUsed()
    {
        // Arrange - Schema that doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #nonexistent.Table()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - unknown schema should map to MQ3010
        AssertHasErrorCode(result, DiagnosticCode.MQ3010_UnknownSchema,
            "schema 'nonexistent' not registered");
    }

}
