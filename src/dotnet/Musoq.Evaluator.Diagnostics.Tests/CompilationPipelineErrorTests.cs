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
[global::Musoq.Tests.Common.CultureTestClass]
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

        var query = "SELECT Name, City, Population FROM #A.Entities() WHERE Population > 50";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Alice", "Warsaw", 100m]);
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
            { "#A", [new BasicEntity("Warsaw", "Poland", 100) { Name = "Alice", Money = 1000.50m }] },
            { "#B", [new BasicEntity("Berlin", "Germany", 200) { Name = "Bob", Money = 2000.75m }] }
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

    private static void AssertNoErrors(QueryAnalysisResult result)
    {
        if (result.HasErrors)
        {
            var errorMessages = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Fail($"Expected no errors but got:\n{errorMessages}");
        }
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

        var query = @"
            WITH cte AS (
                SELECT Name, City, Population 
                FROM #A.Entities() 
                WHERE Population > 50
            )
            SELECT Name, City 
            FROM cte 
            ORDER BY Name ASC";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Alice", "Warsaw"]);
    }

    [TestMethod]
    public void Stage4_CodeGeneration_SetOperator()
    {

        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL (Name)
            SELECT Name FROM #B.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Alice"], ["Bob"]);
    }

    #endregion

}
