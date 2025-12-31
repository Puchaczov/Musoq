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
Musoq run "select NewId() from #system.range(1, 5)"

# Find your largest files  
Musoq run "select Name, Length from #os.files('/home/user', true) where Length > 1000000 order by Length desc take 10"

# Check git commits this month
Musoq run "select Count(1) from #git.repository('.') r cross apply r.Commits c where c.CommittedWhen > '2024-11-01'"
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

- **Huge standard library**: Nearly 1000 standard library methods
- **Quick utilities**: Generate data, check file sizes, count lines
- **File system queries**: Find files, compare directories, analyze disk usage  
- **Git insights**: Who changed what, commit patterns, file history
- **Code analysis**: Search patterns, extract metrics, find dependencies
- **Data transformation**: Convert between formats, clean up data
- **System administration**: Process queries, log analysis, monitoring

All with the declarative power of SQL instead of imperative loops and conditionals.

## 🛠 What You Can Query

### 🔀 Git Repository Queries (`#git`)

#### List Recent Commits
Query commit history with author information.

```sql
select 
    c.Sha,
    c.MessageShort,
    c.Author,
    c.CommittedWhen
from #git.repository('./repo') r 
cross apply r.Commits c
```

#### Query Commits Directly
Faster access to commits without full repository context.

```sql
select
    c.Sha,
    c.Author,
    c.Message
from #git.commits('./repo') c
where c.Author = 'john.doe'
```

#### List All Branches
Query all branches with their tip commit.

```sql
select
    b.FriendlyName,
    b.IsRemote,
    b.Tip.Sha
from #git.branches('./repo') b
```

#### Compare Branches
Find differences between two branches.

```sql
select 
    Difference.Path,
    Difference.ChangeKind
from #git.repository('./repo') repository 
cross apply repository.DifferenceBetween(
    repository.BranchFrom('main'), 
    repository.BranchFrom('feature/my-feature')
) as Difference
```

#### List Tags with Annotations
Query all tags in a repository with their metadata.

```sql
select
    t.FriendlyName,
    t.Message,
    t.IsAnnotated,
    t.Commit.Sha
from #git.tags('./repo') t
```

#### Track File History
See all changes to a specific file over time.

```sql
select
    h.CommitSha,
    h.Author,
    h.FilePath,
    h.ChangeType
from #git.filehistory('./repo', 'README.md') h
```

#### Analyze Branch-Specific Commits
Find commits that are specific to a feature branch.

```sql
with BranchInfo as (
    select
        c.Sha as CommitSha,
        c.Message as CommitMessage,
        c.Author as CommitAuthor
    from #git.repository('./repo') r 
    cross apply r.SearchForBranches('feature/my-feature') b
    cross apply b.GetBranchSpecificCommits(r.Self, b.Self, true) c
)
select CommitSha, CommitMessage, CommitAuthor from BranchInfo
```

#### Query Commit Parents
Analyze commit relationships for merge analysis.

```sql
select 
    c.Sha, 
    p.Sha as ParentSha
from #git.commits('./repo') c 
cross apply c.Parents as p
```

---

### 🏞️ Photo Analysis

#### Generate hashtags using LLM
Generate descriptions of photos by local offline model, then pass the description to more powerfull model to generate hash tags from the descriptions. Avoiding leaking the photos to external model provider.

```sql
with PhotosDescription as (
    select 
        f.Name as Name, 
        l.AskImage('this is the photo of my little child I want you to describe. Be conscise, use only single statement.', f.Base64File()) as Description 
    from #os.files('/some/folder/with/photos', false) f 
    cross apply #ollama.llm('llama3.2-vision:11b-instruct-q4_K_M') l
)
select
    p.Name,
    p.Description,
    l.LlmPerform('this is the description of the photo I want you generate hashtags for. It comes from my child photo album. Return only hashtags separated with comma (#something, #somethingElse). Comma is very important to separate hashtags. Dont forget about it. No description or explanation.', p.Description) as HashTags
from PhotosDescription p cross apply #openai.gpt('gpt-4o', 4096, 0.0) l
```

### Structured extraction from unstructured data
Pass image of receipt, receive shop, product name and price.

```
--image passed to stdin by command: ./Musoq.exe image encode "D:/Some/Receipt.jpg"

select s.Shop, s.ProductName, s.Price from #stdin.LlmExtractFromImage() s
```

Same story but this time we are expecting specific types
```
table Receipt {
    Shop 'System.String',
    ProductName 'System.String',
    Price 'System.Decimal'
};
couple #stdin.LlmExtractFromImage() with table Receipt as SourceOfReceipts;
select s.Shop, s.ProductName, s.Price from SourceOfReceipts('OpenAi', 'gpt-4o') s
```

---

