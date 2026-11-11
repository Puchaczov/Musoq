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
    public void Arithmetic_StringPlusNumber_ShouldHandle()
    {
        // Arrange - adding string to number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + 123 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string + number", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_StringMinusString_ShouldHandle()
    {
        // Arrange - subtracting strings (invalid in most contexts)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name - City FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string - string", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_StringMultiplyNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name * 5 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string * number", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_StringDivideNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name / 2 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string / number", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_BooleanPlusNumber_ShouldHandle()
    {
        // Arrange - adding boolean result to number
        var analyzer = CreateAnalyzer();
        var query = "SELECT (Name = 'test') + Population FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "boolean + number", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Arithmetic_ModuloOnString_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name % 10 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string % number", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Comparison_StringEqualsNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 123";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string = number comparison");
    }

    [TestMethod]
    public void Comparison_NumberEqualsBoolean_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population = true";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "number = boolean comparison", DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Comparison_StringGreaterThanNumber_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name > 100";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string > number comparison");
    }

    [TestMethod]
    public void Comparison_MixedTypesInIN_ShouldHandle()
    {
        // Arrange - IN clause with mixed types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IN (1, 'test', 3.14)";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(
            result,
            "mixed types in IN clause",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3005_TypeMismatch
        );
    }

    [TestMethod]
    public void Comparison_StringBetweenNumbers_ShouldHandle()
    {
        // Arrange - BETWEEN with incompatible types
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name BETWEEN 1 AND 100";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string BETWEEN numbers", DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Comparison_NumberLikePattern_ShouldHandle()
    {
        // Arrange - LIKE on number column
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population LIKE '%test%'";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "number LIKE pattern", DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Boolean_NonBooleanInWhere_ShouldHandle()
    {
        // Arrange - WHERE expects boolean, got string
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string in WHERE (non-boolean)", DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Boolean_NumberInWhere_ShouldHandle()
    {
        // Arrange - WHERE expects boolean, got number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "number in WHERE (non-boolean)", DiagnosticCode.MQ3005_TypeMismatch);
    }

    [TestMethod]
    public void Boolean_AndWithNonBoolean_ShouldHandle()
    {
        // Arrange - AND operator with non-boolean operand
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name AND City";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "string AND string (non-boolean)", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Boolean_OrWithNonBoolean_ShouldHandle()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test' OR Population";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(result, "boolean OR number (non-boolean)", DiagnosticCode.MQ3007_InvalidOperandTypes);
    }

    [TestMethod]
    public void Boolean_NotOnString_ShouldHandle()
    {
        // Arrange - NOT operator on string
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE NOT Name";

        // Act
        var result = analyzer.Analyze(query);
        AssertTypeHandling(
            result,
            "NOT on string (non-boolean)",
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticCode.MQ3005_TypeMismatch
        );
    }
}
