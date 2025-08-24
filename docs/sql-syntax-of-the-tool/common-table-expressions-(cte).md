---
title: Common Table Expressions (CTE)
layout: default
parent: SQL Syntax of the Tool
nav_order: 9
---

# Common Table Expressions (CTE)

`Common Table Expressions` (CTE) are expressions that should be treated as if they create a temporary named result set. Effectively, the clause `with TempResultSetName as (... Inner Query ...) Outer Query` computes the `Inner Query` and stores it within the scope of the `Outer Query`, allowing for its use there. The temporary result set can also be used as the source for another `CTE`. It should be noted that **recursive CTEs** are currently not supported. An example of such an expression can be observed below.

```sql
with FirstTable as (
	select 1 as x from @system.dual()
), SecondTable as (
	select 2 as y from @system.dual()
)
select x, y from FirstTable f inner join SecondTable s on f.x <> s.y
```