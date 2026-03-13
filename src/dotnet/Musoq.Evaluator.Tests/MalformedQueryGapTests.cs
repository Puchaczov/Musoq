using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive gap-filling tests for malformed queries.
///     Each test verifies that a specific kind of user mistake
///     produces a clear, informative error with structured diagnostics
///     rather than succeeding silently or producing a cryptic internal exception.
/// </summary>
[TestClass]
public class MalformedQueryGapTests : NegativeTestsBase
{
    // ========================================================================
    // GAP 1: BETWEEN operator errors
    // Spec confirms BETWEEN support (x BETWEEN a AND b).
    // No existing tests for malformed BETWEEN expressions.
    // ========================================================================

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

    // ========================================================================
    // GAP 2: Field Links (::N) errors
    // Spec (Appendix G) defines ::N for GROUP BY column references.
    // Zero query-level tests exist for invalid field link usage.
    // ========================================================================

    #region Field Link (::N) errors

    [TestMethod]
    public void WhenFieldLinkExceedsGroupByCount_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT ::5, Count(1) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3024_GroupByIndexOutOfRange, DiagnosticPhase.Bind, "out of range");
    }

    [TestMethod]
    public void WhenFieldLinkWithoutGroupBy_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT ::1 FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3024_GroupByIndexOutOfRange, DiagnosticPhase.Bind, "out of range");
    }

    [TestMethod]
    public void WhenFieldLinkZero_ShouldThrowError()
    {
        // ::N is 1-based per spec; ::0 is invalid
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT ::0, Count(1) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is From but received Integer");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenFieldLinkNegative_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT ::-1, Count(1) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is From but received Integer");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenFieldLinkValid_ShouldSucceed()
    {
        var vm = CompileQuery("SELECT ::1, Count(1) FROM #test.people() GROUP BY City");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
    }

    #endregion

    // ========================================================================
    // GAP 3: CASE WHEN without ELSE (mandatory ELSE per spec)
    // Spec (Appendix F) says ELSE is mandatory in all CASE expressions.
    // Existing tests cover empty CASE, missing THEN, missing END — but not
    // the specific case of well-formed WHEN/THEN with missing ELSE.
    // ========================================================================

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

    // ========================================================================
    // GAP 4: Simple CASE expression errors
    // Spec supports CASE expr WHEN val THEN ... ELSE ... END.
    // No existing tests for malformed simple CASE syntax.
    // ========================================================================

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

    // ========================================================================
    // GAP 5: FROM-first (reordered) query errors
    // Spec section 16 describes FROM ... WHERE ... SELECT syntax.
    // Zero negative tests for malformed reordered queries.
    // ========================================================================

    #region FROM-first query errors

    [TestMethod]
    public void WhenFromFirstWithoutSelect_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("FROM #test.people() WHERE Age > 25"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is Select");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenFromFirstWithWhereAfterSelect_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("FROM #test.people() SELECT Name WHERE Age > 25"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "is not expected here");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenFromFirstValid_ShouldSucceed()
    {
        var vm = CompileQuery("FROM #test.people() WHERE Age > 25 SELECT Name");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(4, table.Count);
    }

    [TestMethod]
    public void WhenFromFirstWithGroupByBeforeWhere_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("FROM #test.people() GROUP BY City WHERE Age > 25 SELECT City, Count(1)"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is Select");
        AssertHasGuidance(ex);
    }

    #endregion

    // ========================================================================
    // GAP 6: Numeric literal edge cases
    // Spec (Appendix D) defines type suffixes and hex/bin/octal formats.
    // Very limited existing tests for literal parsing errors.
    // ========================================================================

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

    // ========================================================================
    // GAP 7: CONTAINS operator misuse
    // Only TE062 tests CONTAINS on int column.
    // No tests for CONTAINS syntax errors.
    // ========================================================================

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

    // ========================================================================
    // GAP 8: IN operator edge cases
    // Only PE_EXPR_10 covers IN without parens.
    // No tests for IN with empty list or mixed types.
    // ========================================================================

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
        // Known quality gap: empty NOT IN still falls into a generic fallback path,
        // but the message should at least explain the structural problem.
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age NOT IN ()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2030_UnsupportedSyntax, DiagnosticPhase.Parse, "index was out of range");
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

    // ========================================================================
    // GAP 9: DESC statement errors
    // Only E_DESC_01 covers DESC on non-existent schema.
    // No tests for other malformed DESC syntax.
    // ========================================================================

    #region DESC statement errors

    [TestMethod]
    public void WhenDescWithoutIdentifier_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("DESC"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected schema name");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenDescFunctionsOnNonExistentSchema_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("DESC FUNCTIONS #nonexistent"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3010_UnknownSchema, DiagnosticPhase.Bind, "Unknown schema");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenDescWithValidSchema_ShouldSucceed()
    {
        var vm = CompileQuery("DESC #test");
        var table = vm.Run(TokenSource.Token);

        Assert.IsGreaterThanOrEqualTo(0, table.Count, "DESC #test should compile and execute");
    }

    [TestMethod]
    public void WhenDescWithValidSchemaTable_ShouldSucceed()
    {
        var vm = CompileQuery("DESC #test.people");
        var table = vm.Run(TokenSource.Token);

        Assert.IsGreaterThanOrEqualTo(0, table.Count, "DESC #test.people should compile and execute");
    }

    #endregion

    // ========================================================================
    // GAP 10: ORDER BY edge cases
    // Spec says ORDER BY by position number is NOT supported.
    // No test for this; also no test for ORDER BY on column not in scope.
    // ========================================================================

    #region ORDER BY edge cases

    [TestMethod]
    public void WhenOrderByPositionNumber_ShouldThrowUnsupportedSyntax()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name, Age FROM #test.people() ORDER BY 1"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2030_UnsupportedSyntax, DiagnosticPhase.Parse,
            "ORDER BY column position is not supported");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenOrderByNonExistentColumnAfterGroupBy_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT City, Count(1) FROM #test.people() GROUP BY City ORDER BY NonExistent"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Unknown column 'NonExistent'");
    }

    #endregion

    // ========================================================================
    // GAP 11: APPLY (CROSS/OUTER) semantic errors
    // P_STRUCT_15 covers missing alias. No tests for APPLY semantic errors
    // like non-existent method in APPLY or OUTER APPLY without alias.
    // ========================================================================

    #region APPLY semantic errors

    [TestMethod]
    public void WhenCrossApplyOnNonExistentMethod_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() a CROSS APPLY #test.nonexistent() AS t"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3003_UnknownTable, DiagnosticPhase.Bind, "nonexistent");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenOuterApplyWithoutExplicitAlias_ShouldCompileSuccessfully()
    {
        var vm = CompileQuery("SELECT * FROM #test.people() a OUTER APPLY #test.orders() AS o");
        var table = vm.Run(TokenSource.Token);

        Assert.IsGreaterThanOrEqualTo(0, table.Count, "OUTER APPLY should compile and execute");
    }

    #endregion

    // ========================================================================
    // GAP 12: TABLE/COUPLE semantic errors via CompileQuery
    // Structural syntax tests exist but no NegativeTestsBase-level tests
    // for TABLE/COUPLE semantic errors.
    // ========================================================================

    #region TABLE/COUPLE semantic errors

    [TestMethod]
    public void WhenCoupleReferencesUndefinedTable_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("couple #test.people with table UndefinedTable as Source; select * from Source()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3003_UnknownTable, DiagnosticPhase.Bind, "'UndefinedTable'");
    }

    [TestMethod]
    public void WhenUsingCoupledAliasBeforeCouple_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select * from Source(); table MyTable { Name: string }; couple #test.people with table MyTable as Source"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3003_UnknownTable, DiagnosticPhase.Bind, "'Source'");
    }

    [TestMethod]
    public void WhenTableWithUnknownType_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "table MyTable { Col: banana }; couple #test.people with table MyTable as Source; select Col from Source()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "banana");
    }

    [TestMethod]
    public void WhenCoupleWithoutWithKeyword_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "table MyTable { Name: string }; couple #test.people table MyTable as Source; select Name from Source()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is With but received Table");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenCoupleWithoutAsKeyword_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "table MyTable { Name: string }; couple #test.people with table MyTable Source; select Name from Source()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is As but received Identifier");
        AssertHasGuidance(ex);
    }

    #endregion

    // ========================================================================
    // GAP 13: Set operation key column errors
    // No tests for set operations referencing non-existent key columns.
    // ========================================================================

    #region Set operation key column errors

    [TestMethod]
    public void WhenUnionWithNonExistentKeyColumn_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() UNION (NonExistent) SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistent");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenExceptWithNonExistentKeyColumn_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() EXCEPT (NonExistent) SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistent");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenIntersectWithNonExistentKeyColumn_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() INTERSECT (NonExistent) SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistent");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenUnionWithDifferentColumnCounts_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT Name, Age FROM #test.people() UNION (Name) SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3019_SetOperatorColumnCount, DiagnosticPhase.Bind, "same quantity of columns");
        AssertHasGuidance(ex);
    }

    #endregion

    // ========================================================================
    // GAP 14: HAVING edge cases
    // Existing tests cover HAVING on non-existent column and aggregate in WHERE.
    // No tests for HAVING without GROUP BY or HAVING with non-boolean.
    // ========================================================================

    #region HAVING edge cases

    [TestMethod]
    public void WhenHavingWithoutGroupBy_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() HAVING Count(1) > 1"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Having is not expected");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenHavingWithNonAggregateExpression_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT City, Count(1) FROM #test.people() GROUP BY City HAVING City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind,
            "HAVING clause requires a boolean expression");
        AssertHasGuidance(ex);
    }

    #endregion

    // ========================================================================
    // GAP 15: Escape sequence errors
    // Spec defines valid escape sequences. No tests for invalid ones.
    // ========================================================================

    #region Escape sequence errors

    [TestMethod]
    public void WhenInvalidEscapeSequence_ShouldBeTreatedAsLiteral()
    {
        var vm = CompileQuery("SELECT '\\q' FROM #test.single()");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenIncompleteUnicodeEscape_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT '\\u12' FROM #test.single()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1004_InvalidEscapeSequence, DiagnosticPhase.Parse, "\\u12");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenIncompleteHexEscape_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT '\\x1' FROM #test.single()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1004_InvalidEscapeSequence, DiagnosticPhase.Parse, "\\x1");
        AssertHasGuidance(ex);
    }

    #endregion

    // ========================================================================
    // GAP 16: Chained set operations errors
    // No tests for multiple chained UNION/EXCEPT with mismatched schemas.
    // ========================================================================

    #region Chained set operation errors

    [TestMethod]
    public void WhenChainedUnionWithMismatchedColumnCounts_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT Name FROM #test.people() UNION (Name) SELECT Name FROM #test.people() UNION (Name) SELECT Name, Age FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3019_SetOperatorColumnCount, DiagnosticPhase.Bind, "same quantity of columns");
    }

    #endregion

    // ========================================================================
    // GAP 17: RowNumber() misuse
    // No tests for calling RowNumber with arguments.
    // ========================================================================

    #region RowNumber misuse

    [TestMethod]
    public void WhenRowNumberWithArguments_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT RowNumber(1) FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3029_UnresolvableMethod, DiagnosticPhase.Bind, "RowNumber");
        AssertHasGuidance(ex);
    }

    #endregion

    // ========================================================================
    // GAP 18: DISTINCT + GROUP BY interaction
    // No test for DISTINCT combined with GROUP BY.
    // ========================================================================

    #region DISTINCT + GROUP BY

    [TestMethod]
    public void WhenDistinctWithGroupBy_ShouldCompileSuccessfully()
    {
        var vm = CompileQuery(
            "SELECT DISTINCT City, Count(1) FROM #test.people() GROUP BY City");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
    }

    #endregion

    // ========================================================================
    // GAP 19: Additional parse errors not covered
    // Miscellaneous parser-level errors found during spec analysis.
    // ========================================================================

    #region Additional parse-level gaps

    [TestMethod]
    public void WhenJoinWithoutTableSource_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() a INNER JOIN ON a.Id = 1"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "cannot be used here");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenMultipleFromClauses_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() FROM #test.orders()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is Select");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSelectWithOnlyKeyword_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2005_InvalidSelectList, DiagnosticPhase.Parse, "SELECT list cannot be empty");
    }

    [TestMethod]
    public void WhenNestedSubqueryInFrom_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM (SELECT Name FROM #test.people()) AS sub"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "cannot be used here");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenHavingBeforeGroupBy_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT City, Count(1) FROM #test.people() HAVING Count(1) > 1 GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Having is not expected");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSkipBeforeTake_ShouldProperlyWork()
    {
        var vm = CompileQuery(
            "SELECT Name FROM #test.people() ORDER BY Name SKIP 1 TAKE 2");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void WhenTakeWithZero_ShouldReturnEmpty()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() TAKE 0");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenSkipWithZero_ShouldReturnAll()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() SKIP 0 TAKE 100");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    #endregion

    // ========================================================================
    // GAP 20: Multiple statements / semicolons
    // Spec allows optional semicolons and multiple statements.
    // No tests for invalid multi-statement combinations.
    // ========================================================================

    #region Multiple statements / semicolons

    [TestMethod]
    public void WhenTwoSelectStatements_ShouldThrowError()
    {
        // Known quality gap: multiple statements still fall through to generated-code compilation,
        // but the surfaced message should clearly say query processing failed.
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT 1 FROM #test.single(); SELECT 2 FROM #test.single()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ9999_Unknown, DiagnosticPhase.Runtime, "Query processing failed:");
    }

    [TestMethod]
    public void WhenSemicolonOnly_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(";"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Semicolon is not expected");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenMultipleSemicolons_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(";;;"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Semicolon is not expected");
        AssertHasGuidance(ex);
    }

    #endregion

    // ========================================================================
    // GAP 21: Additional semantic errors not in other test files
    // ========================================================================

    #region Additional semantic gaps

    [TestMethod]
    public void WhenGroupByOnNonExistentColumnWithHaving_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT Count(1) FROM #test.people() GROUP BY NonExistent HAVING Count(1) > 0"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Unknown column 'NonExistent'");
    }

    [TestMethod]
    public void WhenOrderByOnAliasedColumnCorrectly_ShouldSucceed()
    {
        var vm = CompileQuery(
            "SELECT Name AS PersonName FROM #test.people() ORDER BY PersonName");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenMultipleAggregatesWithNonGroupedColumn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT Name, Count(1), Sum(Age) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3012_NonAggregateInSelect, DiagnosticPhase.Bind, "must appear in the GROUP BY");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenCteWithSameNameAsSchemaSource_ShouldCompileSuccessfully()
    {
        var vm = CompileQuery(
            "WITH people AS (SELECT Name FROM #test.people()) SELECT Name FROM people");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenJoinOnConditionAlwaysFalse_ShouldSucceedWithEmptyResult()
    {
        var vm = CompileQuery(
            "SELECT a.Name FROM #test.people() a INNER JOIN #test.orders() o ON 1 = 0");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenLeftJoinWithNonExistentColumnInOn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT a.Name FROM #test.people() a LEFT JOIN #test.orders() o ON a.NonExistent = o.PersonId"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Unknown column 'NonExistent'");
    }

    [TestMethod]
    public void WhenRightJoinWithNonExistentColumnInOn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT a.Name FROM #test.people() a RIGHT JOIN #test.orders() o ON a.Id = o.NonExistent"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Unknown column 'NonExistent'");
    }

    #endregion

    // ========================================================================
    // GAP 22: NULL literal edge cases
    // Tests cover IS NULL/IS NOT NULL but not NULL in expressions.
    // ========================================================================

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

    // ========================================================================
    // GAP 23: LIKE/RLIKE with NULL patterns
    // Spec section 18 says LIKE with NULL produces NULL (not matched).
    // ========================================================================

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

    // ========================================================================
    // GAP 24: DESC FUNCTIONS and DESC method forms
    // Only basic DESC tested. No tests for DESC schema.method() form.
    // ========================================================================

    #region DESC with method form

    [TestMethod]
    public void WhenDescWithMethodForm_ShouldSucceed()
    {
        var vm = CompileQuery("DESC #test.people()");
        var table = vm.Run(TokenSource.Token);

        Assert.IsGreaterThanOrEqualTo(0, table.Count, "DESC method form should compile and execute");
    }

    #endregion

    // ========================================================================
    // GAP 25: Cross-feature errors not yet covered
    // ========================================================================

    #region Cross-feature edge cases

    [TestMethod]
    public void WhenCteWithGroupByAndNonAggregatedColumn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "WITH Grouped AS (SELECT City, Name, Count(1) AS Cnt FROM #test.people() GROUP BY City) SELECT * FROM Grouped"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3012_NonAggregateInSelect, DiagnosticPhase.Bind, "must appear in the GROUP BY");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSetOperationInsideCteWithMismatchedColumns_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "WITH Combined AS (SELECT Name FROM #test.people() UNION (Name) SELECT Name, Age FROM #test.people()) SELECT * FROM Combined"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3019_SetOperatorColumnCount, DiagnosticPhase.Bind, "same quantity of columns");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenJoinBetweenCteAndSchemaWithNonExistentColumn_ShouldThrowError()
    {
        // Known quality gap: primary error is a runtime InvalidCastException
        // but the secondary envelope correctly identifies the unknown column
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "WITH Cte AS (SELECT Name FROM #test.people()) SELECT c.Name FROM Cte c INNER JOIN #test.orders() o ON c.NonExistent = o.PersonId"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistent");
    }

    #endregion

    // ========================================================================
    // GAP 26: Missing alias prefix in multi-table context
    // Spec section 22.1 mentions AliasMissingException for multi-table queries.
    // ========================================================================

    #region Missing alias prefix in multi-table context

    [TestMethod]
    public void WhenFunctionCallWithoutAliasPrefixInJoin_WhenSharedMethod_ShouldAutoResolve()
    {
        var vm = CompileQuery(
            "SELECT ToUpper(Name) FROM #test.people() a INNER JOIN #test.orders() o ON a.Id = o.PersonId");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenColumnWithoutAliasPrefixInJoin_WhenUnambiguous_ShouldSucceed()
    {
        var vm = CompileQuery(
            "SELECT Name FROM #test.people() a INNER JOIN #test.orders() o ON a.Id = o.PersonId");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    #endregion

    // ========================================================================
    // GAP 27: ILIKE operator error (spec section 22.1)
    // Spec says ILIKE should suggest LIKE. Only P_MISC_04 tests through
    // QueryAnalyzer, not CompileQuery.
    // ========================================================================

    #region ILIKE error via CompileQuery

    [TestMethod]
    public void WhenILikeUsed_ShouldThrowErrorSuggestingLike()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Name ILIKE '%ali%'"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "is not expected here");
        AssertHasGuidance(ex);
    }

    #endregion
}
