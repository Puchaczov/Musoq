using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Parser tests: ORDER BY DESC, SELECT DISTINCT, and additional comment tests.
/// </summary>
[TestClass]
public class Parser_OrderByAndDistinctTests
{
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


}
