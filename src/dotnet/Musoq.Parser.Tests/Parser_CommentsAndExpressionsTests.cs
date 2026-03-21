using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Parser tests: Comments, number formats, and arithmetic expressions.
/// </summary>
[TestClass]
public class Parser_CommentsAndExpressionsTests
{
    [TestMethod]
    public void WhenCommentAtTheBegining_ShouldParse()
    {
        var query = """
                    --some comment
                    select
                        1
                    from #some.a() --some comment
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
                    from #some.a()
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
                    from #some.a() --some comment
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
                    from #some.a() 
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
                    from #some.a() 
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
                    from #some.a()
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
                    from #some.a()
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
                    from #some.a() --comment with !@#$%^&*()
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
                    from #some.a() --comment containing SELECT FROM WHERE
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
                    from #some.a()
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
                    from #some.a()
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
                    from #some.a() a--comment before join
                    inner join #some.b() b--comment after join
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
                    from #some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void HexadecimalLiteral_InSelectStatement_ShouldParse()
    {
        var query = "select 0xFF from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void BinaryLiteral_InSelectStatement_ShouldParse()
    {
        var query = "select 0b101 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void OctalLiteral_InSelectStatement_ShouldParse()
    {
        var query = "select 0o77 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void NumberFormats_InArithmeticExpression_ShouldParse()
    {
        var query = "select 0xFF + 0b101 - 0o77 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void NumberFormats_InWhereClause_ShouldParse()
    {
        var query = "select 1 from #some.a() where column = 0xFF";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void NumberFormats_InGroupByClause_ShouldParse()
    {
        var query = "select count(*) from #some.a() group by column + 0xFF";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void NumberFormats_CaseInsensitive_ShouldParse()
    {
        var query = "select 0XFF + 0B101 + 0O77 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void NumberFormats_ComplexArithmetic_ShouldParse()
    {
        var query = "select (0xFF * 0b10) / (0o7 + 1) from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void NumberFormats_InFunctionCall_ShouldParse()
    {
        var query = "select ABS(0xFF - 0b11111111) from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void NumberFormats_WithParentheses_ShouldParse()
    {
        var query = "select (0xFF) + (0b101) * (0o77) from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    [DataRow("select 1 from #some.thing() r cross apply r.Prop.Nested c")]
    [DataRow("select 1 from #some.thing() r cross apply r.Prop.Nested c cross apply c.Prop.Nested2 d")]
    [DataRow("select 1 from #some.thing() r cross apply r.Prop.Nested.Deeply c")]
    public void WhenNestedPropertyUsedWithCrossApply_ShouldPass(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void ComplexNestedArithmeticExpression_ShouldParse()
    {
        var query =
            "select (((((1 + (6 * 2)) + 4 + 4 + 4 + 2 + 8 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 1 + 32 + 1 + 4 + 4 + 4 + 1 + 4 + 4 + 1 + (6 * 4) + 1 + 1 + 1 + 1 + 32 + 1) + 4) + 1 + 1) + 4 + 4) + 4 + 4 + 4 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void VeryLongArithmeticChain_ShouldParseQuickly()
    {
        var numbers = string.Join(" + ", Enumerable.Range(1, 50).Select(i => i.ToString()));
        var query = $"select {numbers} from #a.b()";

        var sw = Stopwatch.StartNew();
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        sw.Stop();

        Assert.IsNotNull(result);
        Assert.IsLessThan(100, sw.ElapsedMilliseconds, $"Parser should be fast but took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void DeeplyNestedParentheses_ShouldParse()
    {
        var expr = "((((((((((1 + 2))))))))))";
        var query = $"select {expr} from #a.b()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void MixedOperatorPrecedence_ShouldParseCorrectly()
    {
        var query = "select 1 + 2 * 3 - 4 / 2 + 5 * 6 - 7 + 8 / 4 from #a.b()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ComplexNestedWithMultipleOperators_ShouldParse()
    {
        var query = "select ((1 + 2) * (3 - 4)) / ((5 + 6) - (7 * 8)) + ((9 / 10) * (11 + 12)) from #a.b()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ExtremeLongExpression_ShouldParseInReasonableTime()
    {
        var numbers = string.Join(" + ", Enumerable.Range(1, 100).Select(i => i.ToString()));
        var query = $"select {numbers} from #a.b()";

        var sw = Stopwatch.StartNew();
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        sw.Stop();

        Assert.IsNotNull(result);
        Assert.IsLessThan(200, sw.ElapsedMilliseconds,
            $"Parser should handle 100 additions in <200ms but took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void MultipleNestedSubExpressions_ShouldParse()
    {
        var query = "select (1 + (2 * (3 - (4 / (5 + 6))))) + (7 - (8 * (9 + (10 / 2)))) from #a.b()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CombinedArithmeticAndParentheses_StressTest()
    {
        var innerExpr = string.Join(" + ", Enumerable.Range(1, 20).Select(i => i.ToString()));
        var query = $"select ((({innerExpr}))) * 2 + ((({innerExpr}))) from #a.b()";

        var sw = Stopwatch.StartNew();
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        sw.Stop();

        Assert.IsNotNull(result);
        Assert.IsLessThan(200, sw.ElapsedMilliseconds,
            $"Parser should handle complex combined expressions in <200ms but took {sw.ElapsedMilliseconds}ms");
    }

}
