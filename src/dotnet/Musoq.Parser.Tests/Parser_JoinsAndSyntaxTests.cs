using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Parser tests: Joins, coupling syntax, syntax errors, and CASE.
/// </summary>
[TestClass]
public class Parser_JoinsAndSyntaxTests
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
    public void SelectWithUnnecessaryFirstComma_ShouldFail()
    {
        var query = "select ,1, 2, 3 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void SelectWithUnnecessaryLastComma_ShouldFail()
    {
        var query = "select 1, from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void SelectWithSelectInsideQuery_ShouldFail()
    {
        var query = "select ,, from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void GroupByWithUnnecessaryFirstComma_ShouldParse()
    {
        var query = "select 1 from #some.a() group by ,1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void GroupByWithUnnecessaryLastComma_ShouldFail()
    {
        var query = "select 1 from #some.a() group by 1,";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void SelectWithMissingFrom_ShouldFail()
    {
        var query = "sleect 1 from #some.a() group by 1,";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void FromTypo_ShouldFail()
    {
        var query = "select 1 form #some.a() group by 1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.Throws<SyntaxException>(() => parser.ComposeAll());

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

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

}
