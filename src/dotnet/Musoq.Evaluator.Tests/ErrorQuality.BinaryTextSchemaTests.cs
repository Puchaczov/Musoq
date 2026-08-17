using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Binary and Text Schema Parse/Semantic Error Tests.
///     These test parse-level and semantic-level errors when defining binary and text interpretation schemas.
///     Covers: P-BIN (binary parse errors), P-TEXT (text parse errors), P-MIX (mixed interaction errors),
///     E-BIN (binary semantic errors), E-TEXT (text semantic errors).
/// </summary>
[TestClass]
public partial class ErrorQualityBinaryTextSchemaTests : BasicEntityTestBase
{













    // ============================================================================
    // P-MIX: Mixed Binary/Text Interaction Parse Errors
    // ============================================================================






    // ============================================================================
    // E-BIN: Binary Schema Semantic/Evaluation Errors (via Analyze())
    // ============================================================================







    // ============================================================================
    // E-TEXT: Text Schema Semantic/Evaluation Errors (via Analyze())
    // ============================================================================







    // ============================================================================
    // Well-formed binary/text schemas that SHOULD succeed (positive baselines)
    // ============================================================================





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

    private static void AssertParseOrSemanticFailure(QueryAnalysisResult result, string context)
    {
        var errors = result.Errors.ToList();
        Assert.IsNotEmpty(errors,
            $"Expected parse or semantic error ({context}) but no diagnostics were reported.");

        if (errors.Any(e => string.IsNullOrWhiteSpace(e.Message)))
            Assert.Fail($"Expected actionable diagnostics ({context}) but one or more errors had empty messages.");
    }

    #endregion

    // ============================================================================
    // P-BIN: Binary Schema Parse-Level Errors
    // ============================================================================

}
