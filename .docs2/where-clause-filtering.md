# WHERE Clause and Filtering

The `WHERE` clause filters data based on specified conditions. Musoq supports standard SQL filtering operations plus extensions for working with complex data types and nested properties.

## Basic WHERE Syntax

```sql
select columns
from data_source
where condition
```

Conditions evaluate to boolean values (true/false) and determine which rows are included in the result set.

## Comparison Operators

### Basic Comparisons

```sql
-- Numeric comparisons
select Name, Length
from #os.files('/downloads', true)
where Length > 1048576  -- Files larger than 1MB

-- String comparisons
select Name, Extension
from #os.files('/documents', true)
where Extension = '.pdf'

-- Date comparisons
select Name, CreationTime
from #os.files('/logs', true)
where CreationTime >= '2024-01-01'
```

### Supported Comparison Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `=` | Equal to | `Length = 1024` |
| `!=` or `<>` | Not equal to | `Extension != '.tmp'` |
| `>` | Greater than | `Length > 1000000` |
| `>=` | Greater than or equal | `CreationTime >= '2024-01-01'` |
| `<` | Less than | `Length < 1024` |
| `<=` | Less than or equal | `CreationTime <= GetDate()` |

### String Comparisons

```sql
-- Case-sensitive string comparison
select Name
from #os.files('/src', true)
where Extension = '.cs'

-- String inequality
select Name
from #os.files('/temp', true)
where Name != 'temp.txt'
```

## Logical Operators

### AND Operator

Combine multiple conditions (all must be true):

```sql
-- Multiple conditions with AND
select Name, Length, Extension
from #os.files('/projects', true)
where Length > 1000 
  and Extension = '.cs'
  and CreationTime > '2024-01-01'

-- Complex AND conditions
select c.Sha, c.Author.Name, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
where c.Author.Email = 'developer@company.com'
  and c.Date >= DateAdd('month', -3, GetDate())
  and Len(c.Message) > 50
```

### OR Operator

Match any of the specified conditions:

```sql
-- Files with multiple extensions
select Name, Extension
from #os.files('/documents', true)
where Extension = '.pdf' 
   or Extension = '.doc' 
   or Extension = '.docx'

-- Size-based OR conditions
select Name, Length
from #os.files('/data', true)
where Length < 1024        -- Small files
   or Length > 10485760    -- Large files (> 10MB)
```

### NOT Operator

Negate a condition:

```sql
-- Exclude specific file types
select Name, Extension
from #os.files('/mixed', true)
where not (Extension = '.tmp' or Extension = '.log')

-- Exclude empty files
select Name, Length
from #os.files('/content', true)
where not Length = 0
```

## Pattern Matching

### LIKE Operator

Pattern matching with wildcards:

```sql
-- Wildcard patterns
select Name
from #os.files('/projects', true)
where Name like '%.cs'          -- Files ending with .cs

select Name
from #os.files('/logs', true)
where Name like 'error%'        -- Files starting with 'error'

select Name
from #os.files('/data', true)
where Name like '%temp%'        -- Files containing 'temp'

-- Single character wildcard
select Name
from #os.files('/files', true)
where Name like 'file?.txt'     -- file1.txt, file2.txt, etc.
```

### Pattern Matching Examples

```sql
-- Git commit message patterns
select c.Sha, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
where c.Message like 'fix:%'
   or c.Message like 'feat:%'
   or c.Message like 'docs:%'

-- Method name patterns in code
select c.Name, m.Name
from #csharp.solution('/project.sln') s
cross apply s.Projects p
cross apply p.Documents d
cross apply d.Classes c
cross apply c.Methods m
where m.Name like 'Get%' 
   or m.Name like 'Set%'
   or m.Name like 'Is%'
```

## Set Membership

### IN Operator

Check if a value exists in a list:

