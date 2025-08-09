using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

class TestIssueRepro
{
    static void Main()
    {
        // The specific failing test case
        var query = "table Example { Id 'System.Int32', Name 'System.String' };\ncouple #a.b with table Example as SourceOfExamples;\nselect 1 from SourceOfExamples('a', 'b')";
        
        Console.WriteLine("=== Debugging StatementsArrayNode Issue ===");
        Console.WriteLine($"Input query: '{query}'");
        Console.WriteLine();
        
        try
        {
            var lexer = new Lexer(query, true);
            var parser = new Musoq.Parser.Parser(lexer);
            var root = parser.ComposeAll();
            
            Console.WriteLine($"Parsed root type: {root.GetType().Name}");
            
            // Check if we have a StatementsArrayNode
            if (root is StatementsArrayNode statementsArray)
            {
                Console.WriteLine($"StatementsArrayNode found with {statementsArray.Statements.Length} statements");
                
                for (int i = 0; i < statementsArray.Statements.Length; i++)
                {
                    var statement = statementsArray.Statements[i];
                    Console.WriteLine($"Statement {i}: {statement.GetType().Name}");
                    Console.WriteLine($"Statement {i} ToString: '{statement.ToString()}'");
                }
                
                // Test the ToString method manually
                Console.WriteLine("=== Testing ToString Logic ===");
                
                var toStringResults = statementsArray.Statements.Select(f => f.ToString()).ToList();
                Console.WriteLine($"Individual ToString results: {toStringResults.Count}");
                
                for (int i = 0; i < toStringResults.Count; i++)
                {
                    Console.WriteLine($"Result {i}: '{toStringResults[i]}'");
                }
                
                // Test the aggregate logic
                if (toStringResults.Count > 0)
                {
                    var aggregated = toStringResults.Aggregate((a, b) => $"{a}{Environment.NewLine}{b}");
                    Console.WriteLine($"Aggregated result: '{aggregated}'");
                }
            }
            else
            {
                Console.WriteLine($"Root is not StatementsArrayNode but {root.GetType().Name}");
            }
            
            Console.WriteLine($"Original root ToString: '{root.ToString()}'");
            
            // Now test with cloning
            Console.WriteLine("\n=== Testing with CloneQueryVisitor ===");
            
            var cloneQueryVisitor = new CloneQueryVisitor();
            var cloneQueryTraverseVisitor = new CloneTraverseVisitor(cloneQueryVisitor);
            
            root.Accept(cloneQueryTraverseVisitor);
            var clonedRoot = cloneQueryVisitor.Root;
            var stringifiedQuery = clonedRoot.ToString();
            
            Console.WriteLine($"Cloned root type: {clonedRoot.GetType().Name}");
            Console.WriteLine($"Stringified query: '{stringifiedQuery}'");
            Console.WriteLine($"Query equals stringified: {query == stringifiedQuery}");
            
            if (query != stringifiedQuery)
            {
                Console.WriteLine("=== MISMATCH DETECTED ===");
                Console.WriteLine($"Expected: '{query}' (length: {query.Length})");
                Console.WriteLine($"Actual:   '{stringifiedQuery}' (length: {stringifiedQuery.Length})");
                
                // Character by character comparison
                var minLength = Math.Min(query.Length, stringifiedQuery.Length);
                for (int i = 0; i < minLength; i++)
                {
                    if (query[i] != stringifiedQuery[i])
                    {
                        Console.WriteLine($"First difference at position {i}: expected '{query[i]}' ({(int)query[i]}), got '{stringifiedQuery[i]}' ({(int)stringifiedQuery[i]})");
                        break;
                    }
                }
                
                if (query.Length != stringifiedQuery.Length)
                {
                    Console.WriteLine($"Length difference: expected {query.Length}, got {stringifiedQuery.Length}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}");
        }
    }
}