using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 1: Structural Syntax Errors.
///     Tests for missing clauses, misordering, CTE errors, TABLE/COUPLE errors,
///     expression/operator parse errors, and schema reference parse errors.
///     Covers: P-STRUCT, P-CTE, P-TC, P-EXPR, P-SCHEMA categories.
/// </summary>
[TestClass]
public partial class ErrorQualityStructuralSyntaxTests : BasicEntityTestBase
{
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

    private static void AssertHasErrorCode(QueryAnalysisResult result, DiagnosticCode expectedCode, string context)
    {
        DiagnosticContractTestAssertions.AssertErrorsHaveCode(result, expectedCode, context);
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

    private static void AssertHasExactDiagnosticCodes(
        QueryAnalysisResult result,
        string context,
        params DiagnosticCode[] expectedCodes)
    {
        Assert.IsNotEmpty(result.Errors, $"Expected diagnostics ({context}) but query succeeded");

        var actual = result.Errors.ToArray();
        Assert.HasCount(expectedCodes.Length, actual,
            $"Expected [{string.Join(", ", expectedCodes)}] ({context}) but got: " +
            string.Join("; ", actual.Select(e => $"[{e.Code}] {e.Message} at {e.Span}")));

        for (var index = 0; index < expectedCodes.Length; index++)
            Assert.AreEqual(expectedCodes[index], actual[index].Code,
                $"Diagnostic {index} ({context}) has code {actual[index].Code}, expected {expectedCodes[index]}.");
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
    // P-STRUCT: Missing clauses and misordering
    // ============================================================================


    // ============================================================================
    // P-CTE: CTE syntax errors
    // ============================================================================


    // ============================================================================
    // P-TC: TABLE/COUPLE syntax errors
    // ============================================================================


    // ============================================================================
    // P-EXPR: Expression/Operator Parse Errors
    // ============================================================================


    // ============================================================================
    // P-SCHEMA: Schema Reference Parse Errors
    // ============================================================================

}
