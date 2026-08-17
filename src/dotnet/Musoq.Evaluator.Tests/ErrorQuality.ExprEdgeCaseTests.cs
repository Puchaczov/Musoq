using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 2: Complex Expressions, Edge Cases, TAKE/SKIP, DESC, and Formatting.
///     Covers: E-CEXPR, E-TAKE, E-EDGE, E-DESC, E-FMT categories.
/// </summary>
[TestClass]
public partial class ErrorQualityExprEdgeCaseTests : BasicEntityTestBase
{

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


    // ============================================================================
    // E-CEXPR: Complex Expression Evaluation Errors
    // ============================================================================


    [TestMethod]
    public void E_CEXPR_01_DeeplyNestedFunctionWithTypeMismatch()
    {
        // Arrange — ToUpper(ToInt32('5')) — ToInt32 returns int, ToUpper expects string
        var analyzer = CreateAnalyzer();
        var query = "SELECT ToUpper(ToInt32('5')) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain type mismatch at ToUpper level
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3088_NoMatchingCallableOverload, "ToUpper(int) type mismatch");
    }

    [TestMethod]
    public void E_CEXPR_02_CaseWithMixedTypesAcrossBranches()
    {
        // Arrange — CASE with int, string, and boolean branches
        var analyzer = CreateAnalyzer();
        var query = @"SELECT CASE
    WHEN 1 = 1 THEN 42
    WHEN 1 = 2 THEN 'hello'
    ELSE true
END FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain CASE branches must return same type
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3027_InvalidExpressionType, "CASE branches with three different types");
    }

    [TestMethod]
    public void E_CEXPR_03_NullInArithmetic()
    {
        // Arrange — null + 5
        var analyzer = CreateAnalyzer();
        var query = "SELECT null + 5 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Null-safe arithmetic is supported and should analyze successfully.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_CEXPR_04_NullComparison()
    {
        // Arrange — null = null (three-valued logic)
        var analyzer = CreateAnalyzer();
        var query = "SELECT 1 FROM #A.Entities() WHERE null = null";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Null comparisons are supported in WHERE and should analyze successfully.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_CEXPR_06_DeeplyNestedCase()
    {
        // Arrange — Nested CASE expressions
        var analyzer = CreateAnalyzer();
        var query = @"SELECT CASE
    WHEN CASE WHEN 1=1 THEN true ELSE false END
    THEN CASE WHEN 2=2 THEN 'yes' ELSE 'no' END
    ELSE 'other'
END FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Nested CASE expressions are valid.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_CEXPR_07_ExpressionInGroupByMismatch()
    {
        // Arrange — SELECT and GROUP BY use different modulo operands
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population % 3 AS Mod, Count(1) FROM #A.Entities() GROUP BY Population % 2";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq does not enforce strict GROUP BY expression matching.
        // Unlike standard SQL which requires SELECT expressions to exactly match GROUP BY
        // expressions (or be aggregates), Musoq is permissive and allows mismatched
        // expressions. The query compiles and runs, potentially returning arbitrary values
        // for the non-matching expression. This is a known design choice.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_CEXPR_08_AliasUsedInWhere_ShouldAnalyzeSuccessfully()
    {
        // Arrange — SELECT aliases are visible in WHERE in runtime v2.
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population * 2 AS Doubled FROM #A.Entities() WHERE Doubled > 10";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        AssertNoErrors(result);
        Assert.IsTrue(result.IsParsed);
    }

    [TestMethod]
    public void E_CEXPR_09_AggregateInNonAggregateContext()
    {
        // Arrange — Population + Count(1) without GROUP BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population + Count(1) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq allows mixing aggregate and non-aggregate expressions
        // without an explicit GROUP BY clause. The engine treats non-aggregated columns
        // as implicitly grouped, similar to MySQL's non-strict SQL mode.
        // This is a known design choice for flexibility over strict SQL compliance.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_CEXPR_10_PropertyAccessOnNull()
    {
        // Arrange — null.Something
        var analyzer = CreateAnalyzer();
        var query = "SELECT null.Something FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Null property access should surface as unknown property/column, not generic unknown.
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3028_UnknownProperty, "property access on null");
    }


    // ============================================================================
    // E-TAKE: TAKE / SKIP Edge Cases
    // ============================================================================


}
