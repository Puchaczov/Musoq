# MUSOQ CODE GENERATION PERFORMANCE ANALYSIS REPORT
=============================================================

## EXECUTIVE SUMMARY

**Total Queries Analyzed**: 29
**Successful Analyses**: 29
**Failed Analyses**: 0

**Key Metrics Averages**:
- Generated Code Lines: 86.3
- Code Complexity Score: 12.6
- Execution Time: 5.2ms
- Memory Usage: -523.1KB

**Areas of Concern**:
- High Complexity Queries: 2
- Slow Execution Queries: 0
- Memory Heavy Queries: 3

## CODE GENERATION METRICS ANALYSIS

### Code Size Distribution
| Metric | Min | Max | Average | Median |
|--------|-----|-----|---------|--------|
| Total Lines | 51.0 | 419.0 | 94.6 | 65.0 |
| Non-Empty Lines | 46.0 | 384.0 | 86.3 | 59.0 |
| Methods | 2.0 | 32.0 | 3.7 | 2.0 |
| Complexity Score | 4.0 | 64.0 | 12.6 | 6.0 |

### Code Patterns Distribution
| Pattern | Total Count | Avg per Query | High Usage Queries |
|---------|-------------|---------------|-------------------|
| Loops | 20 | 0.7 | Group By with Count, Group By with Sum, Having Clause |
| Conditionals | 75 | 2.6 | Many Columns Select, Deep Nested CASE |
| Lambdas | 34 | 1.2 | Multiple CTEs Chain |
| LINQ Operations | 110 | 3.8 | Multiple Aggregations, Complex Grouping, Many Aggregations |
| Object Allocations | 638 | 22.0 | Many Aggregations |
| String Operations | 0 | 0.0 |  |
| Reflection Calls | 231 | 8.0 | Many Columns Select, Many Aggregations, Multiple CTEs Chain |

## PERFORMANCE METRICS ANALYSIS

### Execution Performance
| Metric | Min | Max | Average | 95th Percentile |
|--------|-----|-----|---------|----------------|
| Execution Time (ms) | 2.0 | 23.0 | 5.2 | 14.0 |
| Memory Usage (KB) | -14014.5 | 2659.9 | -523.1 | 1079.1 |

### Performance vs Complexity Correlation

**Top 10 Most Complex Queries:**
- Many Columns Select: Complexity 64, Time 8ms
- Many Aggregations: Complexity 54, Time 10ms
- Complex Grouping: Complexity 21, Time 3ms
- Deep Nested CASE: Complexity 20, Time 4ms
- CTE with Grouping: Complexity 19, Time 3ms
- Multiple Aggregations: Complexity 18, Time 5ms
- Multiple CTEs Chain: Complexity 18, Time 5ms
- Having Clause: Complexity 17, Time 3ms
- Group By with Count: Complexity 14, Time 8ms
- Group By with Sum: Complexity 14, Time 4ms

## CODE PATTERN ANALYSIS

### Identified Performance Anti-Patterns

**Reflection Usage**: 29 queries use reflection
- Average reflection calls per query: 8.0
- Heavy reflection usage in: Multiple Aggregations, RowNumber Function, Simple CTE

**Object Allocations**: 29 queries create objects
- Average allocations per query: 22.0
- Heavy allocation patterns in: Group By with Count, Group By with Sum, Having Clause

**LINQ Operations**: 27 queries use LINQ
- Average LINQ operations per query: 4.1
- Heavy LINQ chaining in: Complex Grouping, Many Aggregations


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
   - Impact: 12 queries with heavy reflection usage
   - Risk: Low - caching strategy
   - Effort: 2-3 days

### Medium Changes (Moderate Risk, Good Impact)

1. **Object Pooling**: Implement pooling for frequently allocated objects
   - Impact: 12 queries with heavy allocations
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
- Execution Time: 11ms
- Memory Usage: 184.0KB
- Row Count: 100

