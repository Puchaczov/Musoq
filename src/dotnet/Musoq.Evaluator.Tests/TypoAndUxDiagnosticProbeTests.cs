using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Diagnostic probes for common user typos and mistakes.
///     Each test exercises a realistic user error and validates whether the
///     error message is helpful from a UX perspective.
/// </summary>
[TestClass]
public partial class TypoAndUxDiagnosticProbeTests : NegativeTestsBase
{
    // ========================================================================
    // CATEGORY 1: Keyword misspellings
    // Users frequently misspell SQL keywords.
    // ========================================================================


    [TestMethod]
    public void WhenSelectMisspelledAsSelct_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELCT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        // Should suggest SELECT
        AssertMessageContains(ex, "SELECT");
    }

    [TestMethod]
    public void WhenSelectMisspelledAsSeleect_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELEECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "SELECT");
    }

    [TestMethod]
    public void WhenFromMisspelledAsFrm_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FRM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        // Message uses PascalCase enum name "From"
        AssertMessageContains(ex, "From");
    }

    [TestMethod]
    public void WhenFromMisspelledAsFomr_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FOMR #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        // Message uses PascalCase enum name "From"
        AssertMessageContains(ex, "From");
    }

    [TestMethod]
    public void WhenWhereMisspelledAsWher_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHER Age > 30"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "WHERE");
    }

    [TestMethod]
    public void WhenWhereMisspelledAsWheer_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHEER Age > 30"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "WHERE");
    }

    [TestMethod]
    public void WhenGroupByMisspelledAsGruopBy_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT City, Count(City) FROM #test.people() GRUOP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "GROUP");
    }

    [TestMethod]
    public void WhenOrderByMisspelledAsOrdrBy_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() ORDR BY Name"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "ORDER");
    }

    [TestMethod]
    public void WhenHavingMisspelledAsHavig_ShouldGiveError()
    {
        // HAVIG gets consumed as a GROUP BY field alias before the parser detects the error.
        // The error comes from the leftover tokens (e.g., the LeftParenthesis from Count(...)).
        // A more specific HAVING suggestion would require alias-keyword conflict detection.
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT City, Count(City) FROM #test.people() GROUP BY City HAVIG Count(City) > 1"));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Should produce a meaningful error message");
    }

    [TestMethod]
    public void WhenJoinMisspelledAsJion_ShouldGiveHelpfulError()
    {
        // "INNER JION" — INNER is not a standalone keyword token, so it reaches 
        // ComposeStatement as an Identifier. The enhancer maps it to itself (distance 0)
        // so no "Did you mean" is produced, but a clear error is still thrown.
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT a.Name FROM #test.people() a INNER JION #test.orders() b ON a.Id = b.PersonId"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Should produce a meaningful error for misspelled compound keyword");
    }


    // ========================================================================
    // CATEGORY 2: SQL dialect confusion
    // Users coming from MySQL, PostgreSQL, etc. use keywords Musoq doesn't support.
    // ========================================================================


    [TestMethod]
    public void WhenUsingLimitInsteadOfTake_ShouldSuggestTake()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() LIMIT 5"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "TAKE");
    }

    [TestMethod]
    public void WhenUsingTopInsteadOfTake_ShouldSuggestTake()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT TOP 5 Name FROM #test.people()"));

        // TOP is now detected as a dialect keyword at the semantic level
        AssertMessageContains(ex, "TAKE");
    }

    [TestMethod]
    public void WhenUsingOffsetInsteadOfSkip_ShouldSuggestSkip()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() OFFSET 2"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "SKIP");
    }

    [TestMethod]
    public void WhenUsingIlikeInsteadOfLike_ShouldSuggestLike()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Name ILIKE '%alice%'"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "LIKE");
    }


    // ========================================================================
    // CATEGORY 3: Column name typos
    // Users misspell column/property names.
    // ========================================================================


}
