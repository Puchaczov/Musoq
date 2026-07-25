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
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected error code {expectedCode} ({context}) but query succeeded. IsParsed: {result.IsParsed}");

        if (result.HasErrors)
        {
            var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.IsTrue(
                result.Errors.Any(e => e.Code == expectedCode),
                $"Expected error code {expectedCode} ({context}) but got:\n{errorDetails}");
        }
    }

    private static void AssertHasOneOfErrorCodes(QueryAnalysisResult result, string context,
        params DiagnosticCode[] expectedCodes)
    {
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but query succeeded");

        if (result.HasErrors)
        {
            var hasExpected = result.Errors.Any(e => expectedCodes.Contains(e.Code));
            if (!hasExpected)
            {
                var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
                Assert.Fail(
                    $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but got:\n{errorDetails}");
            }
        }
    }

    private static void AssertHasExactlyOneErrorCode(
        QueryAnalysisResult result,
        string context,
        DiagnosticCode expectedCode)
    {
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected {expectedCode} ({context}) but query succeeded");

        Assert.HasCount(1, result.Errors, $"Expected exactly one diagnostic ({context}).");
        var diagnostic = result.Errors.First();
        Assert.AreEqual(expectedCode, diagnostic.Code,
            $"Expected {expectedCode} ({context}) but got {diagnostic.Code}.");
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
