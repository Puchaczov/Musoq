using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityStructuralSyntaxTests
{
    #region P-SCHEMA: Schema reference parse errors

    [TestMethod]
    public void P_SCHEMA_01_MissingHashPrefix()
    {
        // Arrange — A.Entities() without # prefix
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — In Musoq, the parser's EnsureHashPrefix method automatically adds
        // the # prefix when parsing schema references. So 'FROM A.Entities()' is
        // equivalent to 'FROM #A.Entities()'. The # prefix is optional.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SCHEMA_02_MissingDotSeparator()
    {
        // Arrange — #AEntities() without dot separator
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #AEntities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate invalid schema reference format
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "schema without dot separator");
    }

    [TestMethod]
    public void P_SCHEMA_03_MissingTableName()
    {
        // Arrange — #A() without table/method name
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #A()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing table/method name
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "schema without table name");
    }

    [TestMethod]
    public void P_SCHEMA_04_MissingSchemaName()
    {
        // Arrange — #.Entities() without schema name
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing schema name
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3010_UnknownSchema, "schema reference with missing schema name");
    }

    [TestMethod]
    public void P_SCHEMA_05_DoubleHash()
    {
        // Arrange — ##A.Entities() with double hash
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM ##A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate invalid schema reference
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "double hash in schema reference");
    }

    [TestMethod]
    public void P_SCHEMA_06_SchemaWithSpaces()
    {
        // Arrange — #A .Entities() with space before dot
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #A .Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — In Musoq, the lexer ignores whitespace between tokens.
        // '#A .Entities()' is tokenized the same as '#A.Entities()'.
        // Whitespace around the dot separator is allowed.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SCHEMA_07_MissingParenthesesEntirely()
    {
        // Arrange — #A.Entities without ()
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #A.Entities";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing parentheses
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "schema reference without parentheses");
    }

    #endregion
}
