using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using System;
using System.Linq;

namespace Musoq.Parser.Tests;

/// <summary>
/// Advanced SQL feature exploration tests.
/// Tests cutting-edge SQL features and documents opportunities for future enhancement.
/// These tests explore the boundaries of what's currently possible and identify
/// areas for future parser improvements.
/// </summary>
[TestClass]
public class AdvancedSqlFeatureExplorationTests
{
    #region EXISTS and NOT EXISTS Support Testing

    [TestMethod]
    public void AdvancedFeature_ExistsOperator_TestCurrentSupport()
    {
        var existsQueries = new[]
        {
            "SELECT Name FROM #test.data() WHERE EXISTS (SELECT 1 FROM #test.other() WHERE other.Id = data.Id)",
            "SELECT Name FROM #test.data() WHERE NOT EXISTS (SELECT 1 FROM #test.other() WHERE other.RefId = data.Id)",
            "SELECT Name FROM #test.data() d WHERE EXISTS (SELECT 1 FROM #test.categories() c WHERE c.Id = d.CategoryId AND c.Active = 1)"
        };

        foreach (var query in existsQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"✅ EXISTS SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"❌ EXISTS NOT SUPPORTED: {query}");
                Console.WriteLine($"   Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart, "Should provide meaningful error context");
            }
        }
    }

    #endregion

    #region Advanced Subquery Patterns

