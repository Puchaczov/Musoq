# GROUP BY Clause and Aggregation

The `GROUP BY` clause groups rows with similar values and enables aggregate calculations across those groups. This is essential for statistical analysis, summarization, and extracting insights from your data.

## Basic GROUP BY Syntax

```sql
select grouping_columns, aggregate_functions
from data_source
group by grouping_columns
```

The `GROUP BY` clause creates groups of rows that share the same values in the specified columns, then applies aggregate functions to calculate summary statistics for each group.

## Simple Grouping

### Single Column Grouping

```sql
-- Count files by extension
select Extension, Count(*) as FileCount
from #os.files('/projects', true)
group by Extension

-- Total size by file extension
select Extension, Sum(Length) as TotalSize
from #os.files('/data', true)
group by Extension
```

### Basic Aggregate Functions

| Function | Description | Example |
|----------|-------------|---------|
| `Count(*)` | Number of rows in group | `Count(*) as RowCount` |
| `Count(column)` | Non-null values in column | `Count(Extension) as FilesWithExt` |
| `Sum(column)` | Total of numeric values | `Sum(Length) as TotalBytes` |
| `Avg(column)` | Average of numeric values | `Avg(Length) as AvgFileSize` |
| `Min(column)` | Smallest value in group | `Min(CreationTime) as OldestFile` |
| `Max(column)` | Largest value in group | `Max(Length) as LargestFile` |

## Multiple Column Grouping

### Hierarchical Grouping

Group by multiple columns to create nested categories:

```sql
-- File statistics by directory and extension
select 
    Directory,
    Extension,
    Count(*) as FileCount,
    Sum(Length) as TotalSize,
    Avg(Length) as AvgSize
from #os.files('/workspace', true)
group by Directory, Extension
order by Directory, Extension

-- Git commit analysis by author and month
select 
    c.Author.Name,
    DatePart('year', c.Date) as Year,
    DatePart('month', c.Date) as Month,
    Count(*) as CommitCount,
    Count(distinct c.Sha) as UniqueCommits
from #git.repository('/repo') r
cross apply r.Commits c
group by c.Author.Name, DatePart('year', c.Date), DatePart('month', c.Date)
order by c.Author.Name, Year, Month
```

## Complex Aggregations

### Statistical Analysis

Calculate comprehensive statistics for each group:

```sql
-- Detailed file size analysis by extension
select 
    Extension,
    Count(*) as FileCount,
    Sum(Length) as TotalBytes,
    Avg(Length) as AvgBytes,
    Min(Length) as SmallestFile,
    Max(Length) as LargestFile,
    StdDev(Length) as SizeStdDev,
    Round(Sum(Length) / 1024.0 / 1024.0, 2) as TotalMB
from #os.files('/analysis', true)
where Extension is not null
group by Extension
having Count(*) >= 5    -- Only extensions with 5+ files
order by Sum(Length) desc
```

### Time-Based Aggregations

```sql
-- Git activity by month and author
select 
    DatePart('year', c.Date) as Year,
    DatePart('month', c.Date) as Month,
    c.Author.Name,
    Count(*) as Commits,
    Count(distinct DatePart('day', c.Date)) as ActiveDays,
    Sum(Len(c.Message)) as TotalMessageChars,
    Avg(Len(c.Message)) as AvgMessageLength
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date >= DateAdd('year', -1, GetDate())
group by 
    DatePart('year', c.Date),
    DatePart('month', c.Date),
    c.Author.Name
order by Year desc, Month desc, Commits desc
```

## Grouping by Expressions

### Calculated Grouping Columns

Group by computed values:

```sql
-- Group files by size categories
select 
    case 
        when Length < 1024 then 'Small (< 1KB)'
        when Length < 1048576 then 'Medium (1KB-1MB)'
        when Length < 10485760 then 'Large (1MB-10MB)'
        else 'Very Large (> 10MB)'
    end as SizeCategory,
    Count(*) as FileCount,
    Round(Sum(Length) / 1024.0 / 1024.0, 2) as TotalMB
from #os.files('/data', true)
group by 
    case 
        when Length < 1024 then 'Small (< 1KB)'
        when Length < 1048576 then 'Medium (1KB-1MB)'
        when Length < 10485760 then 'Large (1MB-10MB)'
        else 'Very Large (> 10MB)'
    end
order by 
    case 
        when case 
            when Length < 1024 then 'Small (< 1KB)'
            when Length < 1048576 then 'Medium (1KB-1MB)'
            when Length < 10485760 then 'Large (1MB-10MB)'
            else 'Very Large (> 10MB)'
        end = 'Small (< 1KB)' then 1
        when case 
            when Length < 1024 then 'Small (< 1KB)'
            when Length < 1048576 then 'Medium (1KB-1MB)'
            when Length < 10485760 then 'Large (1MB-10MB)'
            else 'Very Large (> 10MB)'
        end = 'Medium (1KB-1MB)' then 2
        when case 
            when Length < 1024 then 'Small (< 1KB)'
            when Length < 1048576 then 'Medium (1KB-1MB)'
            when Length < 10485760 then 'Large (1MB-10MB)'
            else 'Very Large (> 10MB)'
        end = 'Large (1MB-10MB)' then 3
        else 4
    end
```

