using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

/// <summary>
/// Tests for invalid SQL query syntax to ensure meaningful error messages are thrown
/// </summary>
[TestClass]
public class InvalidQuerySyntaxTests
{
    [TestMethod]
    public void MissingFromClause_ShouldThrowMeaningfulError()
    {
        var query = "select 1, 2, 3";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void MissingSelectKeyword_ShouldThrowMeaningfulError()
    {
        var query = "from #some.table()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidJoinWithoutOnClause_ShouldThrowMeaningfulError()
    {
        var query = "select a.Name from #some.a() a inner join #some.b() b";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void UnclosedParenthesesInSelectList_ShouldThrowMeaningfulError()
    {
        var query = "select (1 + 2 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void UnclosedParenthesesInFunctionCall_ShouldThrowMeaningfulError()
    {
        var query = "select ABS(5 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidWhereClauseMissingCondition_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() where";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidGroupByMissingColumn_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() group by";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidOrderByMissingColumn_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() order by";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidCaseWhenMissingThen_ShouldThrowMeaningfulError()
    {
        var query = "select case when 1 = 1 else 0 end from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void MultipleCommasInSelectList_ShouldThrowMeaningfulError()
    {
        var query = "select 1,, 2 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void TrailingCommaInGroupBy_ShouldThrowMeaningfulError()
    {
        var query = "select Name, Count(*) from #some.a() group by Name,";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidHavingWithoutGroupBy_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() having Count(*) > 5";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidUnionMissingSideOfQuery_ShouldThrowMeaningfulError()
    {
        var query = "select 1 from #some.a() union";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidExceptMissingSideOfQuery_ShouldThrowMeaningfulError()
    {
        var query = "select 1 from #some.a() except (Column)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidTableReferenceSyntax_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidCTESyntaxMissingAsKeyword_ShouldThrowMeaningfulError()
    {
        var query = "with cte (select 1) select * from cte";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidSelectListSyntax_ShouldThrowMeaningfulError()
    {
        var query = "select * * from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidSubqueryMissingParentheses_ShouldThrowMeaningfulError()
    {
        var query = "select Name from select 1 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void DoubleNegationWithoutParentheses_ShouldThrowMeaningfulError()
    {
        var query = "select --5 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidInOperatorWithoutList_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() where Id in";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidBetweenOperatorMissingAnd_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() where Id between 1 5";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }
}
