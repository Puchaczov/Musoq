using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests for type-related user mistakes in queries.
///     These tests verify that type errors are caught gracefully at the semantic analysis layer
///     (before Roslyn code generation) and provide helpful, readable error messages
///     suitable for LSP and LLM agentic tooling.
/// </summary>
public partial class TypeRelatedMistakesTests
{
    [TestMethod]
    public void Aggregate_SumOnString_ShouldHandle()
    {
        // Arrange - SUM requires numeric type
        var analyzer = CreateAnalyzer();
        var query = "SELECT Sum(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "SUM on string", DiagnosticCode.MQ3088_NoMatchingCallableOverload);
    }

    [TestMethod]
    public void Aggregate_AvgOnString_ShouldHandle()
    {
        // Arrange - AVG requires numeric type
        var analyzer = CreateAnalyzer();
        var query = "SELECT Avg(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "AVG on string", DiagnosticCode.MQ3088_NoMatchingCallableOverload);
    }

    [TestMethod]
    public void Aggregate_MinOnMixedTypes_ShouldHandle()
    {
        // Arrange - MIN should work but on appropriate types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Min(Name + Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "MIN on string + number expression", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Aggregate_SumOnBoolean_ShouldHandle()
    {
        // Arrange - SUM on boolean expression
        var analyzer = CreateAnalyzer();
        var query = "SELECT Sum(Name = 'test') FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "SUM on boolean", DiagnosticCode.MQ3088_NoMatchingCallableOverload);
    }

    [TestMethod]
    public void Aggregate_CountWithWrongArgs_ShouldHandle()
    {
        // Arrange - COUNT with too many args
        var analyzer = CreateAnalyzer();
        var query = "SELECT Count(Name, Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "COUNT with too many arguments", DiagnosticCode.MQ3088_NoMatchingCallableOverload);
    }

    [TestMethod]
    public void Case_MismatchedThenTypes_ShouldHandle()
    {
        // Arrange - THEN branches return different types
        var analyzer = CreateAnalyzer();
        var query = (
            "\n"
            + "            SELECT CASE\n"
            + "                WHEN Population > 100 THEN 'Large'\n"
            + "                WHEN Population > 50 THEN 123\n"
            + "                ELSE true\n"
            + "            END\n"
            + "            FROM #A.Entities()"
        );

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "CASE with mismatched THEN types", DiagnosticCode.MQ3027_InvalidExpressionType);
    }

    [TestMethod]
    public void Case_NonBooleanWhenCondition_ShouldHandle()
    {
        // Arrange - WHEN expects boolean
        var analyzer = CreateAnalyzer();
        var query = (
            "\n"
            + "            SELECT CASE\n"
            + "                WHEN Name THEN 'Has name'\n"
            + "                ELSE 'No name'\n"
            + "            END\n"
            + "            FROM #A.Entities()"
        );

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "CASE WHEN with non-boolean", DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Case_SimpleCase_TypeMismatch_ShouldHandle()
    {
        // Arrange - simple CASE with mismatched comparison types
        var analyzer = CreateAnalyzer();
        var query = (
            "\n"
            + "            SELECT CASE Name\n"
            + "                WHEN 123 THEN 'Number match'\n"
            + "                WHEN 'Warsaw' THEN 'String match'\n"
            + "                ELSE 'Other'\n"
            + "            END\n"
            + "            FROM #A.Entities()"
        );

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "simple CASE with type mismatch");
    }

    [TestMethod]
    public void Coalesce_MixedTypes_ShouldHandle()
    {
        // Arrange - COALESCE with mixed types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Coalesce(Name, 123, true) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "COALESCE with mixed types");
    }

