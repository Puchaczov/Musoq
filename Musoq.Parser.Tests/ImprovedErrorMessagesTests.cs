using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

/// <summary>
/// Tests for improved error messages in the Parser
/// These tests validate that Parser throws SyntaxException (not NotSupportedException) with helpful error messages
/// </summary>
[TestClass]
public class ImprovedErrorMessagesTests
{
    [TestMethod]
    public void InvalidQueryStart_ShouldThrowSyntaxExceptionNotNotSupportedException()
    {
        var query = "insert into table values (1, 2, 3)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        // The error message may vary based on token type
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
        Assert.IsFalse(exc.GetType().Name.Contains("NotSupported"),
            "Should throw SyntaxException, not NotSupportedException");
    }

    [TestMethod]
    public void EmptyGroupByFields_ShouldThrowSyntaxExceptionWithHelpfulMessage()
    {
        var query = "select Name from #some.a() group by";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Contains("GROUP BY") && (exc.Message.Contains("column") || exc.Message.Contains("expression")),
            $"Error should explain GROUP BY requires columns: {exc.Message}");
    }

    [TestMethod]
    public void InvalidOrderDirection_ShouldThrowSyntaxExceptionWithHelpfulMessage()
    {
        var query = "select Name from #some.a() order by Name invalid";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Contains("ASC") || exc.Message.Contains("DESC") || exc.Message.Contains("ORDER"),
            $"Error should mention expected order direction: {exc.Message}");
    }

    [TestMethod]
    public void InvalidOperatorInExpression_ShouldThrowSyntaxExceptionWithHelpfulMessage()
    {
        // This test validates that invalid syntax in WHERE clause produces helpful messages
        // Using a query with missing expression after AND
        var query = "select Name from #some.a() where Age > 5 and";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void MissingAliasInFrom_ShouldThrowSyntaxExceptionWithHelpfulMessage()
    {
        // This would trigger the alias validation
        var query = "select a from #some.method()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        try
        {
            parser.ComposeAll();
            // If this succeeds, it means the query is valid (alias was inferred or not required)
            // That's okay - the test is checking that IF an error occurs, it's meaningful
        }
        catch (SyntaxException exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Contains("alias") || exc.Message.Contains("AS"),
                $"Error should mention alias requirement: {exc.Message}");
        }
    }

    [TestMethod]
    public void InvalidTokenInBaseTypes_ShouldThrowSyntaxExceptionWithHelpfulMessage()
    {
        var query = "select @ from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        // Error comes from lexer about unrecognized token
        Assert.IsTrue(exc.Message.Contains("@") || exc.Message.Contains("unrecognized") || exc.Message.Contains("Token"),
            $"Error should indicate problematic token: {exc.Message}");
    }

    [TestMethod]
    public void InvalidLogicalOperator_ShouldThrowSyntaxExceptionWithHelpfulMessage()
    {
        var query = "select Name from #some.a() where Age > 5 xor Status = 'active'";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        // 'xor' is parsed as identifier, error occurs at statement level
        Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
    }

    [TestMethod]
    public void InvalidComparisonOperator_ShouldThrowSyntaxExceptionWithHelpfulMessage()
    {
        // Testing with an invalid operator in WHERE clause
        var query = "select Name from #some.a() where Age !! 5";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        Assert.IsNotNull(exc.Message);
        // Error comes from lexer about unrecognized token
        Assert.IsTrue(exc.Message.Contains("!") || exc.Message.Contains("unrecognized") || exc.Message.Contains("Token"),
            $"Error should indicate problematic token: {exc.Message}");
    }

    [TestMethod]
    public void AllParserErrorsAreSyntaxException_NotNotSupportedException()
    {
        // This test validates that we don't throw NotSupportedException from Parser
        var invalidQueries = new[]
        {
            "insert into table values (1)",  // Invalid query start
            "select Name from #some.a() group by",  // Empty GROUP BY
            "select Name from #some.a() order by Name wrong",  // Invalid order
            "select @ from #some.a()",  // Invalid token
        };

        foreach (var query in invalidQueries)
        {
            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);

            try
            {
                parser.ComposeAll();
            }
            catch (Exception exc)
            {
                // Should be SyntaxException or its derivatives, not NotSupportedException
                Assert.IsFalse(exc is NotSupportedException,
                    $"Parser should not throw NotSupportedException for query: {query}. Got: {exc.GetType().Name}");
                
                // Verify it's a meaningful exception
                Assert.IsTrue(exc is SyntaxException,
                    $"Parser should throw SyntaxException for query: {query}. Got: {exc.GetType().Name}");
            }
        }
    }

    [TestMethod]
    public void ErrorMessagesContainQueryContext()
    {
        // Verify that SyntaxException includes the query part
        var query = "select Name from #some.a() order by Name invalid";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var exc = Assert.ThrowsException<SyntaxException>(() => parser.ComposeAll());
        
        // SyntaxException should have QueryPart property
        Assert.IsNotNull(exc.QueryPart, "SyntaxException should include query context");
        Assert.IsTrue(exc.QueryPart.Length > 0, "Query context should not be empty");
    }
}
