using Musoq.Benchmarks.Schema;

namespace Musoq.Benchmarks.CodeGeneration;

/// <summary>
/// Test queries representing different complexity levels and patterns for code generation analysis.
/// These queries are based on working examples from Musoq unit tests to ensure compatibility.
/// </summary>
public static class TestQuerySuite
{
    /// <summary>
    /// Simple queries for basic code generation patterns
    /// </summary>
    public static readonly List<(string Name, string Query)> SimpleQueries = new()
    {
        ("Simple Select", "SELECT City, Population FROM #test.Entities()"),
        
        ("Simple Where", "SELECT City FROM #test.Entities() WHERE Population > 1000000"),
        
        ("Simple Order", "SELECT City, Population FROM #test.Entities() ORDER BY Population DESC"),
        
        ("Basic Math", "SELECT City, Population * 2 AS DoublePopulation FROM #test.Entities()"),
        
        ("Array Indexing", "SELECT City FROM #test.Entities() WHERE City[0] = 'N'"),
        
        ("Simple CASE", @"SELECT City, 
                           CASE WHEN Population > 1000000 THEN 'Large' ELSE 'Small' END AS Size 
                         FROM #test.Entities()"),
                         
        ("Like Pattern", "SELECT City FROM #test.Entities() WHERE City LIKE '%York%'"),
        
        ("Not Like Pattern", "SELECT City FROM #test.Entities() WHERE City NOT LIKE '%test%'"),
        
        ("Regex Pattern", @"SELECT City FROM #test.Entities() WHERE City RLIKE '^[A-Z][a-z]+'"),
    };

    /// <summary>
    /// Medium complexity queries with aggregations and grouping
    /// </summary>
    public static readonly List<(string Name, string Query)> MediumQueries = new()
    {
        ("Group By with Count", "SELECT Country, Count(City) FROM #test.Entities() GROUP BY Country"),
        
        ("Group By with Sum", "SELECT Country, Sum(Population) FROM #test.Entities() GROUP BY Country"),
        
        ("Having Clause", @"SELECT Country, Count(City) 
                            FROM #test.Entities() 
                            GROUP BY Country 
                            HAVING Count(City) >= 2"),
                            
        ("Multiple Aggregations", @"SELECT Country, 
                                     Count(City),
                                     Sum(Population),
                                     Avg(Population),
                                     Max(Population),
                                     Min(Population)
                                   FROM #test.Entities() 
                                   GROUP BY Country"),
                                   
        ("Complex Where", @"SELECT City, Population 
                            FROM #test.Entities() 
                            WHERE Population > 500000 
                              AND City LIKE '%City%'"),
                              
        ("Nested CASE", @"SELECT City,
                            CASE 
                              WHEN Population > 5000000 THEN 'Mega'
                              WHEN Population > 1000000 THEN 
                                CASE 
                                  WHEN Country = 'USA' THEN 'Large US'
                                  ELSE 'Large Other'
                                END
                              WHEN Population > 100000 THEN 'Medium'
                              ELSE 'Small'
                            END AS Classification
                          FROM #test.Entities()"),
                          
        ("String Functions", "SELECT Substring(City, 0, 3), Length(City) FROM #test.Entities()"),
        
        ("RowNumber Function", "SELECT City, Country, RowNumber() FROM #test.Entities()"),
    };

    /// <summary>
    /// Complex queries with joins, subqueries, and advanced operations
    /// </summary>
    public static readonly List<(string Name, string Query)> ComplexQueries = new()
    {
        ("Simple CTE", @"WITH p AS (
                           SELECT City, Country, Population 
                           FROM #test.Entities() 
                           WHERE Population > 1000000
                         )
                         SELECT * FROM p ORDER BY Population DESC"),
                           
        ("CTE with Grouping", @"WITH p AS (
                                 SELECT Country, Sum(Population) AS TotalPop
                                 FROM #test.Entities() 
                                 GROUP BY Country
                               )
                               SELECT * FROM p WHERE TotalPop > 5000000"),
                              
        ("Simple Join", @"SELECT a.Country, b.City, b.Population
                          FROM #test.Entities() a
                          INNER JOIN #test.Entities() b ON a.Country = b.Country
                          WHERE a.Population > b.Population"),
                                 
        ("Math Functions", @"SELECT City,
                              Abs(Population - 1000000) AS PopDiff,
                              Round(Population / 1000000.0, 2) AS PopInMillions
                            FROM #test.Entities()
                            WHERE Population > 0"),
                            
        ("String Processing", @"SELECT 
                                 ToUpper(Substring(City, 0, 3)) AS CityPrefix,
                                 ToString(Length(City)) AS CityLength,
                                 Replace(City, ' ', '_') AS ProcessedName
                               FROM #test.Entities()"),
                               
        ("Complex Grouping", @"SELECT 
                                Substring(Country, 0, 2) AS CountryPrefix,
                                Count(City) AS CityCount,
                                Sum(Population) AS TotalPop
                              FROM #test.Entities()
                              GROUP BY Substring(Country, 0, 2)
                              HAVING Count(City) > 1"),
                              
        ("Skip Take", @"SELECT City, Population 
                        FROM #test.Entities() 
                        ORDER BY Population DESC 
                        SKIP 5 TAKE 10"),
    };

    /// <summary>
    /// Performance stress test queries that generate complex code but work within Musoq's capabilities
    /// </summary>
    public static readonly List<(string Name, string Query)> StressTestQueries = new()
    {
        ("Many Columns Select", GenerateManyColumnsQuery(30)),
        
        ("Deep Nested CASE", GenerateDeepNestedCaseQuery(8)),
        
        ("Many Aggregations", GenerateManyAggregationsQuery(10)),
        
        ("Complex String Operations", GenerateComplexStringOperationsQuery(8)),
        
        ("Multiple CTEs Chain", GenerateMultipleCTEsQuery(3)),
    };

    /// <summary>
    /// All queries combined for comprehensive analysis
    /// </summary>
    public static List<(string Name, string Query)> AllQueries => 
        SimpleQueries
            .Concat(MediumQueries)
            .Concat(ComplexQueries)
            .Concat(StressTestQueries)
            .ToList();

    private static string GenerateManyColumnsQuery(int columnCount)
    {
        var columns = new List<string>();
        for (int i = 0; i < columnCount; i++)
        {
            columns.Add($"CASE WHEN Population > {i * 100000} THEN '{i}_High' ELSE '{i}_Low' END AS Column{i}");
        }
        
        return $"SELECT City, {string.Join(", ", columns.ToArray())} FROM #test.Entities()";
    }

    private static string GenerateDeepNestedCaseQuery(int depth)
    {
        string BuildNestedCase(int currentDepth)
        {
            if (currentDepth == 0)
                return $"'Depth{depth}'";
                
            return $@"CASE WHEN Population > {currentDepth * 1000000} 
                           THEN 'Level{currentDepth}' 
                           ELSE {BuildNestedCase(currentDepth - 1)} 
                      END";
        }
        
        return $"SELECT City, {BuildNestedCase(depth)} AS NestedResult FROM #test.Entities()";
    }

    private static string GenerateManyAggregationsQuery(int aggCount)
    {
        var aggregations = new List<string>();
        for (int i = 0; i < aggCount; i++)
        {
            aggregations.Add($"Count(City) AS Count{i}");
            aggregations.Add($"Sum(Population) AS Sum{i}");
        }
        
        return $"SELECT Country, {string.Join(", ", aggregations)} FROM #test.Entities() GROUP BY Country";
    }

    private static string GenerateComplexStringOperationsQuery(int operationCount)
    {
        var operations = new List<string>();
        for (int i = 0; i < operationCount; i++)
        {
            operations.Add($@"Concat(
                ToUpper(Substring(City, 0, {i + 1})), 
                '_', 
                ToLower(Substring(Country, 0, {i + 2})), 
                '_', 
                ToString({i}),
                '_',
                Replace(City, ' ', '_')
            ) AS StringOp{i}");
        }
        
        return $"SELECT City, {string.Join(", ", operations)} FROM #test.Entities()";
    }

    private static string GenerateMultipleCTEsQuery(int cteCount)
    {
        var ctes = new List<string>();
        
        for (int i = 0; i < cteCount; i++)
        {
            if (i == 0)
            {
                ctes.Add($@"CTE{i} AS (
                    SELECT City, Country, Population
                    FROM #test.Entities() 
                    WHERE Population > {i * 100000}
                )");
            }
            else
            {
                ctes.Add($@"CTE{i} AS (
                    SELECT City, Country, Population,
                           Population * {i + 1} AS AdjustedPop{i}
                    FROM CTE{i - 1}
                    WHERE Population > {i * 200000}
                )");
            }
        }
        
        return $@"WITH {string.Join(", ", ctes.ToArray())} 
                  SELECT * FROM CTE{cteCount - 1} 
                  ORDER BY AdjustedPop{cteCount - 1} DESC";
    }
}