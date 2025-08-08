# API Usage and Examples

## Core API Overview

Musoq provides several entry points for different use cases. This document covers the main APIs and usage patterns.

## Primary API Entry Points

### 1. InstanceCreator - Main API

The `InstanceCreator` class provides the primary interface for compiling and executing queries.

```csharp
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;

// Basic query compilation and execution
var query = "SELECT Name, Count(*) FROM #schema.source() GROUP BY Name";
var schemaProvider = new MySchemaProvider();
var loggerResolver = new LoggerResolver();

var compiledQuery = InstanceCreator.CompileForExecution(
    query,
    Guid.NewGuid().ToString(),
    schemaProvider,
    loggerResolver);

var results = compiledQuery.Run();
```

### 2. Analysis API

For analyzing queries without execution:

```csharp
var buildItems = InstanceCreator.CreateForAnalyze(
    query,
    Guid.NewGuid().ToString(),
    schemaProvider,
    loggerResolver);

// Access generated C# code
var generatedCode = buildItems.BuildedCode;

// Access AST
var rootNode = buildItems.RawQuery;

// Access query metadata
var queryInformation = buildItems.QueryInformation;
```

## Schema Provider Implementation

### Basic Schema Provider

```csharp
public class CustomSchemaProvider : ISchemaProvider
{
    private readonly Dictionary<string, ISchema> _schemas = new();

    public CustomSchemaProvider()
    {
        RegisterDefaultSchemas();
    }

    public void RegisterSchema(string name, ISchema schema)
    {
        _schemas[name.ToLowerInvariant()] = schema;
    }

    public ISchema GetSchema(string name)
    {
        return _schemas.TryGetValue(name.ToLowerInvariant(), out var schema) ? schema : null;
    }

    private void RegisterDefaultSchemas()
    {
        RegisterSchema("system", new SystemSchema());
        RegisterSchema("files", new FileSystemSchema());
        RegisterSchema("memory", new InMemorySchema());
    }
}
```

### Multi-Schema Provider with Dynamic Loading

```csharp
public class DynamicSchemaProvider : ISchemaProvider
{
    private readonly Dictionary<string, ISchema> _schemas = new();
    private readonly List<Assembly> _pluginAssemblies = new();

    public void LoadSchemasFromAssembly(Assembly assembly)
    {
        _pluginAssemblies.Add(assembly);
        
        var schemaTypes = assembly.GetTypes()
            .Where(t => typeof(ISchema).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var schemaType in schemaTypes)
        {
            var schema = (ISchema)Activator.CreateInstance(schemaType);
            RegisterSchema(schema.Name, schema);
        }
    }

    public void LoadSchemasFromDirectory(string pluginDirectory)
    {
        var assemblies = Directory.GetFiles(pluginDirectory, "*.dll")
            .Select(Assembly.LoadFrom);

        foreach (var assembly in assemblies)
        {
            LoadSchemasFromAssembly(assembly);
        }
    }
}
```

## Data Source Implementation Examples

### 1. In-Memory Data Source

```csharp
public class InMemorySchema : SchemaBase
{
    private readonly Dictionary<string, IEnumerable<object>> _dataSources = new();

    public InMemorySchema() : base("memory", new MethodsAggregator())
    {
        AddSource<InMemoryRowSource>("table");
    }

    public void AddDataSource(string name, IEnumerable<object> data)
    {
        _dataSources[name.ToLowerInvariant()] = data;
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        var tableName = parameters.Length > 0 ? parameters[0]?.ToString() : name;
        
        if (_dataSources.TryGetValue(tableName.ToLowerInvariant(), out var data))
        {
            return new InMemoryRowSource(data, runtimeContext);
        }

        throw new ArgumentException($"Data source '{tableName}' not found");
    }
}

public class InMemoryRowSource : RowSourceBase<dynamic>
{
    private readonly IEnumerable<object> _data;

    public InMemoryRowSource(IEnumerable<object> data, RuntimeContext context) 
        : base(context)
    {
        _data = data;
    }

    public override IEnumerable<dynamic> GetRows(CancellationToken cancellationToken)
    {
        foreach (var item in _data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}
```

### 2. REST API Data Source

