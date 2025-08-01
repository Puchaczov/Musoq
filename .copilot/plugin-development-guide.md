# Plugin Development Guide

## Overview

Musoq's plugin system is designed for extensibility, allowing developers to add new data sources and function libraries. This guide provides comprehensive information for developing plugins.

## Data Source Plugin Development

### Plugin Architecture

```
ISchema (Interface) → SchemaBase (Base Class) → YourSchema (Implementation)
                                                       ↓
RowSource (Data Iterator) → YourRowSource (Implementation)
                                    ↓
Entity Classes → Your Data Models
```

### Step-by-Step Plugin Creation

#### 1. Define Your Data Entity

```csharp
public class FileEntity
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string Extension { get; set; }
    public bool IsDirectory { get; set; }
}
```

#### 2. Create Row Source Implementation

```csharp
public class FileSystemRowSource : RowSourceBase<FileEntity>
{
    private readonly string _path;
    private readonly bool _recursive;

    public FileSystemRowSource(string path, bool recursive, RuntimeContext context) 
        : base(context)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        _recursive = recursive;
    }

    public override IEnumerable<FileEntity> GetRows(CancellationToken cancellationToken)
    {
        var searchOption = _recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        
        foreach (var filePath in Directory.EnumerateFileSystemEntries(_path, "*", searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var info = new FileInfo(filePath);
            
            yield return new FileEntity
            {
                Name = info.Name,
                FullPath = info.FullName,
                Size = info.Exists ? info.Length : 0,
                LastModified = info.LastWriteTime,
                Extension = info.Extension,
                IsDirectory = Directory.Exists(filePath)
            };
        }
    }
}
```

#### 3. Create Schema Table

```csharp
public class FileSystemTable : ISchemaTable
{
    public string Name => "files";
    
    public ISchemaColumn[] Columns { get; } = 
    {
        new SchemaColumn("Name", typeof(string)),
        new SchemaColumn("FullPath", typeof(string)),
        new SchemaColumn("Size", typeof(long)),
        new SchemaColumn("LastModified", typeof(DateTime)),
        new SchemaColumn("Extension", typeof(string)),
        new SchemaColumn("IsDirectory", typeof(bool))
    };
}
```

#### 4. Implement Schema Class

```csharp
public class FileSystemSchema : SchemaBase
{
    public FileSystemSchema() : base("fs", new MethodsAggregator())
    {
        // Register data sources
        AddSource<FileSystemRowSource>("files");
        
        // Register tables
        AddTable<FileSystemTable>("files");
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return name.ToLowerInvariant() switch
        {
            "files" => new FileSystemTable(),
            _ => throw new NotSupportedException($"Table '{name}' is not supported.")
        };
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return name.ToLowerInvariant() switch
        {
            "files" => new FileSystemRowSource(
                parameters.Length > 0 ? parameters[0]?.ToString() : ".",
                parameters.Length > 1 && Convert.ToBoolean(parameters[1]),
                runtimeContext),
            _ => throw new NotSupportedException($"Source '{name}' is not supported.")
        };
    }
}
```

### Advanced Row Source Patterns

#### Chunked Data Processing

```csharp
public class ChunkedFileSystemRowSource : ChunkedSource<FileEntity>
{
    private readonly string _path;
    private readonly bool _recursive;

    public ChunkedFileSystemRowSource(string path, bool recursive, RuntimeContext context) 
        : base(context)
    {
        _path = path;
        _recursive = recursive;
    }

    protected override IEnumerable<IEnumerable<FileEntity>> GetChunks(CancellationToken cancellationToken)
    {
        const int chunkSize = 1000;
        var chunk = new List<FileEntity>(chunkSize);
        
        foreach (var entity in GetAllFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            chunk.Add(entity);
            
            if (chunk.Count >= chunkSize)
            {
                yield return chunk;
                chunk = new List<FileEntity>(chunkSize);
            }
        }
        
        if (chunk.Count > 0)
            yield return chunk;
    }

    private IEnumerable<FileEntity> GetAllFiles()
    {
        // Implementation here
    }
}
```

#### Parameterized Data Sources

