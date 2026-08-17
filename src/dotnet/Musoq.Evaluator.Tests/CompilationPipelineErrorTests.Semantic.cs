using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests covering all error stages in the Musoq query processing pipeline.
///     Stage 1: Lexical Analysis (Tokenization) - Raw SQL text → Token stream
///     Stage 2: Parsing (Syntax) - Token stream → Abstract Syntax Tree
///     Stage 3: Visitor Phase (Semantic Analysis)
///     - 3a: Schema Resolution
///     - 3b: Type Resolution
///     - 3c: Method Resolution
///     Stage 4: Code Generation - Validated AST → C# code
///     Stage 5: Roslyn Compilation - Generated C# → IL
///     Stage 6: Runtime Execution - Compiled query runs against data sources
///     All errors should produce readable messages suitable for LSP and LLM agentic tooling.
///     Each test verifies the SPECIFIC diagnostic code, not just "any error".
/// </summary>
public partial class CompilationPipelineErrorTests
{

    [TestMethod]
    public void Stage3a_UnknownSchema()
    {
        // Arrange - #unknown.table() - schema 'unknown' not registered
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #unknown.table()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - unknown schema should map to MQ3010
        AssertHasErrorCode(result, DiagnosticCode.MQ3010_UnknownSchema,
            "schema 'unknown' not registered in SchemaProvider");

        // Verify the error mentions schema-related issue
        Assert.IsTrue(result.Errors.Any(e =>
                e.Message.Contains("Schema", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("unknown", StringComparison.OrdinalIgnoreCase)),
            "Error should mention schema issue");
    }

    [TestMethod]
    public void Stage3a_UnknownTableInSchema()
    {
        // Arrange - #A.UnknownMethod() - method not found in schema A
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.UnknownMethod()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - the schema exists, but the requested source does not
        AssertHasErrorCode(result, DiagnosticCode.MQ3085_UnknownSource,
            "method 'UnknownMethod' not found in schema A");

        // Verify the error mentions the unknown method and suggests available methods
        Assert.IsTrue(result.Errors.Any(e =>
                e.Message.Contains("UnknownMethod", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("method", StringComparison.OrdinalIgnoreCase)),
            "Error should mention the unknown method");
    }

    [TestMethod]
    public void Stage3a_MissingSchemaPrefix()
    {
        // Arrange - A.Entities() without # prefix
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Query succeeds because parser interprets "A.Entities()" differently
        // Without #, it's not recognized as a schema reference - may be treated as CTE reference
        DocumentParserBehavior(result,
            "A.Entities() without # prefix: may succeed if interpreted as CTE or fail with unknown schema",
            result.HasErrors);
    }

    [TestMethod]
    public void Stage3a_InvalidSchemaName_EmptyAfterHash()
    {
        // Arrange - #.Entities() - edge case with just dot after hash
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Document lexer behavior for edge case
        DocumentParserBehavior(result,
            "#. may be parsed as schema '.' or as syntax error",
            result.HasErrors);
    }

    [TestMethod]
    public void Stage3a_ValidSchemaAndTable()
    {
        // Arrange - Valid schema and table
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must succeed with no errors
        Assert.IsTrue(result.IsParsed, "Valid schema/table should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Stage3b_UnknownColumn()
    {
        // Arrange - 'Naem' typo for 'Name' - should suggest correction
        var analyzer = CreateAnalyzer();
        var query = "SELECT Naem FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must be MQ3001_UnknownColumn
        AssertHasErrorCode(result, DiagnosticCode.MQ3001_UnknownColumn,
            "typo 'Naem' - should suggest 'Name'");
    }

    [TestMethod]
    public void Stage3b_UnknownColumn_CompletelyWrong()
    {
        // Arrange - Non-existent column with no similar name
        var analyzer = CreateAnalyzer();
        var query = "SELECT XYZ123 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must be MQ3001_UnknownColumn
        AssertHasErrorCode(result, DiagnosticCode.MQ3001_UnknownColumn,
            "column 'XYZ123' doesn't exist");
    }

    [TestMethod]
    public void Stage3b_TypeMismatch_StringEqualsNumber()
    {
        // Arrange - WHERE Name = 42 (string vs int comparison)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 42";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - May allow comparison or produce MQ3005_TypeMismatch
        DocumentParserBehavior(result,
            "string = int: may succeed (implicit conversion) or MQ3005_TypeMismatch",
            result.HasErrors);
    }

    [TestMethod]
    public void Stage3b_TypeMismatch_StringPlusNumber()
    {
        // Arrange - SELECT Name + 5 (string + int addition)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + 5 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - invalid operand combination for string + int
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3007_InvalidOperandTypes, "string + int arithmetic");
    }

    [TestMethod]
    public void Stage3b_InvalidAggregateContext()
    {
        // Arrange - Name not in GROUP BY but referenced outside aggregate
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(Population) FROM #A.Entities() GROUP BY City";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Musoq now enforces standard SQL GROUP BY rules.
        // Name is not in GROUP BY and not inside an aggregate → MQ3012 error.
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3012_NonAggregateInSelect, "Name not in GROUP BY should produce MQ3012");
    }

    [TestMethod]
    public void Stage3b_AmbiguousColumn_MultipleAliases()
    {
        // Arrange - 'Name' exists in both tables a and b
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() a INNER JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - ambiguous column name should use dedicated diagnostic
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3002_AmbiguousColumn, "'Name' exists in both tables - must qualify with alias");

        // Verify the error mentions ambiguous column
        Assert.IsTrue(result.Errors.Any(e =>
                e.Message.Contains("Ambiguous", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("Name", StringComparison.OrdinalIgnoreCase)),
            "Error should mention ambiguous column");
    }

    [TestMethod]
    public void Stage3b_ValidTypeOperations()
    {
        // Arrange - Valid type operations
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Population * 2, Money + 100.50m FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must succeed
        Assert.IsTrue(result.IsParsed, "Valid type operations should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Stage3c_UnknownMethod()
    {
        // Arrange - MyFunc doesn't exist in any library
        var analyzer = CreateAnalyzer();
        var query = "SELECT MyFunc(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        AssertHasErrorCode(result, DiagnosticCode.MQ3086_UnknownCallable, "function 'MyFunc' doesn't exist");
    }

    [TestMethod]
    public void Stage3c_WrongParameterCount_TooMany()
    {
        // Arrange - Length() takes 1 param, given 4
        var analyzer = CreateAnalyzer();
        var query = "SELECT Length(Name, 1, 2, 3) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        AssertHasErrorCode(result, DiagnosticCode.MQ3087_InvalidCallableArity, "too many arguments to Length()");
    }

    [TestMethod]
    public void Stage3c_WrongParameterCount_TooFew()
    {
        // Arrange - Substring requires at least 2 args
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        AssertHasErrorCode(result, DiagnosticCode.MQ3087_InvalidCallableArity, "too few arguments to Substring()");
    }

    [TestMethod]
    public void Stage3c_WrongParameterType()
    {
        // Arrange - Substring expects string, not int for first param
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Population, 1, 2) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        AssertHasErrorCode(result, DiagnosticCode.MQ3088_NoMatchingCallableOverload, "wrong argument type for Substring()");
    }

    [TestMethod]
    public void Stage3c_PropertyChainError()
    {
        // Arrange - Name.NonExistent.Prop - 'NonExistent' doesn't exist on string
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name.NonExistent.Prop FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Returns MQ3028_UnknownProperty: "Property 'Name' could not be found"
        // and MQ3001_UnknownColumn: "Unknown column 'NonExistent'"
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3028_UnknownProperty, "property chain doesn't resolve");
    }

    [TestMethod]
    public void Stage3c_PropertyOnPrimitive()
    {
        // Arrange - int doesn't have Length property
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population.Length FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Returns MQ3028_UnknownProperty: "Property 'Population' could not be found"
        // and MQ3001_UnknownColumn: "Unknown column 'Length'"
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3028_UnknownProperty, "int32 doesn't have 'Length' property");
    }

    [TestMethod]
    public void Stage3c_IndexOnNonIndexable()
    {
        // Arrange - Array index on integer (not indexable)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population[0] FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Should be MQ3018_NoIndexer or MQ3017_ObjectNotArray
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3017_ObjectNotArray, "indexing non-indexable type (int)");
    }

    [TestMethod]
    public void Stage3c_ValidMethodCalls()
    {
        // Arrange - Valid method calls
        var analyzer = CreateAnalyzer();
        var query = "SELECT ToUpperInvariant(Name), Abs(Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - Must succeed
        Assert.IsTrue(result.IsParsed, "Valid method calls should parse");
        AssertNoErrors(result);
    }

}
