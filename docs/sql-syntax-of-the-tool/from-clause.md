---
title: FROM Clause
layout: default
parent: SQL Syntax of the Tool
nav_order: 3
---

# From Clause

You can witness three different types of `from` clauses. The first one is `from @schema.table(param1, ..., paramN)`, the second is `from SomeTableAlias`, and the third `from AnotherAlias(param1, ..., paramN)`. Basic usage often involves the first syntax, the second is utilized when referring to a source coming from **Common Table Expressions (CTEs)**, while the third is mainly used in situations where the data source's return types are unknown but known to the user writing the query.

## from @a.b(param1, ..., paramN)

In query writing, a commonly used command `from` is `from @a.b(param1, ...)`. This syntax specifies that we will be using an external data source. For instance, from `@separatedValues.csv(...)` indicates that we will utilize the source `separatedValues` and the table `csv` whose parameters will be passed `(...)`. This instructs the data source directly on how to retrieve the data and then load it into the evaluator. The data source is parameterized, which allows for configuring what data you want to receive and how you want to receive it. For example, when querying about an operating system disk, you likely wouldn't want to iterate over the entire disk but rather a set of folders that are important to you. Parameterization provides the flexibility to construct the portion of data you are inquiring about. By narrowing your searches, we speed up operation. **This clause must be aliased when using it in conjunction with the join syntax**

## from SomeTableAlias

The use of Common Table Expressions introduces a new kind of data source that is already processed. Using the syntax `with X as (select * from ...)` expresses that the result set from `select * from ...` will be available under the name `X`, which becomes your new data source. You can use multiple table expressions with the following syntax:

```sql
with X as (
      select * from ...
 ), Y as (
      select * from ... where ...
 )
 select * from X ...
```

This syntax is much more flexible and also allows for combining table expressions with each other. Note that this type of data source **cannot** be aliased unlike the first one.

## from SourceOfInvoiceValue(param1, ..., paramN)

The third and last clause you can use is `from` which aliases both the table and data source. This notation can be encountered when the data source we are using is unable to determine the types it will work on; however, as the creator of the query, we have such knowledge. When do we fall into such a situation? For instance, when extracting data from an invoice, although the data source will have context on the columns we want to extract, it will not possess information about the types of columns being extracted. We fill in this knowledge with the syntax below:

```sql
table Invoice {
	ProductName 'string',
	ProductPrize 'decimal'
};
couple table @toolbox.invoices with Invoice as SourceOfInvoiceValues;
```

In this way, we provide the plugin and compiler with sufficient information about the required columns and types in the query.