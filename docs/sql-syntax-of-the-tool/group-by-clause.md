---
title: GROUP BY Clause
layout: default
parent: SQL Syntax of the Tool
nav_order: 5
---

# Group by and Having Operators

The Group by operator allows you to specify columns or expressions that your rows will be grouped by. You can define multiple groups. When grouping, your select clause **must contain only** the grouping expressions or column names or aggregate methods. The use of non-aggregating methods is also permitted as a parameter or result of processing an aggregate method. Columns that are not part of the group by operator are not allowed.

## Aggregate Methods

You can use methods like `Sum`, `SumIncome`, `SumOutcome`, `Count`, `AggregateValue`, and others to perform the aggregation of values in rows belonging to a particular group. Broadly speaking, the rules from SQL apply here, so after using `group by ColumnName`, the only thing you can return in select is `ColumnName` and aggregated values such as `Sum(SomeColumn)`. It should be noted that the `*` operator in aggregation operators is not supported.

`select ColumnName, Count(ColumnName) from @schema.method() group by ColumnName`

You can also group by the values altered by some method. The only rule here is to repeat that part in the select clause (if you need it) both as an indicator of the group and the value of the grouping. It should look like this:

`select SomeMethod(ColumnName), Sum(SomeMethod(ColumnName)) from @schema.method() group by SomeMethod(ColumnName)`
## Parent Aggregation

You can calculate aggregation for parent groups by placing the number of groups you want to skip as a parameter of the aggregate method. For example, if your query groups by the columns `Country` and `City` and you sum their population `Sum(Population)`, you could simultaneously sum the population for the entire country. In such a case, you should add `Sum(1, Population)` as well, which will compute the sum only for `Country` without considering different cities within the country. Aggregate methods can be combined with others in the **select** part.