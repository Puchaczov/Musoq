# Schema and Data Source Syntax

## Overview

Musoq's power comes from its ability to query diverse data sources using a unified SQL-like syntax. The schema system provides a consistent interface to access files, Git repositories, databases, APIs, and more.

## Data Source Syntax

### Basic Schema Syntax
All data sources follow the pattern:
```sql
#schema.method(parameter1, parameter2, ...)
```

**Components:**
- `#` - Prefix identifying a schema
- `schema` - The schema name (e.g., `os`, `git`, `csharp`)
- `method` - The method within the schema (e.g., `files`, `repository`)
- `parameters` - Method-specific parameters in parentheses

### Schema Discovery
Use the `desc` command to explore available columns:

```sql
desc #os.files('/path', true)
desc #git.repository('/repo/path') 
desc #csharp.solution('/path/to/solution.sln')
```

## Core Data Sources

### Operating System (`#os`)

#### Files and Directories
```sql
-- List files in directory
select Name, Length, Extension 
from #os.files('/path/to/directory', recursive)

-- Parameters:
-- - path: Directory path (string)
-- - recursive: Include subdirectories (boolean)
```

#### File Metadata
```sql
-- Get file metadata including EXIF data
select DirectoryName, TagName, Description
from #os.metadata('/path/to/file.jpg')
```

#### Directory Comparison
```sql
-- Compare two directories
select FullName, Status
from #os.dirscompare('/path/to/dir1', '/path/to/dir2')
```

### Git Repositories (`#git`)

#### Repository Analysis
```sql
-- Access Git repository
select * from #git.repository('/path/to/repository') r

-- Available sub-properties:
-- r.Commits - All commits
-- r.Branches - All branches  
-- r.Tags - All tags
-- r.Files - Files in repository
```

#### Commit History
```sql
-- Query commit information
select Sha, Message, AuthorEmail, Date
from #git.repository('/repo/path') r
cross apply r.Commits c
```

#### Branch Information
```sql
-- List all branches
select Name, IsRemote, Tip
from #git.repository('/repo/path') r  
cross apply r.Branches b
```

### C# Code Analysis (`#csharp`)

#### Solution Analysis
```sql
-- Analyze entire solution
select * from #csharp.solution('/path/to/solution.sln') s

-- Available sub-properties:
-- s.Projects - All projects
-- s.Projects.Documents - Source files
-- s.Projects.Documents.Classes - Classes in files
-- s.Projects.Documents.Classes.Methods - Methods in classes
```

#### Class and Method Analysis
```sql
-- Find complex methods
select 
    c.Name as ClassName,
    m.Name as MethodName,
    m.CyclomaticComplexity
from #csharp.solution('/path/solution.sln') s
cross apply s.Projects p
cross apply p.Documents d  
cross apply d.Classes c
cross apply c.Methods m
where m.CyclomaticComplexity > 10
```

### Artificial Intelligence (`#openai`, `#ollama`)

#### OpenAI Integration
```sql
-- Use GPT models for analysis
select 
    gpt.Sentiment(Comment) as Sentiment,
    Comment
from #flat.file('/comments.txt') f
inner join #openai.gpt('gpt-4') gpt on 1 = 1
```

#### Local AI Models (Ollama)
```sql
-- Use local models
select 
    llama.DescribeImage(img.Base64File()) as Description,
    img.Name
from #os.files('/images', false) img
inner join #ollama.models('llava:13b', 0.0) llama on 1 = 1
where img.Extension in ('.jpg', '.png')
```

### File Formats

#### CSV and Delimited Files
```sql
-- Query CSV files
select Column1, Column2, Column3
from #separatedvalues.csv('/path/to/file.csv', hasHeaders, skipLines)

-- Parameters:
-- - path: File path
-- - hasHeaders: First row contains headers (boolean)
-- - skipLines: Number of lines to skip (integer)
```

#### JSON Files
```sql
-- Query JSON data
select PropertyName, PropertyValue
from #json.file('/path/to/file.json')
```

#### Archive Files
```sql
-- Query contents of ZIP files
select Key, IsDirectory, Size
from #archives.file('/path/to/archive.zip')
```

### Database Connectivity

#### Airtable
```sql
-- Query Airtable bases
select * from #airtable.table('baseId', 'tableName', 'apiKey')
```

## Parameter Types and Guidelines

### String Parameters
- Always use single quotes: `'/path/to/file'`
- Escape single quotes with double quotes: `'Can''t stop'`

### Boolean Parameters  
- Use `true` or `false` (case-insensitive)
- **Not** `1`/`0` or `'true'`/`'false'`

### Numeric Parameters
- Integers: `42`, `0`, `-10`
- Decimals: `3.14`, `0.5`

### Example with Mixed Parameters
```sql
select Name, Size
from #os.files('/home/user/documents', true)  -- string, boolean
where Size > 1000000                          -- numeric comparison
```

## Advanced Schema Usage

### Parameterized Sources with Variables
```sql
-- Using variables in paths (when supported by execution environment)
select * from #os.files(@DirectoryPath, @RecursiveFlag)
```

### Cross Apply with Schema Methods
```sql
-- Process each file's metadata
select 
    f.Name,
    m.TagName,
    m.Description  
from #os.files('/photos', false) f
cross apply #os.metadata(f.FullName) m
where f.Extension = '.jpg'
```

### Multiple Schema Integration
```sql
-- Combine file system and AI analysis
select 
    f.Name,
    ai.ExtractText(f.GetContent()) as ExtractedText
from #os.files('/documents', true) f
inner join #openai.gpt('gpt-4') ai on 1 = 1
where f.Extension = '.pdf'
```

## Schema Aliases and Reusability

While you cannot alias schemas themselves, you can alias their results:

```sql
-- Alias the result of a schema method
select fs.Name, fs.Length
from #os.files('/path', true) fs
where fs.Extension = '.txt'
```

## Error Handling

### Common Schema Errors
1. **Invalid schema name**: `#invalid.method()` - schema doesn't exist
2. **Invalid method name**: `#os.invalidmethod()` - method doesn't exist  
3. **Wrong parameter count**: `#os.files('/path')` - missing required parameter
4. **Wrong parameter type**: `#os.files('/path', 'true')` - boolean expected, string provided

### Debugging Tips
1. Use `desc` to verify available schemas and methods
2. Check parameter types and counts
3. Verify file paths exist and are accessible
4. Test with simple queries before building complex ones

## Next Steps

- Learn about [table definitions](./table-definitions.md) for custom schema coupling
- Explore [cross apply operations](./cross-outer-apply.md) for advanced data source relationships
- See [practical examples](./examples-filesystem-analysis.md) of schema usage in real scenarios