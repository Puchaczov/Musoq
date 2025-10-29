using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Tests;

/// <summary>
/// Tests for improved error handling and keyword typo suggestions.
/// These tests verify that when users mistype SQL keywords, they receive helpful
/// suggestions for the correct keywords.
/// </summary>
[TestClass]
public class KeywordTypoTests
{
    private static IReadOnlyList<string> GetSuggestions(Exception ex)
    {
        if (ex is UnknownTokenException ute)
            return ute.Suggestions;
        if (ex is SyntaxException se)
            return se.Suggestions;
        return new List<string>();
    }

    #region Core SQL Statement Keywords

    [TestMethod]
    public void TypoInSelect_ShouldSuggestSelect()
    {
        // Common typos for "select" at the beginning of a statement
        var typos = new[] { "seelct", "selct", "slect", "selec", "selet", "selekt", "sellect" };

        foreach (var typo in typos)
        {
            var query = $"{typo} 1 from #system.dual()";
            Exception exception = null;
            try
            {
                var lexer = new Lexer(query, true);
                var parser = new Parser(lexer);
                parser.ComposeAll();
                Assert.Fail($"Expected exception for typo '{typo}'");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var suggestions = GetSuggestions(exception);
            Assert.IsTrue(suggestions.Contains("select"),
                $"Expected 'select' to be suggested for typo '{typo}', but got: {string.Join(", ", suggestions)}");
            Assert.IsTrue(exception.Message.Contains("Did you mean"),
                $"Expected helpful message for typo '{typo}'");
        }
    }

    [TestMethod]
    public void TypoInWith_ShouldSuggestWith()
    {
        // Common typos for "with" at the beginning of a CTE statement
        var typos = new[] { "wiht", "wih", "wit", "whit", "wth" };

        foreach (var typo in typos)
        {
            var query = $"{typo} cte as (select 1 from #system.dual()) select * from cte";
            Exception exception = null;
            try
            {
                var lexer = new Lexer(query, true);
                var parser = new Parser(lexer);
                parser.ComposeAll();
                Assert.Fail($"Expected exception for typo '{typo}'");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var suggestions = GetSuggestions(exception);
            Assert.IsTrue(suggestions.Contains("with"),
                $"Expected 'with' to be suggested for typo '{typo}', but got: {string.Join(", ", suggestions)}");
        }
    }

    [TestMethod]
    public void TypoInTable_ShouldSuggestTable()
    {
        // Common typos for "table" keyword
        var typos = new[] { "tabel", "tabl", "tablee", "tbale", "talbe" };

        foreach (var typo in typos)
        {
            var query = $"{typo} MyTable{{col1, col2}}";
            Exception exception = null;
            try
            {
                var lexer = new Lexer(query, true);
                var parser = new Parser(lexer);
                parser.ComposeAll();
                Assert.Fail($"Expected exception for typo '{typo}'");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var suggestions = GetSuggestions(exception);
            Assert.IsTrue(suggestions.Contains("table"),
                $"Expected 'table' to be suggested for typo '{typo}', but got: {string.Join(", ", suggestions)}");
        }
    }

    [TestMethod]
    public void TypoInCouple_ShouldSuggestCouple()
    {
        // Common typos for "couple" keyword
        var typos = new[] { "copule", "cuople", "cople" };

        foreach (var typo in typos)
        {
            var query = $"{typo} #some.table with table Test as SourceOfTestValues";
            Exception exception = null;
            try
            {
                var lexer = new Lexer(query, true);
                var parser = new Parser(lexer);
                parser.ComposeAll();
                Assert.Fail($"Expected exception for typo '{typo}'");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var suggestions = GetSuggestions(exception);
            Assert.IsTrue(suggestions.Contains("couple"),
                $"Expected 'couple' to be suggested for typo '{typo}', but got: {string.Join(", ", suggestions)}");
        }
    }

    #endregion

    #region Sort Order Keywords

    [TestMethod]
    public void TypoInDesc_ShouldSuggestDesc()
    {
        // Common typos for "desc" keyword at statement level
        var typos = new[] { "decs", "dsec" };

        foreach (var typo in typos)
        {
            var query = $"{typo} #schema.method()";
            Exception exception = null;
            try
            {
                var lexer = new Lexer(query, true);
                var parser = new Parser(lexer);
                parser.ComposeAll();
                Assert.Fail($"Expected exception for typo '{typo}'");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var suggestions = GetSuggestions(exception);
            Assert.IsTrue(suggestions.Contains("desc") || suggestions.Contains("asc"),
                $"Expected 'desc' or 'asc' to be suggested for typo '{typo}', but got: {string.Join(", ", suggestions)}");
        }
    }

    #endregion

    #region Logical and Comparison Keywords (Recognized by Lexer)

    [TestMethod]
    public void TypoInAnd_ShouldSuggestAnd()
    {
        // Test at lexer level with unrecognized token
        var query = "select 1 from #system.dual() where 1 = 1 annd 2 = 2";
        Exception exception = null;
        try
        {
            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);
            parser.ComposeAll();
            Assert.Fail("Expected exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var suggestions = GetSuggestions(exception);
        Assert.IsTrue(suggestions.Contains("and"),
            $"Expected 'and' to be suggested, but got: {string.Join(", ", suggestions)}");
    }

    [TestMethod]
    public void TypoInOr_ShouldSuggestOr()
    {
        // Test at lexer level with unrecognized token
        var query = "select 1 from #system.dual() where 1 = 1 orr 2 = 2";
        Exception exception = null;
        try
        {
            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);
            parser.ComposeAll();
            Assert.Fail("Expected exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var suggestions = GetSuggestions(exception);
        Assert.IsTrue(suggestions.Contains("or"),
            $"Expected 'or' to be suggested, but got: {string.Join(", ", suggestions)}");
    }

    #endregion

    #region Edge Cases and General Tests

    [TestMethod]
    public void CompletelyWrongKeyword_AtStatementStart_ShouldProvideHelpfulError()
    {
        var query = "zzzzz 1 from #system.dual()";
        Exception exception = null;
        try
        {
            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);
            parser.ComposeAll();
            Assert.Fail("Expected exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Should still have an error message (may or may not have suggestions for completely wrong words)
        Assert.IsNotNull(exception.Message);
        Assert.IsTrue(exception.Message.Contains("not expected") || exception.Message.Contains("not recognized"));
    }

    [TestMethod]
    public void ValidQuery_ShouldNotThrowException()
    {
        var query = "select 1 from #system.dual()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        // Should not throw
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ValidQueryWithJoin_ShouldNotThrowException()
    {
        var query = "select a.col from #system.dual() a inner join #system.dual() b on a.id = b.id";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        // Should not throw
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ValidQueryWithCaseWhen_ShouldNotThrowException()
    {
        var query = "select case when 1 = 1 then 'yes' else 'no' end from #system.dual()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        // Should not throw
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ErrorMessage_ShouldContainHelpfulText()
    {
        var query = "seelct 1 from #system.dual()";
        Exception exception = null;
        try
        {
            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);
            parser.ComposeAll();
            Assert.Fail("Expected exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        Assert.IsTrue(exception.Message.Contains("Did you mean"),
            "Error message should contain 'Did you mean' phrase for user guidance");
    }

    [TestMethod]
    public void UnknownTokenError_ShouldProvideHelpfulSuggestions()
    {
        // Test a character that's not recognized at all
        var query = "select 1 from #system.dual() § unknown";
        Exception exception = null;
        try
        {
            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);
            parser.ComposeAll();
            Assert.Fail("Expected exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Should have a meaningful error (UnknownTokenException or SyntaxException wrapping it)
        Assert.IsNotNull(exception.Message);
        Assert.IsTrue(exception is UnknownTokenException || exception is SyntaxException);
    }

    [TestMethod]
    public void MultipleTyposInQuery_ShouldSuggestForFirst()
    {
        // When there are multiple typos, we should get suggestions for the first one encountered
        var query = "seelct 1 froom #system.dual()";
        Exception exception = null;
        try
        {
            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);
            parser.ComposeAll();
            Assert.Fail("Expected exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var suggestions = GetSuggestions(exception);
        // Should suggest 'select' for 'seelct' since that's the first typo
        Assert.IsTrue(suggestions.Contains("select"),
            $"Expected 'select' to be suggested for first typo, but got: {string.Join(", ", suggestions)}");
    }

    #endregion
}
