using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class QueryFormattingTests
{
    [TestMethod]
    [DataRow("select a.Name from #s.rows() a where a.Price>10 and (a.Flag=1 or a.Flag=2)", "SELECT a.Name\nFROM #s.rows() a\nWHERE\n    a.Price > 10\n    AND (\n        a.Flag = 1\n        OR a.Flag = 2\n    )\n")]
    [DataRow("select a.Name from #s.rows() a where a.Price between 1 and 10 and a.Flag=1", "SELECT a.Name\nFROM #s.rows() a\nWHERE\n    a.Price BETWEEN 1 AND 10\n    AND a.Flag = 1\n")]
    [DataRow("select a.Name,b.Name from #s.rows() a inner join #s.rows() b on a.Id=b.Id and b.Flag=true", "SELECT a.Name, b.Name\nFROM #s.rows() a\nINNER JOIN #s.rows() b\n    ON a.Id = b.Id\n    AND b.Flag = true\n")]
    [DataRow("select a.Name from #s.rows() a cross apply a.Items() b", "SELECT a.Name\nFROM #s.rows() a\nCROSS APPLY a.Items() b\n")]
    [DataRow("select a.Name from #s.rows() a union all select b.Name from #s.rows() b", "SELECT a.Name\nFROM #s.rows() a\nUNION ALL\nSELECT b.Name\nFROM #s.rows() b\n")]
    [DataRow("from #s.rows() a where a.Flag=1 select a.Name", "FROM #s.rows() a\nWHERE\n    a.Flag = 1\nSELECT a.Name\n")]
    [DataRow("select case a.Flag when 1 then 0xFF end from #s.rows() a", "SELECT\n    CASE a.Flag\n        WHEN 1 THEN 0xFF\n    END\nFROM #s.rows() a\n")]
    [DataRow("select case when a.Flag=1 then 'Yes' else 'No' end Answer from #s.rows() a", "SELECT\n    CASE\n        WHEN a.Flag = 1 THEN 'Yes'\n        ELSE 'No'\n    END Answer\nFROM #s.rows() a\n")]
    [DataRow("with p as (select a.Name from #s.rows() a) select p.Name from p", "WITH p AS (\n    SELECT a.Name\n    FROM #s.rows() a\n)\nSELECT p.Name\nFROM p\n")]
    [DataRow("select a.Name,Count(*) Total from #s.rows() a group by a.Name having Count(*)>1 order by a.Name desc skip 1 take 5", "SELECT a.Name, Count(*) Total\nFROM #s.rows() a\nGROUP BY a.Name\nHAVING\n    Count(*) > 1\nORDER BY a.Name DESC\nSKIP 1\nTAKE 5\n")]
    [DataRow("select a.Name from #s.rows() a where a.Id in (1,2,3)", "SELECT a.Name\nFROM #s.rows() a\nWHERE\n    a.Id IN (1, 2, 3)\n")]
    [DataRow("select a.Name from #s.rows() a where exists (select b.Id from #s.rows() b where b.Id=a.Id)", "SELECT a.Name\nFROM #s.rows() a\nWHERE\n    EXISTS (\n        SELECT b.Id\n        FROM #s.rows() b\n        WHERE\n            b.Id = a.Id\n    )\n")]
    [DataRow("select a.Name, -- display name\na.Price from #s.rows() a", "SELECT\n    a.Name, -- display name\n    a.Price\nFROM #s.rows() a\n")]
    [DataRow("-- leading\nselect a.Name from #s.rows() a -- trailing\n", "-- leading\nSELECT a.Name\nFROM #s.rows() a -- trailing\n")]
    [DataRow("select ((a.Price+1)) Label from #s.rows() a", "SELECT ((a.Price + 1)) Label\nFROM #s.rows() a\n")]
    [DataRow("select a.Name from #s.rows() a where a.Price!=0 or a.Price<>1", "SELECT a.Name\nFROM #s.rows() a\nWHERE\n    a.Price != 0\n    OR a.Price <> 1\n")]
    [DataRow("select * exclude (Secret) from #s.rows()", "SELECT * EXCLUDE (Secret)\nFROM #s.rows()\n")]
    [DataRow("with c (Label) as (select Name from #s.rows()) select Label from c", "WITH c (Label) AS (\n    SELECT Name\n    FROM #s.rows()\n)\nSELECT Label\nFROM c\n")]
    public void QuerySyntax_ShouldMatchReviewedLayout(string source, string expected)
    {
        var result = MusoqFormatter.Format(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(expected, result.FormattedText);
        Assert.AreEqual(expected, MusoqFormatter.Format(expected).FormattedText);
        Assert.IsTrue(Parser.Parser.ParseSyntax(expected).Success);
    }

    [TestMethod]
    public void WrappedWindow_ShouldKeepFrameAndSeparateClauses()
    {
        var result = MusoqFormatter.Format("select Sum(Population) over (partition by Region order by Name rows between unbounded preceding and current row) Total from #s.rows()");

        Assert.AreEqual("SELECT\n    Sum(Population) OVER (\n        PARTITION BY Region\n        ORDER BY Name\n        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW\n    ) Total\nFROM #s.rows()\n", result.FormattedText);
        Assert.AreEqual(result.FormattedText, MusoqFormatter.Format(result.FormattedText!).FormattedText);
    }
}
