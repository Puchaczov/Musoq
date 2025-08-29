# MUSOQ CODE GENERATION PERFORMANCE ANALYSIS REPORT
=============================================================

## EXECUTIVE SUMMARY

**Total Queries Analyzed**: 23
**Successful Analyses**: 9
**Failed Analyses**: 14

**Key Metrics Averages**:
- Generated Code Lines: 131.6
- Code Complexity Score: 18.6
- Execution Time: 4.7ms
- Memory Usage: -363.6KB

**Areas of Concern**:
- High Complexity Queries: 1
- Slow Execution Queries: 0
- Memory Heavy Queries: 0

## CODE GENERATION METRICS ANALYSIS

### Code Size Distribution
| Metric | Min | Max | Average | Median |
|--------|-----|-----|---------|--------|
| Total Lines | 51.0 | 659.0 | 143.8 | 64.0 |
| Non-Empty Lines | 46.0 | 604.0 | 131.6 | 58.0 |
| Methods | 2.0 | 52.0 | 9.0 | 2.0 |
| Complexity Score | 4.0 | 104.0 | 18.6 | 5.0 |

### Code Patterns Distribution
| Pattern | Total Count | Avg per Query | High Usage Queries |
|---------|-------------|---------------|-------------------|
| Loops | 1 | 0.1 | Simple Order |
| Conditionals | 67 | 7.4 | Many Columns Select |
| Lambdas | 9 | 1.0 |  |
| LINQ Operations | 8 | 0.9 |  |
| Object Allocations | 175 | 19.4 | Many Columns Select |
| String Operations | 0 | 0.0 |  |
| Reflection Calls | 93 | 10.3 | Many Columns Select |

## PERFORMANCE METRICS ANALYSIS

### Execution Performance
| Metric | Min | Max | Average | 95th Percentile |
|--------|-----|-----|---------|----------------|
| Execution Time (ms) | 2.0 | 11.0 | 4.7 | 11.0 |
| Memory Usage (KB) | -5168.5 | 407.9 | -363.6 | 407.9 |

### Performance vs Complexity Correlation

**Top 10 Most Complex Queries:**
- Many Columns Select: Complexity 104, Time 11ms
- Deep Nested CASE: Complexity 24, Time 3ms
- Nested CASE: Complexity 10, Time 3ms
- Simple CASE: Complexity 6, Time 2ms
- Simple Where: Complexity 5, Time 2ms
- Simple Order: Complexity 5, Time 4ms
- Complex Where: Complexity 5, Time 5ms
- Simple Select: Complexity 4, Time 10ms
- Basic Math: Complexity 4, Time 2ms

## CODE PATTERN ANALYSIS

### Identified Performance Anti-Patterns

**Reflection Usage**: 9 queries use reflection
- Average reflection calls per query: 10.3
- Heavy reflection usage in: Many Columns Select

**Object Allocations**: 9 queries create objects
- Average allocations per query: 19.4
- Heavy allocation patterns in: Many Columns Select

**LINQ Operations**: 8 queries use LINQ
- Average LINQ operations per query: 1.0


## OPTIMIZATION RECOMMENDATIONS

### Small Changes (Low Risk, Quick Wins)

1. **String Builder Usage**: Replace string concatenation with StringBuilder
   - Impact: 0 queries could benefit
   - Risk: Low - direct replacement
   - Effort: 1-2 days

2. **Constant String Interning**: Cache frequently used strings
   - Impact: Reduce memory allocations by 10-15%
   - Risk: Low - additive change
   - Effort: 1 day

3. **Null Check Optimization**: Use pattern matching for null checks
   - Impact: Slight performance improvement in conditional logic
   - Risk: Low - syntactic change
   - Effort: 0.5 days

4. **Reduce Reflection Usage**: Cache MethodInfo/PropertyInfo objects
   - Impact: 1 queries with heavy reflection usage
   - Risk: Low - caching strategy
   - Effort: 2-3 days

### Medium Changes (Moderate Risk, Good Impact)

1. **Object Pooling**: Implement pooling for frequently allocated objects
   - Impact: 1 queries with heavy allocations
   - Risk: Medium - requires lifecycle management
   - Effort: 1-2 weeks

2. **Expression Tree Compilation**: Pre-compile common lambda expressions
   - Impact: 0 queries with complex lambda usage
   - Risk: Medium - changes compilation pipeline
   - Effort: 1-2 weeks

