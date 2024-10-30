---
title: Basic Syntax Elements
layout: default
parent: SQL Syntax of the Tool
nav_order: 2
---

# Preliminary Assumption

I implicitly assume that you know the basics of SQL. If you're not familiar with them, read about it, for example, on [w3schools.com](https://www.w3schools.com/sql/default.asp), and then come back here. This description will not be detailed enough to explain how to use SQL in general.

## Important Notes to Read

1. Until this document states otherwise, you can assume that any function not mentioned here replicates SQL functionality.
2. Column names and methods are **case-sensitive**.
3. Keywords are not case-sensitive.
4. **Queries are strictly typed. The types must match.**
5. The from clause for join syntax **must be aliased** for the parameterizable source. You must refer to the specific join table using this alias.
6. Each first script invocation will be significantly slower than subsequent calls. This delay occurs due to the translation of SQL into C# and compilation.

## How to Know Which Columns are Provided by the Data Source

Each data source provides entry points that the `desc` syntax uses. Additionally, you can utilize the online documentation which should indicate which columns are available for use. The syntax of this expression looks as follows:

```sql
desc #schema.method(param1, ..., paramN)
```

Such an expression will return a table with columns and their corresponding data types that are available for use.