**Generated Code:**
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

        private Table ComputeTable_ko3iko_0_0(ISchemaProvider provider, IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables, IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> queriesInformation, ILogger logger, CancellationToken token)
        {
            var stats = new AmendableQueryStats();
            var ko3ikoScore = new Table("ko3ikoScore", new Column[] { new Column(@"City", typeof(System.String), 0), new Column(@"Population", typeof(System.Decimal), 1) });
            var ko3ikoInferredInfoTable = new ISchemaColumn[] { new Column("City", typeof(System.String), 11), new Column("Country", typeof(System.String), 12), new Column("Population", typeof(System.Decimal), 13) };
            var ko3iko = provider.GetSchema("#test");
            var ko3ikoRows = ko3iko.GetRowSource("Entities", new RuntimeContext(token, ko3ikoInferredInfoTable, positionalEnvironmentVariables[0], queriesInformation["ko3iko:1"], logger), new Object[] { });
            ;
            try
            {
                Parallel.ForEach(ko3ikoRows.Rows, (score) =>
                {
                    token.ThrowIfCancellationRequested();
                    var currentRowStats = stats.IncrementRowNumber();
                    var select = new Object[] { (System.String)((System.String)(score[@"City"])), (System.Decimal)((System.Decimal)(score[@"Population"])) };
                    ko3ikoScore.Add(new ObjectsRow(select, score.Contexts) { });
                });
            }
            catch (AggregateException ex)
            {
                throw ex.InnerExceptions.First();
            }

            return ko3ikoScore;
        }

        public Table Run(CancellationToken token)
        {
            return ComputeTable_ko3iko_0_0(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token);
        }

        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables { get; set; }
        public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation { get; set; }
        public ILogger Logger { get; set; }
    }
}
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
- Memory Usage: 152.0KB
- Row Count: 80

**Generated Code:**
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

        private Table ComputeTable_ko3iko_0_0(ISchemaProvider provider, IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables, IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> queriesInformation, ILogger logger, CancellationToken token)
        {
            var stats = new AmendableQueryStats();
            var ko3ikoScore = new Table("ko3ikoScore", new Column[] { new Column(@"City", typeof(System.String), 0) });
            var ko3ikoInferredInfoTable = new ISchemaColumn[] { new Column("City", typeof(System.String), 11), new Column("Country", typeof(System.String), 12), new Column("Population", typeof(System.Decimal), 13) };
            var ko3iko = provider.GetSchema("#test");
            var ko3ikoRows = ko3iko.GetRowSource("Entities", new RuntimeContext(token, ko3ikoInferredInfoTable, positionalEnvironmentVariables[0], queriesInformation["ko3iko:1"], logger), new Object[] { });
            ;
            try
            {
                Parallel.ForEach(ko3ikoRows.Rows, (score) =>
                {
                    token.ThrowIfCancellationRequested();
                    if (!(((System.Decimal)(score[@"Population"])) > ((int)1000000)))
                    {
                        return;
                    }

                    var currentRowStats = stats.IncrementRowNumber();
                    var select = new Object[] { (System.String)((System.String)(score[@"City"])) };
                    ko3ikoScore.Add(new ObjectsRow(select, score.Contexts) { });
                });
            }
            catch (AggregateException ex)
            {
                throw ex.InnerExceptions.First();
            }

            return ko3ikoScore;
        }

        public Table Run(CancellationToken token)
        {
            return ComputeTable_ko3iko_0_0(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token);
        }

        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables { get; set; }
        public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation { get; set; }
        public ILogger Logger { get; set; }
    }
}
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
- Memory Usage: 151.0KB
- Row Count: 100

**Generated Code:**
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

        private Table ComputeTable_ko3iko_0_0(ISchemaProvider provider, IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables, IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> queriesInformation, ILogger logger, CancellationToken token)
        {
            var stats = new AmendableQueryStats();
            var ko3ikoScore = new Table("ko3ikoScore", new Column[] { new Column(@"City", typeof(System.String), 0), new Column(@"Population", typeof(System.Decimal), 1) });
            var ko3ikoInferredInfoTable = new ISchemaColumn[] { new Column("City", typeof(System.String), 11), new Column("Country", typeof(System.String), 12), new Column("Population", typeof(System.Decimal), 13) };
            var ko3iko = provider.GetSchema("#test");
            var ko3ikoRows = ko3iko.GetRowSource("Entities", new RuntimeContext(token, ko3ikoInferredInfoTable, positionalEnvironmentVariables[0], queriesInformation["ko3iko:1"], logger), new Object[] { });
            ;
            foreach (var score in OrderByDescending(ko3ikoRows, score => (System.Decimal)((System.Decimal)(score[@"Population"]))))
            {
                token.ThrowIfCancellationRequested();
                var currentRowStats = stats.IncrementRowNumber();
                var select = new Object[] { (System.String)((System.String)(score[@"City"])), (System.Decimal)((System.Decimal)(score[@"Population"])) };
                ko3ikoScore.Add(new ObjectsRow(select, score.Contexts) { });
            }

            return ko3ikoScore;
        }

        public Table Run(CancellationToken token)
        {
            return ComputeTable_ko3iko_0_0(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token);
        }

        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables { get; set; }
        public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation { get; set; }
        public ILogger Logger { get; set; }
    }
}
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

