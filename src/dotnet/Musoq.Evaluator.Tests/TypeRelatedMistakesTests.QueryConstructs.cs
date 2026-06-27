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

        // Assert - SUM expects numeric argument
        DocumentTypeHandling(result, "SUM on string",
            DiagnosticCode.MQ3029_UnresolvableMethod);
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
            DiagnosticCode.MQ3029_UnresolvableMethod);
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
            DiagnosticCode.MQ3029_UnresolvableMethod);
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
            DiagnosticCode.MQ3005_TypeMismatch);
    }

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
            DiagnosticCode.MQ3029_UnresolvableMethod);
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
            DiagnosticCode.MQ3029_UnresolvableMethod);
    }

    [TestMethod]
    public void Union_MismatchedColumnTypes_ShouldHandle()
    {
        // Arrange - UNION with different column types and an explicit key list
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL (Name)
            SELECT Population FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Should detect type mismatch or structural issues
        DocumentTypeHandling(result, "UNION with mismatched column types",
            DiagnosticCode.MQ3005_TypeMismatch);
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
            DiagnosticCode.MQ3005_TypeMismatch);
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
            DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Union_OmittedKeyColumns_ShouldAnalyze()
    {
        // Arrange - UNION ALL without key columns compares all projected fields
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL
            SELECT Name FROM #B.Entities()";

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
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL ()
            SELECT Name FROM #B.Entities()";

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

        // Assert - ORDER BY with mixed-type expression
        DocumentTypeHandling(result, "ORDER BY with string + int expression",
            DiagnosticCode.MQ3007_InvalidOperandTypes);
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
            DiagnosticCode.MQ3005_TypeMismatch);
    }

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
            DiagnosticCode.MQ3012_NonAggregateInSelect);
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
            DiagnosticCode.MQ3007_InvalidOperandTypes);
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
            DiagnosticCode.MQ3005_TypeMismatch);
    }

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
            DiagnosticCode.MQ3007_InvalidOperandTypes);
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
            DiagnosticCode.MQ3005_TypeMismatch);
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

        // Assert - Accessing Length on int type
        DocumentTypeHandling(result, "Property access on primitive type",
            DiagnosticCode.MQ3001_UnknownColumn);
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
            DiagnosticCode.MQ3001_UnknownColumn);
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
            DiagnosticCode.MQ2001_UnexpectedToken);
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
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

}
