using System;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Lexing;

namespace DebugTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var query = "table Example { Id 'System.Int32', Name 'System.String' };\ncouple #a.b with table Example as SourceOfExamples;\nselect 1 from SourceOfExamples('a', 'b')";
            
            Console.WriteLine("=== DEBUGGING STRINGIFY ISSUE ===");
            Console.WriteLine($"Original query: '{query}'");
            Console.WriteLine($"Original length: {query.Length}");
            Console.WriteLine();
            
            try
            {
                var lexer = new Lexer(query, true);
                var parser = new Musoq.Parser.Parser(lexer);
                var root = parser.ComposeAll();
                
                Console.WriteLine($"Root type: {root.GetType().Name}");
                Console.WriteLine($"Root toString: '{root.ToString()}'");
                Console.WriteLine();
                
                var cloneQueryVisitor = new CloneQueryVisitor();
                var cloneQueryTraverseVisitor = new CloneTraverseVisitor(cloneQueryVisitor);
                
                root.Accept(cloneQueryTraverseVisitor);

                var stringifiedQuery = cloneQueryVisitor.Root.ToString();
                
                Console.WriteLine($"Cloned root type: {cloneQueryVisitor.Root.GetType().Name}");
                Console.WriteLine($"Stringified query: '{stringifiedQuery}'");
                Console.WriteLine($"Stringified length: {stringifiedQuery.Length}");
                Console.WriteLine();
                
                Console.WriteLine($"Are they equal? {query == stringifiedQuery}");
                
                if (query != stringifiedQuery)
                {
                    Console.WriteLine("DIFFERENCE DETECTED!");
                    Console.WriteLine($"Expected: '{query}'");
                    Console.WriteLine($"Actual: '{stringifiedQuery}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}