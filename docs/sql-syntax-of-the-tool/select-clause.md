---
title: SELECT Clause
layout: default
parent: SQL Syntax of the Tool
nav_order: 6
---

# Select Clause

The `Select` command retrieves data from a specified source. As part of the select query, you can use the column name `ColumnName`, the properties of a complex object `ComplexColumn.Property`, and the method `SomeMethod(...)`. Returned columns or expressions must be aliased when they are used with set operators or as part of common table expressions. For example:

```sql
with Dummy as (
   select 1 + 2 as 'EvaluatedExpression', SomeMethod(SomeColumn) as 'MyMethod', SomeColumn from #dummy.source()
)
select EvaluatedExpression, SomeColumn, MyMethod from Dummmy
```

in this particular case, `SomeMethod(SomeColumn)` and `1 + 2` need to be aliased because they represent a complex expression within the CTE expression. Another example here involves set operators, which require that the columns match on both sides.

```sql
select ColumnName, 1 + 2 as 'EvaluatedExpression', SomeMethod(SomeColumn) as 'MyMethod' from #dummy.source()
union
select OtherMethod(ColumnName) as ColumnName, 3 + 4 as 'EvaluatedExpression', SomeMethod2(SomeColumn) as 'MyMethod' from #dummy.source()
```

As you can see, both operators require the same name. What is not visible here, but is also very important, is that expressions on both sides **must return the same types**.