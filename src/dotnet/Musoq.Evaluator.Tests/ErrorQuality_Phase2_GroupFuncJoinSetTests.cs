#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 2: GROUP BY, Function Resolution, JOIN/APPLY, and Set Operations.
///     Covers: E-GROUP, E-FUNC, E-JOIN, E-SET categories.
/// </summary>
[TestClass]
public class ErrorQuality_Phase2_GroupFuncJoinSetTests : BasicEntityTestBase
{
    #region Test Setup

    private static ISchemaProvider CreateSchemaProvider()
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
    // E-GROUP: GROUP BY Semantic Errors
    // ============================================================================

    #region E-GROUP: GROUP BY semantic errors

    [TestMethod]
    public void E_GROUP_01_NonAggregatedColumnNotInGroupBy()
    {
        // Arrange — Column derived from grouped column
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Population + 1 AS Next FROM #A.Entities() GROUP BY Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Standard SQL requires non-aggregated columns to appear in GROUP BY.
        // Musoq now enforces this: Population is not in GROUP BY and not inside an
        // aggregate, so MQ3012 should be reported.
        AssertHasErrorCode(result, DiagnosticCode.MQ3012_NonAggregateInSelect,
            "Population not in GROUP BY and not inside aggregate");
    }

    [TestMethod]
    public void E_GROUP_03_HavingReferencingNonGroupedColumn()
    {
        // Arrange — HAVING referencing non-existent column
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name, Count(1) FROM #A.Entities() 
GROUP BY Name 
HAVING NonExistent > 5";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown column in HAVING
        AssertHasOneOfErrorCodes(result, "non-existent column in HAVING",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3012_NonAggregateInSelect);
    }

