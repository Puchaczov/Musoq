using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public class PivotParserTests
{
    [TestMethod]
    public void PivotToken_ShouldBeRecognized()
    {
        var query = "pivot";  // Try lowercase
        var lexer = new Lexer(query, true);
        
        var token = lexer.Next(); // Get the first token
        // Let's see what we actually get
        Console.WriteLine($"Token type: {token.TokenType}, Token value: '{token.Value}'");
        Assert.AreEqual(TokenType.Pivot, token.TokenType);
    }
    
    [TestMethod]
    public void ForToken_ShouldBeRecognized() 
    {
        var query = "for";
        var lexer = new Lexer(query, true);
        
        var token = lexer.Next();
        Console.WriteLine($"Token type: {token.TokenType}, Token value: '{token.Value}'");
        Assert.AreEqual(TokenType.For, token.TokenType);
    }
    
    [TestMethod]  
    public void SelectToken_ShouldBeRecognized_AsBaseline()
    {
        var query = "select";
        var lexer = new Lexer(query, true);
        
        var token = lexer.Next();
        Console.WriteLine($"Token type: {token.TokenType}, Token value: '{token.Value}'");
        Assert.AreEqual(TokenType.Select, token.TokenType);
    }

    [TestMethod]
    public void BasicPivotSyntax_ShouldParse()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN ('Books', 'Electronics', 'Fashion')
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void PivotWithMultipleAggregations_ShouldParse()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity), Avg(Revenue), Count(Product)
                FOR Category IN ('Books', 'Electronics', 'Fashion')
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void PivotWithDynamicColumns_ShouldParse()
    {
        var query = @"
            WITH Categories AS (
                SELECT DISTINCT Category 
                FROM #A.Entities()
            )
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN (SELECT Category FROM Categories)
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void PivotWithNumericColumns_ShouldParse()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Count(Product)
                FOR Year IN (2020, 2021, 2022, 2023)
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void PivotWithComplexAggregation_ShouldParse()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity * Revenue)
                FOR Category IN ('Books', 'Electronics')
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void PivotInSubquery_ShouldParse()
    {
        var query = @"
            SELECT *
            FROM (
                SELECT Category, Product, Quantity
                FROM #A.Entities()
                WHERE Quantity > 0
            ) AS data
            PIVOT (
                Sum(Quantity)
                FOR Category IN ('Books', 'Electronics')
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void PivotWithJoin_ShouldParse()
    {
        var query = @"
            SELECT *
            FROM #A.Entities() e
            INNER JOIN #B.Categories() c ON e.Category = c.Name
            PIVOT (
                Sum(e.Quantity)
                FOR e.Category IN ('Books', 'Electronics')
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [ExpectedException(typeof(SyntaxException))]
    public void PivotWithoutAlias_ShouldThrowException()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN ('Books', 'Electronics')
            )";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    [ExpectedException(typeof(SyntaxException))]
    public void PivotWithoutForClause_ShouldThrowException()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                IN ('Books', 'Electronics')
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    [ExpectedException(typeof(SyntaxException))]
    public void PivotWithoutInClause_ShouldThrowException()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category ('Books', 'Electronics')
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    [ExpectedException(typeof(SyntaxException))]
    public void PivotWithoutAggregation_ShouldThrowException()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                FOR Category IN ('Books', 'Electronics')
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    [ExpectedException(typeof(SyntaxException))]
    public void PivotWithEmptyInList_ShouldThrowException()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN ()
            ) AS p";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }
}