```sql
-- File extension filtering
select Name, Extension
from #os.files('/source', true)
where Extension in ('.cs', '.vb', '.fs', '.cpp', '.h')

-- Specific file sizes
select Name, Length
from #os.files('/test', true)
where Length in (1024, 2048, 4096)

-- Author filtering in Git
select c.Sha, c.Author.Name, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
where c.Author.Name in ('Alice', 'Bob', 'Charlie')
```

### NOT IN Operator

Exclude values from a set:

```sql
-- Exclude system and temporary files
select Name, Extension
from #os.files('/workspace', true)
where Extension not in ('.tmp', '.log', '.cache', '.swap')

-- Exclude specific directories
select Name, Directory
from #os.files('/project', true)
where Directory not in ('/project/bin', '/project/obj', '/project/.git')
```

## NULL Handling

### IS NULL and IS NOT NULL

Handle missing or undefined values:

```sql
-- Find files without extension
select Name
from #os.files('/mixed', true)
where Extension is null

-- Find files with extension
select Name, Extension
from #os.files('/documents', true)
where Extension is not null

-- Handle nullable Git properties
select c.Sha, c.Author.Name
from #git.repository('/repo') r
cross apply r.Commits c
where c.Author.Email is not null
  and c.Committer.Email is not null
```

### NULL Comparison Behavior

```sql
-- NULL comparisons always return false
select Name
from #os.files('/test', true)
where Length = null     -- This returns no results

-- Correct NULL checking
select Name
from #os.files('/test', true)
where Length is null    -- This finds NULL lengths
```

## Complex Property Filtering

### Nested Property Access

Filter on nested object properties:

```sql
-- Git commit author properties
select c.Sha, c.Author.Name, c.Author.Email
from #git.repository('/repo') r
cross apply r.Commits c
where c.Author.Name = 'John Doe'
  and c.Author.Email like '%@company.com'

-- File system detailed properties
select Name, Length, Attributes
from #os.files('/system', true)
where Attributes.IsHidden = false
  and Attributes.IsReadOnly = false
```

### Object Property Combinations

```sql
-- Complex Git filtering
select c.Sha, c.Message, c.Author.Name
from #git.repository('/repo') r
cross apply r.Commits c
where c.Author.Name != c.Committer.Name  -- Different author and committer
  and c.Date > DateAdd('week', -1, GetDate())
  and Len(c.Message) between 10 and 100
```

## Function-Based Filtering

### String Functions in WHERE

```sql
-- Case-insensitive filtering
select Name
from #os.files('/documents', true)
where Upper(Extension) = '.PDF'

-- String length filtering
select Name, Extension
from #os.files('/files', true)
where Len(Name) > 20
  and Substring(Name, 1, 4) = 'long'

-- String manipulation
select c.Sha, c.Message
from #git.repository('/repo') r
cross apply r.Commits c
where Trim(c.Message) != ''
  and Left(c.Message, 4) in ('fix:', 'feat', 'docs')
```

### Mathematical Functions

```sql
-- Mathematical conditions
select Name, Length
from #os.files('/data', true)
where Abs(Length - 1024) < 100      -- Files approximately 1KB
  and Power(Length, 0.5) > 32        -- Mathematical calculations
  and Round(Length / 1024.0, 0) = 5  -- Exactly 5KB when rounded
```

### Date and Time Functions

```sql
-- Date-based filtering
select Name, CreationTime
from #os.files('/logs', true)
where DateDiff('day', CreationTime, GetDate()) <= 7  -- Files from last week
  and DatePart('hour', CreationTime) between 9 and 17 -- Created during work hours
  and DatePart('weekday', CreationTime) not in (1, 7) -- Not weekend

-- Git commit date filtering
select c.Sha, c.Message, c.Date
from #git.repository('/repo') r
cross apply r.Commits c
where DatePart('year', c.Date) = 2024
  and DatePart('month', c.Date) in (1, 2, 3)  -- First quarter
```

## Advanced Filtering Patterns

### Range Filtering