### Date-Based Grouping

```sql
-- Group Git commits by day of week
select 
    case DatePart('weekday', c.Date)
        when 1 then 'Sunday'
        when 2 then 'Monday'
        when 3 then 'Tuesday'
        when 4 then 'Wednesday'
        when 5 then 'Thursday'
        when 6 then 'Friday'
        when 7 then 'Saturday'
    end as DayOfWeek,
    Count(*) as CommitCount,
    Count(distinct c.Author.Name) as UniqueAuthors
from #git.repository('/repo') r
cross apply r.Commits c
group by DatePart('weekday', c.Date)
order by DatePart('weekday', c.Date)

-- Group files by creation hour
select 
    DatePart('hour', CreationTime) as Hour,
    Count(*) as FilesCreated,
    Round(Avg(Length), 0) as AvgSize
from #os.files('/logs', true)
where CreationTime >= DateAdd('month', -1, GetDate())
group by DatePart('hour', CreationTime)
order by DatePart('hour', CreationTime)
```

## Advanced Aggregation Functions

### String Aggregations

```sql
-- Concatenate values within groups (conceptual - actual syntax may vary)
select 
    c.Author.Name,
    Count(*) as CommitCount,
    Max(c.Date) as LastCommit,
    Min(c.Date) as FirstCommit
from #git.repository('/repo') r
cross apply r.Commits c
group by c.Author.Name
order by Count(*) desc
```

### Conditional Aggregations

```sql
-- Count different types of files within each directory
select 
    Directory,
    Count(*) as TotalFiles,
    Sum(case when Extension = '.cs' then 1 else 0 end) as CSharpFiles,
    Sum(case when Extension = '.js' then 1 else 0 end) as JavaScriptFiles,
    Sum(case when Extension = '.css' then 1 else 0 end) as CSSFiles,
    Sum(case when Extension in ('.jpg', '.png', '.gif') then 1 else 0 end) as ImageFiles,
    Sum(case when Extension is null then 1 else 0 end) as FilesWithoutExtension
from #os.files('/project', true)
group by Directory
order by Directory
```

### Financial-Style Aggregations

```sql
-- Sum positive and negative values separately (conceptual example)
select 
    c.Author.Name,
    Count(*) as TotalCommits,
    Sum(case when Len(c.Message) > 50 then 1 else 0 end) as DetailedCommits,
    Sum(case when Len(c.Message) <= 50 then 1 else 0 end) as BriefCommits,
    Avg(Len(c.Message)) as AvgMessageLength
from #git.repository('/repo') r
cross apply r.Commits c
group by c.Author.Name
having Count(*) >= 10
order by Count(*) desc
```

## Filtering Groups with HAVING

### Basic HAVING Clause

Filter groups based on aggregate conditions:

```sql
-- Only show extensions with many files
select 
    Extension,
    Count(*) as FileCount,
    Sum(Length) as TotalSize
from #os.files('/large-project', true)
group by Extension
having Count(*) >= 100    -- Only extensions with 100+ files
order by Count(*) desc

-- Active Git contributors only
select 
    c.Author.Name,
    Count(*) as CommitCount,
    Max(c.Date) as LastCommit
from #git.repository('/repo') r
cross apply r.Commits c
group by c.Author.Name
having Count(*) >= 10                               -- At least 10 commits
  and Max(c.Date) >= DateAdd('month', -6, GetDate()) -- Recent activity
order by Count(*) desc
```

### Complex HAVING Conditions

```sql
-- Directories with significant activity and size
select 
    Directory,
    Count(*) as FileCount,
    Sum(Length) as TotalBytes,
    Round(Sum(Length) / 1024.0 / 1024.0, 2) as TotalMB,
    Avg(Length) as AvgFileSize
from #os.files('/codebase', true)
group by Directory
having Count(*) >= 20                    -- At least 20 files
  and Sum(Length) >= 10485760            -- At least 10MB total
  and Avg(Length) >= 1024                -- Average file >= 1KB
order by Sum(Length) desc

-- Team productivity analysis
select 
    c.Author.Name,
    Count(*) as TotalCommits,
    Count(distinct DatePart('day', c.Date)) as ActiveDays,
    Round(Cast(Count(*) as float) / Count(distinct DatePart('day', c.Date)), 2) as CommitsPerDay
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date >= DateAdd('month', -3, GetDate())
group by c.Author.Name
having Count(*) >= 15                    -- At least 15 commits
  and Count(distinct DatePart('day', c.Date)) >= 10  -- Active on 10+ days
  and Cast(Count(*) as float) / Count(distinct DatePart('day', c.Date)) >= 1.5  -- 1.5+ commits/day
order by Count(*) desc
```

## Window Functions with Grouping

### Ranking Within Groups

```sql
-- Rank files by size within each directory
select 
    Directory,
    Name,
    Length,
    Row_Number() over (partition by Directory order by Length desc) as SizeRank,
    Count(*) over (partition by Directory) as FilesInDirectory
from #os.files('/project', true)
where Row_Number() over (partition by Directory order by Length desc) <= 5
order by Directory, SizeRank
```