**Generated Code:**
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

        private Table ComputeTable_ko3iko_0_0(ISchemaProvider provider, IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables, IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> queriesInformation, ILogger logger, CancellationToken token)
        {
            var stats = new AmendableQueryStats();
            var ko3ikoScore = new Table("ko3ikoScore", new Column[] { new Column(@"City", typeof(System.String), 0), new Column(@"DoublePopulation", typeof(System.Decimal), 1) });
            var ko3ikoInferredInfoTable = new ISchemaColumn[] { new Column("City", typeof(System.String), 11), new Column("Country", typeof(System.String), 12), new Column("Population", typeof(System.Decimal), 13) };
            var ko3iko = provider.GetSchema("#test");
            var ko3ikoRows = ko3iko.GetRowSource("Entities", new RuntimeContext(token, ko3ikoInferredInfoTable, positionalEnvironmentVariables[0], queriesInformation["ko3iko:1"], logger), new Object[] { });
            ;
            try
            {
                Parallel.ForEach(ko3ikoRows.Rows, (score) =>
                {
                    token.ThrowIfCancellationRequested();
                    var currentRowStats = stats.IncrementRowNumber();
                    var select = new Object[] { (System.String)((System.String)(score[@"City"])), (System.Decimal)(((System.Decimal)(score[@"Population"])) * ((int)2)) };
                    ko3ikoScore.Add(new ObjectsRow(select, score.Contexts) { });
                });
            }
            catch (AggregateException ex)
            {
                throw ex.InnerExceptions.First();
            }

            return ko3ikoScore;
        }

        public Table Run(CancellationToken token)
        {
            return ComputeTable_ko3iko_0_0(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token);
        }

        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables { get; set; }
        public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation { get; set; }
        public ILogger Logger { get; set; }
    }
}
```

---

### Array Indexing

**Original Query:**
```sql
SELECT City FROM #test.Entities() WHERE City[0] = 'N'
```

**Code Metrics:**
- Lines: 57
- Methods: 2
- Complexity: 5
- LINQ Operations: 1
- Object Allocations: 13

**Performance:**
- Execution Time: 2ms
- Memory Usage: 160.0KB
- Row Count: 0

**Generated Code:**
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

    public class CompiledQuery : BaseOperations, IRunnable
    {
        private Table[] _tableResults = new Table[0];

        private Table ComputeTable_ko3iko_0_0(ISchemaProvider provider, IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables, IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> queriesInformation, ILogger logger, CancellationToken token)
        {
            var stats = new AmendableQueryStats();
            var ko3ikoScore = new Table("ko3ikoScore", new Column[] { new Column(@"City", typeof(System.String), 0) });
            var ko3ikoInferredInfoTable = new ISchemaColumn[] { new Column("City", typeof(System.String), 11), new Column("Country", typeof(System.String), 12), new Column("Population", typeof(System.Decimal), 13) };
            var ko3iko = provider.GetSchema("#test");
            var ko3ikoRows = ko3iko.GetRowSource("Entities", new RuntimeContext(token, ko3ikoInferredInfoTable, positionalEnvironmentVariables[0], queriesInformation["ko3iko:1"], logger), new Object[] { });
            ;
            try
            {
                Parallel.ForEach(ko3ikoRows.Rows, (score) =>
                {
                    token.ThrowIfCancellationRequested();
                    if (!((SafeArrayAccess.GetStringCharacter((string)(score["City"]), 0)) == ('N')))
                    {
                        return;
                    }

                    var currentRowStats = stats.IncrementRowNumber();
                    var select = new Object[] { (System.String)((System.String)(score[@"City"])) };
                    ko3ikoScore.Add(new ObjectsRow(select, score.Contexts) { });
                });
            }
            catch (AggregateException ex)
            {
                throw ex.InnerExceptions.First();
            }

            return ko3ikoScore;
        }

        public Table Run(CancellationToken token)
        {
            return ComputeTable_ko3iko_0_0(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token);
        }

        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables { get; set; }
        public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation { get; set; }
        public ILogger Logger { get; set; }
    }
}
```

---