    [TestMethod]
    public void Coalesce_StringAndNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Coalesce(Name, Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "COALESCE string and number");
    }

    [TestMethod]
    public void Union_MismatchedColumnTypes_ShouldHandle()
    {
        // Arrange - UNION with different column types and an explicit key list
        var analyzer = CreateAnalyzer();
        var query = (
            "\n"
            + "            SELECT Name FROM #A.Entities()\n"
            + "            UNION ALL (Name)\n"
            + "            SELECT Population FROM #B.Entities()"
        );

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "UNION with mismatched column types", DiagnosticCode.MQ3020_SetOperatorColumnTypes);
    }

    [TestMethod]
    public void Except_MismatchedColumnTypes_ShouldHandle()
    {
        // Arrange - EXCEPT with type mismatch
        var analyzer = CreateAnalyzer();
        var query = (
            "\n"
            + "            SELECT Name, Population FROM #A.Entities()\n"
            + "            EXCEPT (Name)\n"
            + "            SELECT Population, Name FROM #B.Entities()"
        );

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(
            result,
            "EXCEPT with mismatched column types",
            DiagnosticCode.MQ3020_SetOperatorColumnTypes,
            DiagnosticCode.MQ3020_SetOperatorColumnTypes
        );
    }

    [TestMethod]
    public void Intersect_MismatchedColumnTypes_ShouldHandle()
    {
        // Arrange - INTERSECT with type mismatch
        var analyzer = CreateAnalyzer();
        var query = (
            "\n"
            + "            SELECT Name FROM #A.Entities()\n"
            + "            INTERSECT (Name)\n"
            + "            SELECT Population FROM #B.Entities()"
        );

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(
            result,
            "INTERSECT with mismatched column types",
            DiagnosticCode.MQ3020_SetOperatorColumnTypes
        );
    }

    [TestMethod]
    public void Union_OmittedKeyColumns_ShouldAnalyze()
    {
        // Arrange - UNION ALL without key columns compares all projected fields
        var analyzer = CreateAnalyzer();
        var query = (
            "\n"
            + "            SELECT Name FROM #A.Entities()\n"
            + "            UNION ALL\n"
            + "            SELECT Name FROM #B.Entities()"
        );

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Union_EmptyKeyColumns_ShouldAnalyze()
    {
        // Arrange - UNION ALL with empty key column list compares all projected fields
        var analyzer = CreateAnalyzer();
        var query = (
            "\n"
            + "            SELECT Name FROM #A.Entities()\n"
            + "            UNION ALL ()\n"
            + "            SELECT Name FROM #B.Entities()"
        );

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        AssertNoErrors(result);
    }

    [TestMethod]
    public void OrderBy_ComplexExpressionType_ShouldHandle()
    {
        // Arrange - ORDER BY with complex expression
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name + Population";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "ORDER BY with string + int expression", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void OrderBy_BooleanExpression_ShouldHandle()
    {
        // Arrange - ORDER BY boolean
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "ORDER BY boolean expression");
    }

    [TestMethod]
    public void GroupBy_NonAggregateExpression_ShouldHandle()
    {
        // Arrange - Using non-grouped column in SELECT with GROUP BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, City FROM #A.Entities() GROUP BY Name";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "Non-aggregated column in GROUP BY", DiagnosticCode.MQ3012_NonAggregateInSelect);
    }

    [TestMethod]
    public void GroupBy_ExpressionTypeMismatch_ShouldHandle()
    {
        // Arrange - GROUP BY on expression with type issues
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + Population, Count(*) FROM #A.Entities() GROUP BY Name + Population";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "GROUP BY with string + int expression", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Having_NonBooleanCondition_ShouldHandle()
    {
        // Arrange - HAVING expects boolean
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(*) FROM #A.Entities() GROUP BY Name HAVING Count(*)";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "HAVING with non-boolean condition", DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Null_ArithmeticWithNull_ShouldHandle()
    {
        // Arrange - Arithmetic with NULL
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population + NULL FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "Arithmetic with NULL literal");
    }

    [TestMethod]
    public void Null_ComparisonWithNull_ShouldHandle()
    {
        // Arrange - Direct comparison with NULL (should use IS NULL)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = NULL";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "Direct comparison with NULL");
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
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Property_OnPrimitiveType_ShouldHandle()
    {
        // Arrange - Accessing property on primitive
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population.Length FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "Property access on primitive type", DiagnosticCode.MQ3028_UnknownProperty);
    }

    [TestMethod]
    public void Property_ChainedOnWrongType_ShouldHandle()
    {
        // Arrange - Chained property access that doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name.Value.Count FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "Chained property access on wrong type", DiagnosticCode.MQ3028_UnknownProperty);
    }

    [TestMethod]
    public void Index_OnNonIndexable_ShouldHandle()
    {
        // Arrange - Array index on non-array
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population[0] FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "Index access on primitive", DiagnosticCode.MQ3017_ObjectNotArray);
    }

    [TestMethod]
    public void Index_StringIndex_ShouldHandle()
    {
        // Arrange - Indexing with string instead of number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name['test'] FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "String index access", DiagnosticCode.MQ3018_NoIndexer);
    }
}