## Performance Optimization

### Efficient Grouping Strategies

```sql
-- Efficient: Filter before grouping
select 
    Extension,
    Count(*) as FileCount,
    Sum(Length) as TotalSize
from #os.files('/large-dataset', true)
where Extension is not null          -- Filter first
  and Length > 0                     -- Exclude empty files
group by Extension
having Count(*) >= 5                 -- Then filter groups
order by Sum(Length) desc

-- Less efficient: Grouping all data then filtering
select 
    Extension,
    Count(*) as FileCount,
    Sum(Length) as TotalSize
from #os.files('/large-dataset', true)
group by Extension
having Count(*) >= 5
  and Sum(Length) > 1048576
order by Sum(Length) desc
```

### Memory-Conscious Grouping

```sql
-- Use TAKE to limit result set size
select 
    Extension,
    Count(*) as FileCount,
    Round(Sum(Length) / 1024.0 / 1024.0, 2) as TotalMB
from #os.files('/huge-directory', true)
group by Extension
order by Count(*) desc
take 20    -- Only top 20 extensions
```

## Common Grouping Patterns

### File System Analysis

```sql
-- Storage analysis by file type
select 
    case 
        when Extension in ('.jpg', '.png', '.gif', '.bmp') then 'Images'
        when Extension in ('.mp4', '.avi', '.mov', '.wmv') then 'Videos'
        when Extension in ('.mp3', '.wav', '.flac') then 'Audio'
        when Extension in ('.pdf', '.doc', '.docx', '.txt') then 'Documents'
        when Extension in ('.zip', '.rar', '.7z') then 'Archives'
        when Extension in ('.exe', '.msi', '.deb', '.rpm') then 'Executables'
        else 'Other'
    end as FileCategory,
    Count(*) as FileCount,
    Round(Sum(Length) / 1024.0 / 1024.0 / 1024.0, 2) as TotalGB,
    Round(Avg(Length) / 1024.0 / 1024.0, 2) as AvgSizeMB
from #os.files('/storage', true)
group by 
    case 
        when Extension in ('.jpg', '.png', '.gif', '.bmp') then 'Images'
        when Extension in ('.mp4', '.avi', '.mov', '.wmv') then 'Videos'
        when Extension in ('.mp3', '.wav', '.flac') then 'Audio'
        when Extension in ('.pdf', '.doc', '.docx', '.txt') then 'Documents'
        when Extension in ('.zip', '.rar', '.7z') then 'Archives'
        when Extension in ('.exe', '.msi', '.deb', '.rpm') then 'Executables'
        else 'Other'
    end
order by Sum(Length) desc
```

### Git Repository Insights

```sql
-- Developer productivity over time
select 
    c.Author.Name,
    DatePart('year', c.Date) as Year,
    DatePart('quarter', c.Date) as Quarter,
    Count(*) as Commits,
    Count(distinct DatePart('day', c.Date)) as ActiveDays,
    Round(Avg(Len(c.Message)), 1) as AvgMessageLength
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date >= DateAdd('year', -2, GetDate())
group by 
    c.Author.Name,
    DatePart('year', c.Date),
    DatePart('quarter', c.Date)
having Count(*) >= 5
order by c.Author.Name, Year, Quarter
```

### Code Quality Analysis

```sql
-- Complexity analysis by project and class
select 
    p.Name as ProjectName,
    Count(distinct c.Name) as ClassCount,
    Count(m.Name) as MethodCount,
    Round(Avg(m.CyclomaticComplexity), 2) as AvgComplexity,
    Max(m.CyclomaticComplexity) as MaxComplexity,
    Sum(m.LinesOfCode) as TotalLinesOfCode
from #csharp.solution('/solution.sln') s
cross apply s.Projects p
cross apply p.Documents d
cross apply d.Classes c
cross apply c.Methods m
group by p.Name
order by Avg(m.CyclomaticComplexity) desc
```

## Best Practices

### Design Principles
- **Start simple**: Begin with basic grouping, then add complexity
- **Meaningful groups**: Group by columns that create logical categories
- **Appropriate aggregations**: Choose aggregate functions that match your analysis goals
- **Filter effectively**: Use WHERE before GROUP BY and HAVING after GROUP BY

### Performance Guidelines
- **Filter before grouping**: Reduce data size before applying GROUP BY
- **Limit result sets**: Use TAKE when you don't need all groups
- **Efficient expressions**: Avoid complex calculations in GROUP BY when possible
- **Index considerations**: Group by columns that can be efficiently sorted

### Readability Guidelines
- **Clear column names**: Use meaningful aliases for aggregate columns
- **Logical ordering**: Order results in a way that supports analysis
- **Consistent formatting**: Maintain consistent patterns across similar grouping queries
- **Document complex logic**: Comment business rules embedded in grouping expressions

The GROUP BY clause is fundamental to data analysis and reporting. Master these patterns to transform raw data into meaningful insights and summaries that drive decision-making.