```sql
-- BETWEEN operator for ranges
select Name, Length
from #os.files('/media', true)
where Length between 1048576 and 10485760  -- 1MB to 10MB

-- Date ranges
select c.Sha, c.Author.Name, c.Date
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date between '2024-01-01' and '2024-12-31'
```

### Conditional Filtering with CASE

```sql
-- Dynamic filtering based on conditions
select Name, Length, Extension
from #os.files('/mixed', true)
where case 
    when Extension = '.log' then Length < 10485760    -- Log files < 10MB
    when Extension = '.zip' then Length > 1048576     -- ZIP files > 1MB
    else Length > 0                                   -- Other files not empty
end
```

### Subquery Filtering

```sql
-- Filter based on subquery results
select Name, Length
from #os.files('/project', true)
where Extension in (
    select Extension
    from #os.files('/templates', true)
    group by Extension
    having Count(*) > 5
)

-- EXISTS filtering
select f.Name
from #os.files('/source', true) f
where exists (
    select 1
    from #os.files('/backup', true) b
    where b.Name = f.Name
)
```

## Performance Optimization

### Early Filtering

```sql
-- Efficient: Filter early in the pipeline
select Name, Length
from #os.files('/large-directory', true)
where Extension = '.cs'      -- Filter files first
  and Length > 1000          -- Then filter by size

-- Less efficient: Complex calculations on all rows
select Name, Complex_Calculation(Length) as Result
from #os.files('/large-directory', true)
where Complex_Calculation(Length) > 100
```

### Index-Friendly Filtering

```sql
-- Use direct column comparisons when possible
where CreationTime >= '2024-01-01'    -- Good

-- Avoid functions on columns in WHERE
where DatePart('year', CreationTime) = 2024  -- Less efficient
```

## Common Filtering Patterns

### File Analysis Patterns

```sql
-- Large files in specific directories
select Name, Length, Directory
from #os.files('/project', true)
where Length > 5242880  -- > 5MB
  and Directory not like '%/bin/%'
  and Directory not like '%/obj/%'
  and Extension in ('.cs', '.cpp', '.h')

-- Recently modified files
select Name, LastWriteTime
from #os.files('/active-project', true)
where DateDiff('hour', LastWriteTime, GetDate()) <= 24
  and Extension != '.tmp'
```

### Git Analysis Patterns

```sql
-- Active contributors
select c.Author.Name, Count(*) as CommitCount
from #git.repository('/repo') r
cross apply r.Commits c
where c.Date >= DateAdd('month', -3, GetDate())
  and c.Author.Email like '%@company.com'
  and c.Message not like 'Merge%'
group by c.Author.Name
having Count(*) >= 5

-- Bug fix commits
select c.Sha, c.Message, c.Author.Name
from #git.repository('/repo') r
cross apply r.Commits c
where (c.Message like '%fix%' 
    or c.Message like '%bug%' 
    or c.Message like '%issue%')
  and c.Message not like '%fix typo%'
  and Len(c.Message) > 10
```

### Code Quality Patterns

```sql
-- Complex methods that need refactoring
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
where m.CyclomaticComplexity > 10
   or m.LinesOfCode > 50
   or (m.ParameterCount > 5 and m.CyclomaticComplexity > 5)
```

## Best Practices

### Condition Ordering
- **Most selective first**: Place conditions that eliminate the most rows first
- **Cheapest operations first**: Simple comparisons before complex functions
- **AND before OR**: Group AND conditions before OR conditions when possible

### Readability
- **Use parentheses**: Clarify complex logical expressions
- **Consistent formatting**: Align conditions for better readability
- **Meaningful conditions**: Write self-documenting filter logic

### Performance
- **Avoid functions on columns**: Use direct comparisons when possible
- **Use appropriate data types**: Ensure type compatibility to avoid conversions
- **Filter early**: Apply WHERE conditions as early as possible in the query

The WHERE clause is essential for extracting meaningful insights from your data. Master these filtering patterns to create precise, efficient queries that surface exactly the information you need.