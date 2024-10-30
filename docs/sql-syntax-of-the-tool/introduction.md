---
title: Introduction
layout: default
parent: SQL Syntax of the Tool
nav_order: 1
---

# Musoq – Introduction

`Musoq` is an engine that enables the use of SQL syntax on a variety of data sources. It allows for querying data in different formats such as files, directories, structured files, archives, and more. Thanks to its extensible design, anything can be a data source since each data source is provided in the same way - as a plugin. While this construct can be demanding in terms of implementation, it offers the possibility to design your own data source - either generic or tailored to a specific need. This is the unique selling point of the solution - the ability to integrate with various data types and the flexibility in processing them.

## What Musoq is not?

Musoq is not an engine for processing huge volumes of data - this solution should be seen as a Swiss army knife for processing and working with smaller data sets - definitely not larger than our computer's memory size. In its current stage, plugins are also not capable of utilizing indexes although that does not mean they won't be able to in the future. Partially, the engine already allows for this (but none of the plugins implement it yet).

## Similarities

The SQL syntax in `Musoq` preserves most of the traditional SQL elements, such as SELECT queries, WHERE conditions, operators like GROUP BY and HAVING. Users familiar with the basics of SQL will find many familiar elements in Musoq, which should facilitate understanding.

## Unique Features of SQL Syntax in SQL

While `Musoq` draws from traditional SQL, it also introduces some unique extensions and modifications to the syntax. These include:

- **Integrated queries to custom data sources**: The ability to execute SQL queries to sources such as file systems, pictures (through EXIF attributes), and even archived files.
- **Integration with AI and external APIs**: `Musoq` enables integration with AI tools such as GPT-3/GPT-4 as well as other APIs, opening doors to advanced analysis and data processing.
- **Flexibility in defining queries**: Users can create custom data sources containing functions and aggregations tailored to their specific needs.
- **Mixing data sources in one query**: A unique feature of `Musoq` is the ability to combine data from different sources in a single query.

**It is important to note that in `Musoq`, all calculations are performed directly upon reading from the data source. There is no intermediate element of placing data in an in-memory database (e.g., SQLite) and translating the user query into traditional SQL and then executing the query. Musoq translates SQL syntax into C#, compiles it, and runs the program written in this way on the data source the user is asking for.**

## Comparison with Traditional SQL

Even though `Musoq` uses SQL syntax, it is not fully SQL compliant. Attention should be paid to certain differences in behavior and handling of queries. `Musoq` extends the capabilities of SQL, adapting them to its unique architecture and applications, which sometimes leads to different behavior and results from traditional databases.