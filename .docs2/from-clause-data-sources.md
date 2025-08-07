# FROM Clause and Data Sources

The `FROM` clause specifies the data source for your query. Musoq's power lies in its ability to query diverse data sources using a unified schema-based syntax.

## Basic FROM Syntax

```sql
select columns
from #schema.method(parameters)
```

The FROM clause in Musoq follows the pattern `#schema.method()` where:
- `#schema` identifies the data source type
- `method` specifies how to access the data
- `parameters` provide configuration for the data source

## Core Data Sources

### File System Sources

#### Files
Query files in directories with flexible filtering:

```sql
-- Query files in a directory (non-recursive)
select Name, Length, Extension
from #os.files('/path/to/directory', false)

-- Query files recursively through subdirectories
select Name, Length, Extension, Directory
from #os.files('/path/to/directory', true)
```

#### Directories
Access directory information:

```sql
-- List directories
select Name, CreationTime, LastWriteTime
from #os.directories('/path')

-- Recursive directory listing
select Name, FullName, Parent
from #os.directories('/path', true)
```

### Git Repository Sources

#### Repository Overview
Access Git repository metadata:

```sql
-- Get repository information
select *
from #git.repository('/path/to/repo')
```

#### Commits
Query commit history with rich metadata:

```sql
-- Access all commits
select c.Sha, c.Author.Name, c.Author.Email, c.Message, c.Date
from #git.repository('/repo') r
cross apply r.Commits c

-- Filter commits by date range
select c.Sha, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date >= '2024-01-01'
```

#### Branches and Tags
Explore repository structure:

```sql
-- List all branches
select b.Name, b.IsRemote, b.Tip.Sha
from #git.repository('/repo') r
cross apply r.Branches b

-- List all tags
select t.Name, t.Target.Sha, t.Message
from #git.repository('/repo') r
cross apply r.Tags t
```

### Code Analysis Sources

#### C# Solution Analysis
Analyze .NET code structure:

```sql
-- Analyze entire solution
select p.Name as ProjectName, p.Language
from #csharp.solution('/path/to/solution.sln') s
cross apply s.Projects p

-- Dive into code structure
select 
    c.Name as ClassName,
    m.Name as MethodName,
    m.LinesOfCode,
    m.CyclomaticComplexity
from #csharp.solution('/project.sln') s
cross apply s.Projects p
cross apply p.Documents d
cross apply d.Classes c
cross apply c.Methods m
```

### Database Sources

#### Direct Database Queries
Connect to external databases:

```sql
-- Query PostgreSQL
select *
from #postgres.query('connection_string', 'SELECT * FROM users')

-- Query SQLite
select *
from #sqlite.query('/path/to/db.sqlite', 'SELECT * FROM products')
```

### AI and Machine Learning Sources

#### OpenAI Integration
Extract structured data using AI:

```sql
-- Analyze text with AI
select *
from #openai.query('your-api-key', 'model-name', 'Your prompt here')

-- Process images with vision models
select *
from #openai.vision('your-api-key', '/path/to/image.jpg', 'Describe this image')
```

## Data Source Parameters

### Path Parameters
Most data sources accept path parameters:

```sql
-- Absolute paths
from #os.files('/home/user/documents', true)

-- Relative paths (from current working directory)
from #os.files('./src', true)

-- Windows paths
from #os.files('C:\\Users\\Username\\Documents', false)
```

### Boolean Flags
Control data source behavior:

```sql
-- Recursive vs non-recursive
from #os.files('/path', true)   -- Include subdirectories
from #os.files('/path', false)  -- Current directory only

-- Include hidden files
from #os.files('/path', true, true)   -- Third parameter for hidden files
```

### Connection Strings
For database sources:

```sql
-- PostgreSQL connection
from #postgres.query(
    'Host=localhost;Database=mydb;Username=user;Password=pass',
    'SELECT * FROM table'
)

-- SQLite file
from #sqlite.query('/database/file.db', 'SELECT * FROM users')
```

## Advanced FROM Patterns

### Multiple Data Sources with Joins

Join data from different sources:

```sql
-- Join files with Git commits
select 
    f.Name as FileName,
    c.Author.Name as LastModifiedBy,
    c.Date as LastCommitDate
from #os.files('/repo/src', true) f
inner join #git.repository('/repo') r on 1 = 1
cross apply r.Commits c
where c.Message like '%' + f.Name + '%'
```

