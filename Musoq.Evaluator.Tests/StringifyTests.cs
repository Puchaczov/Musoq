using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Lexing;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class StringifyTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("select 1 as X from #A.entities()")]
    [DataRow("select 1 as X, 2 as Y from #A.entities()")]
    [DataRow("select 1 as X, 2 as Y, 3 as Z from #A.entities()")]
    [DataRow("select some.Thing from #A.entities()")]
    [DataRow("select 1 from #A.entities() a inner join #B.entities() b on a.Id = b.Id")]
    [DataRow("select 1 from #A.entities() a left outer join #B.entities() b on a.Id = b.Id")]
    [DataRow("select 1 from #A.entities() a right outer join #B.entities() b on a.Id = b.Id")]
    [DataRow("select 1 from #A.entities() a inner join #B.entities() b on a.Id = b.Id inner join #C.entities() c on a.Id = c.Id")]
    [DataRow("select 1 from #A.entities() a inner join #B.entities() b on a.Id = b.Id left outer join #C.entities() c on a.Id = c.Id")]
    [DataRow("select 1 from #A.entities() a inner join #B.entities() b on a.Id = b.Id right outer join #C.entities() c on a.Id = c.Id")]
    
    public void WhenToStringCalled_ShouldReturnSameQuery(string query)
    {
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