```csharp
public class RestApiSchema : SchemaBase
{
    private static readonly HttpClient _httpClient = new();

    public RestApiSchema() : base("api", new MethodsAggregator())
    {
        AddSource<RestApiRowSource>("endpoint");
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        if (name == "endpoint" && parameters.Length > 0)
        {
            var url = parameters[0]?.ToString();
            var headers = parameters.Length > 1 ? parameters[1] as Dictionary<string, string> : null;
            
            return new RestApiRowSource(url, headers, runtimeContext);
        }

        throw new ArgumentException($"Invalid parameters for '{name}'");
    }
}

public class RestApiRowSource : RowSourceBase<dynamic>
{
    private readonly string _url;
    private readonly Dictionary<string, string> _headers;

    public RestApiRowSource(string url, Dictionary<string, string> headers, RuntimeContext context) 
        : base(context)
    {
        _url = url;
        _headers = headers ?? new Dictionary<string, string>();
    }

    public override IEnumerable<dynamic> GetRows(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _url);
        
        foreach (var header in _headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        var response = _httpClient.SendAsync(request, cancellationToken).Result;
        response.EnsureSuccessStatusCode();

        var jsonContent = response.Content.ReadAsStringAsync().Result;
        var jsonDocument = JsonDocument.Parse(jsonContent);

        if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in jsonDocument.RootElement.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return JsonElementToDynamic(element);
            }
        }
        else
        {
            yield return JsonElementToDynamic(jsonDocument.RootElement);
        }
    }

    private dynamic JsonElementToDynamic(JsonElement element)
    {
        var expando = new ExpandoObject() as IDictionary<string, object>;

        foreach (var property in element.EnumerateObject())
        {
            expando[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => property.Value.ToString()
            };
        }

        return expando;
    }
}
```

### 3. Database Data Source

```csharp
public class DatabaseSchema : SchemaBase
{
    public DatabaseSchema() : base("db", new MethodsAggregator())
    {
        AddSource<DatabaseRowSource>("query");
        AddSource<DatabaseRowSource>("table");
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return name switch
        {
            "query" => new DatabaseRowSource(
                parameters[0]?.ToString(), // connection string
                parameters[1]?.ToString(), // SQL query
                parameters.Skip(2).ToArray(), // parameters
                runtimeContext),
            "table" => new DatabaseRowSource(
                parameters[0]?.ToString(), // connection string
                $"SELECT * FROM {parameters[1]}", // table name
                new object[0],
                runtimeContext),
            _ => throw new ArgumentException($"Unknown source: {name}")
        };
    }
}

public class DatabaseRowSource : RowSourceBase<dynamic>
{
    private readonly string _connectionString;
    private readonly string _query;
    private readonly object[] _parameters;

    public DatabaseRowSource(string connectionString, string query, object[] parameters, RuntimeContext context) 
        : base(context)
    {
        _connectionString = connectionString;
        _query = query;
        _parameters = parameters;
    }

    public override IEnumerable<dynamic> GetRows(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = new SqlCommand(_query, connection);
        
        for (int i = 0; i < _parameters.Length; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", _parameters[i] ?? DBNull.Value);
        }

        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var row = new ExpandoObject() as IDictionary<string, object>;
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var fieldName = reader.GetName(i);
                var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[fieldName] = fieldValue;
            }
            
            yield return row;
        }
    }
}
```

## Advanced Usage Patterns

### 1. Query Parameterization

```csharp
public class ParameterizedQueryExample
{
    public Table ExecuteParameterizedQuery(string userInput, DateTime fromDate, DateTime toDate)
    {
        // Use parameters to prevent injection and improve reusability
        var query = @"
            SELECT Name, CreatedAt, Status
            FROM #system.events()
            WHERE Name LIKE @userPattern
              AND CreatedAt BETWEEN @fromDate AND @toDate
            ORDER BY CreatedAt DESC";

        var schemaProvider = new CustomSchemaProvider();
        
        // Create a parameterized context
        var context = new RuntimeContext();
        context.SetParameter("userPattern", $"%{userInput}%");
        context.SetParameter("fromDate", fromDate);
        context.SetParameter("toDate", toDate);

        var compiledQuery = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            new LoggerResolver());

        return compiledQuery.Run();
    }
}
```

### 2. Dynamic Schema Registration

