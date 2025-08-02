# Basic Query Structure

## Overview

Musoq uses SQL-like syntax with some important extensions and modifications. Understanding the basic query structure is essential for effective use of the tool.

## Basic Query Anatomy

The fundamental structure of a Musoq query follows this pattern:

```sql
[table definitions]
[coupling statements]
[with cte_name as (subquery)]
select column_list
from data_source [alias]
[join_clause]
[where condition]
[group by column_list]
[having condition]
[order by column_list]
[skip number]
[take number]
```

## Key Differences from Standard SQL

### 1. Data Source Syntax
Musoq uses the `#schema.method(parameters)` syntax to specify data sources:

```sql
-- Query files in a directory
select Name, Length from #os.files('/path/to/directory', true)

-- Query Git repository commits  
select Sha, Message from #git.repository('/path/to/repo') r cross apply r.Commits c
```

### 2. Case Sensitivity
- **Column names and methods are case-sensitive**
- **Keywords are not case-sensitive**

```sql
-- This works
SELECT Name, Length FROM #os.files('/path', true)

-- This also works  
select Name, Length from #os.files('/path', true)

-- This will fail - wrong column case
select name, length from #os.files('/path', true)  -- ERROR
```

### 3. Strict Typing
Queries are strictly typed - types must match exactly:

```sql
-- This works - comparing string to string
select * from #os.files('/path', true) where Name = 'test.txt'

-- This fails - cannot compare string to number
select * from #os.files('/path', true) where Name = 123  -- ERROR
```

### 4. Mandatory Aliases for Joins
When using joins with parameterizable sources, aliases are required:

```sql
-- Correct - alias 'r' is used
select c.Sha from #git.repository('/path') r cross apply r.Commits c

-- Incorrect - no alias
select Commits.Sha from #git.repository('/path') cross apply Commits  -- ERROR
```

## Essential Query Components

### SELECT Clause
Specifies which columns to return:

```sql
-- Select specific columns
select Name, Length from #os.files('/path', true)

-- Select all columns
select * from #os.files('/path', true)

-- Select with expressions
select Name, Length / 1024 as SizeInKB from #os.files('/path', true)
```

### FROM Clause  
Specifies the data source:

```sql
-- Simple data source
from #os.files('/path/to/directory', true)

-- Data source with alias
from #git.repository('/path/to/repo') r

-- Multiple data sources with joins
from #os.files('/path', true) f
inner join #os.files('/other/path', true) o on f.Name = o.Name
```

### WHERE Clause
Filters rows based on conditions:

```sql
-- Simple condition
where Length > 1000

-- Multiple conditions
where Length > 1000 and Extension = '.txt'

-- Pattern matching
where Name like '%.log'
```

## Minimal Working Examples

### 1. Simple File Listing
```sql
select Name, Length 
from #os.files('/tmp', false)
```

### 2. Filtered Query with Expressions
```sql
select 
    Name,
    Length / 1024 / 1024 as SizeMB
from #os.files('/home', true)
where Extension = '.pdf' and Length > 1000000
order by Length desc
take 10
```

### 3. Basic Aggregation
```sql
select 
    Extension,
    count(*) as FileCount,
    sum(Length) as TotalSize
from #os.files('/documents', true)
group by Extension
having count(*) > 5
order by TotalSize desc
```

## Schema Discovery

Use the `desc` command to discover available columns for any data source:

```sql
desc #os.files('/path', true)
```

This returns a table showing column names and their data types.

## Next Steps

- Learn about [SELECT clause](./select-clause.md) details and expressions
- Understand [data sources](./from-clause-data-sources.md) and how to connect to different types of data
- Explore [filtering](./where-clause-filtering.md) with WHERE clauses

## Common Gotchas

1. **Case sensitivity** - Always match the exact case of column names
2. **Type matching** - Ensure types match in comparisons and joins
3. **Aliases required** - Use aliases when joining parameterizable sources
4. **Schema syntax** - Remember the `#schema.method()` format for data sources
5. **Boolean parameters** - Use `true`/`false`, not `1`/`0` for boolean parameters