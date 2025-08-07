# HAVING Clause

The `HAVING` clause filters groups created by `GROUP BY` based on aggregate conditions. While `WHERE` filters individual rows before grouping, `HAVING` filters groups after aggregation calculations are complete.

## Basic HAVING Syntax

```sql
select columns, aggregate_functions
from data_source
group by columns
having aggregate_condition
```

The `HAVING` clause is evaluated after the `GROUP BY` clause has created groups and aggregate functions have been calculated.

## Fundamental Concepts

### WHERE vs HAVING

Understanding the difference between `WHERE` and `HAVING` is crucial:

```sql
-- WHERE filters rows before grouping
select Extension, Count(*) as FileCount
from #os.files('/data', true)
where Length > 1024          -- Filter files before grouping
group by Extension
having Count(*) >= 10        -- Filter groups after aggregation

-- This query:
-- 1. Filters files larger than 1KB (WHERE)
-- 2. Groups remaining files by extension
-- 3. Only shows extensions with 10+ files (HAVING)
```

### Execution Order

SQL clauses execute in this order:
1. `FROM` - Identify data source
2. `WHERE` - Filter individual rows  
3. `GROUP BY` - Create groups
4. `HAVING` - Filter groups
5. `SELECT` - Choose columns
6. `ORDER BY` - Sort results

## Basic HAVING Conditions

### Count-Based Filtering

Filter groups by the number of items they contain:

```sql
-- Extensions with many files
select Extension, Count(*) as FileCount
from #os.files('/project', true)
group by Extension
having Count(*) >= 50
order by Count(*) desc

-- Active Git contributors
select 
    c.Author.Name,
    Count(*) as CommitCount
from #git.repository('/repo') r
cross apply r.Commits c
group by c.Author.Name
having Count(*) >= 20    -- Only authors with 20+ commits
order by Count(*) desc
```

### Sum-Based Filtering

Filter groups by total values:

```sql
-- Directories consuming significant disk space
select 
    Directory,
    Count(*) as FileCount,
    Sum(Length) as TotalBytes,
    Round(Sum(Length) / 1024.0 / 1024.0, 2) as TotalMB
from #os.files('/workspace', true)
group by Directory
having Sum(Length) >= 10485760    -- At least 10MB
order by Sum(Length) desc

-- File types using substantial storage
select 
    Extension,
    Count(*) as FileCount,
    Round(Sum(Length) / 1024.0 / 1024.0 / 1024.0, 2) as TotalGB
from #os.files('/storage', true)
group by Extension
having Sum(Length) >= 1073741824  -- At least 1GB
order by Sum(Length) desc
```

### Average-Based Filtering

Filter groups by average values:

```sql
-- File types with large average sizes
select 
    Extension,
    Count(*) as FileCount,
    Round(Avg(Length) / 1024.0 / 1024.0, 2) as AvgSizeMB
from #os.files('/media', true)
group by Extension
having Avg(Length) >= 1048576     -- Average size >= 1MB
  and Count(*) >= 5               -- At least 5 files
order by Avg(Length) desc

-- Git authors with detailed commit messages
select 
    c.Author.Name,
    Count(*) as CommitCount,
    Round(Avg(Len(c.Message)), 1) as AvgMessageLength
from #git.repository('/repo') r
cross apply r.Commits c
group by c.Author.Name
having Avg(Len(c.Message)) >= 100  -- Average message >= 100 chars
  and Count(*) >= 10                -- At least 10 commits
order by Avg(Len(c.Message)) desc
```

## Complex HAVING Conditions

### Multiple Aggregate Conditions

Combine multiple aggregate functions in HAVING:

```sql
-- Significant directories with many diverse files
select 
    Directory,
    Count(*) as FileCount,
    Count(distinct Extension) as UniqueExtensions,
    Sum(Length) as TotalBytes,
    Round(Avg(Length), 0) as AvgFileSize
from #os.files('/codebase', true)
group by Directory
having Count(*) >= 50                    -- At least 50 files
  and Count(distinct Extension) >= 5     -- At least 5 different file types
  and Sum(Length) >= 5242880             -- At least 5MB total
  and Avg(Length) >= 1024                -- Average file >= 1KB
order by Count(*) desc, Sum(Length) desc
```

### Logical Operators in HAVING

Use AND, OR, and NOT to create complex conditions:

```sql
-- Active teams or highly productive individuals
select 
    c.Author.Name,
    Count(*) as CommitCount,
    Count(distinct DatePart('day', c.Date)) as ActiveDays,
    Max(c.Date) as LastCommit
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date >= DateAdd('month', -6, GetDate())
group by c.Author.Name
having (Count(*) >= 100 and Count(distinct DatePart('day', c.Date)) >= 30)  -- Very active
    or (Count(*) >= 50 and Count(distinct DatePart('day', c.Date)) >= 40)   -- Consistent
    or (Count(*) >= 200)                                                     -- High volume
order by Count(*) desc
```

### Range Conditions

Filter groups within specific ranges:

```sql
-- Medium-sized directories (not too small, not too large)
select 
    Directory,
    Count(*) as FileCount,
    Round(Sum(Length) / 1024.0 / 1024.0, 2) as TotalMB
from #os.files('/balanced-project', true)
group by Directory
having Count(*) between 10 and 100           -- 10-100 files
  and Sum(Length) between 1048576 and 52428800  -- 1MB-50MB
order by Count(*) desc
```

## Advanced HAVING Patterns

### Conditional Aggregation in HAVING

Use CASE statements within aggregate functions:

```sql
-- Directories with good balance of code vs other files
select 
    Directory,
    Count(*) as TotalFiles,
    Sum(case when Extension in ('.cs', '.js', '.py', '.java') then 1 else 0 end) as CodeFiles,
    Sum(case when Extension not in ('.cs', '.js', '.py', '.java') then 1 else 0 end) as OtherFiles
from #os.files('/mixed-project', true)
group by Directory
having Count(*) >= 20    -- At least 20 files
  and Sum(case when Extension in ('.cs', '.js', '.py', '.java') then 1 else 0 end) >= 10  -- At least 10 code files
  and Sum(case when Extension in ('.cs', '.js', '.py', '.java') then 1 else 0 end) * 100.0 / Count(*) >= 50  -- At least 50% code
order by Directory
```

### Statistical Filtering

Filter based on statistical measures:

```sql
-- File types with consistent sizes (low standard deviation)
select 
    Extension,
    Count(*) as FileCount,
    Round(Avg(Length), 0) as AvgSize,
    Round(StdDev(Length), 0) as SizeStdDev,
    Round(StdDev(Length) / Avg(Length) * 100, 2) as CoefficientOfVariation
from #os.files('/consistent-data', true)
group by Extension
having Count(*) >= 20                           -- At least 20 files
  and StdDev(Length) / Avg(Length) <= 0.5      -- Low variation (CV <= 50%)
order by StdDev(Length) / Avg(Length)

-- Note: StdDev function availability may vary by implementation
```

## Performance Considerations

### Efficient HAVING Usage

```sql
-- Efficient: Filter with WHERE first, then HAVING
select 
    Extension,
    Count(*) as FileCount,
    Sum(Length) as TotalSize
from #os.files('/large-dataset', true)
where Length > 0              -- Filter empty files first (WHERE)
  and Extension is not null   -- Filter files without extension first
group by Extension
having Count(*) >= 100        -- Then filter groups (HAVING)
order by Sum(Length) desc

-- Less efficient: Only using HAVING
select 
    Extension,
    Count(*) as FileCount,
    Sum(Length) as TotalSize
from #os.files('/large-dataset', true)
group by Extension
having Count(*) >= 100
  and Sum(case when Length > 0 then 1 else 0 end) = Count(*)  -- Complex condition
order by Sum(Length) desc
```

### Memory-Conscious Filtering

```sql
-- Limit groups before expensive calculations
select 
    Directory,
    Count(*) as FileCount,
    Sum(Length) as TotalBytes
from #os.files('/huge-filesystem', true)
group by Directory
having Count(*) >= 1000       -- Only large directories
order by Sum(Length) desc
take 50                       -- Limit final results
```

## Common HAVING Patterns

### Top N Analysis

Find the most significant groups:

```sql
-- Top file types by count and size
select 
    Extension,
    Count(*) as FileCount,
    Round(Sum(Length) / 1024.0 / 1024.0, 2) as TotalMB,
    Round(Avg(Length) / 1024.0, 2) as AvgKB
from #os.files('/analysis', true)
group by Extension
having Count(*) >= 10         -- Minimum threshold
order by Count(*) desc, Sum(Length) desc
take 15                       -- Top 15 extensions

-- Most active Git contributors in recent months
select 
    c.Author.Name,
    Count(*) as RecentCommits,
    Max(c.Date) as LastCommit,
    DateDiff('day', Min(c.Date), Max(c.Date)) as ActiveSpan
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date >= DateAdd('month', -6, GetDate())
group by c.Author.Name
having Count(*) >= 25         -- At least 25 commits
order by Count(*) desc
take 10                       -- Top 10 contributors
```