```csharp
public class DatabaseRowSource : RowSourceBase<DatabaseEntity>
{
    private readonly string _connectionString;
    private readonly string _query;
    private readonly object[] _parameters;

    public DatabaseRowSource(
        string connectionString, 
        string query, 
        RuntimeContext context, 
        params object[] parameters) 
        : base(context)
    {
        _connectionString = connectionString;
        _query = query;
        _parameters = parameters;
    }

    public override IEnumerable<DatabaseEntity> GetRows(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        
        using var command = new SqlCommand(_query, connection);
        
        // Add parameters
        for (int i = 0; i < _parameters.Length; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", _parameters[i]);
        }

        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            yield return new DatabaseEntity
            {
                // Map reader columns to entity properties
            };
        }
    }
}
```

### Dynamic Schema Support

#### Runtime Column Discovery

```csharp
public class DynamicRowSource : RowSourceBase<dynamic>
{
    private readonly Func<IEnumerable<dynamic>> _dataProvider;
    private ISchemaColumn[] _columns;

    public DynamicRowSource(Func<IEnumerable<dynamic>> dataProvider, RuntimeContext context) 
        : base(context)
    {
        _dataProvider = dataProvider;
    }

    public override IEnumerable<dynamic> GetRows(CancellationToken cancellationToken)
    {
        var data = _dataProvider();
        
        // Discover columns from first row
        if (_columns == null)
        {
            var firstRow = data.FirstOrDefault();
            if (firstRow != null)
            {
                _columns = DiscoverColumns(firstRow);
            }
        }

        return data;
    }

    private ISchemaColumn[] DiscoverColumns(dynamic row)
    {
        var columns = new List<ISchemaColumn>();
        
        if (row is IDictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                var type = kvp.Value?.GetType() ?? typeof(object);
                columns.Add(new SchemaColumn(kvp.Key, type));
            }
        }

        return columns.ToArray();
    }
}
```

## Function Library Development

### Creating Custom Functions

#### Basic Function Implementation

```csharp
public class TextProcessingLibrary : LibraryBase
{
    [BindableMethod]
    public string RemoveWhitespace(string input)
    {
        return string.IsNullOrEmpty(input) ? input : Regex.Replace(input, @"\s+", "");
    }

    [BindableMethod]
    public string ExtractEmails(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
        var matches = Regex.Matches(text, emailPattern);
        
        return string.Join(", ", matches.Cast<Match>().Select(m => m.Value));
    }

    [BindableMethod]
    public int WordCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, 
                         StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
```

#### Generic Functions

```csharp
public class GenericLibrary : LibraryBase
{
    [BindableMethod]
    public T Coalesce<T>(params T[] values)
    {
        return values.FirstOrDefault(v => v != null && !v.Equals(default(T)));
    }

    [BindableMethod]
    public bool IsNull<T>(T value)
    {
        return value == null || value.Equals(default(T));
    }

    [BindableMethod]
    public T IfNull<T>(T value, T replacement)
    {
        return IsNull(value) ? replacement : value;
    }
}
```

#### Aggregation Functions

```csharp
public class StatisticsLibrary : LibraryBase
{
    [BindableMethod]
    public decimal Median(IEnumerable<decimal> values)
    {
        var sorted = values.Where(v => !IsNull(v)).OrderBy(v => v).ToArray();
        
        if (sorted.Length == 0)
            return 0;

        if (sorted.Length % 2 == 0)
        {
            return (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2;
        }
        else
        {
            return sorted[sorted.Length / 2];
        }
    }

    [BindableMethod]
    public decimal StandardDeviation(IEnumerable<decimal> values)
    {
        var array = values.Where(v => !IsNull(v)).ToArray();
        
        if (array.Length <= 1)
            return 0;

        var mean = array.Average();
        var variance = array.Sum(v => (v - mean) * (v - mean)) / (array.Length - 1);
        
        return (decimal)Math.Sqrt((double)variance);
    }
}
```

### Complex Function Examples

#### JSON Processing Functions

```csharp
public class JsonLibrary : LibraryBase
{
    [BindableMethod]
    public string JsonExtract(string json, string path)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var pathSegments = path.Split('.');
            
            JsonElement current = doc.RootElement;
            
            foreach (var segment in pathSegments)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var property))
                {
                    current = property;
                }
                else if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index))
                {
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        current = current[index];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            
            return current.ToString();
        }
        catch
        {
            return null;
        }
    }

    [BindableMethod]
    public bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

#### HTTP/Web Functions

```csharp
public class WebLibrary : LibraryBase
{
    private static readonly HttpClient _httpClient = new();