3. **Code Generation Templates**: Use templates for common patterns
   - Impact: Reduce generated code size by 20-30%
   - Risk: Medium - affects code generation logic
   - Effort: 2-3 weeks

4. **LINQ Operation Optimization**: Replace LINQ chains with optimized loops
   - Impact: 15-25% performance improvement in data processing
   - Risk: Medium - changes query execution patterns
   - Effort: 2 weeks

### Large Changes (High Risk, High Impact)

1. **Vectorization Support**: Implement SIMD operations for numeric processing
   - Impact: 2-4x performance improvement for numeric-heavy queries
   - Risk: High - platform-specific optimizations
   - Effort: 2-3 months

2. **Parallel Execution Engine**: Automatic parallelization of query operations
   - Impact: 1.5-3x performance improvement on multi-core systems
   - Risk: High - concurrency and thread safety concerns
   - Effort: 3-4 months

3. **Native Code Generation**: Compile to native code instead of IL
   - Impact: 20-40% performance improvement, reduced startup time
   - Risk: High - platform dependencies, debugging complexity
   - Effort: 4-6 months

4. **Query Plan Optimization**: Implement cost-based query optimization
   - Impact: 30-60% improvement for complex queries
   - Risk: High - fundamental changes to execution model
   - Effort: 6-12 months


## APPENDIX A: DETAILED ANALYSIS RESULTS

### Simple Select

**Original Query:**
```sql
SELECT City, Population FROM #test.Entities()
```

**Code Metrics:**
- Lines: 53
- Methods: 2
- Complexity: 4
- LINQ Operations: 1
- Object Allocations: 14

**Performance:**
- Execution Time: 10ms
- Memory Usage: 184.0KB
- Row Count: 100

**Generated Code (first 20 lines):**
```csharp
namespace Query.Compiled_0
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Musoq.Plugins;
    using Musoq.Schema;
    using Musoq.Evaluator;
    using Musoq.Parser.Nodes.From;
    using Musoq.Parser.Nodes;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using System.Dynamic;

    public class CompiledQuery : BaseOperations, IRunnable
    {
        private Table[] _tableResults = new Table[0];
... (truncated)
```

---

### Simple Where

**Original Query:**
```sql
SELECT City FROM #test.Entities() WHERE Population > 1000000
```

**Code Metrics:**
- Lines: 57
- Methods: 2
- Complexity: 5
- LINQ Operations: 1
- Object Allocations: 13

**Performance:**
- Execution Time: 2ms
- Memory Usage: 168.0KB
- Row Count: 80

**Generated Code (first 20 lines):**
```csharp
namespace Query.Compiled_2
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Musoq.Plugins;
    using Musoq.Schema;
    using Musoq.Evaluator;
    using Musoq.Parser.Nodes.From;
    using Musoq.Parser.Nodes;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using System.Dynamic;

    public class CompiledQuery : BaseOperations, IRunnable
    {
        private Table[] _tableResults = new Table[0];
... (truncated)
```

---

### Simple Order

**Original Query:**
```sql
SELECT City, Population FROM #test.Entities() ORDER BY Population DESC
```

**Code Metrics:**
- Lines: 46
- Methods: 2
- Complexity: 5
- LINQ Operations: 0
- Object Allocations: 14

**Performance:**
- Execution Time: 4ms
- Memory Usage: 160.0KB
- Row Count: 100

**Generated Code (first 20 lines):**
```csharp
namespace Query.Compiled_4
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Musoq.Plugins;
    using Musoq.Schema;
    using Musoq.Evaluator;
    using Musoq.Parser.Nodes.From;
    using Musoq.Parser.Nodes;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using System.Dynamic;

    public class CompiledQuery : BaseOperations, IRunnable
    {
        private Table[] _tableResults = new Table[0];
... (truncated)
```

---

### Basic Math

**Original Query:**
```sql
SELECT City, Population * 2 AS DoublePopulation FROM #test.Entities()
```

**Code Metrics:**
- Lines: 53
- Methods: 2
- Complexity: 4
- LINQ Operations: 1
- Object Allocations: 14

**Performance:**
- Execution Time: 2ms
- Memory Usage: 176.0KB
- Row Count: 100

**Generated Code (first 20 lines):**
```csharp
namespace Query.Compiled_6
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Musoq.Plugins;
    using Musoq.Schema;
    using Musoq.Evaluator;
    using Musoq.Parser.Nodes.From;
    using Musoq.Parser.Nodes;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using System.Dynamic;

    public class CompiledQuery : BaseOperations, IRunnable
    {
        private Table[] _tableResults = new Table[0];
... (truncated)
```

