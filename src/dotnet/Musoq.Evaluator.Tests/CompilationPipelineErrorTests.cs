using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
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
[TestClass]
public partial class CompilationPipelineErrorTests : BasicEntityTestBase
{
    // ============================================================================
    // STAGE 5: ROSLYN COMPILATION
    // Generated C# → IL (internal errors - indicates bug in generator)
    // These should never reach the user as syntax/semantic errors
    // ============================================================================

    #region Stage 5: Roslyn Compilation

    [TestMethod]
    public void Stage5_NoRoslynErrorsForValidQueries()
    {
        // Arrange - Valid query should never produce Roslyn compilation errors
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, City, Population FROM #A.Entities() WHERE Population > 50";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - No errors means code generation and Roslyn compilation succeed
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
        AssertNoErrors(result);
    }

    #endregion

    // ============================================================================
    // STAGE 6: RUNTIME EXECUTION
    // Compiled query runs against data sources
    // These errors occur during execution, not analysis
    // ============================================================================

    #region Stage 6: Runtime Execution

    // Note: Runtime errors (file not found, permission denied, etc.) 
    // occur during query execution, not during analysis.
    // The QueryAnalyzer only performs static analysis, so runtime
    // errors are tested in execution tests, not here.

    [TestMethod]
    public void Stage6_RuntimeErrors_NotDetectedDuringAnalysis()
    {
        // Arrange - Query that will succeed analysis but might fail at runtime
        // Runtime errors are detected during execution, not static analysis
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Analysis passes; runtime errors tested in execution tests
        Assert.IsTrue(result.IsParsed, "Static analysis should pass");
        AssertNoErrors(result);
    }

    #endregion

    #region Test Setup

    private static BasicSchemaProvider<BasicEntity> CreateSchemaProvider()
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

    private static void AssertHasErrorCode(QueryAnalysisResult result, DiagnosticCode expectedCode,
        string? context = null)
    {
        var contextInfo = context != null ? $" ({context})" : "";
        DiagnosticContractTestAssertions.AssertErrorsHaveCode(result, expectedCode, contextInfo);
    }

    private static void AssertHasDiagnosticCode(QueryAnalysisResult result, DiagnosticCode expectedCode,
        string context)
    {
        _ = DiagnosticContractTestAssertions.AssertSingleError(result, expectedCode, context);
    }

    private static void AssertHasExactlyOneErrorCode(
        QueryAnalysisResult result,
        string context,
        DiagnosticCode expectedCode)
    {
        _ = DiagnosticContractTestAssertions.AssertSingleError(result, expectedCode, context);
    }

    private static void AssertHasErrorWithMessage(QueryAnalysisResult result, string messageContains, string context)
    {
        var diagnostics = result.Errors.ToList();
        Assert.HasCount(1, diagnostics,
            $"Expected one error containing '{messageContains}' ({context}) but got: " +
            string.Join(" | ", diagnostics.Select(static item => $"[{item.Code}] {item.Message}")));
        Assert.IsTrue(diagnostics[0].Message.Contains(messageContains, StringComparison.OrdinalIgnoreCase),
            $"Expected error containing '{messageContains}' ({context}) but got: {diagnostics[0].Message}");
    }

    private static void AssertNoErrors(QueryAnalysisResult result)
    {
        if (result.HasErrors)
        {
            var errorMessages = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Fail($"Expected no errors but got:\n{errorMessages}");
        }
    }

    private static void DocumentParserBehavior(QueryAnalysisResult result, string expectedBehavior,
        bool shouldHaveErrors)
    {
        Assert.IsNotNull(result, "Result should not be null - analyzer should not crash");

        if (shouldHaveErrors)
            Assert.IsNotEmpty(result.Errors,
                $"Expected errors for behavior: {expectedBehavior}. Got IsParsed={result.IsParsed}, HasErrors={result.HasErrors}");
        else
            Assert.IsFalse(result.HasErrors,
                $"Expected no errors for behavior: {expectedBehavior}. " +
                $"Got: {string.Join("; ", result.Errors.Select(e => $"[{e.Code}] {e.Message}"))}");
    }

    #endregion

    // ============================================================================
    // STAGE 4: CODE GENERATION
    // Validated AST → C# code (rare errors - indicates internal issue)
    // ============================================================================

    #region Stage 4: Code Generation

    [TestMethod]
    public void Stage4_CodeGeneration_ComplexQuery()
    {
        // Arrange - Complex query that exercises code generation paths
        var analyzer = CreateAnalyzer();
        var query = @"
            WITH cte AS (
                SELECT Name, City, Population 
                FROM #A.Entities() 
                WHERE Population > 50
            )
            SELECT Name, City 
            FROM cte 
            ORDER BY Name ASC";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must analyze without errors (code gen would fail at compile time)
        Assert.IsTrue(result.IsParsed, "Complex CTE query should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Stage4_CodeGeneration_SetOperator()
    {
        // Arrange - Set operators exercise complex code paths
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL (Name)
            SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must succeed
        Assert.IsTrue(result.IsParsed, "UNION ALL should parse");
        AssertNoErrors(result);
    }

    #endregion

}
