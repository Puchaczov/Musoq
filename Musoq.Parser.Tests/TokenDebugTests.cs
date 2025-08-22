using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;
using System;

namespace Musoq.Parser.Tests;

/// <summary>
/// Simple debug test to understand token recognition.
/// </summary>
[TestClass]
public class TokenDebugTests
{
    [TestMethod]
    public void Lexer_DebugExistingToken_WITH()
    {
        var lexer = new Lexer(" WITH ", true);  // Add spaces
        var token = lexer.Current();
        
        Console.WriteLine($"WITH token test: {token.TokenType} = '{token.Value}'");
        Assert.AreEqual(TokenType.With, token.TokenType);
    }
    
    [TestMethod]
    public void Lexer_DebugNewToken_OVER()
    {
        var lexer = new Lexer(" OVER ", true);  // Add spaces
        var token = lexer.Current();
        
        Console.WriteLine($"OVER token test: {token.TokenType} = '{token.Value}'");
        Assert.AreEqual(TokenType.Over, token.TokenType);
    }
    
    [TestMethod]
    public void Lexer_DebugSimpleQuery()
    {
        var lexer = new Lexer("SELECT Name OVER", true);
        
        var token1 = lexer.Current();
        Console.WriteLine($"Token 1: {token1.TokenType} = '{token1.Value}'");
        
        lexer.Next();
        var token2 = lexer.Current();
        Console.WriteLine($"Token 2: {token2.TokenType} = '{token2.Value}'");
        
        lexer.Next();
        var token3 = lexer.Current();
        Console.WriteLine($"Token 3: {token3.TokenType} = '{token3.Value}'");
    }
}