### 🔬 C# Code Analysis (`#csharp`)

#### List All Classes in Solution
Find all classes across a C# solution with their metrics.

```sql
select 
    c.Name,
    c.Namespace,
    c.MethodsCount,
    c.PropertiesCount,
    c.LinesOfCode
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects p 
cross apply p.Documents d 
cross apply d.Classes c
```

#### Query All Types in a Project
Quick access to all types (classes, interfaces, enums) in a project.

```sql
select 
    t.Name,
    t.IsClass,
    t.IsInterface,
    t.IsEnum
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects p 
cross apply p.Types t
```

#### Find Methods with High Complexity
Identify methods that may need refactoring based on cyclomatic complexity.

```sql
select
    c.Name as ClassName,
    m.Name as MethodName,
    m.CyclomaticComplexity,
    m.LinesOfCode
from #csharp.solution('./MySolution.sln') s 
cross apply s.GetClassesByNames('MyClass') c
cross apply c.Methods m
where m.CyclomaticComplexity > 5
```

#### Analyze Method Body Structure
Check for empty methods, stub implementations, and statement counts.

```sql
select
    m.Name,
    m.HasBody,
    m.IsEmpty,
    m.StatementsCount,
    m.BodyContainsOnlyTrivia
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects p 
cross apply p.Documents d 
cross apply d.Classes c
cross apply c.Methods m
where c.Name = 'MyClass'
```

#### Analyze Property Accessors
Find auto-properties, init-only setters, and property patterns.

```sql
select
    p.Name,
    p.Type,
    p.IsAutoProperty,
    p.HasGetter,
    p.HasSetter,
    p.HasInitSetter
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects p 
cross apply p.Documents d 
cross apply d.Classes c
cross apply c.Properties p
where c.Name = 'MyClass'
```

#### Find References to a Class
Locate all usages of a specific class across the solution.

```sql
select 
    r.Name,
    rd.StartLine,
    rd.StartColumn,
    rd.EndLine,
    rd.EndColumn
from #csharp.solution('./MySolution.sln') s
cross apply s.GetClassesByNames('MyClass') c
cross apply s.FindReferences(c.Self) rd
cross apply rd.ReferencedClasses r
```

#### Query Interface Definitions
List interfaces with their methods and properties.

```sql
select
    i.Name,
    i.FullName,
    i.Namespace,
    i.BaseInterfaces,
    i.Methods,
    i.Properties
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects pr 
cross apply pr.Documents d 
cross apply d.Interfaces i
```

#### Analyze Enums
List enums with their members.

```sql
select
    e.Name,
    e.FullName,
    e.Namespace,
    e.Members
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects pr 
cross apply pr.Documents d 
cross apply d.Enums e
```

#### Query Project References
List all project-to-project references.

```sql
select
    p.Name as ProjectName,
    ref.Name as ReferencedProject
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects p 
cross apply p.ProjectReferences ref
```

#### Query Library References
List all library/assembly references in projects.

```sql
select
    p.Name as ProjectName,
    lib.Name as LibraryName,
    lib.Version,
    lib.Location
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects p 
cross apply p.LibraryReferences lib
```

#### Query NuGet Packages
List all NuGet packages with license information.

```sql
select 
    p.Name as ProjectName,
    np.Id as PackageId,
    np.Version,
    np.License,
    np.Authors,
    np.IsTransitive
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects p 
cross apply p.GetNugetPackages(false) np
```

#### Analyze Class Attributes
Find classes decorated with specific attributes.

```sql
select
    c.Name,
    a.Name as AttributeName,
    a.ConstructorArguments
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects pr 
cross apply pr.Documents d 
cross apply d.Classes c
cross apply c.Attributes a
```

#### Query Method Parameters
Analyze method parameters with their modifiers.

```sql
select
    m.Name as MethodName,
    p.Name as ParamName,
    p.Type,
    p.IsOptional,
    p.IsParams,
    p.IsRef,
    p.IsOut
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects pr 
cross apply pr.Documents d 
cross apply d.Classes c
cross apply c.Methods m
cross apply m.Parameters p
```

#### Calculate Lack of Cohesion
Analyze class design metrics.

```sql
select 
    c.Name,
    c.MethodsCount,
    c.FieldsCount,
    c.LackOfCohesion,
    c.InheritanceDepth
from #csharp.solution('./MySolution.sln') s 
cross apply s.Projects p 
cross apply p.Documents d 
cross apply d.Classes c
where c.MethodsCount > 2
```

---

### 🔗 Combined Data Source Queries

#### Analyze Git Repositories from File System
Discover and analyze multiple Git repositories.

