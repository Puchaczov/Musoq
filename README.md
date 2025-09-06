# Musoq: SQL Superpowers for Developers

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/Puchaczov/Musoq/graphs/code-frequency)
[![Nuget](https://img.shields.io/badge/Nuget%3F-yes-green.svg)](https://www.nuget.org/packages?q=musoq)
![Tests](https://raw.githubusercontent.com/puchaczov/musoq/master/badges/tests-badge.svg)

**Stop writing throwaway scripts.** Use SQL instead.

Every developer has been there: you need to process some files, check git history, or transform data. Do you write a bash one-liner that breaks on edge cases? A Python script you'll delete in 5 minutes? 

With Musoq, just write a query.

## 🚀 Quick Start

```bash
# Install CLI
# Follow instructions in [CLI repository](https://github.com/Puchaczov/Musoq.CLI)

# Generate some GUIDs
Musoq run query "select NewId() from #system.range(1, 5)"

# Find your largest files  
Musoq run query "select Name, Length from #os.files('/home/user', true) where Length > 1000000 order by Length desc take 10"

# Check git commits this month
Musoq run query "select Count(1) from #git.repository('.') r cross apply r.Commits c where c.CommittedWhen > '2024-11-01'"
```

## 💡 Why SQL for Scripting?

**Instead of this bash nightmare:**
```bash
find . -name "*.js" -exec wc -l {} \; | awk '{sum+=$1} END {print sum}'
```

**Write this:**
```sql
select Sum(Length(f.GetFileContent())) as TotalLines 
from #os.files('.', true) f 
where f.Extension = '.js'
```

**Instead of throwaway Python:**
```python
import os, subprocess, re
# 15 lines of file handling, regex, and loops
```

**Write this:**
```sql
select f.Name, f.Directory 
from #os.files('/project', true) f 
where f.GetFileContent() rlike 'TODO.*urgent'
```

## 🎯 Perfect For Daily Developer Tasks

- **Quick utilities**: Generate data, check file sizes, count lines
- **File system queries**: Find files, compare directories, analyze disk usage  
- **Git insights**: Who changed what, commit patterns, file history
- **Code analysis**: Search patterns, extract metrics, find dependencies
- **Data transformation**: Convert between formats, clean up data
- **System administration**: Process queries, log analysis, monitoring

All with the declarative power of SQL instead of imperative loops and conditionals.

## 🛠 What You Can Query

### Everyday Stuff
```sql
-- Generate test data
select 'User' + ToString(Value) as Username, NewId() as UserId 
from #system.range(1, 100)

-- Find duplicate files by name
select
  Name,
  Count(1) as Duplicates
from #os.files('/downloads', true)
group by Name having Count(Name) > 1

-- Disk space by file type
select
  Extension,
  Sum(Length) / 1024 / 1024 as SizeMB,
  Count(1) as FileCount
from #os.files('/project', true)  
group by Extension
order by Sum(Length) / 1024 / 1024 desc
```

### Development Tasks  
```sql
-- Git contributors last month
select
  c.Author,
  Count(1) as Commits
from #git.repository('.') r
cross apply r.Commits c
where c.CommittedWhen > SubtractDateTimeOffsets(GetDate(), FromString('30.00:00:00'))
group by c.Author
order by c.Commits desc

-- Find TODOs in codebase
select
  files.Name,
  f.LineNumber,
  f.Line
from #os.files('./src', true) files
cross apply #flat.file(files.FullName) f
where files.Extension in ('.cs', '.js', '.py') and f.Line like '%TODO%'

-- Complex C# classes (requires solution analysis)
select
  c.Name,
  c.MethodsCount,
  c.LinesOfCode
from #csharp.solution('./MyProject.sln') s
cross apply s.Projects p
cross apply p.Documents d
cross apply d.Classes c
where c.MethodsCount > 20 order by c.LinesOfCode desc
```

### Data Processing
```sql
-- Transform CSV data
select
  Category,
  Sum(ToDecimal(Amount)) as Total
from #separatedvalues.csv('./sales.csv', true, 0)
group by Category

-- Extract structured data from text using AI
select
  t.ProductName,
  t.Price,
  t.Description  
from #stdin.text('OpenAI', 'gpt-4o') t
where ToDecimal(t.Price) > 100
```

## 🌟 Why You Might Love It

- **No context switching**: Stay in SQL instead of jumping between bash/Python/tools
- **Declarative**: Say what you want, not how to get it
- **Composable**: Complex queries from simple building blocks  
- **Familiar**: You probably already know SQL
- **Fast**: No need to write, debug, and maintain scripts
- **Powerful**: Joins, aggregations, and complex logic built-in

## 📈 Scales From Simple to Sophisticated

Start with basic utilities:
```sql
select NewId() from #system.dual()
```

Scale up to complex analysis:
```sql
-- Analyze codebase evolution over time
with MonthlyStats as (
    select 
        ToString(c.CommittedWhen, 'yyyy-MM') as Month,
        Count(d.Path) as FilesChanged,
        Sum(p.LinesAdded) as LinesAdded,
        Sum(p.LinesDeleted) as LinesDeleted
    from #git.repository('./large-project') r
    cross apply r.Commits c
    cross apply r.DifferenceBetween(c, r.CommitFrom(c.Sha + '^')) d  
    cross apply r.PatchBetween(c, r.CommitFrom(c.Sha + '^')) p
    group by ToString(c.CommittedWhen, 'yyyy-MM')
)
select
  Month,
  FilesChanged,
  LinesAdded - LinesDeleted as NetLines
from MonthlyStats
order by Month desc
take 12
```

## 🎬 Real-World Examples

**File Management:**
```sql
-- Find files not accessed in 6 months
select
  FullName,
  LastAccessTime from #os.files('/old-project', true)
where LastAccessTime < SubtractDateTimeOffsets(GetDate(), FromString('180.00:00:00'))

-- Compare two directories
select
  FullName,
  Status from
#os.dirscompare('/backup', '/current')
where Status != 'The Same'
```

**Development Workflow:**
```sql
-- Which files change together most often?
select f1.Path, f2.Path, Count(1) as CoChanges
from #git.repository('.') r cross apply r.Commits c
cross apply r.DifferenceBetween(c, r.CommitFrom(c.Sha + '^')) f1
cross apply r.DifferenceBetween(c, r.CommitFrom(c.Sha + '^')) f2  
where f1.Path < f2.Path
group by f1.Path, f2.Path
having Count(1) > 5
order by CoChanges desc
```

**Data Processing:**
```sql
-- Extract and count imports from protobuf files
with Imports as (
    select Replace(Replace(Line, 'import "', ''), '";', '') as ImportPath
    from #flat.file('./proto/service.proto') f
    where Line like 'import "%'  
)
select
  ImportPath,
  Count(1) as Usage
from Imports
group by ImportPath
```

## 🚀 Data Sources

Query everything with the same SQL syntax:

**File System & OS**
- Files, directories, processes, metadata
- Archives (ZIP contents without extraction)
- Text files, CSVs, JSON

**Development Tools**  
- Git repositories (commits, diffs, branches)
- C# codebases (classes, methods, complexity)
- Docker containers, Kubernetes resources

**AI & Analysis**
- OpenAI/GPT integration for text/image analysis
- Local LLMs via Ollama
- Extract structure from unstructured data

**Specialized**
- Time-series data and schedules
- CAN bus data for automotive
- Airtable, databases, APIs

## ⚡ Getting Started

1. **[Install CLI](https://github.com/Puchaczov/Musoq.CLI)** - Quick setup guide
2. **Try basic queries** - Generate data, list files, check git
3. **Explore data sources** - See what you can query
4. **Replace your next script** - Use SQL instead

## 📚 Resources

- **[Documentation](https://puchaczov.github.io/Musoq/)** - Guide and examples
- **[Data Sources](https://github.com/Puchaczov/Musoq.DataSources)** - All available plugins
- **[CLI Tool](https://github.com/Puchaczov/Musoq.CLI)** - Command-line interface

## 🎨 Advanced Features

SQL power including:
- Common Table Expressions (CTEs)
- JOINs across different data sources  
- Set operations (UNION, EXCEPT, INTERSECT)
- Regular expressions and pattern matching
- Aggregations
- Custom data type handling through plugins

## 🤔 When to Use Musoq

**✅ Perfect for:**
- One-off data tasks that would need a script
- Combining data from multiple sources  
- Quick analysis and reporting
- File system operations beyond basic commands
- Git repository insights
- Code pattern searches

**❌ Not ideal for:**
- Large-scale data processing (>memory size)
- Real-time/streaming data
- Production ETL pipelines  
- Applications requiring millisecond performance

## 🐛 Philosophy

Musoq is designed around one principle: **eliminate developer friction**.

Stop deciding whether a task is "worth writing a script for." Stop context-switching between tools. Stop debugging bash pipes.

Just write a query.

---

*"Why write loops when you can write queries?"*

## 📄 License

MIT License - see the [LICENSE](LICENSE) file for details.