    [TestMethod]
    public void AdvancedFeature_NestedSubqueries_TestComplexity()
    {
        var nestedSubqueryQueries = new[]
        {
            // Double-nested subquery
            "SELECT Name FROM #test.data() WHERE Id IN (SELECT RefId FROM #test.refs() WHERE CategoryId IN (SELECT Id FROM #test.categories() WHERE Active = 1))",
            
            // Subquery in SELECT clause
            "SELECT Name, (SELECT COUNT(*) FROM #test.orders() WHERE CustomerId = data.Id) AS OrderCount FROM #test.data() data",
            
            // Correlated subquery with multiple references
            "SELECT Name FROM #test.customers() c WHERE (SELECT SUM(Amount) FROM #test.orders() o WHERE o.CustomerId = c.Id AND o.Year = 2023) > 1000"
        };

        foreach (var query in nestedSubqueryQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"✅ NESTED SUBQUERY SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"❌ NESTED SUBQUERY NOT SUPPORTED: {query}");
                Console.WriteLine($"   Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Window Functions Deep Dive

    [TestMethod]
    public void AdvancedFeature_WindowFunctions_ComprehensiveTest()
    {
        var windowFunctionQueries = new[]
        {
            // Basic window functions
            "SELECT Name, ROW_NUMBER() OVER (ORDER BY Id) AS RowNum FROM #test.data()",
            "SELECT Name, RANK() OVER (PARTITION BY Category ORDER BY Value DESC) AS Rank FROM #test.data()",
            
            // Advanced window functions
            "SELECT Name, LAG(Value, 1, 0) OVER (ORDER BY Date) AS PrevValue FROM #test.data()",
            "SELECT Name, LEAD(Value) OVER (ORDER BY Date) AS NextValue FROM #test.data()",
            "SELECT Name, FIRST_VALUE(Value) OVER (PARTITION BY Category ORDER BY Date) AS FirstValue FROM #test.data()",
            "SELECT Name, LAST_VALUE(Value) OVER (PARTITION BY Category ORDER BY Date ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) AS LastValue FROM #test.data()",
            
            // Window functions with complex partitioning
            "SELECT Name, COUNT(*) OVER (PARTITION BY YEAR(Date), Category) AS CountInYearCategory FROM #test.data()",
            
            // Aggregate window functions
            "SELECT Name, SUM(Value) OVER (ORDER BY Date ROWS BETWEEN 2 PRECEDING AND 2 FOLLOWING) AS MovingSum FROM #test.data()"
        };

        var supportedCount = 0;
        foreach (var query in windowFunctionQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"✅ WINDOW FUNCTION SUPPORTED: {query}");
                supportedCount++;
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"❌ WINDOW FUNCTION NOT SUPPORTED: {query}");
                Console.WriteLine($"   Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
        
        Console.WriteLine($"📊 Window function support: {supportedCount}/{windowFunctionQueries.Length} queries supported");
    }

    #endregion

    #region Common Table Expressions (CTE) Advanced Patterns

    [TestMethod]
    public void AdvancedFeature_RecursiveCte_TestSupport()
    {
        var recursiveCteQueries = new[]
        {
            // Simple recursive CTE
            "WITH RECURSIVE hierarchy AS (SELECT Id, Name, ParentId, 0 AS Level FROM #test.data() WHERE ParentId IS NULL UNION ALL SELECT d.Id, d.Name, d.ParentId, h.Level + 1 FROM #test.data() d INNER JOIN hierarchy h ON d.ParentId = h.Id) SELECT * FROM hierarchy",
            
            // Recursive CTE with multiple columns
            "WITH RECURSIVE tree AS (SELECT Id, Name, ParentId, Name AS Path FROM #test.categories() WHERE ParentId IS NULL UNION ALL SELECT c.Id, c.Name, c.ParentId, t.Path + '/' + c.Name FROM #test.categories() c INNER JOIN tree t ON c.ParentId = t.Id) SELECT * FROM tree",
            
            // Multiple CTEs with one recursive
            "WITH base AS (SELECT * FROM #test.data() WHERE Active = 1), RECURSIVE expanded AS (SELECT Id, Name FROM base UNION ALL SELECT b.Id, b.Name + '_expanded' FROM base b INNER JOIN expanded e ON b.ParentId = e.Id) SELECT * FROM expanded"
        };

        foreach (var query in recursiveCteQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"✅ RECURSIVE CTE SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"❌ RECURSIVE CTE NOT SUPPORTED: {query}");
                Console.WriteLine($"   Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Complex Expression Patterns

    [TestMethod]
    public void AdvancedFeature_ComplexExpressions_BoundaryTesting()
    {
        var complexExpressionQueries = new[]
        {
            // Complex conditional expressions
            "SELECT Name, CASE WHEN Value > (SELECT AVG(Value) FROM #test.data()) THEN 'Above Average' WHEN Value > (SELECT MIN(Value) FROM #test.data()) THEN 'Above Minimum' ELSE 'Minimum' END AS Category FROM #test.data()",
            
            // Deeply nested function calls
            "SELECT UPPER(SUBSTRING(TRIM(COALESCE(Name, 'Unknown')), 1, GREATEST(LEN(Name) / 2, 5))) AS ProcessedName FROM #test.data()",
            
            // Complex arithmetic with subqueries
            "SELECT Name, (Value * 1.2 + (SELECT AVG(Tax) FROM #test.rates())) * CASE WHEN Category = 'Premium' THEN 1.1 ELSE 1.0 END AS FinalPrice FROM #test.data()",
            
            // Boolean logic with subqueries
            "SELECT Name FROM #test.data() WHERE (Active = 1 AND Category IN (SELECT Code FROM #test.categories() WHERE Enabled = 1)) OR (Id IN (SELECT CustomerId FROM #test.exceptions()) AND Status = 'Special')"
        };

        foreach (var query in complexExpressionQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"✅ COMPLEX EXPRESSION SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"❌ COMPLEX EXPRESSION NOT SUPPORTED: {query}");
                Console.WriteLine($"   Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Set Operations with Subqueries

    [TestMethod]
    public void AdvancedFeature_SetOperationsWithSubqueries_TestSupport()
    {
        var setOperationQueries = new[]
        {
            // UNION with subqueries
            "SELECT Name FROM (SELECT Name FROM #test.customers()) UNION SELECT Name FROM (SELECT Name FROM #test.suppliers())",
            
            // INTERSECT with complex subqueries
            "SELECT Id FROM (SELECT Id FROM #test.orders() WHERE Amount > 100) INTERSECT SELECT Id FROM (SELECT CustomerId FROM #test.customers() WHERE Premium = 1)",
            
            // EXCEPT with nested queries
            "SELECT Name FROM #test.products() EXCEPT SELECT ProductName FROM (SELECT ProductName FROM #test.discontinued() WHERE Date > '2023-01-01')"
        };

        foreach (var query in setOperationQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"✅ SET OPERATION WITH SUBQUERY SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"❌ SET OPERATION WITH SUBQUERY NOT SUPPORTED: {query}");
                Console.WriteLine($"   Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Advanced JOIN Patterns

    [TestMethod]
    public void AdvancedFeature_ComplexJoinPatterns_TestSupport()
    {
        var complexJoinQueries = new[]
        {
            // Self-join with subquery
            "SELECT e1.Name AS Employee, e2.Name AS Manager FROM #test.employees() e1 LEFT JOIN #test.employees() e2 ON e1.ManagerId = e2.Id WHERE e1.Id IN (SELECT EmployeeId FROM #test.active())",
            
            // JOIN with derived table
            "SELECT c.Name, stats.OrderCount FROM #test.customers() c INNER JOIN (SELECT CustomerId, COUNT(*) AS OrderCount FROM #test.orders() GROUP BY CustomerId) stats ON c.Id = stats.CustomerId",
            
            // Multiple JOINs with subqueries in ON conditions
            "SELECT o.Id, c.Name, p.ProductName FROM #test.orders() o INNER JOIN #test.customers() c ON o.CustomerId = c.Id INNER JOIN #test.products() p ON o.ProductId = p.Id AND p.CategoryId IN (SELECT Id FROM #test.categories() WHERE Active = 1)"
        };

        foreach (var query in complexJoinQueries)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                Console.WriteLine($"✅ COMPLEX JOIN SUPPORTED: {query}");
            }
            catch (SyntaxException ex)
            {
                Console.WriteLine($"❌ COMPLEX JOIN NOT SUPPORTED: {query}");
                Console.WriteLine($"   Error: {ex.Message}");
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Future Enhancement Opportunities

    [TestMethod]
    public void FutureEnhancement_IdentifyImprovementOpportunities()
    {
        Console.WriteLine("🔮 FUTURE ENHANCEMENT OPPORTUNITIES:");
        Console.WriteLine("   1. PIVOT/UNPIVOT operations");
        Console.WriteLine("   2. MERGE/UPSERT statements");
        Console.WriteLine("   3. JSON path expressions (JSON_VALUE, JSON_QUERY)");
        Console.WriteLine("   4. CROSS/OUTER APPLY with table-valued functions");
        Console.WriteLine("   5. Temporal queries (FOR SYSTEM_TIME AS OF)");
        Console.WriteLine("   6. Advanced window frame specifications");
        Console.WriteLine("   7. Columnstore index hints");
        Console.WriteLine("   8. XML query methods");
        Console.WriteLine("   9. Full-text search predicates");
        Console.WriteLine("   10. Spatial data type operations");
        
        // This test always passes - it's for documentation
        Assert.IsTrue(true);
    }

    #endregion

    #region Parser Performance Stress Test

    [TestMethod]
    public void AdvancedFeature_ParserPerformanceStressTest()
    {
        // Test parser performance with increasingly complex queries
        var baseQuery = "SELECT Name FROM #test.data()";
        var complexities = new[]
        {
            baseQuery,
            "SELECT Name FROM #test.data() WHERE Id IN (SELECT Id FROM #test.other())",
            "SELECT Name FROM #test.data() WHERE Id IN (SELECT Id FROM #test.other() WHERE RefId IN (SELECT Id FROM #test.refs()))",
            "SELECT Name, (SELECT COUNT(*) FROM #test.orders() o WHERE o.CustomerId = d.Id AND o.Year IN (SELECT Year FROM #test.years() WHERE Active = 1)) FROM #test.data() d WHERE EXISTS (SELECT 1 FROM #test.categories() c WHERE c.Id = d.CategoryId)"
        };

        foreach (var item in complexities.Select((q, i) => new { Query = q, Index = i }))
        {
            var query = item.Query;
            var index = item.Index;
        {
            var startTime = DateTime.Now;
            try
            {
                var result = ParseQuery(query);
                var duration = DateTime.Now - startTime;
                Assert.IsNotNull(result);
                Console.WriteLine($"✅ COMPLEXITY LEVEL {index}: {duration.TotalMilliseconds}ms - {query.Substring(0, Math.Min(50, query.Length))}...");
            }
            catch (SyntaxException ex)
            {
                var duration = DateTime.Now - startTime;
                Console.WriteLine($"❌ COMPLEXITY LEVEL {index}: {duration.TotalMilliseconds}ms - FAILED: {ex.Message}");
            }
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