```sql
with GitRepos as (
    select 
        dir.Parent.Name as RepoName,
        dir.FullName as GitPath
    from #os.directories('./projects', true) dir
    where dir.Name = '.git'
)
select 
    r.RepoName,
    Count(c.Sha) as CommitCount
from GitRepos r 
cross apply #git.repository(r.GitPath) repo 
cross apply repo.Commits c
group by r.RepoName
order by CommitCount desc
```

#### Diff Files with Hash Comparison
Compare directories using file hashes to detect modifications.

```sql
with SourceFiles as (
    select GetRelativePath('./source') as RelPath, Sha256File() as Hash 
    from #os.files('./source', true)
), 
TargetFiles as (
    select GetRelativePath('./target') as RelPath, Sha256File() as Hash 
    from #os.files('./target', true)
)
select 
    s.RelPath,
    (case when s.Hash <> t.Hash then 'modified' else 'same' end) as Status
from SourceFiles s 
inner join TargetFiles t on s.RelPath = t.RelPath
```

---

### 📁 File System Queries (`#os`)

#### List Files with Size Information
Find all files in a directory with their sizes formatted in human-readable format.

```sql
select 
    Name, 
    ToDecimal(Length) / 1024 as SizeInKB
from #os.files('./directory', true)
where Extension = '.txt'
```

#### Calculate SHA256 Hash of Files
Compute cryptographic hashes for file integrity verification.

```sql
select 
    Name, 
    Sha256File() as Hash
from #os.files('./directory', false)
where Extension = '.dll'
```

#### Compare Two Directories
Find differences between two directories (added, removed, modified files).

```sql
select 
    SourceFileRelative,
    DestinationFileRelative,
    State
from #os.dirscompare('./source', './destination')
where State <> 'TheSame'
```

---

### 📊 CSV/Separated Values (`#separatedvalues`)

#### Basic CSV Query with Aggregation
Analyze banking transactions and calculate monthly income/outcome.

```sql
select 
    ExtractFromDate(OperationDate, 'month') as Month,
    SumIncome(ToDecimal(Money)) as Income,
    SumOutcome(ToDecimal(Money)) as Outcome,
    SumIncome(ToDecimal(Money)) + SumOutcome(ToDecimal(Money)) as Balance
from #separatedvalues.comma('./transactions.csv', true, 0)
group by ExtractFromDate(OperationDate, 'month')
```

#### Join Two CSV Files
Join persons with their grades from separate CSV files.

```sql
select 
    persons.Name, 
    persons.Surname, 
    grades.Subject, 
    grades.Grade
from #separatedvalues.comma('./Persons.csv', true, 0) persons 
inner join #separatedvalues.comma('./Gradebook.csv', true, 0) grades 
    on persons.Id = grades.PersonId
```

#### Typed CSV Query
Read CSV with explicit column types for proper data handling.

```sql
table Employees {
   Id 'System.Int32',
   Name 'System.String',
   Salary 'System.Decimal'
};
couple #separatedvalues.comma with table Employees as SourceOfEmployees;
select Id, Name, Salary from SourceOfEmployees('./employees.csv', true, 0)
where Salary > 50000
```

---

### 🗂️ JSON Queries (`#json`)

#### Query JSON Array
Extract data from a JSON file using a schema definition.

```sql
select 
    Name, 
    Age, 
    Length(Books) as BookCount
from #json.file('./data.json', './data.schema.json')
where Age > 18
```

---

### 📦 Archive Queries (`#archives`)

#### List Archive Contents
Read contents of ZIP or TAR archives and extract text content.

```sql
select 
    Key as FileName, 
    IsDirectory,
    (case when IsDirectory = false then GetTextContent() else '' end) as Content
from #archives.file('./archive.zip')
where Key like '%.txt'
```

---

### ⏰ Time Queries (`#time`)

#### Generate Date Range
Create a sequence of dates for reporting or analysis.

```sql
select 
    Day, 
    Month, 
    Year, 
    DayOfWeek
from #time.interval('2024-01-01 00:00:00', '2024-12-31 00:00:00', 'days')
```

#### Filter Weekend Days
Find only weekend days (Saturday=6, Sunday=0 in DayOfWeek).

```sql
select Day, DayOfWeek
from #time.interval('2024-01-01 00:00:00', '2024-01-31 00:00:00', 'days')
where DayOfWeek = 0 or DayOfWeek = 6
```

---

### 🔧 System Utilities (`#system`)

#### Number Range Generation
Generate a sequence of numbers for various purposes.

```sql
select Value 
from #system.range(1, 100)
where Value % 2 = 0
```

#### Dual Table for Calculations
Use dual table for single-row calculations.

```sql
select 
    2 + 2 as Sum,
    10 * 5 as Product,
    ToDecimal(7) / 3 as Division
from #system.dual()
```

---

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
