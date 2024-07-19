# Musoq

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/Puchaczov/Musoq/graphs/code-frequency)
[![Nuget](https://img.shields.io/badge/Nuget%3F-yes-green.svg)](https://www.nuget.org/packages?q=musoq)
![Tests](https://raw.githubusercontent.com/puchaczov/musoq/master/badges/tests-badge.svg)

Musoq brings SQL power to your data, wherever it lives. Query files, directories, CSVs, and more with familiar SQL syntax – no database required.

## 🌟 Key Features

- **Versatility:** Data sources come as plugins. Visit the [Musoq.DataSources](https://github.com/Puchaczov/Musoq.DataSources) repository where they are all stored..
- **SQL Syntax Variant:** The engine uses SQL syntax variant with support for complex queries.
- **Cross-Platform:** Runs on Linux, Windows, and Docker. MacOS compatibility is anticipated.
- **In-place querying without data movement:** Query data where it resides, without the need to move or load it into a central data store.
- **Extensible architecture for custom data sources:** Add support for custom data sources through a plugin architecture.

## 🚀 Quick Start

To try out Musoq, follow the instructions in our [CLI repository](https://github.com/Puchaczov/Musoq.CLI).

## 💡 Example Queries

Musoq can handle a wide variety of data sources. Here are some examples:

```sql
-- Look for files greater than 1 gig
SELECT 
	FullName 
FROM #os.files('', true) 
WHERE ToDecimal(Length) / 1024 / 1024 / 1024 > 1

-- Look for how many space does the extensions occupies within some directory
SELECT
    Extension,
    Round(Sum(Length) / 1024 / 1024 / 1024, 1) as SpaceOccupiedInGB,
    Count(Extension) as HowManyFiles
FROM #os.files('/some/directory', true)
GROUP BY Extension
HAVING Round(Sum(Length) / 1024 / 1024 / 1024, 1) > 0

-- Get first, last 5 bits from files and consecutive 10 bytes of file with offset of 5 from tail
SELECT
	ToHex(Head(5), '|'),
	ToHex(Tail(5), '|'),
	ToHex(GetFileBytes(10, 5), '|')
FROM #os.files('/some/directory', false)

-- Describe images using AI
SELECT
    llava.DescribeImage(photo.Base64File()),
    photo.FullName
FROM #os.files('/path/to/directory', false) photo 
INNER JOIN #ollama.models('llava:13b', 0.0) llava ON 1 = 1

-- Count tokens in Markdown and C files
SELECT 
   SUM(gpt.CountTokens(f.GetFileContent())) AS TokensCount 
FROM #os.files('/path/to/directory', true) f 
INNER JOIN #openai.gpt('gpt-4') gpt ON 1 = 1 
WHERE f.Extension IN ('.md', '.c')

-- Query CAN DBC files
SELECT  
    ID,
    Name,
    DLC,
    CycleTime
FROM #can.messages('./file.dbc')

-- Compare two directories
SELECT 
    (CASE WHEN SourceFile IS NOT NULL 
     THEN SourceFileRelative 
     ELSE DestinationFileRelative 
     END) AS FullName, 
    (CASE WHEN State = 'TheSame' 
     THEN 'The Same' 
     ELSE State 
     END) AS Status 
FROM #os.dirscompare('E:\DiffDirsTests\A', 'E:\DiffDirsTests\B')

-- Find large files
SELECT 
    FullName 
FROM #os.files('', true) 
WHERE ToDecimal(Length) / 1024 / 1024 / 1024 > 1

-- Compute sentiment on a comments
SELECT 
    csv.PostId,
    csv.Comment,
    gpt.Sentiment(csv.Comment) as Sentiment,
    csv.Date
FROM #separatedvalues.csv('/home/somebody/comments_sample.csv', true, 0) csv
INNER JOIN #openai.gpt('gpt-4-1106-preview') gpt on 1 = 1

-- Query CAN DBC file (messages)
SELECT  
    ID,
    Name,
    DLC,
    CycleTime
FROM #can.messages('./file.dbc')

-- Get only files that extension is .png or .jpg
SELECT 
    FullName 
FROM #os.files('C:/Some/Path/To/Dir', true) 
WHERE Extension = '.png' OR Extension = '.jpg'

-- Group by directory and show size of each
SELECT
	DirectoryName,
	Sum(Length) / 1024 / 1024 as 'MB',
	Min(Length) as 'Min',
	Max(Length) as 'Max',
	Count(FullName) as 'CountOfFiles',
FROM #os.files('/some/path', true)
GROUP BY DirectoryName

-- Prints the values from 1 to 9
SELECT Value FROM #system.range(1, 10)
```

## 🎬 Watch It Live

![Musoq Demo](https://github.com/Puchaczov/Musoq/blob/59603028e8fbe90ce8444077cf3561ff8e698afd/musoq.gif)

## 🛠 Supported Data Sources

- SeparatedValues (CSV, TSV, etc.)
- Archives
- OS (File System - files and directories)
- ...many more, Look at the [Musoq.DataSources](https://github.com/Puchaczov/Musoq.DataSources) repository

## 🔧 Syntax Features

Musoq supports a rich set of SQL-like features:

- Optional query reordering (FROM ... WHERE ... GROUP BY ... HAVING ... SELECT ... SKIP N TAKE N2)
- Use of `*` to select all columns
- GROUP BY and HAVING operators
- SKIP & TAKE operators
- Complex object accessing (`column.Name`)
- User-defined functions and aggregation functions
- Set operators (UNION, UNION ALL, EXCEPT, INTERSECT)
- Parameterizable sources
- LIKE / NOT LIKE operator
- RLIKE / NOT RLIKE operator (regex)
- CONTAINS operator
- CTE expressions
- DESC for schema, schema table constructors and tables
- IN syntax
- INNER, LEFT OUTER, RIGHT OUTER join syntax
- ORDER BY clause

## 🏗 Architecture

### High-level Overview
![Architecture Overview](https://github.com/Puchaczov/Musoq/blob/master/Musoq-Architecture-Engine.png)

### Plugins
Musoq offers a plugin API that all sources use. To learn how to implement your own plugin, you should examine how existing plugins are created.

## 💡 Motivation

Developed out of a need for a versatile tool that could query various data sources with SQL syntax, Musoq aims to minimize the effort and time required for data querying and analysis.

## 📄 License

Musoq is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Note:** While Musoq uses SQL-like syntax, it may not be fully SQL compliant. Some differences may appear, and Musoq implements some experimental syntax and behaviors that are not used by traditional database engines and this is intended!
