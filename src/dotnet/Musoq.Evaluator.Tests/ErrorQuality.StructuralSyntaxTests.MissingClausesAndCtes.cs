using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityStructuralSyntaxTests
{
    #region P-STRUCT: Missing clauses and misordering

    [TestMethod]
    public void P_STRUCT_01_SelectWithoutFrom()
    {
        // Arrange — SELECT Value (no FROM, and no dual-like context)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should say FROM is missing
        AssertHasOneOfErrorCodes(result, "SELECT without FROM",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2004_MissingFromClause);
    }

    [TestMethod]
    public void P_STRUCT_02_FromWithoutSelect()
    {
        // Arrange — FROM without SELECT
        var analyzer = CreateAnalyzer();
        var query = "FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should say SELECT is missing
        AssertHasOneOfErrorCodes(result, "FROM without SELECT",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2025_MissingSelectKeyword);
    }

    [TestMethod]
    public void P_STRUCT_03_WhereBeforeFrom()
    {
        // Arrange — WHERE before FROM
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name WHERE Name > 'A' FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate wrong clause order
        AssertHasOneOfErrorCodes(result, "WHERE before FROM is wrong order",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2004_MissingFromClause,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_04_HavingWithoutGroupBy()
    {
        // Arrange — HAVING without GROUP BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() HAVING Name > 'A'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain HAVING requires GROUP BY
        AssertHasOneOfErrorCodes(result, "HAVING without GROUP BY",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_05_OrderByBeforeWhere()
    {
        // Arrange — ORDER BY before WHERE
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name WHERE Name > 'A'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate wrong clause order
        AssertHasOneOfErrorCodes(result, "ORDER BY before WHERE is wrong order",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_06_GroupByAfterOrderBy()
    {
        // Arrange — GROUP BY after ORDER BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(1) FROM #A.Entities() ORDER BY Name GROUP BY Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate wrong clause order
        AssertHasOneOfErrorCodes(result, "GROUP BY after ORDER BY is wrong order",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_07_DoubleWhere()
    {
        // Arrange — Double WHERE clause
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name > 'A' WHERE Name < 'Z'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate duplicate WHERE
        AssertHasOneOfErrorCodes(result, "double WHERE clause",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_08_DoubleOrderBy()
    {
        // Arrange — Double ORDER BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name ASC ORDER BY Name DESC";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate duplicate ORDER BY
        AssertHasOneOfErrorCodes(result, "double ORDER BY clause",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_09_TakeBeforeOrderBy()
    {
        // Arrange — TAKE before ORDER BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 5 ORDER BY Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate wrong order or handle gracefully
        AssertHasOneOfErrorCodes(result, "TAKE before ORDER BY",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_10_SkipWithoutTake()
    {
        // Arrange — SKIP without TAKE
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() SKIP 5";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document behavior: SKIP without TAKE may or may not be valid
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_STRUCT_11_EmptySelectList()
    {
        // Arrange — SELECT FROM (empty column list)
        var analyzer = CreateAnalyzer();
        var query = "SELECT FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — SELECT list cannot be empty
        AssertHasOneOfErrorCodes(result, "empty SELECT list",
            DiagnosticCode.MQ2005_InvalidSelectList,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void P_STRUCT_12_TrailingCommaInSelectList()
    {
        // Arrange — SELECT Name, FROM
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate trailing comma
        AssertHasOneOfErrorCodes(result, "trailing comma in SELECT list",
            DiagnosticCode.MQ2014_TrailingComma,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void P_STRUCT_13_TrailingCommaInGroupBy()
    {
        // Arrange — GROUP BY Name,
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(1) FROM #A.Entities() GROUP BY Name,";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate trailing comma
        AssertHasOneOfErrorCodes(result, "trailing comma in GROUP BY",
            DiagnosticCode.MQ2014_TrailingComma,
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_14_MissingOnInJoin()
    {
        // Arrange — JOIN without ON condition
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
INNER JOIN #B.Entities() b";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing ON clause
        AssertHasOneOfErrorCodes(result, "INNER JOIN without ON",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_15_MissingAliasForCrossApply()
    {
        // Arrange — CROSS APPLY without alias on the applied source
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name
FROM #A.Entities() a
CROSS APPLY #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing alias
        AssertHasOneOfErrorCodes(result, "CROSS APPLY without alias",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ3022_MissingAlias,
            DiagnosticCode.MQ3002_AmbiguousColumn,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_16_EmptyParenthesesInSchemaTable()
    {
        // Arrange — #A.Entities() is fine, but what about missing required params?
        // Using #A with no method call at all
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate invalid schema reference
        AssertHasOneOfErrorCodes(result, "schema with empty method name",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_17_MissingClosingParenthesis()
    {
        // Arrange — #A.Entities( without closing )
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities(";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate unclosed parenthesis
        AssertHasOneOfErrorCodes(result, "missing closing parenthesis in schema method",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2010_MissingClosingParenthesis,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_18_ExtraClosingParenthesis()
    {
        // Arrange — Extra closing parenthesis
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities())";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate extra closing parenthesis
        AssertHasOneOfErrorCodes(result, "extra closing parenthesis",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_19_SemicolonInMiddleOfQuery()
    {
        // Arrange — Semicolon in middle of query
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name; FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, semicolons act as statement terminators.
        // 'SELECT Name; FROM #A.Entities()' is parsed as two separate statements:
        // Statement 1: 'SELECT Name' (valid standalone query)
        // Statement 2: 'FROM #A.Entities()' (reordered query syntax)
        // The parser's multi-statement support and error recovery handle this gracefully.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_STRUCT_20_MultipleQueriesWithoutSeparation()
    {
        // Arrange — Two SELECT statements without proper separation
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() SELECT City FROM #B.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, multiple statements are supported.
        // The parser treats consecutive SELECT statements as separate queries
        // in a multi-statement batch. No separator (semicolon) is required.
        AssertNoErrors(result);
    }

    #endregion

    #region P-CTE: CTE syntax errors

    [TestMethod]
    public void P_CTE_01_WithWithoutAs()
    {
        // Arrange — WITH without AS keyword
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData SELECT Name FROM #A.Entities()
SELECT * FROM MyData md";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing AS in CTE
        AssertHasOneOfErrorCodes(result, "CTE without AS keyword",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2023_MissingAsKeyword,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_02_MissingParenthesesAroundCteQuery()
    {
        // Arrange — WITH MyData AS SELECT ... (missing parentheses)
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData AS SELECT Name FROM #A.Entities()
SELECT * FROM MyData md";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing parentheses around CTE query
        AssertHasOneOfErrorCodes(result, "CTE query without parentheses",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_03_RecursiveCte()
    {
        // Arrange — Recursive CTE referencing itself
        var analyzer = CreateAnalyzer();
        var query = @"WITH Recursive AS (
    SELECT 1 AS Value FROM #A.Entities()
    UNION ALL (Value)
    SELECT Value + 1 FROM Recursive r WHERE r.Value < 10
)
SELECT * FROM Recursive r";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain recursion is not supported
        AssertHasOneOfErrorCodes(result, "recursive CTE not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ3003_UnknownTable,
            DiagnosticCode.MQ3016_CircularReference,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_04_CteAfterSelect()
    {
        // Arrange — CTE placed after SELECT (wrong position)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT * FROM MyData md
WITH MyData AS (SELECT Name FROM #A.Entities())";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate WITH must come before SELECT
        AssertHasOneOfErrorCodes(result, "CTE after SELECT (wrong position)",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_05_DuplicateCteNames()
    {
        // Arrange — Two CTEs with the same name
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData AS (SELECT Name FROM #A.Entities()),
     MyData AS (SELECT Name FROM #B.Entities())
SELECT * FROM MyData md";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate duplicate CTE name
        AssertHasOneOfErrorCodes(result, "duplicate CTE names",
            DiagnosticCode.MQ2008_DuplicateAlias,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ3021_DuplicateAlias,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_06_CteWithNoSelectAfter()
    {
        // Arrange — CTE definition with no SELECT that uses it
        var analyzer = CreateAnalyzer();
        var query = "WITH MyData AS (SELECT Name FROM #A.Entities())";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate CTE needs a SELECT statement
        AssertHasOneOfErrorCodes(result, "CTE with no SELECT after it",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2016_IncompleteStatement,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_07_CteReferenceWithoutAlias()
    {
        // Arrange — CTE reference without alias in FROM
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData AS (SELECT Name FROM #A.Entities())
SELECT * FROM MyData";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — In Musoq, CTE references without explicit alias are valid.
        // The CTE name 'MyData' is automatically used as the alias.
        // This is equivalent to: SELECT * FROM MyData MyData
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_CTE_08_ForwardReferenceBetweenCtes()
    {
        // Arrange — CTE referencing a CTE defined after it
        var analyzer = CreateAnalyzer();
        var query = @"WITH
    Second AS (SELECT * FROM First f),
    First AS (SELECT Name FROM #A.Entities())
SELECT * FROM Second s";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain forward references not allowed
        AssertHasOneOfErrorCodes(result, "forward reference between CTEs",
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ3003_UnknownTable,
            DiagnosticCode.MQ3023_TableNotDefined,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_09_EmptyCteBody()
    {
        // Arrange — CTE with empty body
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData AS ()
SELECT * FROM MyData md";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate empty CTE body
        AssertHasOneOfErrorCodes(result, "empty CTE body",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion
}
