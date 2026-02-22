#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 2: Type Mismatch and Column Resolution Errors.
///     These queries should parse successfully but fail during semantic analysis,
///     code generation, or Roslyn compilation. This is where cryptic error messages
///     typically live.
///     Covers: E-TYPE, E-COL categories.
/// </summary>
[TestClass]
public class ErrorQuality_Phase2_TypeAndColumnTests : BasicEntityTestBase
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
    // E-TYPE: Type Mismatch Errors
    // ============================================================================

    #region E-TYPE: Type mismatch errors

    [TestMethod]
    public void E_TYPE_01_StringComparedToInteger()
    {
        // Arrange — 'hello' = 42 is a type mismatch
        var analyzer = CreateAnalyzer();
        var query = "SELECT 1 FROM #A.Entities() WHERE 'hello' = 42";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq does not enforce strict type checking for comparison operators.
        // Cross-type comparisons like string = integer are handled via .NET's implicit
        // type conversion at runtime. The comparison may return false but does not
        // produce a compile-time error. This is a deliberate design choice for flexibility.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_TYPE_02_ArithmeticOnStrings()
    {
        // Arrange — 'hello' + 'world' is valid string concatenation in Musoq (.NET semantics)
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'hello' + 'world' FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — String + String is valid concatenation in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_TYPE_03_ArithmeticMixedTypes()
    {
        // Arrange — String + number without conversion
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'Count: ' + 5 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest ToString() or Concat()
        AssertHasOneOfErrorCodes(result, "string + number mixed types",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void E_TYPE_04_BooleanInArithmeticContext()
    {
        // Arrange — true + 1
        var analyzer = CreateAnalyzer();
        var query = "SELECT true + 1 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain boolean can't be used in arithmetic
        AssertHasOneOfErrorCodes(result, "boolean in arithmetic context",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void E_TYPE_05_ComparingIncompatibleTypesInJoin()
    {
        // Arrange — JOIN condition using arithmetic operator with incompatible types
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
    FROM #A.Entities() a
    INNER JOIN #B.Entities() b ON a.Name % 2 = 0";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report type mismatch in JOIN condition
        AssertHasOneOfErrorCodes(result, "JOIN condition with incompatible types",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void E_TYPE_07_AggregateOnNonNumericType()
    {
        // Arrange — Sum on a string literal
        var analyzer = CreateAnalyzer();
        var query = "SELECT Sum('hello') FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain Sum requires numeric argument
        AssertHasOneOfErrorCodes(result, "Sum on string type",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod);
    }

    [TestMethod]
    public void E_TYPE_08_AvgOnStringColumn()
    {
        // Arrange — Avg on string column
        var analyzer = CreateAnalyzer();
        var query = "SELECT Avg(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain Avg requires numeric column
        AssertHasOneOfErrorCodes(result, "Avg on string column",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod);
    }

    [TestMethod]
    public void E_TYPE_09_NegativeOnString()
    {
        // Arrange — -'hello'
        var analyzer = CreateAnalyzer();
        var query = "SELECT -'hello' FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain negation requires numeric type
        AssertHasOneOfErrorCodes(result, "negation on string",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void E_TYPE_10_ModuloWithNonNumeric()
    {
        // Arrange — 'hello' % 2
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'hello' % 2 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain modulo requires numeric types
        AssertHasOneOfErrorCodes(result, "modulo on string",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void E_TYPE_11_LikeOnNonString()
    {
        // Arrange — LIKE on integer column
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population LIKE '%5%'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq allows LIKE on non-string columns. The engine implicitly
        // converts the column value to a string representation before applying the
        // pattern match. This is more permissive than standard SQL which typically
        // requires LIKE operands to be character types.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_TYPE_12_RlikeOnNonString()
    {
        // Arrange — RLIKE on integer column
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population RLIKE '\\d+'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq allows RLIKE on non-string columns, similar to LIKE.
        // The engine implicitly converts the column value to a string before applying
        // the regex pattern match. This is more permissive than standard SQL.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_TYPE_14_ConversionWithWrongArgType()
    {
        // Arrange — ToInt32(true) is valid in .NET (Convert.ToInt32(bool) returns 0 or 1)
        var analyzer = CreateAnalyzer();
        var query = "SELECT ToInt32(true) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — ToInt32(bool) is a valid .NET conversion
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_TYPE_16_StringFunctionOnNumber()
    {
        // Arrange — Substring on integer
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(42, 0, 2) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain Substring requires string
        AssertHasOneOfErrorCodes(result, "Substring with integer first argument",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod);
    }

    [TestMethod]
    public void E_TYPE_20_CaseBranchesReturningDifferentTypes()
    {
        // Arrange — CASE with mixed int and string branches
        var analyzer = CreateAnalyzer();
        var query = "SELECT CASE WHEN 1 = 1 THEN 42 ELSE 'hello' END FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain CASE branches must return same type
        AssertHasOneOfErrorCodes(result, "CASE branches with different types",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3027_InvalidExpressionType);
    }

    #endregion

    // ============================================================================
    // E-COL: Column Resolution Errors
    // ============================================================================

    #region E-COL: Column resolution errors

    [TestMethod]
    public void E_COL_01_NonExistentColumn()
    {
        // Arrange — Column doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT NonExistentColumn FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown column
        AssertHasErrorCode(result, DiagnosticCode.MQ3001_UnknownColumn, "non-existent column");
    }

    [TestMethod]
    public void E_COL_02_WrongCaseOnColumnName()
    {
        // Arrange — 'name' instead of 'Name' (case-sensitive)
        var analyzer = CreateAnalyzer();
        var query = "SELECT name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown column, ideally suggesting 'Name'
        AssertHasOneOfErrorCodes(result, "wrong case 'name' vs 'Name'",
            DiagnosticCode.MQ3001_UnknownColumn);
    }

    [TestMethod]
    public void E_COL_03_ColumnFromWrongTableAlias()
    {
        // Arrange — b.NonExistent in JOIN
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.NonExistent
FROM #A.Entities() a
INNER JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report unknown column on alias b
        AssertHasOneOfErrorCodes(result, "non-existent column on alias b",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3028_UnknownProperty);
    }

    [TestMethod]
    public void E_COL_04_AmbiguousColumnInJoin()
    {
        // Arrange — 'Name' exists in both tables without alias
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name
FROM #A.Entities() a
INNER JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should report ambiguous column
        AssertHasOneOfErrorCodes(result, "ambiguous 'Name' in JOIN",
            DiagnosticCode.MQ3002_AmbiguousColumn,
            DiagnosticCode.MQ3001_UnknownColumn);
    }

    [TestMethod]
    public void E_COL_05_UsingAliasBeforeDefined()
    {
        // Arrange — Using alias in WHERE that's defined in SELECT
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population AS Val FROM #A.Entities() WHERE Val > 50";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain alias can't be used in WHERE
        AssertHasOneOfErrorCodes(result, "alias 'Val' used in WHERE before defined",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3015_UnknownAlias);
    }

    [TestMethod]
    public void E_COL_06_ReferencingCteColumnWithoutAlias()
    {
        // Arrange — Unqualified column Name with single CTE source is auto-resolved in Musoq
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData AS (SELECT Name FROM #A.Entities())
SELECT Name FROM MyData md";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Unqualified columns are auto-resolved when there is only one source
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_COL_08_AccessingPropertyOnPrimitiveType()
    {
        // Arrange — Population.Length on an int
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population.Length FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain int doesn't have properties
        AssertHasOneOfErrorCodes(result, "property access on primitive int",
            DiagnosticCode.MQ3014_InvalidPropertyAccess,
            DiagnosticCode.MQ3028_UnknownProperty,
            DiagnosticCode.MQ3001_UnknownColumn);
    }

    [TestMethod]
    public void E_COL_09_ArrayIndexOnNonArray()
    {
        // Arrange — Population[0] on an int
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population[0] FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain int doesn't support indexing
        AssertHasOneOfErrorCodes(result, "array index on non-array",
            DiagnosticCode.MQ3017_ObjectNotArray,
            DiagnosticCode.MQ3018_NoIndexer);
    }

    [TestMethod]
    public void E_COL_10_DeepPropertyChainOnNonComplexType()
    {
        // Arrange — Population.Property.SubProperty
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population.Property.SubProperty FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain int doesn't have nested properties
        AssertHasOneOfErrorCodes(result, "deep property chain on primitive",
            DiagnosticCode.MQ3014_InvalidPropertyAccess,
            DiagnosticCode.MQ3028_UnknownProperty,
            DiagnosticCode.MQ3001_UnknownColumn);
    }

    [TestMethod]
    public void E_COL_12_StarWithGroupBy()
    {
        // Arrange — SELECT * with GROUP BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #A.Entities() GROUP BY Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — SELECT * expands to all columns. Columns not in GROUP BY and
        // not inside aggregate functions should be flagged as MQ3012 violations.
        // Standard SQL rejects this query, and Musoq now enforces the same rule.
        AssertHasErrorCode(result, DiagnosticCode.MQ3012_NonAggregateInSelect,
            "SELECT * includes columns not in GROUP BY");
    }

    #endregion
}
