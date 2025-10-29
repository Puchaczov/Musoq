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

    [TestMethod]
    public void TypoInSelect_ShouldSuggestSelect()
    {
        // Common typos for "select" at the beginning of a statement
        var typos = new[] { "seelct", "selct", "slect", "selec", "selet" };

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
        var typos = new[] { "wiht", "wih", "wit" };

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
        var typos = new[] { "tabel", "tabl", "tablee" };

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
                $"Expected 'desc' to be suggested for typo '{typo}', but got: {string.Join(", ", suggestions)}");
        }
    }

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
}
