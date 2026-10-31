using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class ScopedLayoutRegressionTests
{
    [TestMethod]
    [DataRow("select q.Name from (/* keep */ from #s.rows() a select a.Name) q",
        "SELECT q.Name\nFROM (\n    /* keep */\n    FROM #s.rows() a\n    SELECT a.Name\n) q\n")]
    [DataRow("select * from (/* keep */ pivot values { (Quarter: 'Q1', Amount: 10d) } sales on Quarter in ('Q1' as Q1) using Sum(Amount) as Sales) q",
        "SELECT *\nFROM (\n    /* keep */\n    PIVOT VALUES { (Quarter: 'Q1', Amount: 10d) } sales\n        ON Quarter IN ('Q1' AS Q1) USING Sum(Amount) AS Sales\n) q\n")]
    [DataRow("select * from (-- keep\n unpivot values { (Region: 'North', Q1:10d,Q2:20d) } s on Quarter in (s.Q1 as Q1,s.Q2 as Q2) using Sales keep s.Region as Region) q",
        "SELECT *\nFROM (\n    -- keep\n    UNPIVOT VALUES { (Region: 'North', Q1: 10d, Q2: 20d) } s\n        ON Quarter IN (s.Q1 AS Q1, s.Q2 AS Q2) USING Sales KEEP s.Region AS Region\n) q\n")]
    public void CommentedQueryFamilies_ShouldUseValidatedQueryRegions(string source, string expected)
    {
        var result = MusoqFormatter.Format(source);

        Assert.AreEqual(expected, result.FormattedText);
        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(expected).ToArray());
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(expected));
        Assert.AreEqual(expected, MusoqFormatter.Format(expected).FormattedText);
    }

    [TestMethod]
    [DataRow("/* keep */", "    /* keep */\n")]
    [DataRow("-- keep\n", "    -- keep\n")]
    public void LeadingSubqueryComment_ShouldPreserveQueryClauseLayout(string comment, string expectedComment)
    {
        var source = "select q.Name from (" + comment + " select a.Name from #s.rows() a) q";
        var expected = "SELECT q.Name\nFROM (\n" + expectedComment + "    SELECT a.Name\n    FROM #s.rows() a\n) q\n";
        var result = MusoqFormatter.Format(source);

        Assert.AreEqual(expected, result.FormattedText);
        Assert.AreEqual(expected, MusoqFormatter.Format(expected).FormattedText);
        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(expected).ToArray());
    }

    [TestMethod]
    public void NestedCaseInWhenCondition_ShouldUseTheOuterThenBoundary()
    {
        const string source = "select case when case a.X when 1 then 2 else 3 end = 2 then 1 else 0 end from #s.rows() a";
        const string expected = "SELECT\n    CASE\n        WHEN\n            CASE a.X\n                WHEN 1 THEN 2\n                ELSE 3\n            END = 2\n        THEN\n            1\n        ELSE 0\n    END\nFROM #s.rows() a\n";
        var result = MusoqFormatter.Format(source);

        Assert.AreEqual(expected, result.FormattedText);
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(expected));
        Assert.AreEqual(expected, MusoqFormatter.Format(expected).FormattedText);
    }

    [TestMethod]
    public void CommentedCteQueryGroup_ShouldRetainItsQueryScope()
    {
        const string source = "select q.Name from (/* keep */ with c as (select Name from #s.rows()) select Name from c) q";
        const string expected = "SELECT q.Name\nFROM (\n    /* keep */\n    WITH c AS (\n        SELECT Name\n        FROM #s.rows()\n    )\n    SELECT Name\n    FROM c\n) q\n";
        var result = MusoqFormatter.Format(source);

        Assert.AreEqual(expected, result.FormattedText);
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(expected));
    }

    [TestMethod]
    public void LaterDiagnosticQuery_ShouldNotChangeAnEarlierStatementLayout()
    {
        const string first = "let value: int = 1;";
        const string later = "profile select Name from #s.rows()";
        var expectedFirst = MusoqFormatter.Format(first).FormattedText;
        var result = MusoqFormatter.Format(first + later);

        Assert.IsTrue(result.IsSuccess);
        StringAssert.StartsWith(result.FormattedText, expectedFirst! + "\n");
        Assert.AreEqual(AstSignature.Parse(first + later), AstSignature.Parse(result.FormattedText!));
    }
}
