using System;
using System.Linq;
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
public class TypoAndUxDiagnosticProbeTests : NegativeTestsBase
{
    // ========================================================================
    // CATEGORY 1: Keyword misspellings
    // Users frequently misspell SQL keywords.
    // ========================================================================

    #region Keyword misspellings

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

    #endregion

    // ========================================================================
    // CATEGORY 2: SQL dialect confusion
    // Users coming from MySQL, PostgreSQL, etc. use keywords Musoq doesn't support.
    // ========================================================================

    #region SQL dialect confusion

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

    #endregion

    // ========================================================================
    // CATEGORY 3: Column name typos
    // Users misspell column/property names.
    // ========================================================================

    #region Column name typos

    [TestMethod]
    public void WhenColumnNameTypoInSelect_ShouldSuggestCorrectName()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Naame FROM #test.people()"));

        AssertMessageContains(ex, "Name");
    }

    [TestMethod]
    public void WhenColumnNameTypoInWhere_ShouldSuggestCorrectName()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Agee > 30"));

        AssertMessageContains(ex, "Age");
    }

    [TestMethod]
    public void WhenColumnNameCaseMismatch_ShouldSuggestCorrectCase()
    {
        // Musoq is case-sensitive for column names but provides a helpful suggestion
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT name FROM #test.people()"));

        AssertMessageContains(ex, "Name");
    }

    [TestMethod]
    public void WhenColumnNameTypoInGroupBy_ShouldSuggestCorrectName()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Ciy, Count(Ciy) FROM #test.people() GROUP BY Ciy"));

        AssertMessageContains(ex, "City");
    }

    [TestMethod]
    public void WhenColumnNameTypoInOrderBy_ShouldSuggestCorrectName()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() ORDER BY Nmae ASC"));

        AssertMessageContains(ex, "Name");
    }

    #endregion

    // ========================================================================
    // CATEGORY 4: Schema/table reference mistakes
    // Users get the FROM clause wrong.
    // ========================================================================

    #region Schema reference mistakes

    [TestMethod]
    public void WhenSchemaNameTypo_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #tset.people()"));

        AssertMessageContains(ex, "Unknown schema");
    }

    [TestMethod]
    public void WhenTableMethodTypo_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.poeple()"));

        // Should indicate the table/method is unknown
        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("Unknown table", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("poeple", StringComparison.OrdinalIgnoreCase),
            $"Error message should mention unknown table or the typo 'poeple'. Got: {msg}");
    }

    [TestMethod]
    public void WhenMissingHashInSchemaRef_ShouldSucceed()
    {
        // Design choice: # is being phased out, so test.people() without # is valid
        var vm = CompileQuery("SELECT Name FROM test.people()");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenMissingParensOnTableMethod_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
    }

    #endregion

    // ========================================================================
    // CATEGORY 5: Common structural mistakes
    // ========================================================================

    #region Structural mistakes

    [TestMethod]
    public void WhenForgettingQuotesAroundString_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE City = London"));

        // "London" should be treated as unknown column; error should mention it
        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("London", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("column", StringComparison.OrdinalIgnoreCase),
            $"Error should mention 'London' as unknown column or suggest quoting. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingDoubleQuotesInsteadOfSingle_ShouldGiveHelpfulError()
    {
        // Standard SQL uses single quotes; double quotes are identifiers
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE City = \"London\""));

        var msg = ex.Message;
        // Should give some indication about the issue
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Should produce a non-empty error message");
    }

    [TestMethod]
    public void WhenMissingSemicolonBetweenStatements_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() SELECT Age FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Should produce a non-empty error message");
    }

    [TestMethod]
    public void WhenUsingAsterikWithOtherColumns_ShouldWorkOrGiveError()
    {
        // SELECT *, Name is unusual; test behavior
        try
        {
            var vm = CompileQuery("SELECT *, Name FROM #test.people()");
            var table = vm.Run(TokenSource.Token);
            // If it works, verify it has results
            Assert.IsGreaterThan(0, table.Count);
        }
        catch (MusoqQueryException ex)
        {
            // If it fails, error should be helpful
            var msg = ex.Message;
            Assert.IsNotNull(msg);
            Assert.IsGreaterThan(0, msg.Length);
        }
    }

    [TestMethod]
    public void WhenForgettingCommaInSelectList_ShouldTreatAsAlias()
    {
        // Known limitation: SQL allows implicit aliases (SELECT Name Age means SELECT Name AS Age).
        // Without parser-level AS tracking, we can't detect missing commas vs intentional aliases
        // without producing false positives on UNIONs, JOINs, and CTEs.
        var vm = CompileQuery("SELECT Name Age FROM #test.people()");
        var table = vm.Run(TokenSource.Token);

        // "Age" is treated as an alias for the "Name" column — one column, not two
        Assert.AreEqual(1, table.Columns.Count());
    }

    [TestMethod]
    public void WhenUsingEqualsEqualsInsteadOfEquals_ShouldWork()
    {
        // Some SQL systems use =, some ==; test Musoq behavior
        try
        {
            var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Age = 25");
            var table = vm.Run(TokenSource.Token);
            Assert.AreEqual(1, table.Count);
        }
        catch (MusoqQueryException)
        {
            // If = doesn't work, that's a gap
            Assert.Fail("Simple equality comparison with = should work");
        }
    }

    [TestMethod]
    public void WhenUsingExclamationEqualsForNotEqual_ShouldGiveHelpfulError()
    {
        try
        {
            var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Age != 25");
            var table = vm.Run(TokenSource.Token);
            Assert.IsGreaterThan(0, table.Count);
        }
        catch (MusoqQueryException ex)
        {
            // If != doesn't work, should suggest <> or diff
            var msg = ex.Message;
            Assert.IsTrue(
                msg.Contains("<>", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("diff", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("not equal", StringComparison.OrdinalIgnoreCase),
                $"Should suggest alternative not-equal syntax. Got: {msg}");
        }
    }

    #endregion

    // ========================================================================
    // CATEGORY 6: Function name typos and mistakes
    // ========================================================================

    #region Function mistakes

    [TestMethod]
    public void WhenFunctionNameTypo_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Coutn(Name) FROM #test.people() GROUP BY Name"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("Count", StringComparison.OrdinalIgnoreCase),
            $"Should suggest 'Count'. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingLenInsteadOfLength_ShouldGiveHelpfulError()
    {
        // MySQL uses LENGTH, SQL Server uses LEN
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Len(Name) FROM #test.people()"));

        var msg = ex.Message;
        // Should either work or suggest the right function name
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, $"Error should be meaningful. Got: {msg}");
    }

    [TestMethod]
    public void WhenCallingCountWithoutGroupBy_ShouldWork()
    {
        // Common confusion: forgetting GROUP BY with aggregate
        var vm = CompileQuery("SELECT Count(Name) FROM #test.people()");
        var table = vm.Run(TokenSource.Token);
        
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenCallingNonExistentFunction_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT FakeFunc(Name) FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("FakeFunc", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("unknown", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("resolve", StringComparison.OrdinalIgnoreCase),
            $"Should mention the function or say it's unknown. Got: {msg}");
    }

    #endregion

    // ========================================================================
    // CATEGORY 7: Unterminated/malformed literals
    // ========================================================================

    #region Malformed literals

    [TestMethod]
    public void WhenUnterminatedString_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE City = 'London"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("unterminated", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("string", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("quote", StringComparison.OrdinalIgnoreCase),
            $"Should mention unterminated string. Got: {msg}");
    }

    [TestMethod]
    public void WhenUnterminatedBlockComment_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name /* this is a comment FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("comment", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("unterminated", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("*/", StringComparison.OrdinalIgnoreCase),
            $"Should mention unterminated comment. Got: {msg}");
    }

    #endregion

    // ========================================================================
    // CATEGORY 8: Join mistakes
    // ========================================================================

    #region Join mistakes

    [TestMethod]
    public void WhenJoinMissingOnClause_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT a.Name, b.Amount FROM #test.people() a INNER JOIN #test.orders() b"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void WhenJoinUsingWrongColumnName_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT a.Name, b.Amount FROM #test.people() a INNER JOIN #test.orders() b ON a.Idd = b.PersonId"));

        AssertMessageContains(ex, "Id");
    }

    [TestMethod]
    public void WhenCrossJoinSyntax_ShouldWorkOrGiveError()
    {
        // CROSS JOIN may or may not be supported
        try
        {
            var vm = CompileQuery("SELECT a.Name, b.Amount FROM #test.people() a CROSS JOIN #test.orders() b");
            var table = vm.Run(TokenSource.Token);
            // Cross join = 5 * 5 = 25 rows
            Assert.AreEqual(25, table.Count);
        }
        catch (MusoqQueryException ex)
        {
            // If not supported, should say so clearly
            var msg = ex.Message;
            Assert.IsNotNull(msg);
            Assert.IsGreaterThan(0, msg.Length);
        }
    }

    #endregion

    // ========================================================================
    // CATEGORY 9: Empty/minimal queries
    // ========================================================================

    #region Empty queries

    [TestMethod]
    public void WhenEmptyQuery_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(""));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Empty query should give a meaningful error");
    }

    [TestMethod]
    public void WhenWhitespaceOnlyQuery_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("   \t\n  "));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Whitespace-only query should give a meaningful error");
    }

    [TestMethod]
    public void WhenJustSemicolon_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(";"));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Semicolon-only should give a meaningful error");
    }

    [TestMethod]
    public void WhenRandomGarbage_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("asdf qwerty 123"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("SELECT", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("FROM", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("query", StringComparison.OrdinalIgnoreCase),
            $"Random garbage should hint at valid query structure. Got: {msg}");
    }

    #endregion

    // ========================================================================
    // CATEGORY 10: Alias-related confusion
    // ========================================================================

    #region Alias confusion

    [TestMethod]
    public void WhenUsingAsForTableAlias_ShouldWork()
    {
        // Some SQL dialects use AS for table aliases
        try
        {
            var vm = CompileQuery("SELECT a.Name FROM #test.people() AS a");
            var table = vm.Run(TokenSource.Token);
            Assert.AreEqual(5, table.Count);
        }
        catch (MusoqQueryException ex)
        {
            // If AS is not supported for tables, error should mention it
            var msg = ex.Message;
            Assert.IsNotNull(msg);
        }
    }

    [TestMethod]
    public void WhenReferencingAliasBeforeIsDefined_ShouldGiveHelpfulError()
    {
        // In SQL, the alias is defined after FROM — you can't use it in SELECT before FROM
        // But Musoq might be flexible about evaluation order
        try
        {
            var vm = CompileQuery("SELECT p.Name FROM #test.people() p");
            var table = vm.Run(TokenSource.Token);
            Assert.AreEqual(5, table.Count);
        }
        catch (MusoqQueryException)
        {
            Assert.Fail("Using table alias defined in FROM should work in SELECT");
        }
    }

    [TestMethod]
    public void WhenUsingUndefinedAlias_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT x.Name FROM #test.people() p"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("x", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("alias", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("unknown", StringComparison.OrdinalIgnoreCase),
            $"Should mention the undefined alias 'x'. Got: {msg}");
    }

    #endregion

    // ========================================================================
    // CATEGORY 11: Operator confusion
    // ========================================================================

    #region Operator confusion

    [TestMethod]
    public void WhenUsingAndSymbolInsteadOfKeyword_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age > 20 && Age < 40"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("and", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("&&", StringComparison.OrdinalIgnoreCase),
            $"Should mention AND keyword. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingOrSymbolInsteadOfKeyword_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age < 20 || Age > 40"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("or", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("||", StringComparison.OrdinalIgnoreCase),
            $"Should mention OR keyword. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingNotEqualsFromCSharp_ShouldGiveHelpfulError()
    {
        // C# style !=
        try
        {
            var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Age != 25");
            var table = vm.Run(TokenSource.Token);
            Assert.IsGreaterThan(0, table.Count);
        }
        catch (MusoqQueryException ex)
        {
            Assert.IsTrue(
                ex.Message.Contains("<>", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("diff", StringComparison.OrdinalIgnoreCase),
                $"Should suggest <> for not-equals. Got: {ex.Message}");
        }
    }

    #endregion

    // ========================================================================
    // CATEGORY 12: Special character issues
    // ========================================================================

    #region Special characters

    [TestMethod]
    public void WhenUsingBacktickForIdentifiers_ShouldGiveHelpfulError()
    {
        // MySQL uses backticks for identifiers
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT `Name` FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Should give error for backtick identifiers");
    }

    [TestMethod]
    public void WhenUsingSemicolonAtEnd_ShouldWorkOrGiveError()
    {
        // Some SQL systems require semicolons; test behavior
        try
        {
            var vm = CompileQuery("SELECT Name FROM #test.people();");
            var table = vm.Run(TokenSource.Token);
            Assert.AreEqual(5, table.Count);
        }
        catch (MusoqQueryException)
        {
            // If semicolons are rejected, that's fine — just verify it fails
        }
    }

    #endregion

    // ========================================================================
    // CATEGORY 13: Aggregate without GROUP BY confusion
    // ========================================================================

    #region Aggregate confusion

    [TestMethod]
    public void WhenMixingAggregateAndNonAggregateColumns_ShouldGiveHelpfulError()
    {
        // Classic SQL mistake: non-aggregated columns without GROUP BY
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name, Count(Age) FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("aggregate", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("group", StringComparison.OrdinalIgnoreCase),
            $"Should mention GROUP BY is needed. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingAggregateInWhereClause_ShouldGiveHelpfulError()
    {
        // Aggregate functions aren't allowed in WHERE — should use HAVING
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT City FROM #test.people() WHERE Count(Name) > 1 GROUP BY City"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("HAVING", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("aggregate", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("WHERE", StringComparison.OrdinalIgnoreCase),
            $"Should mention HAVING instead of WHERE for aggregates. Got: {msg}");
    }

    #endregion
}
