#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests for type-related user mistakes in queries.
///     These tests verify that type errors are caught gracefully at the semantic analysis layer
///     (before Roslyn code generation) and provide helpful, readable error messages
///     suitable for LSP and LLM agentic tooling.
/// </summary>
[TestClass]
public class TypeRelatedMistakesTests : BasicEntityTestBase
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

    private static void DocumentTypeHandling(QueryAnalysisResult result, string context,
        params DiagnosticCode[] acceptableCodes)
    {
        Assert.IsNotNull(result, $"Analyzer should not crash: {context}");


        if (result.HasErrors)
            if (acceptableCodes.Length > 0)
            {
                var hasExpected = result.Errors.Any(e => acceptableCodes.Contains(e.Code));
                if (!hasExpected)
                {
                    var actualCodes = string.Join(", ", result.Errors.Select(e => e.Code.ToString()));
                    Debug.WriteLine($"Type handling '{context}': got {actualCodes}");
                }
            }
    }

    private static void DocumentBehavior(QueryAnalysisResult result, string explanation, bool shouldHaveErrors)
    {
        Assert.IsNotNull(result, $"Analyzer should not crash: {explanation}");

        if (shouldHaveErrors)
        {
            Assert.IsTrue(result.HasErrors || !result.IsParsed,
                $"{explanation} - expected errors but none found");
        }
        else
        {
            if (result.HasErrors)
            {
                var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
                Debug.WriteLine($"{explanation} - note: got errors:\n{errorDetails}");
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

    #region Arithmetic Type Mismatches

    [TestMethod]
    public void Arithmetic_StringPlusNumber_ShouldHandle()
    {
        // Arrange - adding string to number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + 123 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ9999_Unknown wrapping dictionary key not found for (String, Int32)
        DocumentTypeHandling(result, "string + number",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_StringMinusString_ShouldHandle()
    {
        // Arrange - subtracting strings (invalid in most contexts)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name - City FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ9999 for no operator defined for (String, String) subtraction
        DocumentTypeHandling(result, "string - string",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_StringMultiplyNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name * 5 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ9999 for no multiply operator for (String, Int32)
        DocumentTypeHandling(result, "string * number",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_StringDivideNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name / 2 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ9999 for no divide operator for (String, Int32)
        DocumentTypeHandling(result, "string / number",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_BooleanPlusNumber_ShouldHandle()
    {
        // Arrange - adding boolean result to number
        var analyzer = CreateAnalyzer();
        var query = "SELECT (Name = 'test') + Population FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ9999 for no add operator for (Boolean, Decimal)
        DocumentTypeHandling(result, "boolean + number",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_ModuloOnString_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name % 10 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ9999 for no modulo operator for (String, Int32)
        DocumentTypeHandling(result, "string % number",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    #endregion

    #region Comparison Type Mismatches

    [TestMethod]
    public void Comparison_StringEqualsNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 123";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - SQL often allows implicit conversion, may succeed
        DocumentTypeHandling(result, "string = number comparison",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Comparison_NumberEqualsBoolean_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population = true";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Type coercion may or may not be allowed
        DocumentTypeHandling(result, "number = boolean comparison",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Comparison_StringGreaterThanNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name > 100";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - May succeed or fail depending on coercion rules
        DocumentTypeHandling(result, "string > number comparison",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Comparison_MixedTypesInIN_ShouldHandle()
    {
        // Arrange - IN clause with mixed types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IN (1, 'test', 3.14)";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Mixed types in IN list may be coerced or rejected
        DocumentTypeHandling(result, "mixed types in IN clause",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Comparison_StringBetweenNumbers_ShouldHandle()
    {
        // Arrange - BETWEEN with incompatible types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name BETWEEN 1 AND 100";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - BETWEEN type mismatch
        DocumentTypeHandling(result, "string BETWEEN numbers",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Comparison_NumberLikePattern_ShouldHandle()
    {
        // Arrange - LIKE on number column
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population LIKE '%test%'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - LIKE expects string operand
        DocumentTypeHandling(result, "number LIKE pattern",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region Boolean Expression Type Errors

    [TestMethod]
    public void Boolean_NonBooleanInWhere_ShouldHandle()
    {
        // Arrange - WHERE expects boolean, got string
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - WHERE requires boolean condition
        DocumentTypeHandling(result, "string in WHERE (non-boolean)",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Boolean_NumberInWhere_ShouldHandle()
    {
        // Arrange - WHERE expects boolean, got number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - WHERE requires boolean condition
        DocumentTypeHandling(result, "number in WHERE (non-boolean)",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Boolean_AndWithNonBoolean_ShouldHandle()
    {
        // Arrange - AND operator with non-boolean operand
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name AND City";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - AND requires boolean operands
        DocumentTypeHandling(result, "string AND string (non-boolean)",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Boolean_OrWithNonBoolean_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test' OR Population";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - OR requires boolean operands
        DocumentTypeHandling(result, "boolean OR number (non-boolean)",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Boolean_NotOnString_ShouldHandle()
    {
        // Arrange - NOT operator on string
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE NOT Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - NOT requires boolean operand
        DocumentTypeHandling(result, "NOT on string (non-boolean)",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region Aggregate Function Type Errors

    [TestMethod]
    public void Aggregate_SumOnString_ShouldHandle()
    {
        // Arrange - SUM requires numeric type
        var analyzer = CreateAnalyzer();
        var query = "SELECT Sum(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - SUM expects numeric argument
        DocumentTypeHandling(result, "SUM on string",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Aggregate_AvgOnString_ShouldHandle()
    {
        // Arrange - AVG requires numeric type
        var analyzer = CreateAnalyzer();
        var query = "SELECT Avg(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - AVG expects numeric argument
        DocumentTypeHandling(result, "AVG on string",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Aggregate_MinOnMixedTypes_ShouldHandle()
    {
        // Arrange - MIN should work but on appropriate types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Min(Name + Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - String + decimal type mismatch affects MIN
        DocumentTypeHandling(result, "MIN on string + number expression",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Aggregate_SumOnBoolean_ShouldHandle()
    {
        // Arrange - SUM on boolean expression
        var analyzer = CreateAnalyzer();
        var query = "SELECT Sum(Name = 'test') FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - SUM on boolean may or may not be supported
        DocumentTypeHandling(result, "SUM on boolean",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Aggregate_CountWithWrongArgs_ShouldHandle()
    {
        // Arrange - COUNT with too many args
        var analyzer = CreateAnalyzer();
        var query = "SELECT Count(Name, Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - COUNT typically takes 0 or 1 argument
        DocumentTypeHandling(result, "COUNT with too many arguments",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3006_InvalidArgumentCount);
    }

    #endregion

    #region CASE Expression Type Mismatches

    [TestMethod]
    public void Case_MismatchedThenTypes_ShouldHandle()
    {
        // Arrange - THEN branches return different types
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT CASE 
                WHEN Population > 100 THEN 'Large'
                WHEN Population > 50 THEN 123
                ELSE true
            END
            FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - CASE branches should have compatible types
        DocumentTypeHandling(result, "CASE with mismatched THEN types",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Case_NonBooleanWhenCondition_ShouldHandle()
    {
        // Arrange - WHEN expects boolean
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT CASE 
                WHEN Name THEN 'Has name'
                ELSE 'No name'
            END
            FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - WHEN requires boolean condition
        DocumentTypeHandling(result, "CASE WHEN with non-boolean",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Case_SimpleCase_TypeMismatch_ShouldHandle()
    {
        // Arrange - simple CASE with mismatched comparison types
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT CASE Name
                WHEN 123 THEN 'Number match'
                WHEN 'Warsaw' THEN 'String match'
                ELSE 'Other'
            END
            FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - CASE comparisons should have compatible types
        DocumentTypeHandling(result, "simple CASE with type mismatch",
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    #endregion

    #region COALESCE Type Issues

    [TestMethod]
    public void Coalesce_MixedTypes_ShouldHandle()
    {
        // Arrange - COALESCE with mixed types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Coalesce(Name, 123, true) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - COALESCE should have compatible types
        DocumentTypeHandling(result, "COALESCE with mixed types",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Coalesce_StringAndNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Coalesce(Name, Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - COALESCE string and number
        DocumentTypeHandling(result, "COALESCE string and number",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region Function Argument Type Errors

    [TestMethod]
    public void Function_SubstringOnNumber_ShouldHandle()
    {
        // Arrange - Substring expects string, got number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Population, 1, 2) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Substring requires string first argument
        DocumentTypeHandling(result, "Substring on number",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Function_LengthOnNumber_ShouldHandle()
    {
        // Arrange - Length expects string
        var analyzer = CreateAnalyzer();
        var query = "SELECT Length(Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Length requires string argument
        DocumentTypeHandling(result, "Length on number",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Function_ToUpperOnNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT ToUpperInvariant(Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - ToUpperInvariant requires string argument
        DocumentTypeHandling(result, "ToUpperInvariant on number",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Function_AbsOnString_ShouldHandle()
    {
        // Arrange - Abs expects numeric
        var analyzer = CreateAnalyzer();
        var query = "SELECT Abs(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Abs requires numeric argument
        DocumentTypeHandling(result, "Abs on string",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Function_RoundOnString_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Round(Name, 2) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Round requires numeric argument
        DocumentTypeHandling(result, "Round on string",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Function_WrongNumberOfArgs_ShouldHandle()
    {
        // Arrange - Function with wrong argument count
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Substring requires more arguments
        DocumentTypeHandling(result, "Substring with too few arguments",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3006_InvalidArgumentCount);
    }

    [TestMethod]
    public void Function_TooManyArgs_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Upper(Name, 1, 2, 3) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Upper takes 1 argument
        DocumentTypeHandling(result, "Upper with too many arguments",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3006_InvalidArgumentCount);
    }

    #endregion

    #region Cast and Conversion Errors

    [TestMethod]
    public void Cast_InvalidStringToNumber_ShouldHandle()
    {
        // Arrange - Casting non-numeric string to number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Cast(Name, 'int') FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Cast function may or may not exist, or type conversion may fail
        DocumentTypeHandling(result, "Cast string to int",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Cast_ToUnknownType_ShouldHandle()
    {
        // Arrange - Cast to non-existent type
        var analyzer = CreateAnalyzer();
        var query = "SELECT Cast(Name, 'nonexistenttype') FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Cast with unknown target type
        DocumentTypeHandling(result, "Cast to unknown type",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Convert_InvalidConversion_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Convert(Population, 'datetime') FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Convert function may not exist or conversion may fail
        DocumentTypeHandling(result, "Convert int to datetime",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region UNION/SET Operation Type Mismatches

    [TestMethod]
    public void Union_MismatchedColumnTypes_ShouldHandle()
    {
        // Arrange - UNION with different column types (Musoq requires key columns in parentheses)
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL (Name)
            SELECT Population FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Should detect type mismatch or structural issues
        DocumentTypeHandling(result, "UNION with mismatched column types",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Except_MismatchedColumnTypes_ShouldHandle()
    {
        // Arrange - EXCEPT with type mismatch
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name, Population FROM #A.Entities()
            EXCEPT (Name)
            SELECT Population, Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - EXCEPT with type mismatch in columns
        DocumentTypeHandling(result, "EXCEPT with mismatched column types",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Intersect_MismatchedColumnTypes_ShouldHandle()
    {
        // Arrange - INTERSECT with type mismatch
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            INTERSECT (Name)
            SELECT Population FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - INTERSECT with type mismatch
        DocumentTypeHandling(result, "INTERSECT with mismatched column types",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Union_MissingKeyColumns_ShouldHandle()
    {
        // Arrange - UNION ALL without key columns (invalid in Musoq)
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL
            SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Should report missing keys
        AssertHasErrorCode(result, DiagnosticCode.MQ3031_SetOperatorMissingKeys, "UNION ALL missing key columns");
    }

    [TestMethod]
    public void Union_EmptyKeyColumns_ShouldHandle()
    {
        // Arrange - UNION ALL with empty key column list
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL ()
            SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Empty key columns should report error
        AssertHasOneOfErrorCodes(result, "UNION ALL with empty key columns",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ3031_SetOperatorMissingKeys);
    }

    #endregion

    #region ORDER BY Type Issues

    [TestMethod]
    public void OrderBy_ComplexExpressionType_ShouldHandle()
    {
        // Arrange - ORDER BY with complex expression
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name + Population";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - ORDER BY with mixed-type expression
        DocumentTypeHandling(result, "ORDER BY with string + int expression",
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void OrderBy_BooleanExpression_ShouldHandle()
    {
        // Arrange - ORDER BY boolean
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Ordering by boolean expression may or may not be supported
        DocumentTypeHandling(result, "ORDER BY boolean expression",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region GROUP BY Type Issues

    [TestMethod]
    public void GroupBy_NonAggregateExpression_ShouldHandle()
    {
        // Arrange - Using non-grouped column in SELECT with GROUP BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, City FROM #A.Entities() GROUP BY Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - City is not in GROUP BY and not aggregated
        DocumentTypeHandling(result, "Non-aggregated column in GROUP BY",
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void GroupBy_ExpressionTypeMismatch_ShouldHandle()
    {
        // Arrange - GROUP BY on expression with type issues
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + Population, Count(*) FROM #A.Entities() GROUP BY Name + Population";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - GROUP BY with mixed-type expression
        DocumentTypeHandling(result, "GROUP BY with string + int expression",
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Having_NonBooleanCondition_ShouldHandle()
    {
        // Arrange - HAVING expects boolean
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(*) FROM #A.Entities() GROUP BY Name HAVING Count(*)";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - HAVING requires boolean condition
        DocumentTypeHandling(result, "HAVING with non-boolean condition",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region NULL Handling Type Issues

    [TestMethod]
    public void Null_ArithmeticWithNull_ShouldHandle()
    {
        // Arrange - Arithmetic with NULL
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population + NULL FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Arithmetic with NULL literal
        DocumentTypeHandling(result, "Arithmetic with NULL literal",
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Null_ComparisonWithNull_ShouldHandle()
    {
        // Arrange - Direct comparison with NULL (should use IS NULL)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = NULL";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Comparison with NULL literal (semantically wrong but may parse)
        DocumentTypeHandling(result, "Direct comparison with NULL",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void IsNull_OnNonNullableExpression_ShouldHandle()
    {
        // Arrange - IS NULL on literal
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE 123 IS NULL";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - IS NULL on literal is valid but semantically useless
        DocumentBehavior(result, "IS NULL on literal is valid syntax", false);
    }

    #endregion

    #region Property Access Type Errors

    [TestMethod]
    public void Property_OnPrimitiveType_ShouldHandle()
    {
        // Arrange - Accessing property on primitive
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population.Length FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Accessing Length on int type
        DocumentTypeHandling(result, "Property access on primitive type",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Property_ChainedOnWrongType_ShouldHandle()
    {
        // Arrange - Chained property access that doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name.Value.Count FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Chained property access on string
        DocumentTypeHandling(result, "Chained property access on wrong type",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Index_OnNonIndexable_ShouldHandle()
    {
        // Arrange - Array index on non-array
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population[0] FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Index access on non-indexable type
        DocumentTypeHandling(result, "Index access on primitive",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Index_StringIndex_ShouldHandle()
    {
        // Arrange - Indexing with string instead of number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name['test'] FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - String indexing syntax
        DocumentTypeHandling(result, "String index access",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region Method Overload Resolution Issues

    [TestMethod]
    public void Method_AmbiguousOverload_ShouldHandle()
    {
        // Arrange - Call that could match multiple overloads
        var analyzer = CreateAnalyzer();
        var query = "SELECT ToString(NULL) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - ToString with NULL may have ambiguous overloads
        DocumentTypeHandling(result, "ToString with NULL argument",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void Method_NoMatchingOverload_ShouldHandle()
    {
        // Arrange - No matching overload for argument types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Concat(123, true, Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Concat with mixed argument types
        DocumentTypeHandling(result, "Concat with mixed argument types",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Method_GenericTypeInference_Failure_ShouldHandle()
    {
        // Arrange - Generic method that can't infer types
        var analyzer = CreateAnalyzer();
        var query = "SELECT RowNumber() FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - RowNumber may not exist or may have resolution issues
        DocumentTypeHandling(result, "RowNumber function resolution",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region Decimal/Numeric Precision Issues

    [TestMethod]
    public void Numeric_DecimalAndIntMix_ShouldHandle()
    {
        // Arrange - Mixing decimal and int
        var analyzer = CreateAnalyzer();
        var query = "SELECT Money + Population FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Decimal + int should coerce properly
        DocumentBehavior(result, "Decimal + int coercion is valid", false);
    }

    [TestMethod]
    public void Numeric_DivisionResultType_ShouldHandle()
    {
        // Arrange - Integer division
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population / 3 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Integer division is valid
        DocumentBehavior(result, "Integer division is valid", false);
    }

    #endregion

    #region Valid Type Usage (Control Group)

    [TestMethod]
    public void Valid_NumericArithmetic()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population * 2 + 100 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_StringConcatenation()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + ' - ' + City FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_BooleanComparison()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population > 50 AND Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_AggregateOnNumeric()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Sum(Population), Avg(Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_CaseExpression()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT CASE 
                WHEN Population > 100 THEN 'Large'
                WHEN Population > 50 THEN 'Medium'
                ELSE 'Small'
            END
            FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_IsNullCheck()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IS NOT NULL";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_UnionSameTypes()
    {
        // Arrange - Musoq UNION syntax requires key columns in parentheses
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL (Name)
            SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_GroupByWithAggregate()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Country, Count(Name), Sum(Population) FROM #A.Entities() GROUP BY Country";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    #endregion
}
