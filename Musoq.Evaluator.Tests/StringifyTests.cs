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
    [DataRow("select c.ContainerName, c2.ImageName, c.ContainerId from #stdin.text('Ollama', 'llama3.1') c inner join #stdin.text('Ollama', 'llama3.1') c2 on c.ContainerId = c2.ContainerId")]
    [DataRow("table Example {};")]
    [DataRow("table Example { Id 'System.Int32' };")]
    [DataRow("table Example { Id 'System.Int32', Name 'System.String' };")]
    [DataRow("table Example { Id 'System.Int32', Name 'System.String' };\r\ncouple #a.b with table Example as SourceOfExamples;\r\nselect 1 from SourceOfExamples('a', 'b')")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 = 4")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s group by s.Column2")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s order by s.Column1 desc")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s skip 10")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s take 5")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 > 10 group by s.Column2")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column2 = 'Test' order by s.Column1")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 < 20 skip 5")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column2 <> 'Example' take 3")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s group by s.Column1, s.Column2 order by s.Column1")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s group by s.Column2 skip 15")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s group by s.Column1 take 8")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s order by s.Column2, s.Column1 desc skip 7")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s order by s.Column1 take 12")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s skip 20 take 10")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 >= 5 group by s.Column2 order by s.Column1")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column2 like '%test%' group by s.Column1 skip 3")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 <= 15 group by s.Column2 take 6")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column2 is not null order by s.Column1 skip 8")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 = 10 order by s.Column2 take 4")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 in (1, 2, 3) skip 2 take 5")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s group by s.Column1, s.Column2 order by s.Column1 skip 10")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s group by s.Column2 order by s.Column1 desc take 7")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s group by s.Column1 skip 5 take 15")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s order by s.Column2, s.Column1 skip 12 take 8")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 <> 0 group by s.Column2 order by s.Column1 skip 6")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column2 = 'Value' group by s.Column1 order by s.Column2 take 9")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 > 100 group by s.Column2 skip 3 take 7")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column2 like 'A%' order by s.Column1 skip 4 take 8")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s group by s.Column1, s.Column2 order by s.Column1 skip 5 take 10")]
    [DataRow("select s.Column1, s.Column2 from #some.thing() s where s.Column1 > 0 group by s.Column2 order by s.Column1 skip 2 take 5")]
    [DataRow("select t.Name, Count(t.Name) from #some.thing(true) t group by t.Name having Count(t.Name) > 1")]
    [DataRow("select t.* from #some.thing(true) t")]
    [DataRow("select somethingVeryLong.* from #some.thing(true) somethingVeryLong")]
    [DataRow("select somethingVeryLong2.* from #some.thing(true) somethingVeryLong2")]
    [DataRow("select b.* from #some.thing() a cross apply #some.thing(a.SomeProperty) b")]
    [DataRow("select b.* from #some.thing() a cross apply a.Property b")]
    [DataRow("select b.* from #some.thing() a outer apply #some.thing(a.SomeProperty) b")]
    [DataRow("select b.* from #some.thing() a outer apply a.TestMethod(b.Something) b")]
    
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