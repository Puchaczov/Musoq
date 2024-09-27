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

## 💡 Where To Use It

Musoq might be using in various places, including:

### 📂 File System Analysis

```sql
-- Look for files greater than 1 gig
SELECT 
	FullName 
FROM #os.files('/some/path', true) 
WHERE ToDecimal(Length) / 1024 / 1024 / 1024 > 1

-- Look for how many space does the extensions occupies within some directory
SELECT
    Extension,
    Round(Sum(Length) / 1024 / 1024 / 1024, 1) as SpaceOccupiedInGB,
    Count(Extension) as HowManyFiles
FROM #os.files('/some/directory', true)
GROUP BY Extension
HAVING Round(Sum(Length) / 1024 / 1024 / 1024, 1) > 0

-- Query your images folder, filter to include only .jpg files and show it's EXIF metadata
SELECT
    f.Name,
    m.DirectoryName,
    m.TagName,
    m.Description
FROM #os.files('./Images', false) f CROSS APPLY #os.metadata(f.FullName) m
WHERE f.Extension = '.jpg'

-- Get first, last 5 bits from files and consecutive 10 bytes of file with offset of 5 from tail
SELECT
	ToHex(Head(5), '|'),
	ToHex(Tail(5), '|'),
	ToHex(GetFileBytes(10, 5), '|')
FROM #os.files('/some/directory', false)

--diff between two folders
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

-- Compute Sha on files
SELECT
   FullName,
   f.Sha256File()
FROM #os.files('@qfs/', false) f
```

### 📦 Archive Exploration

```sql
-- Query .csv files from archive file
table PeopleDetails {
	Name 'System.String',
	Surname 'System.String',
	Age 'System.Int32'
};
couple #separatedvalues.comma with table PeopleDetails as SourceOfPeopleDetails;
with Files as (
	select 
		a.Key as InZipPath
	from #archives.file('./Files/Example2/archive.zip') a
	where 
		a.IsDirectory = false and
		a.Contains(a.Key, '/') = false and 
		a.Key like '%.csv'
)
select 
	f.InZipPath, 
	b.Name, 
	b.Surname, 
	b.Age 
from #archives.file('./Files/Example2/archive.zip') a
inner join Files f on f.InZipPath = a.Key
cross apply SourceOfPeopleDetails(a.GetStreamContent(), true, 0) as b;
```

### 🖼️ Image Analysis with AI

```sql
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

-- Extract data from recipe image
select s.Shop, s.ProductName, s.Price from #stdin.image('OpenAi', 'gpt-4o') s

-- Compute sentiment on a comments
SELECT 
    csv.PostId,
    csv.Comment,
    gpt.Sentiment(csv.Comment) as Sentiment,
    csv.Date
FROM #separatedvalues.csv('/home/somebody/comments_sample.csv', true, 0) csv
INNER JOIN #openai.gpt('gpt-4-1106-preview') gpt on 1 = 1
```

🔍 SQL-Powered Data Extraction

```sql
-- Extract imports from proto file:
-- import "some/some_message_1"
-- ant turn them into:
-- some/SomeMessage1
with Events as (
    select
        Replace(
            Replace(
                Line,
                'import "',
                ''
            ),
            '.proto";',
            ''
        ) as Namespace
    from #flat.file('/path/to/file.proto') f
    where
        Length(Line) > 6 and
        Head(Line, 6) = 'import' and
        IndexOf(Line, 'some') <> -1
)
select
    Choose(
        0,
        Split(e.Namespace, '/')
    ) +
    '/' +
    Replace(
        ToTitleCase(
            Choose(
                1,
                Split(e.Namespace, '/')
            )
        ),
        '_',
        ''
    ) as Events
from Events e

-- Count word frequencies from text
with p as (
    select 
        Replace(Replace(ToLowerInvariant(w.Value), '.', ''), ',', '') as Word
    from #flat.file('/some/path/to/text/file.txt') f cross apply f.Split(f.Line, ' ') w
)
select
    Count(p.Word, 1) as AllWordsCount, 
    Count(p.Word) as SpecificWordCount,
    Round(ToDecimal((Count(p.Word) * 100)) / Count(p.Word, 1), 2) as WordFrequencies,
    Word
from p group by p.Word having Count(p.Word) > 1
```

### 🤖 AI-Assisted Text Structuring

```sql
-- Extract structured data from unstructured text
select s.Who, s.Age from #stdin.text('Ollama', 'llama3.1') s where ToInt32(s.Age) > 26 and ToInt32(s.Age) < 75
```

### 🔄 Universal Table Querying

```sql
-- Count occurrences of each name in a table with headers
select t.Name, Count(t.Name) from #stdin.table(true) t group by t.Name having Count(t.Name) > 1
```

### 🔧 CAN DBC File Analysis

```sql
select 
    m.Id, 
    m.Name, 
    m.DLC, 
    m.Transmitter, 
    m.Comment as MessageComment, 
    m.CycleTime,
    s.Name, 
    s.StartBit, 
    s.Length, 
    s.ByteOrder, 
    s.InitialValue, 
    s.Factor, 
    s.IsInteger, 
    s.Offset, 
    s.Minimum, 
    s.Maximum, 
    s.Unit,
    s.Comment as SignalsComment
from #can.messages('@qfs/Model3CAN.dbc') m cross apply m.Signals s
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

- Parameterizable sources
- Optional query reordering (FROM ... WHERE ... GROUP BY ... HAVING ... SELECT ... SKIP N TAKE N2)
- Use of `*` to select all columns
- GROUP BY and HAVING operators
- SKIP & TAKE operators
- Set operators (UNION, UNION ALL, EXCEPT, INTERSECT)
- LIKE / NOT LIKE operator
- RLIKE / NOT RLIKE operator (regex)
- CONTAINS operator
- CTE expressions
- IN operator
- INNER, LEFT OUTER, RIGHT OUTER JOIN operator
- ORDER BY operator
- CROSS / OUTER APPLY operator

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
