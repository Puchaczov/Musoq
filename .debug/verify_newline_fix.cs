using System;
using Musoq.Parser.Lexing;
using Musoq.Evaluator.Visitors;

// Verify that the ToString() method produces consistent output across platforms
class VerifyNewlineFix 
{
    public static void Main()
    {
        var query = "table Example { Id 'System.Int32', Name 'System.String' };\ncouple #a.b with table Example as SourceOfExamples;\nselect 1 from SourceOfExamples('a', 'b')";
        
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var root = parser.ComposeAll();
        
        var cloneQueryVisitor = new CloneQueryVisitor();
        var cloneQueryTraverseVisitor = new CloneTraverseVisitor(cloneQueryVisitor);
        
        root.Accept(cloneQueryTraverseVisitor);

        var stringifiedQuery = cloneQueryVisitor.Root.ToString();
        
        Console.WriteLine("Original query:");
        Console.WriteLine(query);
        Console.WriteLine("\nStringified query:");
        Console.WriteLine(stringifiedQuery);
        Console.WriteLine("\nMatches: " + (query == stringifiedQuery));
        
        // Check that we're using Unix line endings
        Console.WriteLine("Contains \\r\\n: " + stringifiedQuery.Contains("\r\n"));
        Console.WriteLine("Contains \\n only: " + stringifiedQuery.Contains("\n") && !stringifiedQuery.Contains("\r\n"));
    }
}