---

### Simple CASE

**Original Query:**
```sql
SELECT City, 
                           CASE WHEN Population > 1000000 THEN 'Large' ELSE 'Small' END AS Size 
                         FROM #test.Entities()
```

**Code Metrics:**
- Lines: 65
- Methods: 3
- Complexity: 6
- LINQ Operations: 1
- Object Allocations: 14

**Performance:**
- Execution Time: 2ms
- Memory Usage: 165.0KB
- Row Count: 100

**Generated Code (first 20 lines):**
```csharp
namespace Query.Compiled_8
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Musoq.Plugins;
    using Musoq.Schema;
    using Musoq.Evaluator;
    using Musoq.Parser.Nodes.From;
    using Musoq.Parser.Nodes;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using System.Dynamic;
    using Musoq.Schema.DataSources;

    public class CompiledQuery : BaseOperations, IRunnable
    {
... (truncated)
```

---

## APPENDIX B: FAILED ANALYSES

### Simple Count
**Error**: Method COUNT with argument types System.Object[] cannot be resolved
**Query**:
```sql
SELECT COUNT(*) FROM #test.Entities()
```

### String Operations
**Error**: Method CONCAT with argument types System.String, System.String, System.String cannot be resolved
**Query**:
```sql
SELECT CONCAT(City, ' - ', Country) AS FullName FROM #test.Entities()
```

### Group By with Count
**Error**: Method COUNT with argument types System.Object[] cannot be resolved
**Query**:
```sql
SELECT Country, COUNT(*) AS CityCount FROM #test.Entities() GROUP BY Country
```

### Group By with Sum
**Error**: Method SUM with argument types System.Decimal cannot be resolved
**Query**:
```sql
SELECT Country, SUM(Population) AS TotalPopulation FROM #test.Entities() GROUP BY Country
```

### Having Clause
**Error**: Method COUNT with argument types System.Object[] cannot be resolved
**Query**:
```sql
SELECT Country, COUNT(*) AS CityCount 
                            FROM #test.Entities() 
                            GROUP BY Country 
                            HAVING COUNT(*) > 5
```

### Multiple Aggregations
**Error**: Method COUNT with argument types System.Object[] cannot be resolved
**Query**:
```sql
SELECT Country, 
                                     COUNT(*) AS CityCount,
                                     SUM(Population) AS TotalPop,
                                     AVG(Population) AS AvgPop,
                                     MAX(Population) AS MaxPop,
                                     MIN(Population) AS MinPop
                                   FROM #test.Entities() 
                                   GROUP BY Country
```

### Multiple CTEs
**Error**: Method COUNT with argument types System.Object[] cannot be resolved
**Query**:
```sql
WITH LargeCities AS (
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
                           SELECT * FROM CountryStats ORDER BY LargeCityCount DESC
```

### Window Functions
**Error**: Failed to parse SQL query: Expected token is From but received Identifier.
**Query**:
```sql
SELECT City, Country, Population,
                                ROW_NUMBER() OVER (PARTITION BY Country ORDER BY Population DESC) AS Rank,
                                SUM(Population) OVER (PARTITION BY Country) AS CountryTotal,
                                LAG(Population) OVER (PARTITION BY Country ORDER BY Population) AS PrevPop
                              FROM #test.Entities()
```

### Complex Aggregation
**Error**: Failed to parse SQL query: Expected token is RightParenthesis but received Identifier.
**Query**:
```sql
SELECT Country,
                                   COUNT(DISTINCT City) AS UniqueCities,
                                   PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY Population) AS MedianPop,
                                   STRING_AGG(City, ', ') AS CityList
                                 FROM #test.Entities()
                                 GROUP BY Country
                                 HAVING COUNT(*) >= 3
```

### Heavy String Processing
**Error**: Failed to parse SQL query: Expected token is RightParenthesis but received As.
**Query**:
```sql
SELECT 
                                       UPPER(LEFT(City, 3)) + '_' + 
                                       LOWER(RIGHT(Country, 2)) + '_' + 
                                       CAST(LEN(City) AS VARCHAR) + '_' +
                                       REPLACE(REPLACE(City, ' ', '_'), '-', '_') AS ProcessedName,
                                       SUBSTRING(City, 1, CHARINDEX(' ', City + ' ') - 1) AS FirstWord,
                                       REVERSE(Country) AS ReversedCountry
                                     FROM #test.Entities()
```

