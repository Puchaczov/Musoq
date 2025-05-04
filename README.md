# Musoq: SQL Superpowers for Developers

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/Puchaczov/Musoq/graphs/code-frequency)
[![Nuget](https://img.shields.io/badge/Nuget%3F-yes-green.svg)](https://www.nuget.org/packages?q=musoq)
![Tests](https://raw.githubusercontent.com/puchaczov/musoq/master/badges/tests-badge.svg)

**One query language for everything.** Musoq brings the power of SQL to diverse data sources without requiring a database. Query your filesystem, Git repos, code structure, and more—all with the SQL syntax you already know.

## 🚀 Quick Start

To try out Musoq, follow the instructions in [CLI repository](https://github.com/Puchaczov/Musoq.CLI).

## 🌟 Why Musoq?

Musoq transforms how developers interact with their data:

- **Query anything with SQL:** From files and directories to Git history and C# code structure
- **Data insights without databases:** Analyze in place—no import/export necessary
- **Beyond grep and find:** Perform complex data operations with a familiar, declarative language
- **Unified syntax across sources:** Use one query language for all your development assets
- **Extensible plugin architecture:** Easily add new data sources to your query toolkit

Musoq exists to eliminate tedious loops and scripts for everyday data tasks, helping you extract insights faster.

## 🎯 Perfect For

- **Code analysis:** Extract metrics and patterns from your codebase
- **Git repository insights:** Understand contributor patterns and code evolution
- **Data transformation:** Convert between formats with minimal effort
- **System administration:** Query processes, files, and system metadata
- **AI-enhanced analysis:** Combine SQL with AI models for advanced text and image analysis

## 🛠 Supported Data Sources

### Development Assets
- **Git:** Extract insights from commit history, branches, diffs, and more
- **Roslyn:** Analyze C# code structure, methods, complexity, and patterns
- **OS:** Query your filesystem, processes, and system metadata
- **Docker/K8s:** Explore containers and Kubernetes resources *(experimental)*

### Data & Documents
- **Archives:** Query ZIP and archive contents without extraction *(experimental)*
- **Files:** Parse text files, CSVs, JSONs, and more with SQL
- **Images:** Extract metadata and use AI to analyze image content
- **Databases:** Direct queries to Postgres, SQLite, and Airtable

### AI-Enhanced Analysis
- **OpenAI/Ollama:** Extract structured data from unstructured content
- **Image Understanding:** Use LLMs for understanding image content
- **Text Extraction:** Turn plaintext into structured, queryable data

View the [practilcal examples and applications](https://puchaczov.github.io/Musoq/practical-examples-and-applications.html) from the docs to understand how you can use it.

## 💡 Real-World Examples

### Git Insights: Who's Contributing What?

```sql
-- Top contributors by number of commits
select
    c.AuthorEmail,
    Count(c.Sha) as CommitCount
from #git.repository('/path/to/repo') r
cross apply r.Commits c
group by c.AuthorEmail
having Count(c.Sha) > 10
order by Count(c.Sha) desc
take 10
```

### Code Quality Analysis: Finding Complex Methods

```sql
-- Top 3 methods with highest complexity
select
    c.Name as ClassName,
    m.Name as MethodName,
    Max(m.CyclomaticComplexity) as HighestComplexity
from #csharp.solution('/some/path/Musoq.sln') s
cross apply s.Projects p 
cross apply p.Documents d 
cross apply d.Classes c 
cross apply c.Methods m 
group by c.Name, m.Name
order by Max(m.CyclomaticComplexity) desc
take 3
```

### Storage Management: Where's Your Space Going?

```sql
-- Analyze disk space usage by file extension
SELECT
    Extension,
    Round(Sum(Length) / 1024 / 1024 / 1024, 1) as SpaceOccupiedInGB,
    Count(Extension) as FileCount
FROM #os.files('/some/directory', true)
GROUP BY Extension
HAVING Round(Sum(Length) / 1024 / 1024 / 1024, 1) > 0
ORDER BY SpaceOccupiedInGB DESC
```

### AI-Enhanced Analysis: Extract Structure from Unstructured Data

```sql
-- Extract structured data from unstructured text
select s.Who, s.Age from #stdin.text('from-text-extraction-model') s 
where ToInt32(s.Age) > 26 and ToInt32(s.Age) < 75

-- Extract product info from receipt images
select s.Shop, s.ProductName, s.Price 
from #stdin.image('from-image-extraction-model') s
```

## 🤔 When to Use Musoq

Musoq shines when you need to:

- **Extract insights** from code, Git history, or system data
- **Transform data** between formats without writing custom scripts
- **Combine sources** that normally don't speak to each other
- **Avoid the overhead** of importing data into a database
- **Leverage SQL skills** for non-database tasks

Musoq is ideal for small to medium datasets where the cognitive efficiency of SQL outweighs raw performance requirements.

## 📑 Documentation & Resources

- **[Documentation](https://puchaczov.github.io/Musoq/)**: Project overview and documentation
- **[Data Sources](https://github.com/Puchaczov/Musoq.DataSources)**: All data sources resides here
- **[CLI Tool](https://github.com/Puchaczov/Musoq.CLI)**: CLI tool that allows to runs musoq queries

## 🔧 Advanced Features

Musoq supports a rich set of SQL-like features:

- Parameterizable sources
- Common Table Expressions (CTEs)
- CROSS/OUTER APPLY operators
- Set operations (UNION, EXCEPT, INTERSECT)
- Regular expression matching
- Advanced filtering with WHERE/HAVING
- JOIN operations across disparate sources
- Pagination with SKIP/TAKE

## 🚀 Roadmap

Key areas of development include:

- Comprehensive documentation
- Runtime efficiency improvements
- Parallel query execution
- Recursive CTE support
- Enhanced JSON/XML handling
- Subquery support
- Improved error handling

Have an idea? [Submit a feature request](https://github.com/Puchaczov/Musoq/issues/new).

## 🌱 Project Maturity

Musoq is an actively developed tool used in professional environments. It's designed with these principles:

- **Read-only by design:** Focuses exclusively on querying, not modifying data
- **Developer-friendly syntax:** Prioritizes ease of use over strict SQL compliance
- **Right-sized solutions:** Optimized for small to medium datasets
- **Pragmatic innovations:** Introduces syntax extensions when they simplify complex tasks

I use Musoq daily across various workplaces, refining it based on real-world needs.

## 📄 License

Musoq is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🛠 Supported Data Sources (Complete)

### Operating System & Files
- **OS**: Query your filesystem, processes, and system metadata - from file contents to EXIF data
- **Archives**: Treat ZIP and other archive files as queryable tables
- **FlatFile**: Work with any text-based files as database tables
- **SeparatedValues**: Handle CSV, TSV and other delimited files with SQL capabilities

### Development Tools
- **Git**: Query Git repositories - analyze commits, diffs, branches and more
- **Roslyn**: Analyze C# code structure, metrics and patterns using SQL
- **Docker**: Query containers, images and Docker resources *(experimental)*
- **Kubernetes**: Interact with K8s clusters, pods and services *(experimental)*

### Database & Storage
- **Airtable**: Access Airtable bases through SQL interface
- **Json**: Query JSON files with SQL syntax

### AI & Analysis
- **OpenAI**: Enhance queries with GPT models for text extraction and analysis
- **Ollama**: Use open-source LLMs for data extraction and processing

### Domain-Specific
- **CANBus**: Analyze CAN bus data and DBC files for automotive applications
- **Time**: Work with time-series data and schedules

### Utility
- **System**: Core utilities including ranges, dual tables and common functions

## 🎬 More Examples

### Git Repository Analysis

```sql
-- How many commits does the repository have
select
    Count(1) as CommitsCount
from #git.repository('D:\repos\efcore') r
cross apply r.Commits c
group by 'fake'
```

### Solution Analysis

```sql
-- How many lines of code does the project contain?
select 
    Sum(c.LinesOfCode) as TotalLinesOfCode,
    Sum(c.MethodsCount) as TotalMethodsCount
from #csharp.solution('/some/path/Musoq.sln') s 
cross apply s.Projects p 
cross apply p.Documents d 
cross apply d.Classes c 
group by 'fake'

-- Extract all SQL queries from tests
select 
    p.RowNumber() as RowNumber, 
    p.Name, 
    c.Name, 
    m.Name, 
    g.ToBase64(g.GetBytes(g.LlmPerform('You are C# developer. Your task is to extract SQL query without any markdown characters. If no sql, then return empty string', m.Body))) as QueryBase64
from #csharp.solution('/some/path/Musoq.sln') s 
inner join #openai.gpt('gpt-4o') g on 1 = 1 
cross apply s.Projects p 
cross apply p.Documents d 
cross apply d.Classes c 
cross apply c.Attributes a 
cross apply c.Methods m 
where a.Name = 'TestClassAttribute'
```

### File System Operations

```sql
-- Find large files
SELECT 
	FullName 
FROM #os.files('/some/path', true) 
WHERE ToDecimal(Length) / 1024 / 1024 / 1024 > 1

-- Query image EXIF metadata
SELECT
    f.Name,
    m.DirectoryName,
    m.TagName,
    m.Description
FROM #os.files('./Images', false) f CROSS APPLY #os.metadata(f.FullName) m
WHERE f.Extension = '.jpg'

-- Compare directories
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
```

### Archive Exploration

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

### Data Extraction & Transformation

```sql
-- Count word frequencies within text
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

-- Transform imports in proto files
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
```

### AI Integration

```sql
-- Describe images using AI
SELECT
    llava.DescribeImage(photo.Base64File()),
    photo.FullName
FROM #os.files('/path/to/directory', false) photo 
INNER JOIN #ollama.models('llava:13b', 0.0) llava ON 1 = 1

-- Count tokens in files
SELECT 
   SUM(gpt.CountTokens(f.GetFileContent())) AS TokensCount 
FROM #os.files('/path/to/directory', true) f 
INNER JOIN #openai.gpt('gpt-4') gpt ON 1 = 1 
WHERE f.Extension IN ('.md', '.c')

-- Sentiment analysis on comments
SELECT 
    csv.PostId,
    csv.Comment,
    gpt.Sentiment(csv.Comment) as Sentiment,
    csv.Date
FROM #separatedvalues.csv('/home/somebody/comments_sample.csv', true, 0) csv
INNER JOIN #openai.gpt('gpt-4-1106-preview') gpt on 1 = 1
```

### Domain Specific Analysis

```sql
-- Analyze CAN bus data
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

## 🏗 Architecture

### High-level Overview
![Architecture Overview](https://github.com/Puchaczov/Musoq/blob/master/Musoq-Architecture-Engine.png)

### Plugins
Musoq offers a plugin API that all sources use. To learn how to implement your own plugin, you should examine how existing plugins are created.

---

**Note:** While Musoq uses SQL-like syntax, it may not be fully SQL compliant. Some differences may appear, and Musoq implements some experimental syntax and behaviors that are not used by traditional database engines. This is by design to optimize for developer productivity and clarity.
