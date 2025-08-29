using Musoq.Benchmarks.Schema;

namespace Musoq.Benchmarks.CodeGeneration;

/// <summary>
/// Test queries representing different complexity levels and patterns for code generation analysis
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
        
        ("Simple Count", "SELECT COUNT(*) FROM #test.Entities()"),
        
        ("Simple Order", "SELECT City, Population FROM #test.Entities() ORDER BY Population DESC"),
        
        ("Basic Math", "SELECT City, Population * 2 AS DoublePopulation FROM #test.Entities()"),
        
        ("String Operations", "SELECT CONCAT(City, ' - ', Country) AS FullName FROM #test.Entities()"),
        
        ("Simple CASE", @"SELECT City, 
                           CASE WHEN Population > 1000000 THEN 'Large' ELSE 'Small' END AS Size 
                         FROM #test.Entities()"),
    };

    /// <summary>
    /// Medium complexity queries with aggregations and grouping
    /// </summary>
    public static readonly List<(string Name, string Query)> MediumQueries = new()
    {
        ("Group By with Count", "SELECT Country, COUNT(*) AS CityCount FROM #test.Entities() GROUP BY Country"),
        
        ("Group By with Sum", "SELECT Country, SUM(Population) AS TotalPopulation FROM #test.Entities() GROUP BY Country"),
        
        ("Having Clause", @"SELECT Country, COUNT(*) AS CityCount 
                            FROM #test.Entities() 
                            GROUP BY Country 
                            HAVING COUNT(*) > 5"),
                            
        ("Multiple Aggregations", @"SELECT Country, 
                                     COUNT(*) AS CityCount,
                                     SUM(Population) AS TotalPop,
                                     AVG(Population) AS AvgPop,
                                     MAX(Population) AS MaxPop,
                                     MIN(Population) AS MinPop
                                   FROM #test.Entities() 
                                   GROUP BY Country"),
                                   
        ("Complex Where", @"SELECT City, Population 
                            FROM #test.Entities() 
                            WHERE Population > 500000 
                              AND Country IN ('USA', 'Canada', 'Mexico')
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
    };

    /// <summary>
    /// Complex queries with joins, subqueries, and advanced operations
    /// </summary>
    public static readonly List<(string Name, string Query)> ComplexQueries = new()
    {
        ("Multiple CTEs", @"WITH LargeCities AS (
                             SELECT City, Country, Population 
                             FROM #test.Entities() 
                             WHERE Population > 1000000
                           ),
                           CountryStats AS (
                             SELECT Country, 
                                    COUNT(*) AS LargeCityCount,
                                    AVG(Population) AS AvgPopulation
                             FROM LargeCities 
                             GROUP BY Country
                           )
                           SELECT * FROM CountryStats ORDER BY LargeCityCount DESC"),
                           
        ("Window Functions", @"SELECT City, Country, Population,
                                ROW_NUMBER() OVER (PARTITION BY Country ORDER BY Population DESC) AS Rank,
                                SUM(Population) OVER (PARTITION BY Country) AS CountryTotal,
                                LAG(Population) OVER (PARTITION BY Country ORDER BY Population) AS PrevPop
                              FROM #test.Entities()"),
                              
        ("Complex Aggregation", @"SELECT Country,
                                   COUNT(DISTINCT City) AS UniqueCities,
                                   PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY Population) AS MedianPop,
                                   STRING_AGG(City, ', ') AS CityList
                                 FROM #test.Entities()
                                 GROUP BY Country
                                 HAVING COUNT(*) >= 3"),
                                 
        ("Heavy String Processing", @"SELECT 
                                       UPPER(LEFT(City, 3)) + '_' + 
                                       LOWER(RIGHT(Country, 2)) + '_' + 
                                       CAST(LEN(City) AS VARCHAR) + '_' +
                                       REPLACE(REPLACE(City, ' ', '_'), '-', '_') AS ProcessedName,
                                       SUBSTRING(City, 1, CHARINDEX(' ', City + ' ') - 1) AS FirstWord,
                                       REVERSE(Country) AS ReversedCountry
                                     FROM #test.Entities()"),
                                     
        ("Multiple Operations", @"SELECT City,
                                   ABS(Population - 1000000) AS PopDiff,
                                   SQRT(Population) AS SqrtPop,
                                   LOG(Population + 1) AS LogPop,
                                   POWER(Population / 1000000.0, 2) AS PopSquared,
                                   ROUND(Population / 1000000.0, 2) AS PopInMillions
                                 FROM #test.Entities()
                                 WHERE Population > 0"),
    };

    /// <summary>
    /// Performance stress test queries that generate very complex code
    /// </summary>
    public static readonly List<(string Name, string Query)> StressTestQueries = new()
    {
        ("Many Columns Select", GenerateManyColumnsQuery(50)),
        
        ("Deep Nested CASE", GenerateDeepNestedCaseQuery(10)),
        
        ("Many Aggregations", GenerateManyAggregationsQuery(20)),
        
        ("Complex String Operations", GenerateComplexStringOperationsQuery(15)),
        
        ("Multiple CTEs Chain", GenerateMultipleCTEsQuery(5)),
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
            aggregations.Add($"COUNT(CASE WHEN Population > {i * 50000} THEN 1 END) AS Count{i}");
            aggregations.Add($"SUM(CASE WHEN Population > {i * 50000} THEN Population ELSE 0 END) AS Sum{i}");
        }
        
        return $"SELECT Country, {string.Join(", ", aggregations)} FROM #test.Entities() GROUP BY Country";
    }

    private static string GenerateComplexStringOperationsQuery(int operationCount)
    {
        var operations = new List<string>();
        for (int i = 0; i < operationCount; i++)
        {
            operations.Add($@"CONCAT(
                UPPER(SUBSTRING(City, 1, {i + 1})), 
                '_', 
                LOWER(SUBSTRING(Country, 1, {i + 2})), 
                '_', 
                CAST({i} AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_{i}'), '-', '_{i}')
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
                    SELECT City, Country, Population, 
                           ROW_NUMBER() OVER (ORDER BY Population) AS Rank{i}
                    FROM #test.Entities() 
                    WHERE Population > {i * 100000}
                )");
            }
            else
            {
                ctes.Add($@"CTE{i} AS (
                    SELECT City, Country, Population, Rank{i - 1},
                           Population * {i + 1} AS AdjustedPop{i},
                           RANK() OVER (PARTITION BY Country ORDER BY Population) AS Rank{i}
                    FROM CTE{i - 1}
                    WHERE Rank{i - 1} <= {10 + i * 5}
                )");
            }
        }
        
        return $@"WITH {string.Join(", ", ctes.ToArray())} 
                  SELECT * FROM CTE{cteCount - 1} 
                  ORDER BY AdjustedPop{cteCount - 1} DESC";
    }
}