using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive gap-filling tests for malformed queries.
///     Each test verifies that a specific kind of user mistake
///     produces a clear, informative error with structured diagnostics
///     rather than succeeding silently or producing a cryptic internal exception.
/// </summary>
[TestClass]
public partial class MalformedQueryErrorTests : NegativeTestsBase
{
    // ========================================================================
    // GAP 1: BETWEEN operator errors
    // Spec confirms BETWEEN support (x BETWEEN a AND b).
    // No existing tests for malformed BETWEEN expressions.
    // ========================================================================


    // ========================================================================
    // GAP 2: Legacy ::N syntax errors
    // Runtime v2 reserves :: for postfix casts and uses GROUP BY ordinals.
    // These tests keep old prefix ::N syntax rejected at parse time.
    // ========================================================================


    // ========================================================================
    // GAP 3: CASE WHEN without ELSE (mandatory ELSE per spec)
    // Spec (Appendix F) says ELSE is mandatory in all CASE expressions.
    // Existing tests cover empty CASE, missing THEN, missing END — but not
    // the specific case of well-formed WHEN/THEN with missing ELSE.
    // ========================================================================


    // ========================================================================
    // GAP 4: Simple CASE expression errors
    // Spec supports CASE expr WHEN val THEN ... ELSE ... END.
    // No existing tests for malformed simple CASE syntax.
    // ========================================================================


    // ========================================================================
    // GAP 5: FROM-first (reordered) query errors
    // Spec section 16 describes FROM ... WHERE ... SELECT syntax.
    // Zero negative tests for malformed reordered queries.
    // ========================================================================


    // ========================================================================
    // GAP 6: Numeric literal edge cases
    // Spec (Appendix D) defines type suffixes and hex/bin/octal formats.
    // Very limited existing tests for literal parsing errors.
    // ========================================================================


    // ========================================================================
    // GAP 7: CONTAINS operator misuse
    // Only TE062 tests CONTAINS on int column.
    // No tests for CONTAINS syntax errors.
    // ========================================================================


    // ========================================================================
    // GAP 8: IN operator edge cases
    // Only PE_EXPR_10 covers IN without parens.
    // No tests for IN with empty list or mixed types.
    // ========================================================================


    // ========================================================================
    // GAP 9: DESC statement errors
    // Only E_DESC_01 covers DESC on non-existent schema.
    // No tests for other malformed DESC syntax.
    // ========================================================================


    // ========================================================================
    // GAP 10: ORDER BY edge cases
    // Spec says ORDER BY by position number is NOT supported.
    // No test for this; also no test for ORDER BY on column not in scope.
    // ========================================================================


    // ========================================================================
    // GAP 11: APPLY (CROSS/OUTER) semantic errors
    // P_STRUCT_15 covers missing alias. No tests for APPLY semantic errors
    // like non-existent method in APPLY or OUTER APPLY without alias.
    // ========================================================================


    // ========================================================================
    // GAP 12: TABLE/COUPLE semantic errors via CompileQuery
    // Structural syntax tests exist but no NegativeTestsBase-level tests
    // for TABLE/COUPLE semantic errors.
    // ========================================================================


    // ========================================================================
    // GAP 13: Set operation key column errors
    // No tests for set operations referencing non-existent key columns.
    // ========================================================================


    // ========================================================================
    // GAP 14: HAVING edge cases
    // Existing tests cover HAVING on non-existent column and aggregate in WHERE.
    // No tests for HAVING without GROUP BY or HAVING with non-boolean.
    // ========================================================================


    // ========================================================================
    // GAP 15: Escape sequence errors
    // Spec defines valid escape sequences. No tests for invalid ones.
    // ========================================================================


    // ========================================================================
    // GAP 16: Chained set operations errors
    // No tests for multiple chained UNION/EXCEPT with mismatched schemas.
    // ========================================================================


    // ========================================================================
    // GAP 17: RowNumber() misuse
    // No tests for calling RowNumber with arguments.
    // ========================================================================


    // ========================================================================
    // GAP 18: DISTINCT + GROUP BY interaction
    // No test for DISTINCT combined with GROUP BY.
    // ========================================================================


    // ========================================================================
    // GAP 19: Additional parse errors not covered
    // Miscellaneous parser-level errors found during spec analysis.
    // ========================================================================


    // ========================================================================
    // GAP 20: Multiple statements / semicolons
    // Spec allows optional semicolons and multiple statements.
    // No tests for invalid multi-statement combinations.
    // ========================================================================


    // ========================================================================
    // GAP 21: Additional semantic errors not in other test files
    // ========================================================================


    // ========================================================================
    // GAP 22: NULL literal edge cases
    // Tests cover IS NULL/IS NOT NULL but not NULL in expressions.
    // ========================================================================


    // ========================================================================
    // GAP 23: LIKE/RLIKE with NULL patterns
    // Spec section 18 says LIKE with NULL produces NULL (not matched).
    // ========================================================================


    // ========================================================================
    // GAP 24: DESC FUNCTIONS and DESC method forms
    // Only basic DESC tested. No tests for DESC schema.method() form.
    // ========================================================================


    // ========================================================================
    // GAP 25: Cross-feature errors not yet covered
    // ========================================================================


    // ========================================================================
    // GAP 26: Missing alias prefix in multi-table context
    // Spec section 22.1 mentions AliasMissingException for multi-table queries.
    // ========================================================================


    // ========================================================================
    // GAP 27: ILIKE operator error (spec section 22.1)
    // Spec says ILIKE should suggest LIKE. Only P_MISC_04 tests through
    // QueryAnalyzer, not CompileQuery.
    // ========================================================================

}