### Quality Thresholds

Identify groups meeting quality criteria:

```sql
-- Well-maintained code modules (frequent commits, recent activity)
select 
    Substring(c.Message, 1, 20) as CommitPrefix,
    Count(*) as CommitCount,
    Count(distinct c.Author.Name) as Contributors,
    Max(c.Date) as LastCommit,
    DateDiff('day', Max(c.Date), GetDate()) as DaysSinceLastCommit
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date >= DateAdd('month', -12, GetDate())
group by Substring(c.Message, 1, 20)
having Count(*) >= 15                               -- Active development
  and Count(distinct c.Author.Name) >= 2           -- Multiple contributors
  and DateDiff('day', Max(c.Date), GetDate()) <= 30 -- Recent activity
order by Count(*) desc

-- Directories with balanced file distribution
select 
    Directory,
    Count(*) as TotalFiles,
    Count(distinct Extension) as FileTypes,
    Round(Count(distinct Extension) * 100.0 / Count(*), 2) as DiversityPercent
from #os.files('/project', true)
group by Directory
having Count(*) >= 20                              -- Significant size
  and Count(distinct Extension) >= 3              -- Multiple file types
  and Count(distinct Extension) * 100.0 / Count(*) >= 15  -- Good diversity (15%+)
order by Count(*) desc
```

### Outlier Detection

Find groups that deviate from normal patterns:

```sql
-- Unusually large files by type
select 
    Extension,
    Count(*) as FileCount,
    Round(Avg(Length) / 1024.0 / 1024.0, 2) as AvgSizeMB,
    Round(Max(Length) / 1024.0 / 1024.0, 2) as MaxSizeMB,
    Round(Max(Length) / Avg(Length), 2) as SizeRatio
from #os.files('/outlier-analysis', true)
group by Extension
having Count(*) >= 10                    -- Enough samples
  and Max(Length) / Avg(Length) >= 5     -- Largest file is 5x+ average
order by Max(Length) / Avg(Length) desc

-- Git authors with unusual commit patterns
select 
    c.Author.Name,
    Count(*) as TotalCommits,
    Round(Avg(Len(c.Message)), 1) as AvgMessageLength,
    Max(Len(c.Message)) as LongestMessage,
    Min(Len(c.Message)) as ShortestMessage
from #git.repository('/repo') r
cross apply r.Commits c
group by c.Author.Name
having Count(*) >= 20                             -- Significant activity
  and (Max(Len(c.Message)) >= 500               -- Very long messages
    or Min(Len(c.Message)) <= 10                 -- Very short messages
    or Max(Len(c.Message)) / Avg(Len(c.Message)) >= 5)  -- High variation
order by Count(*) desc
```

## Best Practices

### Design Guidelines
- **Use WHERE first**: Filter individual rows before grouping when possible
- **Meaningful thresholds**: Set HAVING conditions based on business requirements
- **Combine conditions logically**: Use AND/OR appropriately to create clear filter logic
- **Consider data distribution**: Set thresholds that make sense for your data

### Performance Guidelines
- **Filter early**: Use WHERE to reduce data before GROUP BY operations
- **Simple expressions**: Keep HAVING conditions as simple as possible
- **Limit results**: Use TAKE after HAVING to control result set size
- **Appropriate aggregates**: Choose efficient aggregate functions for your filtering needs

### Readability Guidelines
- **Clear conditions**: Write HAVING conditions that clearly express business rules
- **Consistent formatting**: Align complex HAVING conditions for readability
- **Meaningful thresholds**: Use round numbers or business-meaningful values when possible
- **Document complex logic**: Comment unusual or domain-specific filtering rules

### Common Mistakes to Avoid
- **Don't use column aliases in HAVING**: Reference the actual aggregate function
- **Don't filter individual row values**: Use WHERE for row-level filtering
- **Don't create overly complex conditions**: Break complex HAVING into multiple conditions
- **Don't forget about NULL handling**: Consider how NULLs affect your aggregate calculations

The HAVING clause is essential for creating meaningful summaries from grouped data. Use it to focus on the most significant, relevant, or interesting groups in your analysis, turning raw data into actionable insights.