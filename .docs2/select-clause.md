# SELECT Clause

The `SELECT` clause defines what data to retrieve from your query. In Musoq, the SELECT clause supports standard SQL functionality plus powerful extensions for working with diverse data sources.

## Basic SELECT Syntax

```sql
select column1, column2, column3
from #schema.datasource()
```

### Simple Column Selection

Select specific columns by name:

```sql
-- Select specific columns from files
select Name, Length, Extension
from #os.files('/path/to/directory', true)
```

### SELECT All Columns

Use the asterisk (`*`) to select all available columns:

```sql
-- Select all columns from Git commits
select *
from #git.commits('/path/to/repo')
```

## Column Aliases

Assign custom names to columns using the `AS` keyword:

```sql
-- Create meaningful column names
select 
    Name as FileName,
    Length as SizeInBytes,
    Length / 1024 as SizeInKB
from #os.files('/docs', true)
```

The `AS` keyword is optional:

```sql
-- Equivalent syntax without AS
select 
    Name FileName,
    Length SizeInBytes,
    Length / 1024 SizeInKB
from #os.files('/docs', true)
```

## Expressions in SELECT

### Arithmetic Expressions

Perform calculations directly in the SELECT clause:

```sql
-- Calculate file sizes in different units
select 
    Name,
    Length,
    Length / 1024 as KiloBytes,
    Length / 1024 / 1024 as MegaBytes,
    Round(Length / 1024.0 / 1024.0, 2) as MegaBytesRounded
from #os.files('/data', true)
where Length > 1000000
```

### String Operations

Manipulate text data with string functions:

```sql
-- Extract and format file information
select 
    Upper(Name) as UpperName,
    Substring(Name, 1, 10) as FirstTenChars,
    Concat(Name, ' (', Length, ' bytes)') as NameWithSize
from #os.files('/docs', false)
```

### Conditional Expressions

Use CASE expressions for conditional logic:

```sql
-- Categorize files by size
select 
    Name,
    Length,
    case 
        when Length < 1024 then 'Small'
        when Length < 1048576 then 'Medium'
        else 'Large'
    end as SizeCategory
from #os.files('/downloads', true)
```

## Complex Property Access

### Nested Property Navigation

Access nested properties using dot notation:

```sql
-- Access nested Git commit properties
select 
    c.Sha,
    c.Author.Name as AuthorName,
    c.Author.Email as AuthorEmail,
    c.Committer.Date as CommitDate
from #git.repository('/repo') r
cross apply r.Commits c
```

### Self Property Access

Access the entire object using the `Self` keyword:

```sql
-- Work with complete objects
select 
    Self.Name,
    Self.Length,
    Self as CompleteFileInfo
from #os.files('/temp', false)
```

## Function Calls in SELECT

### Built-in Functions

Use Musoq's extensive function library:

```sql
-- Date and mathematical functions
select 
    Name,
    CreationTime,
    DateDiff('day', CreationTime, GetDate()) as DaysOld,
    Abs(Length - 1024) as DistanceFromKB,
    Power(Length / 1024, 2) as KBSquared
from #os.files('/logs', true)
```

### Aggregate Functions

Apply aggregation functions (typically used with GROUP BY):

```sql
-- Summary statistics by file extension
select 
    Extension,
    Count(*) as FileCount,
    Sum(Length) as TotalSize,
    Avg(Length) as AverageSize,
    Max(Length) as LargestFile,
    Min(Length) as SmallestFile
from #os.files('/projects', true)
group by Extension
```

## Advanced SELECT Features

### Type Casting

Convert between data types explicitly:

```sql
-- Explicit type conversions
select 
    Name,
    Cast(Length as 'System.String') as LengthAsString,
    Cast(CreationTime as 'System.String') as CreationTimeAsString
from #os.files('/data', false)
```

### NULL Handling

Handle null values gracefully:

```sql
-- Provide defaults for null values
select 
    Name,
    Coalesce(Extension, 'no-extension') as FileExtension,
    case when Length is null then 0 else Length end as SafeLength
from #os.files('/mixed', true)
```

### Complex Calculations

Combine multiple operations:

```sql
-- Complex file analysis
select 
    Name,
    Extension,
    Length,
    Round(
        case 
            when Length = 0 then 0
            else Length / (1024.0 * 1024.0)
        end, 
        3
    ) as MegaBytes,
    case 
        when Extension in ('.jpg', '.png', '.gif') then 'Image'
        when Extension in ('.txt', '.md', '.doc') then 'Document'
        when Extension in ('.exe', '.dll', '.so') then 'Executable'
        else 'Other'
    end as FileCategory
from #os.files('/workspace', true)
```

## SELECT with Multiple Data Sources

When working with joins or multiple data sources, qualify column names:

```sql
-- Qualified column names in joins
select 
    f.Name as FileName,
    f.Length as FileSize,
    d.Name as DirectoryName
from #os.files('/path', false) f
inner join #os.directories('/path') d on f.Directory = d.FullName
```

## Best Practices

### Column Naming
- Use descriptive aliases for calculated columns
- Maintain consistent naming conventions
- Avoid reserved keywords as column names

### Performance Considerations
- Select only the columns you need (avoid `SELECT *` in production)
- Use appropriate data types for calculations
- Consider the impact of complex expressions on query performance

### Readability
- Format complex SELECT clauses with proper indentation
- Group related columns together
- Use meaningful aliases that describe the data

## Common Patterns

### File Analysis Pattern
```sql
select 
    Name as FileName,
    Extension as FileType,
    Round(Length / 1024.0, 2) as SizeKB,
    CreationTime as Created,
    LastWriteTime as Modified
from #os.files('/analysis', true)
```

### Git Analysis Pattern
```sql
select 
    c.Sha as CommitHash,
    c.Author.Name as Developer,
    c.Message as CommitMessage,
    DateDiff('day', c.Date, GetDate()) as DaysAgo
from #git.repository('/repo') r
cross apply r.Commits c
```

### Code Analysis Pattern
```sql
select 
    c.Name as ClassName,
    m.Name as MethodName,
    m.LinesOfCode as LOC,
    m.CyclomaticComplexity as Complexity
from #csharp.solution('/project.sln') s
cross apply s.Projects p
cross apply p.Documents d
cross apply d.Classes c
cross apply c.Methods m
```

The SELECT clause is the foundation of data retrieval in Musoq. Master these patterns to effectively extract and transform data from any source.