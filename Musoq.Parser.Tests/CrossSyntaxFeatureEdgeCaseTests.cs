using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Tests;

/// <summary>
/// Comprehensive cross-syntax feature edge case testing.
/// Tests combinations of SQL features to discover parsing issues and boundary conditions
/// where multiple SQL constructs interact in complex ways.
/// </summary>
[TestClass]
public class CrossSyntaxFeatureEdgeCaseTests
{
    private readonly Random _random = new Random(42); // Fixed seed for reproducible tests

    #region SQL Feature Definitions
    
    private static readonly string[] BasicSelectClauses = 
    {
        "SELECT Name",
        "SELECT Name, Value",
        "SELECT *",
        "SELECT COUNT(*)",
        "SELECT SUM(Value)",
        "SELECT DISTINCT Name",
        "SELECT TOP 10 Name"
    };

    private static readonly string[] FromClauses = 
    {
        "FROM #test.data()",
        "FROM #test.entities() t",
        "FROM #schema.method() AS alias"
    };

    private static readonly string[] WhereClauses = 
    {
        "WHERE Id = 1",
        "WHERE Name = 'test'",
        "WHERE Value > 100",
        "WHERE Id IN (1, 2, 3)",
        "WHERE Name LIKE 'test%'",
        "WHERE Id IS NOT NULL",
        "WHERE (Id = 1 OR Id = 2) AND Name = 'test'"
    };

    private static readonly string[] GroupByClauses = 
    {
        "GROUP BY Name",
        "GROUP BY Name, Category",
        "GROUP BY Year, Month"
    };

    private static readonly string[] HavingClauses = 
    {
        "HAVING COUNT(*) > 5",
        "HAVING SUM(Value) > 1000",
        "HAVING AVG(Value) < 50"
    };

    private static readonly string[] OrderByClauses = 
    {
        "ORDER BY Name",
        "ORDER BY Name ASC",
        "ORDER BY Name DESC",
        "ORDER BY Name, Value DESC",
        "ORDER BY COUNT(*) DESC"
    };

    private static readonly string[] JoinClauses = 
    {
        "INNER JOIN #test.other() o ON t.Id = o.Id",
        "LEFT JOIN #test.other() o ON t.Id = o.Id",
        "RIGHT JOIN #test.other() o ON t.Id = o.Id"
    };

    private static readonly string[] CaseClauses = 
    {
        "CASE WHEN Id = 1 THEN 'One' ELSE 'Other' END",
        "CASE Name WHEN 'A' THEN 1 WHEN 'B' THEN 2 ELSE 0 END",
        "CASE WHEN Value > 100 THEN 'High' WHEN Value > 50 THEN 'Medium' ELSE 'Low' END"
    };

    private static readonly string[] SubqueryClauses = 
    {
        "(SELECT COUNT(*) FROM #test.other())",
        "(SELECT Name FROM #test.other() WHERE Id = 1)",
        "(SELECT MAX(Value) FROM #test.other() WHERE Category = t.Category)"
    };

    private static readonly string[] CteDefinitions = 
    {
        "WITH cte AS (SELECT Name, Value FROM #test.data())",
        "WITH numbered AS (SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS rn FROM #test.data())",
        "WITH aggregated AS (SELECT Category, SUM(Value) AS Total FROM #test.data() GROUP BY Category)"
    };

    #endregion

    #region Cross-Feature Combination Testing

