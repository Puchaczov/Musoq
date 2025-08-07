# ORDER BY Clause and Sorting

The `ORDER BY` clause sorts query results by one or more columns or expressions. Musoq supports comprehensive sorting capabilities including multi-column sorting, custom sort orders, and sorting by computed expressions.

## Basic ORDER BY Syntax

```sql
select columns
from data_source
order by column [asc|desc]
```

Results are sorted in ascending order by default. Use `desc` for descending order.

## Single Column Sorting

### Ascending Order (Default)

```sql
-- Sort files by name (A to Z)
select Name, Length
from #os.files('/documents', true)
order by Name

-- Explicit ascending order
select Name, Length
from #os.files('/documents', true)
order by Name asc
```

### Descending Order

```sql
-- Sort files by size (largest first)
select Name, Length
from #os.files('/downloads', true)
order by Length desc

-- Sort Git commits by date (newest first)
select c.Sha, c.Message, c.Date
from #git.repository('/repo') r
cross apply r.Commits c
order by c.Date desc
```

## Multiple Column Sorting

### Hierarchical Sorting

Sort by multiple columns with different precedence:

```sql
-- Sort by extension first, then by size within each extension
select Name, Extension, Length
from #os.files('/projects', true)
order by Extension, Length desc

-- Sort Git commits by author, then by date
select c.Author.Name, c.Date, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
order by c.Author.Name, c.Date desc
```

### Mixed Sort Directions

Combine ascending and descending sorts:

```sql
-- Group by extension (A-Z), then largest files first within each group
select Name, Extension, Length
from #os.files('/data', true)
order by Extension asc, Length desc

-- Sort commits by author name (A-Z), then newest first
select c.Author.Name, c.Date, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
order by c.Author.Name asc, c.Date desc
```

## Sorting by Expressions

### Calculated Values

Sort by computed expressions:

```sql
-- Sort files by size in MB
select Name, Length, Round(Length / 1024.0 / 1024.0, 2) as MegaBytes
from #os.files('/media', true)
order by Length / 1024.0 / 1024.0 desc

-- Sort commits by message length
select c.Sha, c.Message, Len(c.Message) as MessageLength
from #git.repository('/repo') r
cross apply r.Commits c
order by Len(c.Message) desc
```

### String Expressions

Sort by string manipulations:

```sql
-- Sort files by uppercase name (case-insensitive)
select Name, Extension
from #os.files('/mixed-case', true)
order by Upper(Name)

-- Sort by file extension in uppercase
select Name, Extension
from #os.files('/documents', true)
order by Upper(Extension), Name
```

### Date and Time Expressions

Sort by date calculations:

```sql
-- Sort files by days since creation (newest first)
select Name, CreationTime, DateDiff('day', CreationTime, GetDate()) as DaysOld
from #os.files('/logs', true)
order by DateDiff('day', CreationTime, GetDate()) asc

-- Sort commits by day of week, then by time
select c.Sha, c.Date, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
order by DatePart('weekday', c.Date), DatePart('hour', c.Date)
```

## Conditional Sorting

### CASE-Based Sorting

Custom sort orders using CASE expressions:

```sql
-- Custom priority sorting for file types
select Name, Extension
from #os.files('/project', true)
order by 
    case Extension
        when '.cs' then 1      -- C# files first
        when '.js' then 2      -- JavaScript second
        when '.css' then 3     -- CSS third
        when '.html' then 4    -- HTML fourth
        else 5                 -- Everything else last
    end,
    Name

-- Priority sorting for Git commit types
select c.Sha, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
order by 
    case 
        when c.Message like 'fix:%' then 1     -- Bug fixes first
        when c.Message like 'feat:%' then 2    -- Features second
        when c.Message like 'docs:%' then 3    -- Documentation third
        else 4                                 -- Others last
    end,
    c.Date desc
```

### Size-Based Custom Sorting

```sql
-- Sort files by size categories
select Name, Length, 
    case 
        when Length < 1024 then 'Small'
        when Length < 1048576 then 'Medium'
        when Length < 10485760 then 'Large'
        else 'Very Large'
    end as SizeCategory
from #os.files('/data', true)
order by 
    case 
        when Length < 1024 then 1
        when Length < 1048576 then 2
        when Length < 10485760 then 3
        else 4
    end,
    Length desc
```

## Sorting with Aggregations

### GROUP BY with ORDER BY

Sort aggregated results:

```sql
-- File count by extension, sorted by count
select Extension, Count(*) as FileCount
from #os.files('/source', true)
group by Extension
order by Count(*) desc

-- Total size by directory, sorted by size
select Directory, Sum(Length) as TotalSize
from #os.files('/project', true)
group by Directory
order by Sum(Length) desc
```

### Complex Aggregation Sorting

```sql
-- Git contributors sorted by activity
select 
    c.Author.Name,
    Count(*) as CommitCount,
    Max(c.Date) as LastCommit,
    Min(c.Date) as FirstCommit
from #git.repository('/repo') r
cross apply r.Commits c
group by c.Author.Name
order by Count(*) desc, Max(c.Date) desc

-- File analysis by extension with multiple metrics
select 
    Extension,
    Count(*) as FileCount,
    Sum(Length) as TotalSize,
    Avg(Length) as AvgSize,
    Max(Length) as LargestFile
from #os.files('/codebase', true)
group by Extension
order by Sum(Length) desc, Count(*) desc
```

## Advanced Sorting Patterns

### Null Handling in Sorting