### Multiple Operations
**Error**: Method ABS with argument types System.Decimal cannot be resolved
**Query**:
```sql
SELECT City,
                                   ABS(Population - 1000000) AS PopDiff,
                                   SQRT(Population) AS SqrtPop,
                                   LOG(Population + 1) AS LogPop,
                                   POWER(Population / 1000000.0, 2) AS PopSquared,
                                   ROUND(Population / 1000000.0, 2) AS PopInMillions
                                 FROM #test.Entities()
                                 WHERE Population > 0
```

### Many Aggregations
**Error**: Failed to parse SQL query: Expected token is Else but received End.
**Query**:
```sql
SELECT Country, COUNT(CASE WHEN Population > 0 THEN 1 END) AS Count0, SUM(CASE WHEN Population > 0 THEN Population ELSE 0 END) AS Sum0, COUNT(CASE WHEN Population > 50000 THEN 1 END) AS Count1, SUM(CASE WHEN Population > 50000 THEN Population ELSE 0 END) AS Sum1, COUNT(CASE WHEN Population > 100000 THEN 1 END) AS Count2, SUM(CASE WHEN Population > 100000 THEN Population ELSE 0 END) AS Sum2, COUNT(CASE WHEN Population > 150000 THEN 1 END) AS Count3, SUM(CASE WHEN Population > 150000 THEN Population ELSE 0 END) AS Sum3, COUNT(CASE WHEN Population > 200000 THEN 1 END) AS Count4, SUM(CASE WHEN Population > 200000 THEN Population ELSE 0 END) AS Sum4, COUNT(CASE WHEN Population > 250000 THEN 1 END) AS Count5, SUM(CASE WHEN Population > 250000 THEN Population ELSE 0 END) AS Sum5, COUNT(CASE WHEN Population > 300000 THEN 1 END) AS Count6, SUM(CASE WHEN Population > 300000 THEN Population ELSE 0 END) AS Sum6, COUNT(CASE WHEN Population > 350000 THEN 1 END) AS Count7, SUM(CASE WHEN Population > 350000 THEN Population ELSE 0 END) AS Sum7, COUNT(CASE WHEN Population > 400000 THEN 1 END) AS Count8, SUM(CASE WHEN Population > 400000 THEN Population ELSE 0 END) AS Sum8, COUNT(CASE WHEN Population > 450000 THEN 1 END) AS Count9, SUM(CASE WHEN Population > 450000 THEN Population ELSE 0 END) AS Sum9, COUNT(CASE WHEN Population > 500000 THEN 1 END) AS Count10, SUM(CASE WHEN Population > 500000 THEN Population ELSE 0 END) AS Sum10, COUNT(CASE WHEN Population > 550000 THEN 1 END) AS Count11, SUM(CASE WHEN Population > 550000 THEN Population ELSE 0 END) AS Sum11, COUNT(CASE WHEN Population > 600000 THEN 1 END) AS Count12, SUM(CASE WHEN Population > 600000 THEN Population ELSE 0 END) AS Sum12, COUNT(CASE WHEN Population > 650000 THEN 1 END) AS Count13, SUM(CASE WHEN Population > 650000 THEN Population ELSE 0 END) AS Sum13, COUNT(CASE WHEN Population > 700000 THEN 1 END) AS Count14, SUM(CASE WHEN Population > 700000 THEN Population ELSE 0 END) AS Sum14, COUNT(CASE WHEN Population > 750000 THEN 1 END) AS Count15, SUM(CASE WHEN Population > 750000 THEN Population ELSE 0 END) AS Sum15, COUNT(CASE WHEN Population > 800000 THEN 1 END) AS Count16, SUM(CASE WHEN Population > 800000 THEN Population ELSE 0 END) AS Sum16, COUNT(CASE WHEN Population > 850000 THEN 1 END) AS Count17, SUM(CASE WHEN Population > 850000 THEN Population ELSE 0 END) AS Sum17, COUNT(CASE WHEN Population > 900000 THEN 1 END) AS Count18, SUM(CASE WHEN Population > 900000 THEN Population ELSE 0 END) AS Sum18, COUNT(CASE WHEN Population > 950000 THEN 1 END) AS Count19, SUM(CASE WHEN Population > 950000 THEN Population ELSE 0 END) AS Sum19 FROM #test.Entities() GROUP BY Country
```

