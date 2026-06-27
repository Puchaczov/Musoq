using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityDialectConfusionTests
{
    #region P-JOIN: JOIN syntax variations

    [TestMethod]
    public void P_JOIN_01_CrossJoin_ShouldValidateSyntax()
    {
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
CROSS JOIN #B.Entities() b";

        var result = analyzer.ValidateSyntax(query);

        Assert.IsFalse(result.HasErrors, "CROSS JOIN should be accepted by the parser.");
        Assert.IsTrue(result.IsParsed, "CROSS JOIN query should parse successfully.");
    }

    [TestMethod]
    public void P_JOIN_02_NaturalJoin()
    {
        // Arrange — NATURAL JOIN is not supported
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name FROM #A.Entities() a NATURAL JOIN #B.Entities() b";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error
        AssertHasOneOfErrorCodes(result, "NATURAL JOIN not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_JOIN_03_FullOuterJoin()
    {
        // Arrange — FULL OUTER JOIN is valid Musoq syntax
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
FULL OUTER JOIN #B.Entities() b ON a.Name = b.Name";

        var result = analyzer.ValidateSyntax(query);

        AssertNoErrors(result);
        Assert.IsTrue(result.IsParsed, "FULL OUTER JOIN query should parse successfully.");
    }

    [TestMethod]
    public void P_JOIN_04_FullJoin_Shorthand()
    {
        // Arrange — FULL JOIN shorthand is valid Musoq syntax
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
FULL JOIN #B.Entities() b ON a.Name = b.Name";

        var result = analyzer.ValidateSyntax(query);

        AssertNoErrors(result);
        Assert.IsTrue(result.IsParsed, "FULL JOIN query should parse successfully.");
    }

    [TestMethod]
    public void P_JOIN_05_JoinWithoutInnerKeyword()
    {
        // Arrange — JOIN without INNER keyword is valid in Musoq (= INNER JOIN)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — bare JOIN is accepted as INNER JOIN in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_JOIN_06_LeftJoinWithoutOuterKeyword()
    {
        // Arrange — LEFT JOIN without OUTER is valid in Musoq (= LEFT OUTER JOIN)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
LEFT JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — LEFT JOIN is accepted as LEFT OUTER JOIN in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_JOIN_07_RightJoinWithoutOuterKeyword()
    {
        // Arrange — RIGHT JOIN without OUTER is valid in Musoq (= RIGHT OUTER JOIN)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
RIGHT JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — RIGHT JOIN is accepted as RIGHT OUTER JOIN in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_JOIN_08_UsingClauseInsteadOfOn()
    {
        // Arrange — USING clause instead of ON
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name
FROM #A.Entities() a
INNER JOIN #B.Entities() b USING (Name)";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should suggest ON clause instead
        AssertHasOneOfErrorCodes(result, "USING should suggest ON",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    #region P-SUB: Subquery attempts

    [TestMethod]
    public void P_SUB_01_SubqueryInWhere_In()
    {
        // Arrange — Subquery in WHERE with IN is now supported via CTE rewrite
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
WHERE Name IN (SELECT Name FROM #B.Entities())";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should parse successfully. Subquery IN is now supported.
        Assert.IsTrue(result.IsParsed,
            "Subquery in WHERE IN should parse successfully");
    }

    [TestMethod]
    public void P_SUB_02_SubqueryInSelect()
    {
        // Arrange — Subquery in SELECT (scalar subquery)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name, (SELECT Count(1) FROM #B.Entities()) AS Total
FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Scalar subqueries in SELECT are supported.
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SUB_03_SubqueryInFrom()
    {
        // Arrange — Subquery in FROM (derived table)
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM (SELECT Name FROM #A.Entities()) sub";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Derived tables are supported.
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SUB_04_ExistsSubquery()
    {
        // Arrange — EXISTS subquery
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities() a
WHERE EXISTS (SELECT 1 FROM #B.Entities() b WHERE b.Name = a.Name)";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — EXISTS subqueries are supported.
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SUB_05_NotExistsSubquery()
    {
        // Arrange — NOT EXISTS subquery
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities() a
WHERE NOT EXISTS (SELECT 1 FROM #B.Entities() b WHERE b.Name = a.Name)";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — NOT EXISTS subqueries are supported.
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SUB_06_ScalarSubqueryComparison()
    {
        // Arrange — Scalar subquery in comparison
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
WHERE Population > (SELECT Population FROM #B.Entities())";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Scalar subqueries in comparisons are supported.
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SUB_07_AnySubquery()
    {
        // Arrange — ANY subquery
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
WHERE Population > ANY (SELECT Population FROM #B.Entities())";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — ANY quantified subqueries are supported.
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SUB_08_AllSubquery()
    {
        // Arrange — ALL subquery
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
WHERE Population > ALL (SELECT Population FROM #B.Entities())";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — ALL quantified subqueries are supported.
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    #endregion
}
