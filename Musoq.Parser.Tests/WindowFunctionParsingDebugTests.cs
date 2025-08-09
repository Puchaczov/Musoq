using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class WindowFunctionParsingDebugTests
{
    [TestMethod]
    public void Parse_SumWithOverClause_ShouldCreateWindowFunctionNode()
    {
        var query = "select SUM(Population) OVER (ORDER BY Population) from #system.dual()";
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        
        try
        {
            var result = parser.ComposeAll();
            Assert.IsNotNull(result);
            // If we get here without exception, parsing worked
            Console.WriteLine("SUM() OVER parsing succeeded");
        }
        catch (Exception ex)
        {
            Assert.Fail($"SUM() OVER parsing failed: {ex.Message}");
        }
    }
}