```csharp
public class DynamicQueryEngine
{
    private readonly DynamicSchemaProvider _schemaProvider;

    public DynamicQueryEngine()
    {
        _schemaProvider = new DynamicSchemaProvider();
    }

    public void RegisterDataSource<T>(string schemaName, string sourceName, IEnumerable<T> data)
    {
        var schema = new GenericInMemorySchema<T>(schemaName, sourceName, data);
        _schemaProvider.RegisterSchema(schemaName, schema);
    }

    public Table ExecuteQuery(string query)
    {
        var compiledQuery = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            _schemaProvider,
            new LoggerResolver());

        return compiledQuery.Run();
    }
}

// Usage
var engine = new DynamicQueryEngine();

// Register different data sources
engine.RegisterDataSource("sales", "orders", salesData);
engine.RegisterDataSource("inventory", "products", productData);
engine.RegisterDataSource("users", "customers", customerData);

// Execute complex queries across multiple sources
var results = engine.ExecuteQuery(@"
    SELECT 
        o.OrderId,
        c.CustomerName,
        p.ProductName,
        o.Quantity * p.Price as TotalValue
    FROM #sales.orders() o
    INNER JOIN #users.customers() c ON o.CustomerId = c.Id
    INNER JOIN #inventory.products() p ON o.ProductId = p.Id
    WHERE o.OrderDate >= '2023-01-01'
    ORDER BY TotalValue DESC");
```

### 3. Streaming Large Datasets

```csharp
public class StreamingQueryProcessor
{
    public async IAsyncEnumerable<object[]> ExecuteStreamingQuery(
        string query, 
        ISchemaProvider schemaProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var compiledQuery = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            new LoggerResolver());

        // Execute query and stream results
        var table = await compiledQuery.RunAsync(cancellationToken);
        
        foreach (var row in table)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
        }
    }
}

// Usage
var processor = new StreamingQueryProcessor();

await foreach (var row in processor.ExecuteStreamingQuery(
    "SELECT * FROM #large.dataset() WHERE Status = 'Active'",
    schemaProvider,
    cancellationToken))
{
    // Process row without loading entire result set into memory
    ProcessRow(row);
}
```

### 4. Query Analysis and Optimization

```csharp
public class QueryAnalyzer
{
    public QueryAnalysisResult AnalyzeQuery(string query, ISchemaProvider schemaProvider)
    {
        var buildItems = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            new LoggerResolver());

        var result = new QueryAnalysisResult
        {
            IsValid = buildItems.IsValid,
            GeneratedCode = buildItems.BuildedCode,
            UsedSchemas = ExtractUsedSchemas(buildItems),
            EstimatedComplexity = CalculateComplexity(buildItems),
            OptimizationSuggestions = GenerateOptimizationSuggestions(buildItems)
        };

        return result;
    }

    private string[] ExtractUsedSchemas(BuildItems buildItems)
    {
        return buildItems.QueryInformation.Values
            .Select(qi => qi.FromNode)
            .OfType<SchemaFromNode>()
            .Select(sfn => sfn.Schema)
            .Distinct()
            .ToArray();
    }

    private int CalculateComplexity(BuildItems buildItems)
    {
        // Analyze AST for complexity indicators
        var complexity = 0;
        
        // Count JOINs
        complexity += CountJoins(buildItems.RawQuery) * 2;
        
        // Count subqueries
        complexity += CountSubqueries(buildItems.RawQuery) * 3;
        
        // Count aggregations
        complexity += CountAggregations(buildItems.RawQuery);
        
        return complexity;
    }

    private string[] GenerateOptimizationSuggestions(BuildItems buildItems)
    {
        var suggestions = new List<string>();
        
        // Analyze for common optimization opportunities
        if (HasUnnecessarySelectStar(buildItems.RawQuery))
        {
            suggestions.Add("Consider selecting only required columns instead of SELECT *");
        }

        if (HasMissingWhereClause(buildItems.RawQuery))
        {
            suggestions.Add("Consider adding WHERE clause to filter data early");
        }

        return suggestions.ToArray();
    }
}

public class QueryAnalysisResult
{
    public bool IsValid { get; set; }
    public string GeneratedCode { get; set; }
    public string[] UsedSchemas { get; set; }
    public int EstimatedComplexity { get; set; }
    public string[] OptimizationSuggestions { get; set; }
}
```

### 5. Error Handling and Logging

