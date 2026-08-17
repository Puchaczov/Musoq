using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 1: SQL Dialect Confusion.
///     Tests queries where users from other SQL dialects (PostgreSQL, MySQL, SQL Server, SQLite)
///     try syntax that Musoq doesn't support. The error messages should suggest
///     Musoq-specific alternatives.
///     Covers: P-LIMIT, P-AGG, P-JOIN, P-SUB, P-SET, P-WIN, P-MISC categories.
/// </summary>
[TestClass]
public partial class ErrorQualityDialectConfusionTests : BasicEntityTestBase
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
    // P-LIMIT: LIMIT / OFFSET instead of TAKE / SKIP
    // People coming from PostgreSQL, MySQL, SQL Server will try these reflexively.
    // Expected hint: Suggest TAKE and SKIP keywords with correct syntax.
    // ============================================================================


    // ============================================================================
    // P-AGG: COUNT(*) and aggregate syntax
    // Expected hint: COUNT(*) → suggest Count(1); COUNT(DISTINCT x) is supported directly.
    // ============================================================================


    // ============================================================================
    // P-JOIN: JOIN syntax variations
    // Expected hints: FULL OUTER JOIN accepted. JOIN alone → INNER JOIN. LEFT JOIN → LEFT OUTER JOIN. USING → ON.
    // JOIN alone → INNER JOIN. LEFT JOIN → LEFT OUTER JOIN. USING → ON.
    // ============================================================================


    // ============================================================================
    // P-SUB: Subquery attempts
    // Subqueries are not supported; suggest CTE with appropriate restructuring pattern.
    // ============================================================================


    // ============================================================================
    // P-SET: Set operations without column specification
    // Omitted set-operation keys compare all projected values.
    // ============================================================================


    // ============================================================================
    // P-WIN: Window functions (not supported)
    // Expected hint: Window functions (OVER clause) are not supported.
    // Suggest GROUP BY for aggregation or CTE-based alternatives.
    // ============================================================================


    // ============================================================================
    // P-MISC: Miscellaneous SQL dialect confusion
    // Tests for operators, casting, and functions from other dialects.
    // ============================================================================

}
