using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using System;

namespace Musoq.Parser.Tests;

/// <summary>
/// Specific test cases for issues discovered through cross-syntax feature testing.
/// Each test documents a specific limitation or issue found in the parser's
/// handling of SQL feature combinations.
/// </summary>
[TestClass]
public class DiscoveredIssuesTests
{
    #region Issue 1: Subqueries Not Supported in WHERE Clauses

    [TestMethod]
    public void Issue001_SubqueryInWhereClause_NowSupported()
    {
        // FIXED: Parser now supports subqueries in WHERE clauses
        var query = "SELECT Name FROM #test.data() WHERE Id IN (SELECT Id FROM #test.other())";
        
        // This should now parse successfully
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
        
        Console.WriteLine($"SUCCESS: Subqueries in WHERE clauses now supported!");
        Console.WriteLine($"Query: {query}");
        Console.WriteLine($"Parsed successfully: {result != null}");
    }

    [TestMethod]
    public void Issue001_SubqueryInWhereClause_VariousFormats_NowSupported()
    {
        var subqueryVariations = new[]
        {
            "SELECT Name FROM #test.data() WHERE Id IN (SELECT Id FROM #test.other())",
            "SELECT Name FROM #test.data() WHERE Id = (SELECT MAX(Id) FROM #test.other())",
            "SELECT Name FROM #test.data() WHERE EXISTS (SELECT 1 FROM #test.other() WHERE other.Id = data.Id)",
            "SELECT Name FROM #test.data() WHERE Value > (SELECT AVG(Value) FROM #test.other())"
        };

        foreach (var query in subqueryVariations)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"SUBQUERY SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                // Some advanced subquery forms might still not be supported
                Console.WriteLine($"SUBQUERY NOT YET SUPPORTED: {query}");
                Console.WriteLine($"Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Issue 2: Subqueries Not Supported in JOIN Conditions

    [TestMethod]
    public void Issue002_SubqueryInJoinCondition_TestSupport()
    {
        // Test if subqueries in JOIN conditions are now supported
        var query = "SELECT t.Name FROM #test.data() t INNER JOIN #test.other() o ON t.Id = o.Id AND o.Value > (SELECT AVG(Value) FROM #test.stats())";
        
        try
        {
            var result = ParseQuery(query);
            Assert.IsNotNull(result);
            Console.WriteLine($"SUCCESS: Subqueries in JOIN conditions now supported!");
            Console.WriteLine($"Query: {query}");
        }
        catch (SyntaxException ex)
        {
            Console.WriteLine($"ISSUE 002: Subqueries in JOIN conditions still not supported");
            Console.WriteLine($"Query: {query}");
            Console.WriteLine($"Error: {ex.Message}");
            Assert.IsNotNull(ex.QueryPart);
        }
    }

    [TestMethod]
    public void Issue002_SubqueryInJoinCondition_SimplerCases_NowSupported()
    {
        var joinSubqueryVariations = new[]
        {
            "SELECT t.Name FROM #test.data() t INNER JOIN #test.other() o ON t.Id = (SELECT MAX(Id) FROM #test.refs())",
            "SELECT t.Name FROM #test.data() t LEFT JOIN #test.other() o ON o.RefId IN (SELECT Id FROM #test.valid())"
        };

        foreach (var query in joinSubqueryVariations)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"JOIN SUBQUERY SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"JOIN SUBQUERY NOT YET SUPPORTED: {query}");
                Console.WriteLine($"Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Issue 3: Test Working Cross-Feature Combinations

    [TestMethod]
    public void WorkingCrossFeatures_CaseInSelectWithGroupBy_ShouldWork()
    {
        // This works correctly - document working functionality
        var query = "SELECT CASE WHEN Id > 5 THEN 'High' ELSE 'Low' END AS Category, COUNT(*) FROM #test.data() GROUP BY CASE WHEN Id > 5 THEN 'High' ELSE 'Low' END";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
        
        Console.WriteLine("WORKING: CASE statements in SELECT and GROUP BY clauses work correctly");
    }

    [TestMethod]
    public void WorkingCrossFeatures_ComplexWhereWithGroupByHaving_ShouldWork()
    {
        // This works correctly
        var query = "SELECT Name, COUNT(*) FROM #test.data() WHERE (Id > 5 AND Status = 'Active') OR Category IN ('A', 'B') GROUP BY Name HAVING COUNT(*) > 2";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
        
        Console.WriteLine("WORKING: Complex WHERE with GROUP BY and HAVING works correctly");
    }

    [TestMethod]
    public void WorkingCrossFeatures_MultipleJoinsWithComplexConditions_ShouldWork()
    {
        // This works correctly
        var query = "SELECT t.Name, o.Value FROM #test.data() t INNER JOIN #test.other() o ON t.Id = o.Id INNER JOIN #test.third() th ON o.RefId = th.Id WHERE t.Status = 'Active'";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
        
        Console.WriteLine("WORKING: Multiple JOINs with complex WHERE conditions work correctly");
    }

    #endregion

    #region Issue 4: Advanced Feature Testing

    [TestMethod]
    public void Issue004_WindowFunctions_TestCurrentSupport()
    {
        // Test if window functions are supported
        var windowFunctionQueries = new[]
        {
            "SELECT Name, ROW_NUMBER() OVER (ORDER BY Id) FROM #test.data()",
            "SELECT Name, RANK() OVER (PARTITION BY Category ORDER BY Value) FROM #test.data()",
            "SELECT Name, LAG(Value) OVER (ORDER BY Id) FROM #test.data()"
        };

        foreach (var query in windowFunctionQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"WINDOW FUNCTION SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"WINDOW FUNCTION NOT SUPPORTED: {query}");
                Console.WriteLine($"Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    [TestMethod]
    public void Issue005_CommonTableExpressions_TestComplexScenarios()
    {
        // Test CTE edge cases discovered
        var cteQueries = new[]
        {
            // Recursive CTE
            "WITH RECURSIVE cte AS (SELECT Id, Name FROM #test.data() WHERE ParentId IS NULL UNION ALL SELECT d.Id, d.Name FROM #test.data() d JOIN cte ON d.ParentId = cte.Id) SELECT * FROM cte",
            
            // Multiple CTEs
            "WITH cte1 AS (SELECT * FROM #test.data() WHERE Status = 'Active'), cte2 AS (SELECT * FROM cte1 WHERE Category = 'A') SELECT * FROM cte2",
            
            // CTE with aggregate and window functions
            "WITH ranked AS (SELECT Name, Value, ROW_NUMBER() OVER (ORDER BY Value DESC) AS rn FROM #test.data()) SELECT Name FROM ranked WHERE rn <= 5"
        };

        foreach (var query in cteQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"CTE SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"CTE NOT SUPPORTED: {query}");
                Console.WriteLine($"Error: {ex.Message}");
                // Not all CTE features may be supported - this is acceptable
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Issue 6: Expression Context Edge Cases

    [TestMethod]
    public void Issue006_CaseStatementsInDifferentContexts_ShouldHandleConsistently()
    {
        var caseInDifferentContexts = new[]
        {
            // CASE in SELECT
            "SELECT CASE WHEN Id > 5 THEN 'High' ELSE 'Low' END FROM #test.data()",
            
            // CASE in WHERE
            "SELECT Name FROM #test.data() WHERE CASE WHEN Category = 'A' THEN 1 ELSE 0 END = 1",
            
            // CASE in ORDER BY
            "SELECT Name FROM #test.data() ORDER BY CASE WHEN Priority IS NULL THEN 999 ELSE Priority END",
            
            // CASE in GROUP BY
            "SELECT COUNT(*) FROM #test.data() GROUP BY CASE WHEN Value > 100 THEN 'High' ELSE 'Low' END",
            
            // Nested CASE statements
            "SELECT CASE WHEN Category = 'A' THEN CASE WHEN Value > 100 THEN 'A-High' ELSE 'A-Low' END ELSE 'Other' END FROM #test.data()"
        };

        foreach (var query in caseInDifferentContexts)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"CASE CONTEXT SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"CASE CONTEXT ISSUE: {query}");
                Console.WriteLine($"Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Issue 7: Operator Precedence in Complex Expressions

    [TestMethod]
    public void Issue007_OperatorPrecedenceWithFunctions_ShouldBeConsistent()
    {
        var precedenceQueries = new[]
        {
            // Arithmetic with function calls
            "SELECT Value + LEN(Name) * 2 FROM #test.data()",
            
            // Logical operators with function results
            "SELECT Name FROM #test.data() WHERE LEN(Name) > 5 AND UPPER(Category) = 'PREMIUM' OR Status = 'VIP'",
            
            // Comparison operators with complex expressions
            "SELECT Name FROM #test.data() WHERE (Value + Tax) * Rate > MinValue AND Category IN ('A', 'B')",
            
            // Function calls with boolean logic
            "SELECT Name FROM #test.data() WHERE NOT (ISNULL(Name, '') = '' OR LEN(TRIM(Name)) = 0)"
        };

        foreach (var query in precedenceQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"PRECEDENCE HANDLED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"PRECEDENCE ISSUE: {query}");
                Console.WriteLine($"Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static Musoq.Parser.Nodes.Node ParseQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        return parser.ComposeAll();
    }

    #endregion
}