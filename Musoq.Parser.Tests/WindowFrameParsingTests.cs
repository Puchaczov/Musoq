using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

/// <summary>
/// Tests for advanced window frame syntax (ROWS BETWEEN) parsing.
/// </summary>
[TestClass]
public class WindowFrameParsingTests
{
    [TestMethod]
    public void WhenParsingBasicRowsFrame_ShouldSucceed()
    {
        var query = "select Dummy, RANK() OVER (ORDER BY Dummy rows unbounded preceding) from #system.dual()";
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        
        try
        {
            var result = parser.ComposeAll();
            Assert.IsNotNull(result);
            // If we get here without exception, the parsing worked
        }
        catch (System.Exception ex)
        {
            Assert.Fail($"Parsing failed: {ex.Message}");
        }
    }

    [TestMethod]
    public void WhenParsingRowsBetweenFrame_ShouldSucceed()
    {
        var query = "select Dummy, SUM(Dummy) OVER (PARTITION BY Dummy ORDER BY Dummy rows between unbounded preceding and current row) from #system.dual()";
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        
        try
        {
            var result = parser.ComposeAll();
            Assert.IsNotNull(result);
            // If we get here without exception, the parsing worked
        }
        catch (System.Exception ex)
        {
            Assert.Fail($"Parsing failed: {ex.Message}");
        }
    }

    [TestMethod]
    public void WhenParsingRowsWithNumericFrame_ShouldSucceed()
    {
        var query = "select Dummy, AVG(Dummy) OVER (ORDER BY Dummy rows between 2 preceding and 1 following) from #system.dual()";
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        
        try
        {
            var result = parser.ComposeAll();
            Assert.IsNotNull(result);
            // If we get here without exception, the parsing worked
        }
        catch (System.Exception ex)
        {
            Assert.Fail($"Parsing failed: {ex.Message}");
        }
    }

    [TestMethod]
    public void WhenParsingWindowFrameWithComplexExpression_ShouldSucceed()
    {
        var query = "select Country, Population, SUM(Population) OVER (PARTITION BY Country ORDER BY Population rows between unbounded preceding and current row) as RunningSum from #system.dual()";
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        
        try
        {
            var result = parser.ComposeAll();
            Assert.IsNotNull(result);
            // If we get here without exception, the parsing worked
        }
        catch (System.Exception ex)
        {
            Assert.Fail($"Parsing failed: {ex.Message}");
        }
    }
}