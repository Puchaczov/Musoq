using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class MalformedQueryErrorTests
{
    #region BETWEEN operator errors

    [TestMethod]
    public void WhenBetweenMissingUpperBound_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age BETWEEN 20 AND"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "cannot be used here");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenBetweenMissingLowerBound_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age BETWEEN AND 30"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "cannot be used here");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenBetweenMissingAndKeyword_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age BETWEEN 20 30"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is And");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenBetweenMissingBothBounds_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age BETWEEN"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "cannot be used here");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenBetweenMissingLeftOperand_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE BETWEEN 20 AND 30"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Invalid operand types");
        AssertHasGuidance(ex);
    }

    #endregion

    #region Legacy prefix ::N syntax errors

    [TestMethod]
    public void WhenLegacyPrefixDoubleColonNumberInSelect_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT ::5, Count(1) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "DoubleColon");
    }

    [TestMethod]
    public void WhenLegacyPrefixDoubleColonNumberWithoutGroupBy_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT ::1 FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "DoubleColon");
    }

    [TestMethod]
    public void WhenLegacyPrefixDoubleColonZero_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT ::0, Count(1) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "DoubleColon");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenLegacyPrefixDoubleColonNegative_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT ::-1, Count(1) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "DoubleColon");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenExplicitGroupKey_ShouldSucceed()
    {
        var vm = CompileQuery("SELECT City, Count(1) FROM #test.people() GROUP BY City");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region CASE WHEN without ELSE

    [TestMethod]
    public void WhenCaseWhenWithoutElse_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT CASE WHEN Age > 25 THEN 'old' END FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is Else but received End");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenCaseWhenMultipleBranchesWithoutElse_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT CASE WHEN Age > 40 THEN 'senior' WHEN Age > 25 THEN 'mid' END FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is Else but received End");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSimpleCaseWithoutElse_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT CASE Age WHEN 25 THEN 'young' WHEN 35 THEN 'mid' END FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is Else but received End");
        AssertHasGuidance(ex);
    }

    #endregion

    #region Simple CASE expression errors

    [TestMethod]
    public void WhenSimpleCaseValid_ShouldSucceed()
    {
        var vm = CompileQuery(
            "SELECT CASE City WHEN 'London' THEN 'UK' WHEN 'Paris' THEN 'FR' ELSE 'Other' END FROM #test.people()");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenSimpleCaseMissingInputExpression_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT CASE THEN 'yes' ELSE 'no' END FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "cannot be used here");
        AssertHasGuidance(ex);
    }

    #endregion

    #region Numeric literal edge cases

    [TestMethod]
    public void WhenHexLiteralOverflow_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT 0xFFFFFFFFFFFFFFFFFF FROM #test.single()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Hexadecimal value");
        AssertMessageContains(ex, "too large");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenBinaryLiteralOverflow_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT 0b11111111111111111111111111111111111111111111111111111111111111111 FROM #test.single()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Binary value");
        AssertMessageContains(ex, "too large");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenOctalLiteralOverflow_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT 0o7777777777777777777777 FROM #test.single()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Octal value");
        AssertMessageContains(ex, "too large");
        AssertHasGuidance(ex);
    }

    #endregion

    #region CONTAINS operator errors

    [TestMethod]
    public void WhenContainsWithEmptyArgList_ShouldThrowParseError()
    {
        // Known quality gap: produces "Index was outside the bounds of the array"
        // rather than a user-friendly message about empty argument list
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Name CONTAINS ()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenContainsMissingParentheses_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Name CONTAINS 'Alice'"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is LeftParenthesis");
        AssertHasGuidance(ex);
    }

    #endregion

    #region IN operator edge cases

    [TestMethod]
    public void WhenInWithEmptyList_ShouldCompileSuccessfully()
    {
        var vm = CompileQuery("SELECT * FROM #test.people() WHERE Age IN ()");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count, "IN () should match nothing");
    }

    [TestMethod]
    public void WhenNotInWithEmptyList_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age NOT IN ()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2030_UnsupportedSyntax, DiagnosticPhase.Parse, "NOT IN with an empty list is not supported");
    }

    [TestMethod]
    public void WhenInWithUnclosedParenthesis_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age IN (25, 30"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is RightParenthesis");
        AssertHasGuidance(ex);
    }

    #endregion

    #region NULL literal edge cases

    [TestMethod]
    public void WhenNullInArithmetic_ShouldReturnNull()
    {
        var vm = CompileQuery("SELECT NULL + 1 FROM #test.single()");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenNullComparedToNull_ShouldReturnAllRows()
    {
        var vm = CompileQuery("SELECT 1 FROM #test.people() WHERE NULL = NULL");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenIsNullOnNonNullableColumn_ShouldSucceed()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Name IS NOT NULL");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    #endregion

    #region LIKE/RLIKE with NULL patterns

    [TestMethod]
    public void WhenLikeWithNullPattern_ShouldReturnEmpty()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Name LIKE NULL");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenRlikeWithNullPattern_ShouldReturnEmpty()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Name RLIKE NULL");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count);
    }

    #endregion
}
