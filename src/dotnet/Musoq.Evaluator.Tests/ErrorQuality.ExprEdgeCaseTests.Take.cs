using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityExprEdgeCaseTests
{
    [TestMethod]
    public void E_TAKE_01_TakeZero()
    {
        // Arrange — TAKE 0
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 0";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — TAKE 0 is valid (empty result set at execution time).
        AssertNoErrors(result);
    }

    [TestMethod]
    public void E_TAKE_02_TakeNegative()
    {
        // Arrange — TAKE -1
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE -1";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error on negative TAKE
        AssertHasOneOfErrorCodes(result, "TAKE with negative value",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void E_TAKE_04_SkipNegative()
    {
        // Arrange — SKIP -1
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() SKIP -1";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error on negative SKIP
        AssertHasOneOfErrorCodes(result, "SKIP with negative value",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void E_TAKE_05_TakeWithNonInteger()
    {
        // Arrange — TAKE 3.5
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 3.5";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error on non-integer TAKE
        AssertHasOneOfErrorCodes(result, "TAKE with non-integer",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void E_TAKE_06_SkipWithNonInteger()
    {
        // Arrange — SKIP 2.5
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() SKIP 2.5";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error on non-integer SKIP
        AssertHasOneOfErrorCodes(result, "SKIP with non-integer",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void E_TAKE_07_TakeWithExpression()
    {
        // Arrange — TAKE 2 + 3 (is expression supported?)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 2 + 3";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Expressions are not supported in TAKE; parser expects a literal.
        AssertHasErrorCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "TAKE with expression 2 + 3");
    }

    [TestMethod]
    public void E_TAKE_08_VeryLargeTake()
    {
        // Arrange — TAKE with very large number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 999999999";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Large literal TAKE values are accepted by analysis.
        AssertNoErrors(result);
    }


    // ============================================================================
    // E-EDGE: Edge Cases in Expressions and Literals
    // ============================================================================


}
