using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserTests
{
    [TestMethod]
    public void CheckReorderedQueryWithJoin_ShouldConstructQuery()
    {
        var query =
            "from @some.a() s1 inner join @some.b() s2 on s1.col = s2.col where s1.col2 = '1' group by s2.col3 select s1.col4, s2.col4 skip 1 take 1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CouplingSyntax_ComposeSchemaMethodWithKeywordAsMethod_ShouldParse()
    {
        var query = "couple @some.table with table Test as SourceOfTestValues;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CouplingSyntax_ComposeSchemaMethodWithWordAsMethod_ShouldParse()
    {
        var query = "couple @some.something with table Test as SourceOfTestValues;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void CouplingSyntax_ComposeSchemaMethodWithWordFinishedWithNumberAsMethod_ShouldParse()
    {
        var query = "couple @some.something4 with table Test as SourceOfTestValues;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void SelectWithUnnecessaryFirstComma_ShouldFail()
    {
        var query = "select ,1, 2, 3 from @some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void SelectWithUnnecessaryLastComma_ShouldFail()
    {
        var query = "select 1, from @some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void SelectTwoCommas_ShouldFail()
    {
        var query = "select ,, from @some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void GroupByWithUnnecessaryFirstComma_ShouldParse()
    {
        var query = "select 1 from @some.a() group by ,1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void GroupByWithUnnecessaryLastComma_ShouldFail()
    {
        var query = "select 1 from @some.a() group by 1,";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void SelectTypo_ShouldFail()
    {
        var query = "sleect 1 from @some.a() group by 1,";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void FromTypo_ShouldFail()
    {
        var query = "select 1 form @some.a() group by 1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());

        Assert.AreEqual("select 1 form @some.", exc.QueryPart);
    }

    [TestMethod]
    public void SemicolonAtTheEnd_ShouldPass()
    {
        var query = "select 1 from @some.a() order by x;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCaseWhenWithMissingEnd_ShouldFail()
    {
        var query = "select case when 1 = 1 then 1 else 0 from @some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void WhenCommentAtTheBegining_ShouldParse()
    {
        var query = """
                    --some comment
                    select
                        1
                    from @some.a() --some comment
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentAfterColumn_ShouldParse()
    {
        var query = """
                    select
                        1 --some comment
                    from @some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentAfterQuery_ShouldParse()
    {
        var query = """
                    select
                        1
                    from @some.a() --some comment
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentAtTheNewLineAfterQuery_ShouldParse()
    {
        var query = """
                    select
                        1
                    from @some.a() 
                    --some comment
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentAfterColumnAndAtTheNextLine_ShouldParse()
    {
        var query = """
                    select
                        1 --some comment
                        --some comment
                    from @some.a() 
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentBetweenKeywords_ShouldParse()
    {
        var query = """
                    select --some comment
                        1
                    from @some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenMultipleCommentsOnSameLine_ShouldParse()
    {
        var query = """
                    select --first comment --second comment
                        1
                    from @some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentContainsSpecialCharacters_ShouldParse()
    {
        var query = """
                    select
                        1
                    from @some.a() --comment with !@#$%^&*()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentContainsSQLKeywords_ShouldParse()
    {
        var query = """
                    select
                        1
                    from @some.a() --comment containing SELECT FROM WHERE
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenEmptyComment_ShouldParse()
    {
        var query = """
                    select
                        1 --
                    from @some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentHasLeadingSpaces_ShouldParse()
    {
        var query = """
                    select
                        1 --    spaced comment
                    from @some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentsAroundJoins_ShouldParse()
    {
        var query = """
                    select
                        1
                    from @some.a() a--comment before join
                    inner join @some.b() b--comment after join
                        on a.id = b.id
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void WhenCommentContainsDoubleHyphen_ShouldParse()
    {
        var query = """
                    select
                        1 -- comment with -- inside
                    from @some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    [DataRow("select 1 from @some.thing() r cross apply r.Prop.Nested c")]
    [DataRow("select 1 from @some.thing() r cross apply r.Prop.Nested c cross apply c.Prop.Nested2 d")]
    [DataRow("select 1 from @some.thing() r cross apply r.Prop.Nested.Deeply c")]
    public void WhenNestedPropertyUsedWithCrossApply_ShouldPass(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }
}