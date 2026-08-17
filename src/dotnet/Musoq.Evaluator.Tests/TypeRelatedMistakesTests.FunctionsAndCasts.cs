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
    public void Function_SubstringOnNumber_ShouldHandle()
    {
        // Arrange - Substring expects string, got number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Population, 1, 2) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Substring requires string first argument
        DocumentTypeHandling(result, "Substring on number",
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            DiagnosticCode.MQ3087_InvalidCallableArity);
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            DiagnosticCode.MQ3087_InvalidCallableArity);
    }

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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload);
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload);
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload);
    }

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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload);
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
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
            DiagnosticCode.MQ3088_NoMatchingCallableOverload);
    }

    [TestMethod]
    public void Numeric_DecimalAndIntMix_ShouldHandle()
    {
        // Arrange - Mixing decimal and int
        var analyzer = CreateAnalyzer();
        var query = "SELECT Money + Population FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Decimal + int should coerce properly
        AssertNoErrors(result);
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
        AssertNoErrors(result);
    }

}
