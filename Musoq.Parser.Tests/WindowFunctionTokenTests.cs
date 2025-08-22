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
        var lexer = new Lexer(" OVER ", true);  // Add spaces for proper token boundary recognition
        var token = lexer.Next();  // Use Next() instead of Current()
        
        Console.WriteLine($"OVER token test: {token.TokenType} = '{token.Value}'");
        Assert.AreEqual(TokenType.Over, token.TokenType);
        Assert.AreEqual("over", token.Value);  // Updated to lowercase as per token constants
    }

    [TestMethod]
    public void Lexer_ShouldRecognizePARTITIONToken()
    {
        var lexer = new Lexer(" PARTITION ", true);  // Add spaces for proper token boundary recognition
        var token = lexer.Next();  // Use Next() instead of Current()
        
        Assert.AreEqual(TokenType.Partition, token.TokenType);
        Assert.AreEqual("partition", token.Value);  // Updated to lowercase as per token constants
        Console.WriteLine($"PARTITION token recognized: {token.TokenType} = {token.Value}");
    }

    [TestMethod]
    public void Lexer_ShouldRecognizeBYToken()
    {
        var lexer = new Lexer(" BY ", true);  // Add spaces for proper token boundary recognition
        var token = lexer.Next();  // Use Next() instead of Current()
        
        Assert.AreEqual(TokenType.By, token.TokenType);
        Assert.AreEqual("by", token.Value);  // Updated to lowercase as per token constants
        Console.WriteLine($"BY token recognized: {token.TokenType} = {token.Value}");
    }

    [TestMethod]
    public void Lexer_ShouldRecognizeWindowFunctionSequence()
    {
        var lexer = new Lexer("ROW_NUMBER() OVER (ORDER BY Id)", true);
        
        // Get first token properly
        var rowNumberToken = lexer.Next();
        Console.WriteLine($"Token 1: {rowNumberToken.TokenType} = {rowNumberToken.Value}");
        Assert.AreEqual(TokenType.Function, rowNumberToken.TokenType);
        
        var leftParenToken = lexer.Next();
        Console.WriteLine($"Token 2: {leftParenToken.TokenType} = {leftParenToken.Value}");
        Assert.AreEqual(TokenType.LeftParenthesis, leftParenToken.TokenType);
        
        var rightParenToken = lexer.Next();
        Console.WriteLine($"Token 3: {rightParenToken.TokenType} = {rightParenToken.Value}");
        Assert.AreEqual(TokenType.RightParenthesis, rightParenToken.TokenType);
        
        var overToken = lexer.Next();
        Console.WriteLine($"Token 4: {overToken.TokenType} = {overToken.Value}");
        Assert.AreEqual(TokenType.Over, overToken.TokenType);
        
        var leftParenToken2 = lexer.Next();
        Console.WriteLine($"Token 5: {leftParenToken2.TokenType} = {leftParenToken2.Value}");
        Assert.AreEqual(TokenType.LeftParenthesis, leftParenToken2.TokenType);
        
        var orderToken = lexer.Next();
        Console.WriteLine($"Token 6: {orderToken.TokenType} = {orderToken.Value}");
        // ORDER in this context should be an Identifier since it's separate from BY
        Assert.AreEqual(TokenType.Identifier, orderToken.TokenType);
        
        var byToken = lexer.Next();
        Console.WriteLine($"Token 7: {byToken.TokenType} = {byToken.Value}");
        Assert.AreEqual(TokenType.By, byToken.TokenType);
    }
}