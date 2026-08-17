using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 2: Type Mismatch and Column Resolution Errors.
///     These queries should parse successfully but fail during semantic analysis,
///     code generation, or Roslyn compilation. This is where cryptic error messages
///     typically live.
///     Covers: E-TYPE, E-COL categories.
/// </summary>
[TestClass]
public class ErrorQualityTypeAndColumnTests : BasicEntityTestBase
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3007_InvalidOperandTypes, "string + number mixed types");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3007_InvalidOperandTypes, "boolean in arithmetic context");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3007_InvalidOperandTypes, "JOIN condition with incompatible types");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3088_NoMatchingCallableOverload, "Sum on string type");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3088_NoMatchingCallableOverload, "Avg on string column");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3007_InvalidOperandTypes, "negation on string");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3007_InvalidOperandTypes, "modulo on string");
    }

    [TestMethod]
    public void E_TYPE_11_LikeOnNonString()
    {
        // Arrange — LIKE on integer column
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population LIKE '%5%'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — LIKE should now require string operands and report a clear bind-time diagnostic.
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3005_TypeMismatch, "LIKE on non-string");
    }

    [TestMethod]
    public void E_TYPE_12_RlikeOnNonString()
    {
        // Arrange — RLIKE on integer column
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population RLIKE '\\d+'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — RLIKE should now require string operands and report a clear bind-time diagnostic.
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3005_TypeMismatch, "RLIKE on non-string");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3088_NoMatchingCallableOverload, "Substring with integer first argument");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3027_InvalidExpressionType, "CASE branches with different types");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3001_UnknownColumn, "wrong case 'name' vs 'Name'");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3001_UnknownColumn, "non-existent column on alias b");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3002_AmbiguousColumn, "ambiguous 'Name' in JOIN");
    }

    [TestMethod]
    public void E_COL_05_UsingSelectAliasInWhere_ShouldAnalyzeSuccessfully()
    {
        // Arrange — SELECT aliases are visible in WHERE in runtime v2.
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population AS Val FROM #A.Entities() WHERE Val > 50";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        AssertNoErrors(result);
        Assert.IsTrue(result.IsParsed);
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3028_UnknownProperty, "property access on primitive int");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3017_ObjectNotArray, "array index on non-array");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3028_UnknownProperty, "deep property chain on primitive");
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
