using System;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Lexing;

namespace DebugTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var query = "table Example { Id 'System.Int32', Name 'System.String' };\ncouple #a.b with table Example as SourceOfExamples;\nselect 1 from SourceOfExamples('a', 'b')";
            
            Console.WriteLine("Original query:");
            Console.WriteLine(query);
            Console.WriteLine();
            
            var lexer = new Lexer(query, true);
            var parser = new Musoq.Parser.Parser(lexer);
            var root = parser.ComposeAll();
            
            var cloneQueryVisitor = new CloneQueryVisitor();
            var cloneQueryTraverseVisitor = new CloneTraverseVisitor(cloneQueryVisitor);
            
            root.Accept(cloneQueryTraverseVisitor);

            var stringifiedQuery = cloneQueryVisitor.Root.ToString();
            
            Console.WriteLine("Stringified query:");
            Console.WriteLine($"'{stringifiedQuery}'");
            Console.WriteLine();
            
            Console.WriteLine($"Are they equal? {query == stringifiedQuery}");
            Console.WriteLine($"Original length: {query.Length}");
            Console.WriteLine($"Stringified length: {stringifiedQuery.Length}");
            
            if (query != stringifiedQuery)
            {
                Console.WriteLine("DIFFERENCE DETECTED!");
                Console.WriteLine("Expected (original):");
                foreach (char c in query)
                {
                    Console.Write($"'{c}' ({(int)c}) ");
                }
                Console.WriteLine();
                Console.WriteLine("Actual (stringified):");
                foreach (char c in stringifiedQuery)
                {
                    Console.Write($"'{c}' ({(int)c}) ");
                }
                Console.WriteLine();
            }
        }
    }
}