    [BindableMethod]
    public string HttpGet(string url)
    {
        try
        {
            var response = _httpClient.GetStringAsync(url).Result;
            return response;
        }
        catch
        {
            return null;
        }
    }

    [BindableMethod]
    public string UrlEncode(string input)
    {
        return Uri.EscapeDataString(input ?? string.Empty);
    }

    [BindableMethod]
    public string UrlDecode(string input)
    {
        return Uri.UnescapeDataString(input ?? string.Empty);
    }
}
```

## Plugin Registration and Deployment

### Registration in Schema Provider

```csharp
public class CustomSchemaProvider : ISchemaProvider
{
    private readonly Dictionary<string, ISchema> _schemas = new();

    public CustomSchemaProvider()
    {
        // Register built-in schemas
        RegisterSchema("fs", new FileSystemSchema());
        RegisterSchema("web", new WebSchema());
        RegisterSchema("json", new JsonSchema());
    }

    public void RegisterSchema(string name, ISchema schema)
    {
        _schemas[name.ToLowerInvariant()] = schema;
    }

    public ISchema GetSchema(string name)
    {
        return _schemas.TryGetValue(name.ToLowerInvariant(), out var schema) ? schema : null;
    }
}
```

### Function Library Registration

```csharp
public class ExtendedMethodsAggregator : MethodsAggregator
{
    public ExtendedMethodsAggregator()
    {
        // Register custom libraries
        RegisterLibrary(new TextProcessingLibrary());
        RegisterLibrary(new GenericLibrary());
        RegisterLibrary(new StatisticsLibrary());
        RegisterLibrary(new JsonLibrary());
        RegisterLibrary(new WebLibrary());
    }
}
```

## Testing Plugin Development

### Unit Testing Data Sources

```csharp
[TestClass]
public class FileSystemRowSourceTests
{
    [TestMethod]
    public void Should_Return_Files_From_Directory()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var context = new RuntimeContext();
        var rowSource = new FileSystemRowSource(tempDir, false, context);

        // Act
        var files = rowSource.GetRows(CancellationToken.None).ToList();

        // Assert
        Assert.IsTrue(files.Count > 0);
        Assert.IsTrue(files.All(f => !string.IsNullOrEmpty(f.Name)));
    }
}
```

### Integration Testing with Query Engine

```csharp
[TestClass]
public class FileSystemSchemaIntegrationTests : BasicEntityTestBase
{
    [TestMethod]
    public void Should_Execute_File_Query()
    {
        // Arrange
        var schemaProvider = new CustomSchemaProvider();
        var query = "SELECT Name, Size FROM #fs.files('.', false) WHERE Extension = '.txt'";

        // Act
        var compiledQuery = CreateAndRunVirtualMachine(query, schemaProvider: schemaProvider);
        var results = compiledQuery.Run();

        // Assert
        Assert.IsNotNull(results);
        Assert.IsTrue(results.Columns.Any(c => c.ColumnName == "Name"));
        Assert.IsTrue(results.Columns.Any(c => c.ColumnName == "Size"));
    }
}
```

### Performance Testing

```csharp
[TestMethod]
public void Should_Handle_Large_Dataset_Efficiently()
{
    var stopwatch = Stopwatch.StartNew();
    
    var rowSource = new FileSystemRowSource("/large/directory", true, new RuntimeContext());
    var count = rowSource.GetRows(CancellationToken.None).Count();
    
    stopwatch.Stop();
    
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, "Query should complete within 5 seconds");
    Assert.IsTrue(count > 0, "Should return some results");
}
```

## Best Practices

### Performance Optimization

1. **Lazy Evaluation**: Always use `yield return` for data enumeration
2. **Cancellation Support**: Check cancellation tokens frequently
3. **Resource Management**: Implement proper disposal patterns
4. **Memory Efficiency**: Avoid loading entire datasets into memory

### Error Handling

1. **Graceful Degradation**: Handle errors without crashing
2. **Meaningful Exceptions**: Provide detailed error messages
3. **Logging Integration**: Use the provided logging infrastructure
4. **Validation**: Validate parameters early and thoroughly

### Code Quality

1. **Type Safety**: Use strongly-typed entities where possible
2. **Immutability**: Prefer immutable data structures
3. **Thread Safety**: Ensure thread-safe implementations
4. **Testing**: Write comprehensive unit and integration tests

This guide provides the foundation for building robust, efficient plugins for the Musoq query engine.