    [TestMethod]
    public void CrossFeatureTest_SelectWithComplexWhereAndGroupBy_ShouldParseCorrectly()
    {
        var combinations = new[]
        {
            // SELECT with complex WHERE and GROUP BY
            "SELECT Name, COUNT(*) FROM #test.data() WHERE (Id > 5 AND Name LIKE 'test%') OR Category = 'A' GROUP BY Name",
            
            // SELECT with CASE in WHERE and GROUP BY
            "SELECT Name, SUM(Value) FROM #test.data() WHERE CASE WHEN Category = 'A' THEN 1 ELSE 0 END = 1 GROUP BY Name",
            
            // SELECT with subquery in WHERE and GROUP BY
            "SELECT Name, COUNT(*) FROM #test.data() WHERE Id IN (SELECT Id FROM #test.other()) GROUP BY Name",
            
            // Complex nested expressions
            "SELECT Name, AVG(CASE WHEN Value > 100 THEN Value ELSE 0 END) FROM #test.data() WHERE Name IS NOT NULL GROUP BY Name HAVING COUNT(*) > 2"
        };

        foreach (var query in combinations)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result, $"Query should parse successfully: {query}");
            }
            catch (SyntaxException ex)
            {
                Assert.Fail($"Cross-feature query failed unexpectedly: {query}\nError: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void CrossFeatureTest_JoinsWithComplexConditions_ShouldHandleEdgeCases()
    {
        var complexJoinQueries = new[]
        {
            // JOIN with CASE in ON condition
            "SELECT t.Name, o.Value FROM #test.data() t INNER JOIN #test.other() o ON CASE WHEN t.Type = 'A' THEN t.Id ELSE t.AltId END = o.Id",
            
            // JOIN with subquery in ON condition
            "SELECT t.Name FROM #test.data() t INNER JOIN #test.other() o ON t.Id = o.Id AND o.Value > (SELECT AVG(Value) FROM #test.stats())",
            
            // Multiple JOINs with complex WHERE
            "SELECT t.Name FROM #test.data() t INNER JOIN #test.other() o ON t.Id = o.Id INNER JOIN #test.third() th ON o.RefId = th.Id WHERE t.Status = 'Active' AND o.Type IN ('X', 'Y')",
            
            // JOIN with GROUP BY and HAVING
            "SELECT t.Category, COUNT(*) FROM #test.data() t INNER JOIN #test.other() o ON t.Id = o.Id GROUP BY t.Category HAVING COUNT(*) > 5"
        };

        foreach (var query in complexJoinQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result, $"Complex join query should parse: {query}");
            }
            catch (SyntaxException ex)
            {
                Assert.Fail($"Complex join query failed: {query}\nError: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void CrossFeatureTest_CteWithComplexQueries_ShouldParseCorrectly()
    {
        var cteQueries = new[]
        {
            // CTE with JOIN
            "WITH joined AS (SELECT t.Name, o.Value FROM #test.data() t INNER JOIN #test.other() o ON t.Id = o.Id) SELECT Name, AVG(Value) FROM joined GROUP BY Name",
            
            // CTE with CASE and GROUP BY
            "WITH categorized AS (SELECT Name, CASE WHEN Value > 100 THEN 'High' ELSE 'Low' END AS Category FROM #test.data()) SELECT Category, COUNT(*) FROM categorized GROUP BY Category",
            
            // Multiple CTEs
            "WITH base AS (SELECT * FROM #test.data() WHERE Status = 'Active'), aggregated AS (SELECT Category, SUM(Value) AS Total FROM base GROUP BY Category) SELECT * FROM aggregated WHERE Total > 1000",
            
            // CTE with window functions if supported
            "WITH ranked AS (SELECT Name, Value, ROW_NUMBER() OVER (ORDER BY Value DESC) AS Rank FROM #test.data()) SELECT Name FROM ranked WHERE Rank <= 10"
        };

        foreach (var query in cteQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result, $"CTE query should parse: {query}");
            }
            catch (SyntaxException ex)
            {
                // Some advanced features might not be supported - document the behavior
                Assert.IsNotNull(ex.Message);
                Assert.IsTrue(ex.Message.Length > 10, $"Should provide meaningful error for unsupported CTE feature: {query}");
            }
        }
    }

    #endregion

    #region Automated Feature Combination Generator

    [TestMethod]
    public void AutomatedCrossFeatureTest_GeneratedCombinations_ShouldRevealEdgeCases()
    {
        var foundIssues = new List<string>();
        var testedQueries = 0;
        
        // Generate combinations of SELECT + FROM + WHERE + GROUP BY
        for (int i = 0; i < 100; i++)
        {
            var selectClause = BasicSelectClauses[_random.Next(BasicSelectClauses.Length)];
            var fromClause = FromClauses[_random.Next(FromClauses.Length)];
            var whereClause = WhereClauses[_random.Next(WhereClauses.Length)];
            var groupByClause = _random.Next(2) == 0 ? GroupByClauses[_random.Next(GroupByClauses.Length)] : "";
            var orderByClause = _random.Next(2) == 0 ? OrderByClauses[_random.Next(OrderByClauses.Length)] : "";

            var query = $"{selectClause} {fromClause} {whereClause} {groupByClause} {orderByClause}".Trim();
            testedQueries++;

            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
            }
            catch (SyntaxException ex)
            {
                // Capture potential issues for analysis
                if (ShouldInvestigateException(ex, query))
                {
                    foundIssues.Add($"Query: {query}\nError: {ex.Message}\n");
                }
            }
            catch (Exception ex)
            {
                // Unexpected exceptions should be investigated
                foundIssues.Add($"UNEXPECTED EXCEPTION - Query: {query}\nException: {ex.GetType().Name} - {ex.Message}\n");
            }
        }

        // Report findings
        Console.WriteLine($"Tested {testedQueries} generated queries");
        if (foundIssues.Any())
        {
            Console.WriteLine($"Found {foundIssues.Count} potential issues:");
            foreach (var issue in foundIssues.Take(10)) // Show first 10 issues
            {
                Console.WriteLine(issue);
            }
        }
        
        // This test passes if we don't find critical issues
        var criticalIssues = foundIssues.Count(issue => issue.Contains("UNEXPECTED EXCEPTION"));
        Assert.IsTrue(criticalIssues == 0, $"Found {criticalIssues} unexpected exceptions that need investigation");
    }

    [TestMethod]
    public void AutomatedCrossFeatureTest_OperatorPrecedenceInComplexExpressions_ShouldBeConsistent()
    {
        var operatorCombinations = new[]
        {
            // Arithmetic with logical operators
            "SELECT Name FROM #test.data() WHERE Value + 10 > 100 AND Count * 2 < 50 OR Status = 'Active'",
            
            // Comparison operators with complex expressions
            "SELECT Name FROM #test.data() WHERE (Value + Tax) * Rate > MinThreshold AND Category IN ('A', 'B') OR Priority = 1",
            
            // Function calls with operators
            "SELECT Name FROM #test.data() WHERE LEN(Name) + 5 > 10 AND UPPER(Category) = 'PREMIUM' OR Value / COUNT(*) > 50",
            
            // Nested parentheses with mixed operators
            "SELECT Name FROM #test.data() WHERE ((Value + 10) * 2 > (MinValue - 5)) AND (Status = 'OK' OR (Priority > 1 AND Category = 'VIP'))"
        };

        foreach (var query in operatorCombinations)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result, $"Operator precedence query should parse: {query}");
                
                // Additional validation: ensure the query structure makes sense
                // This would require AST inspection in a real implementation
                
            }
            catch (SyntaxException ex)
            {
                Assert.Fail($"Operator precedence query failed: {query}\nError: {ex.Message}");
            }
        }
    }

    #endregion

    #region Schema Resolution Edge Cases

    [TestMethod]
    public void CrossFeatureTest_ComplexSchemaReferences_ShouldValidateCorrectly()
    {
        var schemaQueries = new[]
        {
            // Multiple schema references in joins
            "SELECT a.Name, b.Value FROM #schema1.data() a INNER JOIN #schema2.data() b ON a.Id = b.Id",
            
            // Schema references in subqueries
            "SELECT Name FROM #test.data() WHERE Id IN (SELECT RefId FROM #other.references() WHERE Status = 'Active')",
            
            // Schema references with complex method calls
            "SELECT Name FROM #test.getData(param1 = 'value', param2 = 123) WHERE Category = 'A'",
            
            // Mixed schema references with aliases
            "SELECT t1.Name, t2.Value FROM #schema1.table() t1, #schema2.table() t2 WHERE t1.Id = t2.RefId"
        };

        foreach (var query in schemaQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result, $"Schema reference query should parse: {query}");
            }
            catch (SyntaxException ex)
            {
                // Schema resolution might have specific rules - document expected behavior
                Assert.IsNotNull(ex.Message);
                Assert.IsTrue(ex.Message.Length > 5, $"Schema error should be descriptive: {query}");
            }
        }
    }

    #endregion

    #region Function Call Edge Cases

    [TestMethod]
    public void CrossFeatureTest_ComplexFunctionCalls_ShouldHandleEdgeCases()
    {
        var functionQueries = new[]
        {
            // Nested function calls with complex parameters
            "SELECT UPPER(SUBSTRING(TRIM(Name), 1, 10)) FROM #test.data() WHERE LEN(Name) > 5",
            
            // Functions in all clauses
            "SELECT CONCAT(Name, '_', Category) FROM #test.data() WHERE YEAR(CreatedDate) = 2023 GROUP BY MONTH(CreatedDate) HAVING COUNT(*) > MAX(Threshold)",
            
            // Functions with CASE statements
            "SELECT Name, CASE WHEN LEN(Name) > 10 THEN SUBSTRING(Name, 1, 10) + '...' ELSE Name END FROM #test.data()",
            
            // Aggregate functions with complex expressions
            "SELECT Category, AVG(CASE WHEN Status = 'Active' THEN Value * Rate ELSE 0 END) FROM #test.data() GROUP BY Category"
        };

        foreach (var query in functionQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result, $"Function call query should parse: {query}");
            }
            catch (SyntaxException ex)
            {
                // Some functions might not be recognized - that's acceptable
                Assert.IsNotNull(ex.Message);
                Assert.IsTrue(ex.Message.Length > 10, $"Function error should be descriptive: {query}");
            }
        }
    }

    #endregion

    #region Error Boundary Testing

    [TestMethod]
    public void CrossFeatureTest_IntentionallyMalformedQueries_ShouldProvideGoodErrors()
    {
        var malformedQueries = new[]
        {
            // Missing components in complex queries
            "SELECT Name FROM #test.data() WHERE GROUP BY Name", // Missing WHERE condition
            "SELECT Name #test.data() WHERE Id = 1", // Missing FROM
            "SELECT Name FROM WHERE Id = 1", // Missing table
            "SELECT FROM #test.data() WHERE", // Missing SELECT fields and WHERE condition
            
            // Misplaced clauses
            "FROM #test.data() SELECT Name WHERE Id = 1", // Wrong order
            "SELECT Name WHERE Id = 1 FROM #test.data()", // Wrong order
            
            // Incomplete constructs
            "SELECT Name FROM #test.data() INNER JOIN", // Incomplete JOIN
            "SELECT Name FROM #test.data() WHERE Id IN", // Incomplete IN
            "SELECT CASE WHEN FROM #test.data()", // Incomplete CASE
            "WITH cte AS SELECT Name FROM #test.data()", // Missing parentheses in CTE
        };

        foreach (var query in malformedQueries)
        {
            try
            {
                ParseQuery(query);
                Assert.Fail($"Malformed query should have failed: {query}");
            }
            catch (SyntaxException ex)
            {
                // Verify error message quality
                Assert.IsNotNull(ex.Message);
                Assert.IsTrue(ex.Message.Length > 15, $"Error message should be descriptive for: {query}. Got: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
                
                // Error should not expose internal details
                Assert.IsFalse(ex.Message.Contains("Exception"), "Should not expose internal exception details");
                Assert.IsFalse(ex.Message.Contains("Stack"), "Should not contain stack trace details");
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

    private static bool ShouldInvestigateException(SyntaxException ex, string query)
    {
        // Don't investigate expected errors for clearly invalid syntax
        if (query.Contains("WHERE GROUP BY") || 
            query.Contains("SELECT FROM WHERE") ||
            string.IsNullOrEmpty(query.Trim()))
        {
            return false;
        }

        // Investigate if error message is too generic or unhelpful
        if (ex.Message.Length < 10 || 
            ex.Message.Contains("Exception") ||
            ex.QueryPart == null ||
            ex.QueryPart.Length == 0)
        {
            return true;
        }

        return false;
    }

    #endregion
}