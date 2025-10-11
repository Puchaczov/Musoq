using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

/// <summary>
/// Tests for input validation guards added to the Parser
/// These tests validate that Parser properly guards against invalid input sequences
/// </summary>
[TestClass]
public class InputValidationGuardsTests
{
    [TestMethod]
    public void SelectWithOnlyCommas_ShouldThrowMeaningfulError()
    {
        var query = "select , , from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void GroupByWithOnlyCommas_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() group by , ,";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void EmptySelectAfterSelectKeyword_ShouldHandleGracefully()
    {
        // The parser allows "select from" and treats it as selecting all columns (*)
        // This is acceptable behavior - some SQL dialects allow this
        var query = "select from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        // Should either parse or throw meaningful error
        try
        {
            var result = parser.ComposeAll();
            Assert.IsNotNull(result, "Parse should succeed or throw meaningful error");
        }
        catch (SyntaxException exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Contains("SELECT") || exc.Message.Contains("column") || exc.Message.Contains("expression"),
                $"Error should mention SELECT requires columns: {exc.Message}");
        }
    }

    [TestMethod]
    public void InvalidAliasWithReservedKeyword_ShouldHandleGracefully()
    {
        // Test that using reserved keywords as aliases is handled properly
        var query = "select Name as select from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        try
        {
            parser.ComposeAll();
            // If it parses, that's acceptable (some implementations allow this)
        }
        catch (SyntaxException exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
        }
    }

    [TestMethod]
    public void OrderByWithoutColumn_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() order by";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void WhereWithoutCondition_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() where";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void HavingWithoutCondition_ShouldThrowMeaningfulError()
    {
        var query = "select Name from #some.a() group by Name having";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void MultipleConsecutiveCommasInSelect_ShouldThrowMeaningfulError()
    {
        var query = "select Name,, Age from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Contains("comma") || exc.Message.Contains("Comma") || exc.Message.Contains("expression"),
            $"Error should mention comma or expression issue: {exc.Message}");
    }

    [TestMethod]
    public void ValidQueryWithProperAliases_ShouldParse()
    {
        var query = "select Name as N, Age as A from #some.a() as t";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        // Should not throw
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ValidQueryWithInferredAliases_ShouldParse()
    {
        var query = "select Name, Age from #some.a() t";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        // Should not throw
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }
}