```csharp
public class RobustQueryExecutor
{
    private readonly ILogger _logger;
    private readonly ISchemaProvider _schemaProvider;

    public RobustQueryExecutor(ISchemaProvider schemaProvider, ILogger logger)
    {
        _schemaProvider = schemaProvider;
        _logger = logger;
    }

    public async Task<QueryExecutionResult> ExecuteQuerySafelyAsync(
        string query, 
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        var result = new QueryExecutionResult { Query = query };
        
        try
        {
            _logger.LogInformation("Starting query execution: {Query}", query);
            
            var stopwatch = Stopwatch.StartNew();
            
            // Set up timeout
            using var timeoutCts = timeout != default 
                ? new CancellationTokenSource(timeout) 
                : new CancellationTokenSource();
            
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            // Compile query
            var compiledQuery = InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                _schemaProvider,
                new LoggerResolver());

            result.CompilationTimeMs = stopwatch.ElapsedMilliseconds;
            
            // Execute query
            var table = await compiledQuery.RunAsync(combinedCts.Token);
            
            stopwatch.Stop();
            
            result.IsSuccess = true;
            result.Results = table;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds - result.CompilationTimeMs;
            result.TotalTimeMs = stopwatch.ElapsedMilliseconds;
            result.RowCount = table.Count;
            
            _logger.LogInformation(
                "Query executed successfully in {TotalTime}ms, returned {RowCount} rows",
                result.TotalTimeMs,
                result.RowCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "Query execution was cancelled";
            _logger.LogWarning("Query execution was cancelled: {Query}", query);
        }
        catch (OperationCanceledException) when (timeout != default)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"Query execution timed out after {timeout.TotalSeconds} seconds";
            _logger.LogWarning("Query execution timed out: {Query}", query);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            _logger.LogError(ex, "Query execution failed: {Query}", query);
        }

        return result;
    }
}

public class QueryExecutionResult
{
    public string Query { get; set; }
    public bool IsSuccess { get; set; }
    public Table Results { get; set; }
    public string ErrorMessage { get; set; }
    public Exception Exception { get; set; }
    public long CompilationTimeMs { get; set; }
    public long ExecutionTimeMs { get; set; }
    public long TotalTimeMs { get; set; }
    public int RowCount { get; set; }
}
```

## Testing API Usage

### Unit Testing with Mock Data

```csharp
[TestClass]
public class ApiUsageTests
{
    [TestMethod]
    public async Task Should_Execute_Query_With_Mock_Data()
    {
        // Arrange
        var testData = new[]
        {
            new { Name = "Alice", Age = 30, Department = "Engineering" },
            new { Name = "Bob", Age = 25, Department = "Marketing" },
            new { Name = "Charlie", Age = 35, Department = "Engineering" }
        };

        var schemaProvider = new CustomSchemaProvider();
        var schema = new InMemorySchema();
        schema.AddDataSource("employees", testData);
        schemaProvider.RegisterSchema("test", schema);

        var query = @"
            SELECT Department, COUNT(*) as EmployeeCount, AVG(Age) as AverageAge
            FROM #test.table('employees')
            GROUP BY Department
            ORDER BY EmployeeCount DESC";

        // Act
        var executor = new RobustQueryExecutor(schemaProvider, Mock.Of<ILogger>());
        var result = await executor.ExecuteQuerySafelyAsync(query);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.Results.Count); // 2 departments
        Assert.IsTrue(result.Results.Columns.Any(c => c.ColumnName == "Department"));
        Assert.IsTrue(result.Results.Columns.Any(c => c.ColumnName == "EmployeeCount"));
    }
}
```

### 6. Window Functions Usage

Musoq supports window functions for analytical queries that operate across sets of rows. Here are practical examples:

