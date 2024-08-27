using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Lexing;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class StringifyTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenToStringCalled_ShouldReturnSameQuery()
    {
        const string query = "select 1 as X from #A.entities()";
        
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var root = parser.ComposeAll();
        
        var cloneQueryVisitor = new CloneQueryVisitor();
        var cloneQueryTraverseVisitor = new CloneTraverseVisitor(cloneQueryVisitor);
        
        root.Accept(cloneQueryTraverseVisitor);

        var stringifiedQuery = cloneQueryVisitor.Root.ToString();
        
        Assert.AreEqual(query, stringifiedQuery);
    }
    
    [TestMethod]
    public void WhenAliasNotUsed_ShouldNotReturnWithAlias()
    {
        const string query = "select some.Thing from #A.entities()";
        
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var root = parser.ComposeAll();
        
        var cloneQueryVisitor = new CloneQueryVisitor();
        var cloneQueryTraverseVisitor = new CloneTraverseVisitor(cloneQueryVisitor);
        
        root.Accept(cloneQueryTraverseVisitor);

        var stringifiedQuery = cloneQueryVisitor.Root.ToString();
        
        Assert.AreEqual(query, stringifiedQuery);
    }
}