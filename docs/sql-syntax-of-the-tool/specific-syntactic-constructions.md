---
title: Specific Syntactic Constructions
layout: default
parent: SQL Syntax of the Tool
nav_order: 10
---

# Useful Language-Specific Features

1. **Accessing Object Properties** - When a column is of a composite type, you can use the query syntax `ColumnName.SomeProperty` to obtain the value of `SomeProperty` from the `ColumnName` object. The type of `ColumnName` must include the `SomeProperty`. If it has the appropriate property, the column becomes the type that `SomeProperty` is. You can use the chaining call syntax like `ColumnName.SomeProperty.DifferentProperty`. It is also permissible to obtain values from arrays using the syntax `ColumnName.Prop[0]` as well as obtaining values from dictionaries with `ColumnName.Prop['test']`.
2. **Like / not like Operators** - These operators can be used to search for specific patterns in a query. Both the `%` and `_` syntax are supported to find values matching the pattern.
3. **Rlike / not rlike Operators** - This is the same operator as `like`, but instead of using a wildcard symbol, here we specify a regular expression.
4. **Contains Operator** - this operator checks whether the column contains a given string of characters.
5. **In Operator** - this operator is useful when you need to access a column, and multiple values are considered valid. The expression looks like this: `ColumnName in ('abc', 'cda')`
6. **Is null / is not null Operators** - This operator is used to check whether a column is empty. You can use it for both values and columns expressed by reference. It should be noted that using it for values does not make sense and in fact, the evaluator will discard such a check. Usage looks like `ColumnName is null` or `ColumnName is not null`