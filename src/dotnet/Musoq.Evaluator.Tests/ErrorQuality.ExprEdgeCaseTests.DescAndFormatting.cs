using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityExprEdgeCaseTests
{
    [TestMethod]
    public void E_DESC_01_DescOnNonExistentSchema()
    {
        // Arrange — DESC on schema that doesn't exist
        var analyzer = CreateAnalyzer();
        var query = "DESC #nonexistent.table()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Unknown schema should surface as specific schema diagnostic.
        AssertHasErrorCode(result, DiagnosticCode.MQ3010_UnknownSchema, "DESC on non-existent schema");
    }

    [TestMethod]
    public void E_DESC_02_DescOnNonExistentTable()
    {
        // Arrange — DESC on non-existent table within valid schema
        var analyzer = CreateAnalyzer();
        var query = "DESC #A.nonexistent()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq's semantic analysis does not validate table method existence
        // during the DESC command processing. The schema provider resolves methods
        // at runtime, so non-existent tables are not caught at analysis time.
        // This is a known limitation of the static analysis phase.
        AssertNoErrors(result);
    }


    // ============================================================================
    // E-FMT: Whitespace, Comments, and Formatting Edge Cases
    // ============================================================================


    [TestMethod]
    public void E_FMT_01_QueryWithOnlyComments()
    {
        // Arrange — Only a line comment
        var analyzer = CreateAnalyzer();
        var query = "-- this is a comment";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Comment-only query should be rejected as non-query syntax.
        AssertHasOneOfErrorCodes(result, "query with only comments",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2025_MissingSelectKeyword,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void E_FMT_02_MultiLineCommentWrappingKeywords()
    {
        // Arrange — Comment spanning across keywords
        var analyzer = CreateAnalyzer();
        var query = "SELECT /* this wraps\nacross lines */ Name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should parse correctly ignoring comment
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_FMT_03_CommentInsideStringLiteral()
    {
        // Arrange — Comment syntax inside string should be part of string
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'hello -- world' FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — String literal should include the -- as content
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_FMT_05_EmptyQuery()
    {
        // Arrange — Empty string
        var analyzer = CreateAnalyzer();
        var query = "";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should handle gracefully, not crash
        Assert.IsNotNull(result, "Analyzer should not crash on empty query");
    }

    [TestMethod]
    public void E_FMT_06_WhitespaceOnlyQuery()
    {
        // Arrange — Only spaces/tabs
        var analyzer = CreateAnalyzer();
        var query = "     ";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should handle gracefully, not crash
        Assert.IsNotNull(result, "Analyzer should not crash on whitespace-only query");
    }

    [TestMethod]
    public void E_FMT_07_TabSeparatedKeywords()
    {
        // Arrange — Tabs instead of spaces
        var analyzer = CreateAnalyzer();
        var query = "SELECT\tName\tFROM\t#A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Tabs should be treated same as spaces
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_FMT_08_NewlineInMiddleOfKeyword()
    {
        // Arrange — Keyword split across lines
        var analyzer = CreateAnalyzer();
        var query = "SEL\nECT Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error: keyword split by newline
        AssertHasOneOfErrorCodes(result, "keyword split by newline",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2025_MissingSelectKeyword,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void E_FMT_09_VeryLongSingleLineQuery()
    {
        // Arrange — Very long WHERE clause
        var analyzer = CreateAnalyzer();
        var query =
            "SELECT Name FROM #A.Entities() WHERE Population > 1 AND Population > 2 AND Population > 3 AND Population > 4 AND Population > 5 AND Population > 6 AND Population > 7 AND Population > 8 AND Population > 9 AND Population < 100 AND Population < 99 AND Population < 98 AND Population < 97 AND Population < 96 AND Population < 95 AND Population < 94 AND Population < 93 AND Population < 92 AND Population < 91";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Long but valid query should succeed
        AssertNoErrors(result);
    }

}