```sql
-- Handle null values in sorting
select Name, Extension, Length
from #os.files('/mixed', true)
order by 
    case when Extension is null then 1 else 0 end,  -- Nulls last
    Extension,
    Length desc

-- Sort with null-safe comparisons
select c.Sha, c.Author.Name, c.Author.Email
from #git.repository('/repo') r
cross apply r.Commits c
order by 
    case when c.Author.Email is null then 'zzz' else c.Author.Email end,
    c.Date desc
```

### Nested Property Sorting

Sort by complex object properties:

```sql
-- Sort Git commits by author properties
select c.Sha, c.Author.Name, c.Author.Email, c.Committer.Name
from #git.repository('/repo') r
cross apply r.Commits c
order by c.Author.Name, c.Committer.Name, c.Date desc

-- Sort code methods by complexity metrics
select 
    c.Name as ClassName,
    m.Name as MethodName,
    m.CyclomaticComplexity,
    m.LinesOfCode
from #csharp.solution('/project.sln') s
cross apply s.Projects p
cross apply p.Documents d
cross apply d.Classes c
cross apply c.Methods m
order by m.CyclomaticComplexity desc, m.LinesOfCode desc
```

## Performance Considerations

### Efficient Sorting Strategies

```sql
-- Efficient: Sort by indexed or primary columns
select Name, Length
from #os.files('/data', true)
order by CreationTime desc    -- Efficient for time-based data

-- Less efficient: Complex expressions in ORDER BY
select Name, Length
from #os.files('/data', true)
order by Substring(Name, 1, 5), Length / 1024.0 / 1024.0
```

### Large Dataset Sorting

```sql
-- Use TAKE for large datasets to limit sorting overhead
select Name, Length
from #os.files('/huge-directory', true)
order by Length desc
take 100    -- Only sort enough to get top 100

-- Combine with filtering for better performance
select Name, Length
from #os.files('/large-dataset', true)
where Extension in ('.cs', '.js', '.py')    -- Filter first
order by Length desc
take 50
```

## Sorting with Window Functions

### ROW_NUMBER for Ranking

```sql
-- Rank files by size within each directory
select 
    Directory,
    Name,
    Length,
    Row_Number() over (partition by Directory order by Length desc) as SizeRank
from #os.files('/project', true)
order by Directory, SizeRank
```

## Common Sorting Patterns

### File System Analysis

```sql
-- Top 10 largest files
select Name, Length, Directory
from #os.files('/workspace', true)
order by Length desc
take 10

-- Files grouped by extension, sorted by size
select Extension, Name, Length
from #os.files('/documents', true)
where Extension is not null
order by Extension, Length desc

-- Recently modified files first
select Name, LastWriteTime, Length
from #os.files('/active', true)
order by LastWriteTime desc, Length desc
```

### Git Repository Analysis

```sql
-- Recent commits by all authors
select c.Author.Name, c.Date, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
order by c.Date desc
take 50

-- Contributors by commit count
select 
    c.Author.Name,
    Count(*) as Commits,
    Max(c.Date) as LastCommit
from #git.repository('/repo') r
cross apply r.Commits c
group by c.Author.Name
order by Count(*) desc, Max(c.Date) desc

-- Commits by message type and recency
select c.Sha, c.Message, c.Date, c.Author.Name
from #git.repository('/repo') r
cross apply r.Commits c
order by 
    case 
        when c.Message like 'fix:%' then 1
        when c.Message like 'feat:%' then 2
        else 3
    end,
    c.Date desc
```

### Code Quality Analysis

```sql
-- Methods sorted by complexity
select 
    p.Name as Project,
    c.Name as Class,
    m.Name as Method,
    m.CyclomaticComplexity,
    m.LinesOfCode
from #csharp.solution('/solution.sln') s
cross apply s.Projects p
cross apply p.Documents d
cross apply d.Classes c
cross apply c.Methods m
order by 
    m.CyclomaticComplexity desc,
    m.LinesOfCode desc,
    p.Name,
    c.Name,
    m.Name
```

### Multi-Source Sorting

```sql
-- Combine and sort data from multiple sources
select 
    'File' as Type,
    f.Name as Name,
    f.Length as Size,
    f.CreationTime as Date
from #os.files('/project', true) f
union all
select 
    'Commit' as Type,
    c.Sha as Name,
    Len(c.Message) as Size,
    c.Date as Date
from #git.repository('/project') r
cross apply r.Commits c
order by Date desc, Type, Size desc
```

## Best Practices

### Performance Guidelines
- **Limit sorting scope**: Use WHERE clauses to reduce data before sorting
- **Use TAKE/SKIP**: Limit results when you don't need the entire dataset
- **Avoid complex expressions**: Simple column sorts are more efficient than calculated expressions
- **Consider data types**: Ensure consistent data types for optimal sorting performance

### Readability Guidelines
- **Clear sort logic**: Use meaningful column names or aliases in ORDER BY
- **Consistent direction**: Be explicit about ASC/DESC even when using defaults
- **Group related sorts**: Keep related sorting columns together
- **Document complex sorts**: Comment unusual or business-specific sorting logic

### Design Patterns
- **Primary, secondary, tertiary**: Design hierarchical sorts from most to least important
- **User-friendly defaults**: Sort by the most commonly needed order first
- **Stable sorting**: Use additional columns to ensure consistent ordering for equal values

The ORDER BY clause transforms raw data into meaningful, organized results. Master these sorting patterns to present data in the most useful and insightful way for your analysis needs.