### Subqueries in FROM

Use subqueries as data sources:

```sql
-- Query derived data
select 
    Extension,
    TotalFiles,
    TotalSize
from (
    select 
        Extension,
        Count(*) as TotalFiles,
        Sum(Length) as TotalSize
    from #os.files('/data', true)
    group by Extension
) as FileStats
where TotalFiles > 10
```

### Common Table Expressions (CTEs)

Define reusable data sources:

```sql
-- Use CTE for complex data preparation
with LargeFiles as (
    select Name, Length, Extension
    from #os.files('/workspace', true)
    where Length > 1000000
),
FileStats as (
    select 
        Extension,
        Count(*) as FileCount,
        Avg(Length) as AvgSize
    from LargeFiles
    group by Extension
)
select * from FileStats
order by FileCount desc
```

## Schema Discovery

### DESC Command
Explore available schemas and methods:

```sql
-- List all available schemas
desc schemas

-- Explore a specific schema
desc #os

-- Get detailed information about a method
desc #os.files
```

### Dynamic Schema Exploration
Query schema metadata programmatically:

```sql
-- Find available data sources
select SchemaName, MethodName, Description
from #schema.methods()
where SchemaName like '%git%'
```

## Data Source Coupling

### Table Definitions
Define custom table structures:

```sql
-- Define a custom table structure
table PersonTable {
    Name 'System.String',
    Age 'System.Int32',
    Email 'System.String'
};

-- Couple with a data source
couple #csv.file('/data/people.csv') with table PersonTable as People;

-- Query the coupled data
select Name, Age
from People
where Age > 25
```

## Performance Considerations

### Efficient Data Source Usage

**Choose appropriate recursion levels:**
```sql
-- Efficient: Only when needed
from #os.files('/specific/path', false)

-- Less efficient: Unnecessary recursion
from #os.files('/', true)
```

**Filter early in complex sources:**
```sql
-- Efficient: Filter at source level when possible
select c.Sha, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
where c.Author.Email = 'user@example.com'
```

**Use appropriate batch sizes:**
```sql
-- Process large datasets in chunks
select Name, Length
from #os.files('/large/directory', true)
order by Length desc
take 1000
```

## Error Handling

### Common FROM Clause Errors

**Invalid paths:**
```sql
-- This will fail if path doesn't exist
from #os.files('/nonexistent/path', true)
```

**Missing schema:**
```sql
-- This will fail if schema is not available
from #unknown.source()
```

**Incorrect parameters:**
```sql
-- This will fail with wrong parameter count
from #os.files()  -- Missing required parameters
```

### Defensive Patterns
```sql
-- Check if directory exists before querying
select Name, Length
from #os.files('/possibly/missing/path', true)
where Directory is not null
```

## Best Practices

### Data Source Selection
- **Start specific**: Begin with narrow data sources and expand as needed
- **Use appropriate recursion**: Only recurse when you need subdirectory data
- **Leverage schema capabilities**: Use built-in filtering and selection when available

### Query Organization
- **Consistent aliasing**: Use clear, consistent aliases for data sources
- **Logical grouping**: Group related data sources in complex queries
- **Documentation**: Comment complex data source configurations

### Performance Optimization
- **Minimize data retrieval**: Select only necessary columns early
- **Use efficient joins**: Prefer cross apply over complex joins when appropriate
- **Batch processing**: Use TAKE and SKIP for large datasets

## Common Patterns

### File Analysis Pattern
```sql
-- Standard file system analysis
select 
    Name,
    Extension,
    Length,
    Directory,
    CreationTime
from #os.files('/project', true)
where Extension in ('.cs', '.js', '.py')
```

### Git Repository Analysis Pattern
```sql
-- Comprehensive Git analysis
select 
    c.Sha,
    c.Author.Name,
    c.Message,
    Count(*) over() as TotalCommits
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date >= DateAdd('month', -6, GetDate())
```

### Multi-Source Integration Pattern
```sql
-- Combine multiple data sources
select 
    'File' as SourceType,
    f.Name as Name,
    f.Length as Size
from #os.files('/data', true) f
union all
select 
    'Commit' as SourceType,
    c.Sha as Name,
    Len(c.Message) as Size
from #git.repository('/repo') r
cross apply r.Commits c
```

The FROM clause is where Musoq's versatility shines. Master these data source patterns to unlock powerful analysis capabilities across your entire development ecosystem.