### Complex String Operations
**Error**: Failed to parse SQL query: Expected token is RightParenthesis but received As.
**Query**:
```sql
SELECT City, CONCAT(
                UPPER(SUBSTRING(City, 1, 1)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 2)), 
                '_', 
                CAST(0 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_0'), '-', '_0')
            ) AS StringOp0, CONCAT(
                UPPER(SUBSTRING(City, 1, 2)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 3)), 
                '_', 
                CAST(1 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_1'), '-', '_1')
            ) AS StringOp1, CONCAT(
                UPPER(SUBSTRING(City, 1, 3)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 4)), 
                '_', 
                CAST(2 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_2'), '-', '_2')
            ) AS StringOp2, CONCAT(
                UPPER(SUBSTRING(City, 1, 4)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 5)), 
                '_', 
                CAST(3 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_3'), '-', '_3')
            ) AS StringOp3, CONCAT(
                UPPER(SUBSTRING(City, 1, 5)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 6)), 
                '_', 
                CAST(4 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_4'), '-', '_4')
            ) AS StringOp4, CONCAT(
                UPPER(SUBSTRING(City, 1, 6)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 7)), 
                '_', 
                CAST(5 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_5'), '-', '_5')
            ) AS StringOp5, CONCAT(
                UPPER(SUBSTRING(City, 1, 7)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 8)), 
                '_', 
                CAST(6 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_6'), '-', '_6')
            ) AS StringOp6, CONCAT(
                UPPER(SUBSTRING(City, 1, 8)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 9)), 
                '_', 
                CAST(7 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_7'), '-', '_7')
            ) AS StringOp7, CONCAT(
                UPPER(SUBSTRING(City, 1, 9)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 10)), 
                '_', 
                CAST(8 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_8'), '-', '_8')
            ) AS StringOp8, CONCAT(
                UPPER(SUBSTRING(City, 1, 10)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 11)), 
                '_', 
                CAST(9 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_9'), '-', '_9')
            ) AS StringOp9, CONCAT(
                UPPER(SUBSTRING(City, 1, 11)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 12)), 
                '_', 
                CAST(10 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_10'), '-', '_10')
            ) AS StringOp10, CONCAT(
                UPPER(SUBSTRING(City, 1, 12)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 13)), 
                '_', 
                CAST(11 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_11'), '-', '_11')
            ) AS StringOp11, CONCAT(
                UPPER(SUBSTRING(City, 1, 13)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 14)), 
                '_', 
                CAST(12 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_12'), '-', '_12')
            ) AS StringOp12, CONCAT(
                UPPER(SUBSTRING(City, 1, 14)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 15)), 
                '_', 
                CAST(13 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_13'), '-', '_13')
            ) AS StringOp13, CONCAT(
                UPPER(SUBSTRING(City, 1, 15)), 
                '_', 
                LOWER(SUBSTRING(Country, 1, 16)), 
                '_', 
                CAST(14 AS VARCHAR),
                '_',
                REPLACE(REPLACE(City, ' ', '_14'), '-', '_14')
            ) AS StringOp14 FROM #test.Entities()
```

### Multiple CTEs Chain
**Error**: Failed to parse SQL query: Expected token is From but received Identifier.
**Query**:
```sql
WITH CTE0 AS (
                    SELECT City, Country, Population, 
                           ROW_NUMBER() OVER (ORDER BY Population) AS Rank0
                    FROM #test.Entities() 
                    WHERE Population > 0
                ), CTE1 AS (
                    SELECT City, Country, Population, Rank0,
                           Population * 2 AS AdjustedPop1,
                           RANK() OVER (PARTITION BY Country ORDER BY Population) AS Rank1
                    FROM CTE0
                    WHERE Rank0 <= 15
                ), CTE2 AS (
                    SELECT City, Country, Population, Rank1,
                           Population * 3 AS AdjustedPop2,
                           RANK() OVER (PARTITION BY Country ORDER BY Population) AS Rank2
                    FROM CTE1
                    WHERE Rank1 <= 20
                ), CTE3 AS (
                    SELECT City, Country, Population, Rank2,
                           Population * 4 AS AdjustedPop3,
                           RANK() OVER (PARTITION BY Country ORDER BY Population) AS Rank3
                    FROM CTE2
                    WHERE Rank2 <= 25
                ), CTE4 AS (
                    SELECT City, Country, Population, Rank3,
                           Population * 5 AS AdjustedPop4,
                           RANK() OVER (PARTITION BY Country ORDER BY Population) AS Rank4
                    FROM CTE3
                    WHERE Rank3 <= 30
                ) 
                  SELECT * FROM CTE4 
                  ORDER BY AdjustedPop4 DESC
```

