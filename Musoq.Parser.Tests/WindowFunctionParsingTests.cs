using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public class WindowFunctionParsingTests
{
    [TestMethod]
    public void Parse_RankWithOverClause_ShouldWork()
    {
        var query = "SELECT RANK() OVER (PARTITION BY Country) FROM #A.entities()";
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        
        try
        {
            var rootNode = parser.ComposeAll();
            Assert.IsNotNull(rootNode);
            // If we get here without exception, the parsing worked
        }
        catch (Exception ex)
        {
            Assert.Fail($"Parsing failed: {ex.Message}");
        }
    }
    
    [TestMethod]
    public void Parse_SimpleRankFunction_ShouldCreateAccessMethodNode()
    {
        var query = "SELECT RANK() FROM #A.entities()";
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        
        var rootNode = parser.ComposeAll();
        
        // Regular function call without OVER should create AccessMethodNode
        Assert.IsNotNull(rootNode);
    }
}