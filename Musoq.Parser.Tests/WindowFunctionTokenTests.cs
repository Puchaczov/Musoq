using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;
using System;

namespace Musoq.Parser.Tests;

/// <summary>
/// Tests to verify that window function tokens are properly recognized by the lexer.
/// </summary>
[TestClass]
public class WindowFunctionTokenTests
{
    [TestMethod]
    public void Lexer_ShouldRecognizeOVERToken()
    {
        var lexer = new Lexer("OVER", true);
        var token = lexer.Current();
        
        Console.WriteLine($"OVER token test: {token.TokenType} = '{token.Value}'");
        Assert.AreEqual(TokenType.Over, token.TokenType);
        Assert.AreEqual("OVER", token.Value);
    }

    [TestMethod]
    public void Lexer_ShouldRecognizePARTITIONToken()
    {
        var lexer = new Lexer("PARTITION", true);
        var token = lexer.Current();
        
        Assert.AreEqual(TokenType.Partition, token.TokenType);
        Assert.AreEqual("PARTITION", token.Value);
        Console.WriteLine($"PARTITION token recognized: {token.TokenType} = {token.Value}");
    }

    [TestMethod]
    public void Lexer_ShouldRecognizeBYToken()
    {
        var lexer = new Lexer("BY", true);
        var token = lexer.Current();
        
        Assert.AreEqual(TokenType.By, token.TokenType);
        Assert.AreEqual("BY", token.Value);
        Console.WriteLine($"BY token recognized: {token.TokenType} = {token.Value}");
    }

    [TestMethod]
    public void Lexer_ShouldRecognizeWindowFunctionSequence()
    {
        var lexer = new Lexer("ROW_NUMBER() OVER (ORDER BY Id)", true);
        
        // ROW_NUMBER() should be a function
        var rowNumberToken = lexer.Current();
        Console.WriteLine($"Token 1: {rowNumberToken.TokenType} = {rowNumberToken.Value}");
        
        lexer.Next();
        var leftParenToken = lexer.Current();
        Console.WriteLine($"Token 2: {leftParenToken.TokenType} = {leftParenToken.Value}");
        
        lexer.Next();
        var rightParenToken = lexer.Current();
        Console.WriteLine($"Token 3: {rightParenToken.TokenType} = {rightParenToken.Value}");
        
        lexer.Next();
        var overToken = lexer.Current();
        Console.WriteLine($"Token 4: {overToken.TokenType} = {overToken.Value}");
        Assert.AreEqual(TokenType.Over, overToken.TokenType);
        
        lexer.Next();
        var leftParenToken2 = lexer.Current();
        Console.WriteLine($"Token 5: {leftParenToken2.TokenType} = {leftParenToken2.Value}");
        
        lexer.Next();
        var orderToken = lexer.Current();
        Console.WriteLine($"Token 6: {orderToken.TokenType} = {orderToken.Value}");
        
        lexer.Next();
        var byToken = lexer.Current();
        Console.WriteLine($"Token 7: {byToken.TokenType} = {byToken.Value}");
        Assert.AreEqual(TokenType.By, byToken.TokenType);
    }
}