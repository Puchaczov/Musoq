using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

/// <summary>
///     Parser tests: Joins, coupling syntax, syntax errors, and CASE.
/// </summary>
[TestClass]
public class ParserJoinsAndSyntaxTests
{
    [TestMethod]
    public void CheckReorderedQueryWithJoin_ShouldConstructQuery()
    {
        var query =
            "from #some.a() s1 inner join #some.b() s2 on s1.col = s2.col where s1.col2 = '1' group by s2.col3 select s1.col4, s2.col4 skip 1 take 1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithShortInnerJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithShortLeftJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 left join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithShortRightJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 right join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithShortFullJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 full join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithFullOuterJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 full outer join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithShortInnerJoinUppercase_ShouldConstructQuery()
    {
        var query =
            "SELECT 1 FROM #some.a() S1 JOIN #some.b() S2 ON S1.COL = S2.COL";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithShortLeftJoinUppercase_ShouldConstructQuery()
    {
        var query =
            "SELECT 1 FROM #some.a() S1 LEFT JOIN #some.b() S2 ON S1.COL = S2.COL";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithShortFullJoinUppercase_ShouldConstructQuery()
    {
        var query =
            "SELECT 1 FROM #some.a() S1 FULL JOIN #some.b() S2 ON S1.COL = S2.COL";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithFullOuterJoinUppercase_ShouldConstructQuery()
    {
        var query =
            "SELECT 1 FROM #some.a() S1 FULL OUTER JOIN #some.b() S2 ON S1.COL = S2.COL";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithSemiJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 semi join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithLeftSemiJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 left semi join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithAntiJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 anti join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithLeftAntiSemiJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 left anti semi join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CheckRegularQueryWithCrossJoin_ShouldConstructQuery()
    {
        var query = "select 1 from #some.a() s1 cross join #some.b() s2";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void SemiJoinWithoutOn_ShouldFail()
    {
        var query = "select 1 from #some.a() s1 semi join #some.b() s2";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void AntiJoinWithoutOn_ShouldFail()
    {
        var query = "select 1 from #some.a() s1 anti join #some.b() s2";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void FullOuterJoinWithoutOn_ShouldFail()
    {
        var query = "select 1 from #some.a() s1 full outer join #some.b() s2";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void RowPresencePredicates_ShouldConstructQuery()
    {
        var query = @"
select
    case
        when s2 is missing then 'left-only'
        when s1 is present then 'matched'
        else 'right-only'
    end
from #some.a() s1
full outer join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CrossJoinWithOn_ShouldFail()
    {
        var query = "select 1 from #some.a() s1 cross join #some.b() s2 on s1.col = s2.col";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void CouplingSyntax_ComposeSchemaMethodWithKeywordAsMethod_ShouldParse()
    {
        var query = "couple #some.table with table Test as SourceOfTestValues;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CouplingSyntax_ComposeSchemaMethodWithWordAsMethod_ShouldParse()
    {
        var query = "couple #some.something with table Test as SourceOfTestValues;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CouplingSyntax_ComposeSchemaMethodWithWordFinishedWithNumberAsMethod_ShouldParse()
    {
        var query = "couple #some.something4 with table Test as SourceOfTestValues;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    [DataRow("couple #some.something with settings profile as SourceOfTestValues;")]
    [DataRow("couple #some.something with table Test and settings profile as SourceOfTestValues;")]
    [DataRow("couple #some.something with settings profile and table Test as SourceOfTestValues;")]
    public void CouplingSyntax_WithSettingsOptions_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    [DataRow("couple #some.something with table First and table Second as SourceOfTestValues;")]
    [DataRow("couple #some.something with settings first and settings second as SourceOfTestValues;")]
    [DataRow("couple #some.something with as SourceOfTestValues;")]
    public void CouplingSyntax_WithInvalidSettingsOptions_ShouldFail(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void SelectWithUnnecessaryFirstComma_ShouldFail()
    {
        var query = "select ,1, 2, 3 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void SelectWithUnnecessaryLastComma_ShouldFail()
    {
        var query = "select 1, from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void SelectWithSelectInsideQuery_ShouldFail()
    {
        var query = "select ,, from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void GroupByWithUnnecessaryFirstComma_ShouldParse()
    {
        var query = "select 1 from #some.a() group by ,1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void GroupByWithUnnecessaryLastComma_ShouldFail()
    {
        var query = "select 1 from #some.a() group by 1,";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void SelectWithMissingFrom_ShouldFail()
    {
        var query = "sleect 1 from #some.a() group by 1,";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void FromTypo_ShouldFail()
    {
        var query = "select 1 form #some.a() group by 1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.Throws<SyntaxException>(parser.ComposeAll);

        Assert.AreEqual("select 1 form #some.", exc.QueryPart);
    }

    [TestMethod]
    public void SemicolonAtTheEnd_ShouldPass()
    {
        var query = "select 1 from #some.a() order by x;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCaseWhenWithMissingEnd_ShouldFail()
    {
        var query = "select case when 1 = 1 then 1 else 0 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

}