```csharp
public class WindowFunctionsExamples
{
    public void BasicWindowFunctions()
    {
        var data = new[]
        {
            new { Country = "USA", Population = 331000000, GDP = 21.43m },
            new { Country = "China", Population = 1439000000, GDP = 14.34m },
            new { Country = "Japan", Population = 125800000, GDP = 4.94m },
            new { Country = "Germany", Population = 83240000, GDP = 3.86m }
        };

        var query = @"
            SELECT Country, Population, GDP,
                   RowNumber() as RowNum,
                   Rank() as PopulationRank,
                   DenseRank() as GDPRank
            FROM #data.entities()
            ORDER BY Population DESC";

        var result = ExecuteQuery(query, data);
        
        // Results will show sequential numbering and ranking
        // Row 1: China, 1439000000, 14.34, 1, 1, 1
        // Row 2: USA, 331000000, 21.43, 2, 2, 2
        // Row 3: Japan, 125800000, 4.94, 3, 3, 3
        // Row 4: Germany, 83240000, 3.86, 4, 4, 4
    }

    public void LagLeadFunctions()
    {
        var data = new[]
        {
            new { Quarter = "Q1", Revenue = 1000000m },
            new { Quarter = "Q2", Revenue = 1200000m },
            new { Quarter = "Q3", Revenue = 1100000m },
            new { Quarter = "Q4", Revenue = 1350000m }
        };

        var query = @"
            SELECT Quarter, Revenue,
                   Lag(Revenue, 1, 0) as PreviousRevenue,
                   Lead(Revenue, 1, 0) as NextRevenue,
                   Lag(Quarter, 1, 'START') as PrevQuarter,
                   Lead(Quarter, 1, 'END') as NextQuarter
            FROM #data.entities()
            ORDER BY Quarter";

        var result = ExecuteQuery(query, data);
        
        // Note: Current implementation returns default values
        // Future enhancement will provide actual lag/lead functionality
    }

    public void CombinedAnalytics()
    {
        var query = @"
            SELECT Region, Product, Sales,
                   RowNumber() as Position,
                   Rank() as SalesRank,
                   Lag(Product, 1, 'N/A') as PreviousProduct
            FROM #sales.data()
            ORDER BY Sales DESC";

        // Combines window functions with ordering for comprehensive analytics
    }

    private Table ExecuteQuery<T>(string query, IEnumerable<T> data)
    {
        var schemaProvider = new BasicSchemaProvider<T>(new Dictionary<string, IEnumerable<T>>
        {
            ["entities"] = data
        });

        var compiledQuery = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            new LoggerResolver());

        return compiledQuery.Run();
    }
}

public class WindowFunctionsAdvanced
{
    public void WindowVsGroupByComparison()
    {
        // Window functions preserve all rows, unlike GROUP BY
        
        var windowQuery = @"
            SELECT Department, Employee, Salary,
                   RowNumber() as EmployeeNumber,
                   Rank() as SalaryRank
            FROM #hr.employees()
            ORDER BY Salary DESC";
        
        var groupByQuery = @"
            SELECT Department, COUNT(*) as EmployeeCount, AVG(Salary) as AvgSalary
            FROM #hr.employees()
            GROUP BY Department";

        // Window query: Returns all employee rows with rankings
        // GROUP BY query: Returns summary rows per department
    }

    public void WindowFunctionPatterns()
    {
        // Pattern 1: Sequential numbering with gaps for ties
        var rankingQuery = @"
            SELECT Name, Score, Rank() as Position
            FROM #competition.results()
            ORDER BY Score DESC";

        // Pattern 2: Dense ranking without gaps
        var denseRankingQuery = @"
            SELECT Name, Score, DenseRank() as Position
            FROM #competition.results()
            ORDER BY Score DESC";

        // Pattern 3: Previous/next value access with defaults
        var lagLeadQuery = @"
            SELECT Date, Price,
                   Lag(Price, 1, Price) as PrevPrice,
                   Lead(Price, 1, Price) as NextPrice
            FROM #stock.prices()
            ORDER BY Date";
    }
}
```

### Window Functions Implementation Notes

**Current State:**
- `RowNumber()` - Fully functional sequential numbering
- `Rank()` - Basic implementation (behaves like RowNumber)
- `DenseRank()` - Basic implementation (behaves like RowNumber)
- `Lag<T>()` - Returns default value (placeholder implementation)
- `Lead<T>()` - Returns default value (placeholder implementation)

**Architecture Integration:**
- Uses `[InjectQueryStats]` for row context access
- Compatible with ORDER BY clauses for meaningful results
- Preserves all rows (unlike GROUP BY aggregation)
- Foundation exists for advanced OVER clause syntax

**Future Enhancements:**
- OVER clause with PARTITION BY and ORDER BY
- Window frame specifications (ROWS BETWEEN)
- Aggregate window functions (SUM() OVER, COUNT() OVER)
- True lag/lead implementation with offset access

This comprehensive API guide covers the main usage patterns and provides practical examples for integrating Musoq into applications.