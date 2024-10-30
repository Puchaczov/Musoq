---
title: WHERE Clause
layout: default
parent: SQL Syntax of the Tool
nav_order: 4
---

# Where Clause

This clause allows you to filter expressions to retrieve only the rows that meet certain conditions. Here you can use `ColumnName` columns, their properties `ColumnName.Property`, methods `SomeMethod(...)` as well as expressions like `3 + 1 + SomeMethod(..)`. The result of a method can be used as a parameter for another method, as long as the return type from the method call matches the definition of the external method. To combine conditions, you can use the syntax `and` and `or`.

```sql
... where ColumnName = 'something' and SomeMethod(DifferentColumnName) > 7 ...
```