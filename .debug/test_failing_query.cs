using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Lexing;

namespace Debug
{
    [TestClass]
    public class TestFailingQuery
    {
        [TestMethod]
        public void TestFailingQueryDebug()
        {
            var query = "table Example { Id 'System.Int32', Name 'System.String' };\r\ncouple #a.b with table Example as SourceOfExamples;\r\nselect 1 from SourceOfExamples('a', 'b')";
            
            var lexer = new Lexer(query, true);
            var parser = new Musoq.Parser.Parser(lexer);
            var root = parser.ComposeAll();
            
            var cloneQueryVisitor = new CloneQueryVisitor();
            var cloneQueryTraverseVisitor = new CloneTraverseVisitor(cloneQueryVisitor);
            
            root.Accept(cloneQueryTraverseVisitor);

            var stringifiedQuery = cloneQueryVisitor.Root.ToString();
            
            Console.WriteLine("Original:");
            Console.WriteLine(query);
            Console.WriteLine("\nStringified:");
            Console.WriteLine(stringifiedQuery);
            Console.WriteLine("\nOriginal bytes:");
            Console.WriteLine(string.Join(" ", query.Select(c => ((int)c).ToString())));
            Console.WriteLine("\nStringified bytes:");
            Console.WriteLine(string.Join(" ", stringifiedQuery.Select(c => ((int)c).ToString())));
        }
    }
}