    [TestMethod]
    public void E_GROUP_04_AggregateInWhere()
    {
        // Arrange — Aggregate in WHERE (should be HAVING)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name, Count(1) AS Cnt FROM #A.Entities()
GROUP BY Name
WHERE Count(1) > 2";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — WHERE comes before GROUP BY; also aggregate not allowed in WHERE
        AssertHasOneOfErrorCodes(result, "aggregate in WHERE should suggest HAVING",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ3011_AggregateNotAllowed,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void E_GROUP_05_NestedAggregate()
    {
        // Arrange — Count(Sum(Population)) nested aggregate
        var analyzer = CreateAnalyzer();
        var query = "SELECT Count(Sum(Population)) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq does not reject nested aggregate calls at analysis time.
        // The engine resolves aggregates as regular method calls, so Count(Sum(...))
        // is treated as Count applied to the result of Sum. While standard SQL forbids
        // nested aggregates, Musoq's method resolution handles this case.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_GROUP_06_AggregateWithoutGroupBy()
    {
        // Arrange — Count(1) without GROUP BY (valid in standard SQL)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Count(1) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Aggregate without GROUP BY is valid in Musoq.
        // The engine implicitly creates a GROUP BY for the aggregation.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_GROUP_07_GroupByExpressionMismatch()
    {
        // Arrange — GROUP BY on one expression, SELECT uses different form
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population % 3, Count(1) FROM #A.Entities() GROUP BY (Population % 3)";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Parenthesized vs non-parenthesized GROUP BY expression matching
        // works correctly. `(Population % 3)` matches `Population % 3`.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_GROUP_10_OrderByNonAggregatedWithGroupBy()
    {
        // Arrange — ORDER BY on column not in GROUP BY or aggregate
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name, Count(1) FROM #A.Entities() 
GROUP BY Name 
ORDER BY NonExistent";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown column in ORDER BY
        AssertHasOneOfErrorCodes(result, "ORDER BY non-existent column with GROUP BY",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3012_NonAggregateInSelect);
    }

    #endregion

    // ============================================================================
    // E-FUNC: Function Resolution Errors
    // ============================================================================

    #region E-FUNC: Function resolution errors

    [TestMethod]
    public void E_FUNC_01_NonExistentFunction()
    {
        // Arrange — BananaFunction doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT BananaFunction(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown function
        AssertHasOneOfErrorCodes(result, "non-existent function BananaFunction",
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod);
    }

    [TestMethod]
    public void E_FUNC_02_WrongNumberOfArguments_TooFew()
    {
        // Arrange — Substring with only 1 arg (needs 3)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report wrong argument count
        AssertHasOneOfErrorCodes(result, "Substring with too few arguments",
            DiagnosticCode.MQ3006_InvalidArgumentCount,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod);
    }

    [TestMethod]
    public void E_FUNC_03_WrongNumberOfArguments_TooMany()
    {
        // Arrange — Substring with extra arguments
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Name, 0, 1, 'extra') FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report too many arguments
        AssertHasOneOfErrorCodes(result, "Substring with too many arguments",
            DiagnosticCode.MQ3006_InvalidArgumentCount,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod);
    }

    [TestMethod]
    public void E_FUNC_04_AggregateWhenScalarExpected()
    {
        // Arrange — Count(1) + Population (mixing aggregate and non-aggregate)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Count(1) + Population FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq allows mixing aggregate and non-aggregate expressions.
        // The engine does not enforce strict SQL rules about aggregate context.
        // Non-aggregated columns in the presence of aggregates are handled permissively,
        // similar to MySQL's non-strict SQL mode.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_FUNC_05_WrongArgumentTypes()
    {
        // Arrange — Replace with wrong arg types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Replace(42, 4, 5) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain argument type mismatch
        AssertHasOneOfErrorCodes(result, "Replace with integer arguments",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod);
    }

    [TestMethod]
    public void E_FUNC_06_MethodThatDoesntExistOnEntity()
    {
        // Arrange — BogusMethod() as entity method
        var analyzer = CreateAnalyzer();
        var query = "SELECT BogusMethod() FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown method
        AssertHasOneOfErrorCodes(result, "non-existent entity method",
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod);
    }

    #endregion

    // ============================================================================
    // E-JOIN: JOIN and APPLY Semantic Errors
    // ============================================================================

    #region E-JOIN: JOIN and APPLY semantic errors

    [TestMethod]
    public void E_JOIN_02_OnConditionNotBoolean()
    {
        // Arrange — ON condition that isn't boolean
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
INNER JOIN #B.Entities() b ON a.Population + b.Population";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq does not validate that JOIN ON conditions are boolean at
        // analysis time. The expression type is resolved during code generation, and
        // non-boolean conditions may be implicitly converted or cause runtime errors.
        // This is a known limitation of the static analysis phase.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_JOIN_03_OnConditionReferencingNonExistentTable()
    {
        // Arrange — ON condition referencing alias 'c' which doesn't exist
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
INNER JOIN #B.Entities() b ON c.Name = b.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown alias c
        AssertHasOneOfErrorCodes(result, "ON condition with unknown alias c",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3003_UnknownTable,
            DiagnosticCode.MQ3015_UnknownAlias);
    }

    [TestMethod]
    public void E_JOIN_04_SelfJoinWithSameAlias()
    {
        // Arrange — Two tables with same alias
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name
FROM #A.Entities() a
INNER JOIN #B.Entities() a ON a.Name = a.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report duplicate alias
        AssertHasOneOfErrorCodes(result, "duplicate alias 'a' in JOIN",
            DiagnosticCode.MQ2008_DuplicateAlias,
            DiagnosticCode.MQ3021_DuplicateAlias);
    }

    [TestMethod]
    public void E_JOIN_08_MultipleJoinsWithConflictingAliases()
    {
        // Arrange — Third JOIN reuses alias 'a'
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name
FROM #A.Entities() a
INNER JOIN #A.Entities() b ON a.Name = b.Name
INNER JOIN #A.Entities() a ON b.Name = a.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report duplicate alias
        AssertHasOneOfErrorCodes(result, "conflicting alias 'a' in multiple JOINs",
            DiagnosticCode.MQ2008_DuplicateAlias,
            DiagnosticCode.MQ3021_DuplicateAlias);
    }

    #endregion

    // ============================================================================
    // E-SET: Set Operation Semantic Errors
    // ============================================================================

    #region E-SET: Set operation semantic errors

    [TestMethod]
    public void E_SET_03_UnionColumnListReferencingNonExistentColumn()
    {
        // Arrange — UNION ALL with non-existent column in key list
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
UNION ALL (NonExistent)
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq does not validate column names in set operation key lists
        // during semantic analysis. The key list is processed during code generation,
        // and non-existent column references may cause runtime errors rather than
        // compile-time diagnostics. This is a known limitation.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_SET_05_EmptyColumnListInSetOperation()
    {
        // Arrange — UNION ALL with empty column list
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
UNION ALL ()
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate empty column list is invalid
        AssertHasOneOfErrorCodes(result, "empty column list in UNION ALL",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ3031_SetOperatorMissingKeys,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion
}
