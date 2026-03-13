---
title: JOIN Clause
layout: default
parent: SQL Syntax of the Tool
nav_order: 7
---

# Support for Joins

Currently, the supported operators are `join` or `inner join`, `left join` or `left outer join`, and `right join` or `right outer join`. The syntax for the `inner join` expression is as follows:

```sql
...from schema.method(...) a inner join schema.method() b on a.SomeColumn = b.SomeOtherColumn where ...
```

while the `left outer join` syntax is:

```sql
...from schema.method(...) a left outer join schema.method() b on a.SomeColumn = b.SomeOtherColumn where ...
```

The shorter `left join` form is equivalent:

```sql
...from schema.method(...) a left join schema.method() b on a.SomeColumn = b.SomeOtherColumn where ...
```

and `right outer join`:

```sql
...from schema.method(...) a right outer join schema.method() b on a.SomeColumn = b.SomeOtherColumn where ...
```

The shorter `right join` form is equivalent:

```sql
...from schema.method(...) a right join schema.method() b on a.SomeColumn = b.SomeOtherColumn where ...
```

When using a source that is parameterized (for example, `schema.method()`), we must provide it with an alias.