---
title: Set Operators
layout: default
parent: SQL Syntax of the Tool
nav_order: 8
---

# Set Operators

Set operators in `Musoq` differ slightly from the operators used in traditional SQL. They compare individual rows and calculate the result for the entire query. In `Musoq`, you must specify which columns will participate in the comparison, which looks like this: `query1 union all (Column1, Column2, ...) query2`. Columns must have the same names and order. If you want to use different columns for comparison, you need to give such a column an alias in order to match its name. Here's how it should look:

```sql
select Column from #schema.method()
union all (Column)
select Column2 as Column from #schema.method()
``` 

You cannot mix the return types of the columns, and the number of values returned for both columns must be the same.

1. **Union all** - Combines result sets without eliminating any potential duplicates.
2. **Union** - Combines result sets and removes duplicated rows, selecting the first occurrence.
3. **Except** - Returns differing rows from the left query that do not appear in the right query.
4. **Intersect** - Returns only those rows that are present in both the left and right queries.