using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserTests
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

    [TestMethod]
    public void OrderByWithDescKeyword_ShouldParse()
    {
        var query = "select Name from #some.a() order by Name desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByWithMultipleColumnsDesc_ShouldParse()
    {
        var query = "select Name from #some.a() order by Name desc, Age desc, City desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByWithMixedAscDesc_ShouldParse()
    {
        var query = "select Name from #some.a() order by Name asc, Age desc, City";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescWithComplexExpression_ShouldParse()
    {
        var query = "select Name from #some.a() order by (Age * 2 + 5) desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescWithFunctionCall_ShouldParse()
    {
        var query = "select Name from #some.a() order by ToUpper(Name) desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescWithCaseWhen_ShouldParse()
    {
        var query = "select Name from #some.a() order by case when Age > 18 then 1 else 0 end desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescCaseInsensitive_ShouldParse()
    {
        var query = "select Name from #some.a() order by Name DESC";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescWithWhitespace_ShouldParse()
    {
        var query = "select Name from #some.a() order by   Name   desc  ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescWithGroupBy_ShouldParse()
    {
        var query = "select Name, Count(*) from #some.a() group by Name order by Name desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescInCte_ShouldParse()
    {
        var query = "with cte as (select Name from #some.a() order by Name desc) select * from cte";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescWithSkipTake_ShouldParse()
    {
        var query = "select Name from #some.a() order by Name desc skip 10 take 20";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescWithComments_ShouldParse()
    {
        var query = @"
            select Name from #some.a() 
            order by 
                Name desc -- sort by name descending
        ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescWithTableAlias_ShouldParse()
    {
        var query = "select a.Name from #some.a() a order by a.Name desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescWithJoin_ShouldParse()
    {
        var query =
            "select a.Name from #some.a() a inner join #some.b() b on a.Id = b.Id order by a.Name desc, b.Age desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void OrderByDescMultiline_ShouldParse()
    {
        var query = @"
            select Name 
            from #some.a() 
            order by 
                Name desc,
                Age desc,
                City desc
        ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void WhenTwoCommentsWithEmptyLineThenQuery_ShouldParse()
    {
        var query = """
                    --comment 1
                    --comment 2

                    select 1 from #some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void WhenThreeCommentsWithEmptyLinesThenQuery_ShouldParse()
    {
        var query = """
                    --comment 1
                    --comment 2
                    --comment 3


                    select 1 from #some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void WhenEmptyLineBetweenCommentsThenQuery_ShouldParse()
    {
        var query = """
                    --comment 1

                    --comment 2

                    select 1 from #some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void WhenMultiLineCommentWithEmptyLineThenQuery_ShouldParse()
    {
        var query = """
                    /* comment 1
                       comment 2 */

                    select 1 from #some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void WhenMixedCommentsWithEmptyLinesThenQuery_ShouldParse()
    {
        var query = """
                    --single line comment
                    /* multi-line
                       comment */

                    select 1 from #some.a()
                    """;

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.IsNotNull(parser.ComposeAll());
    }

    [TestMethod]
    public void SelectDistinct_LexerShouldProduceDistinctToken()
    {
        var query = "select distinct Name from #some.a()";

        var lexer = new Lexer(query, true);


        var token1 = lexer.Next();
        Assert.AreEqual(TokenType.Select, token1.TokenType,
            $"Token 1 should be Select, got {token1.TokenType} with value '{token1.Value}'");


        var token2 = lexer.Next();
        Assert.AreEqual(TokenType.Distinct, token2.TokenType,
            $"Token 2 should be Distinct, got {token2.TokenType} with value '{token2.Value}'");
    }

    [TestMethod]
    public void SelectDistinct_BasicQuery_ShouldParse()
    {
        var query = "select distinct Name from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);


        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);
        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);
        Assert.IsTrue(singleSet.Query.Select.IsDistinct, "IsDistinct should be true");
    }

    [TestMethod]
    public void SelectDistinct_MultipleColumns_ShouldParse()
    {
        var query = "select distinct Name, Age, City from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SelectDistinct_WithWhere_ShouldParse()
    {
        var query = "select distinct Name from #some.a() where Age > 18";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SelectDistinct_WithOrderBy_ShouldParse()
    {
        var query = "select distinct Name from #some.a() order by Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SelectDistinct_WithGroupBy_ShouldParse()
    {
        var query = "select distinct Name, Count(*) from #some.a() group by Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SelectDistinct_UpperCase_ShouldParse()
    {
        var query = "SELECT DISTINCT Name FROM #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SelectDistinct_MixedCase_ShouldParse()
    {
        var query = "Select Distinct Name From #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SelectDistinct_WithExpression_ShouldParse()
    {
        var query = "select distinct Name, Age + 1 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SelectDistinct_ReorderedQuery_ShouldParse()
    {
        var query = "from #some.a() select distinct Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SelectDistinct_WithSkipTake_ShouldParse()
    {
        var query = "select distinct Name from #some.a() skip 5 take 10";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }


    [TestMethod]
    public void SchemaWithoutHash_BasicSelect_ShouldParse()
    {
        var query = "select 1 from some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_SelectWithColumns_ShouldParse()
    {
        var query = "select Name, Age from schema.entities()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_ReorderedQuery_ShouldParse()
    {
        var query = "from some.a() select 1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithWhereClause_ShouldParse()
    {
        var query = "select Name from some.a() where Name = 'test'";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithGroupBy_ShouldParse()
    {
        var query = "select Name, Count(Name) from some.a() group by Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithOrderBy_ShouldParse()
    {
        var query = "select Name from some.a() order by Name desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_InnerJoin_ShouldParse()
    {
        var query = "select a.Name from some.a() a inner join other.b() b on a.Id = b.Id";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_LeftOuterJoin_ShouldParse()
    {
        var query = "select a.Name from some.a() a left outer join other.b() b on a.Id = b.Id";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_CrossApply_ShouldParse()
    {
        var query = "select a.Name, b.Value from some.first() a cross apply some.second(a.Country) b";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_OuterApply_ShouldParse()
    {
        var query = "select a.Name, b.Value from some.first() a outer apply some.second(a.Country) b";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithCte_ShouldParse()
    {
        var query = "with cte as (select Name from some.a()) select * from cte";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithSkipTake_ShouldParse()
    {
        var query = "select Name from some.a() skip 10 take 5";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_UnionOperator_ShouldParse()
    {
        var query = "select Name from some.a() union (Name) select Name from other.b()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithHash_StillWorks_ShouldParse()
    {
        var query = "select 1 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaMixedSyntax_WithAndWithoutHash_ShouldParse()
    {
        var query = "select a.Name from #some.a() a inner join other.b() b on a.Id = b.Id";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_ComplexQuery_ShouldParse()
    {
        var query = @"
            select a.Name, b.Value, Count(a.Name)
            from schema.entities() a 
            inner join other.data() b on a.Id = b.Id
            where a.Active = true
            group by a.Name, b.Value
            having Count(a.Name) > 1
            order by a.Name desc
            skip 5 take 10";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithAlias_ShouldParse()
    {
        var query = "select s.Name from some.a() s";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithAsAlias_ShouldParse()
    {
        var query = "select s.Name from some.a() as s";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithMethodParameters_ShouldParse()
    {
        var query = "select 1 from some.method('param1', 123)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_CaseInsensitiveKeywords_ShouldParse()
    {
        var query = "SELECT Name FROM some.a() WHERE Name = 'test'";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }
}
