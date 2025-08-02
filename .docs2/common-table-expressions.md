# Common Table Expressions (CTEs)

## Overview

Common Table Expressions (CTEs) in Musoq provide a powerful way to create temporary named result sets that can be referenced within a query. CTEs improve query readability, enable complex data transformations, and support recursive operations.

## Basic CTE Syntax

### Simple CTE Structure
```sql
with cte_name as (
    select column1, column2
    from data_source
    where condition
)
select * from cte_name;
```

### Multiple CTEs
```sql
with 
first_cte as (
    select column1, column2 from source1
),
second_cte as (
    select column3, column4 from source2
)
select * from first_cte
union all
select * from second_cte;
```

## Basic CTE Examples

### Simple Data Filtering
```sql
with large_files as (
    select Name, Length, Extension
    from #os.files('/documents', true)
    where Length > 1000000
)
select 
    Extension,
    count(*) as FileCount,
    sum(Length) as TotalSize
from large_files
group by Extension
order by TotalSize desc;
```

### Data Transformation
```sql
with normalized_data as (
    select 
        Upper(Trim(Name)) as CleanName,
        Length / 1024 / 1024 as SizeMB,
        Extension
    from #os.files('/projects', true)
    where Extension in ('.cs', '.js', '.py')
)
select 
    CleanName,
    SizeMB,
    Extension,
    case 
        when SizeMB < 1 then 'Small'
        when SizeMB < 10 then 'Medium'
        else 'Large'
    end as SizeCategory
from normalized_data
order by SizeMB desc;
```

## Advanced CTE Patterns

### Aggregation and Window Functions
```sql
with commit_stats as (
    select 
        c.AuthorEmail,
        count(*) as CommitCount,
        min(c.Date) as FirstCommit,
        max(c.Date) as LastCommit
    from #git.repository('/repo/path') r
    cross apply r.Commits c
    group by c.AuthorEmail
),
ranked_contributors as (
    select 
        AuthorEmail,
        CommitCount,
        FirstCommit,
        LastCommit,
        rank() over (order by CommitCount desc) as Rank
    from commit_stats
)
select 
    AuthorEmail,
    CommitCount,
    FirstCommit,
    LastCommit,
    Rank
from ranked_contributors
where Rank <= 10;
```

### Complex Data Processing
```sql
with method_complexity as (
    select 
        p.Name as ProjectName,
        c.Name as ClassName,
        m.Name as MethodName,
        m.CyclomaticComplexity,
        m.LinesOfCode
    from #csharp.solution('/solution/path.sln') s
    cross apply s.Projects p
    cross apply p.Documents d
    cross apply d.Classes c
    cross apply c.Methods m
    where m.CyclomaticComplexity > 1
),
project_summary as (
    select 
        ProjectName,
        count(*) as MethodCount,
        avg(CyclomaticComplexity) as AvgComplexity,
        max(CyclomaticComplexity) as MaxComplexity,
        sum(LinesOfCode) as TotalLinesOfCode
    from method_complexity
    group by ProjectName
)
select 
    ProjectName,
    MethodCount,
    round(AvgComplexity, 2) as AvgComplexity,
    MaxComplexity,
    TotalLinesOfCode,
    case 
        when AvgComplexity > 10 then 'High Risk'
        when AvgComplexity > 5 then 'Medium Risk'
        else 'Low Risk'
    end as RiskLevel
from project_summary
order by AvgComplexity desc;
```

## CTEs with Different Data Sources

### File System Analysis
```sql
with file_analysis as (
    select 
        f.Name,
        f.Length,
        f.Extension,
        f.DirectoryName,
        case 
            when f.Extension in ('.jpg', '.png', '.gif') then 'Image'
            when f.Extension in ('.mp4', '.avi', '.mov') then 'Video'
            when f.Extension in ('.pdf', '.doc', '.txt') then 'Document'
            else 'Other'
        end as FileCategory
    from #os.files('/media', true) f
    where f.Length > 0
),
category_stats as (
    select 
        FileCategory,
        count(*) as FileCount,
        sum(Length) as TotalSize,
        avg(Length) as AvgSize
    from file_analysis
    group by FileCategory
)
select 
    FileCategory,
    FileCount,
    round(TotalSize / 1024.0 / 1024.0, 2) as TotalSizeMB,
    round(AvgSize / 1024.0, 2) as AvgSizeKB
from category_stats
order by TotalSize desc;
```

### Git Repository Analysis
```sql
with author_activity as (
    select 
        c.AuthorEmail,
        c.Date,
        DatePart('year', c.Date) as Year,
        DatePart('month', c.Date) as Month
    from #git.repository('/repo/path') r
    cross apply r.Commits c
    where c.Date >= DateAdd('year', -1, GetDate())
),
monthly_activity as (
    select 
        AuthorEmail,
        Year,
        Month,
        count(*) as CommitCount
    from author_activity
    group by AuthorEmail, Year, Month
),
activity_trends as (
    select 
        AuthorEmail,
        Year,
        Month,
        CommitCount,
        lag(CommitCount, 1) over (partition by AuthorEmail order by Year, Month) as PrevMonthCommits
    from monthly_activity
)
select 
    AuthorEmail,
    Year,
    Month,
    CommitCount,
    PrevMonthCommits,
    case 
        when PrevMonthCommits is null then 'New'
        when CommitCount > PrevMonthCommits then 'Increasing'
        when CommitCount < PrevMonthCommits then 'Decreasing'
        else 'Stable'
    end as Trend
from activity_trends
where CommitCount > 5
order by Year desc, Month desc, CommitCount desc;
```

## CTEs with Joins and Cross Apply

### Multi-Source Analysis
```sql
with large_files as (
    select 
        FullName,
        Name,
        Length,
        Extension
    from #os.files('/projects', true)
    where Length > 10000000  -- Files larger than 10MB
),
file_metadata as (
    select 
        lf.FullName,
        lf.Name,
        lf.Length,
        lf.Extension,
        m.TagName,
        m.Description
    from large_files lf
    cross apply #os.metadata(lf.FullName) m
    where lf.Extension in ('.jpg', '.png', '.tiff')
)
select 
    Name,
    round(Length / 1024.0 / 1024.0, 2) as SizeMB,
    TagName,
    Description
from file_metadata
where TagName in ('Image Width', 'Image Height', 'Camera Make')
order by Length desc;
```

### Complex Data Relationships
```sql
with project_files as (
    select 
        p.Name as ProjectName,
        d.Name as FileName,
        d.LinesOfCode,
        c.Name as ClassName
    from #csharp.solution('/solution.sln') s
    cross apply s.Projects p
    cross apply p.Documents d
    cross apply d.Classes c
    where d.LinesOfCode > 100
),
project_stats as (
    select 
        ProjectName,
        count(distinct FileName) as FileCount,
        count(distinct ClassName) as ClassCount,
        sum(LinesOfCode) as TotalLinesOfCode,
        avg(LinesOfCode) as AvgLinesPerFile
    from project_files
    group by ProjectName
),
solution_summary as (
    select 
        sum(FileCount) as TotalFiles,
        sum(ClassCount) as TotalClasses,
        sum(TotalLinesOfCode) as TotalLinesOfCode,
        avg(AvgLinesPerFile) as OverallAvgLinesPerFile
    from project_stats
)
select 
    ps.ProjectName,
    ps.FileCount,
    ps.ClassCount,
    ps.TotalLinesOfCode,
    round(ps.AvgLinesPerFile, 2) as AvgLinesPerFile,
    round((ps.TotalLinesOfCode * 100.0) / ss.TotalLinesOfCode, 2) as PercentOfSolution
from project_stats ps
cross join solution_summary ss
order by ps.TotalLinesOfCode desc;
```

## Recursive CTEs (Future Feature)

### Hierarchical Data Processing
```sql
-- Note: Recursive CTEs are planned for future releases
with recursive directory_tree as (
    -- Anchor: Start with root directory
    select 
        Name,
        FullName,
        0 as Level
    from #os.directories('/root/path')
    where ParentDirectory is null
    
    union all
    
    -- Recursive: Add child directories
    select 
        d.Name,
        d.FullName,
        dt.Level + 1
    from #os.directories('/root/path') d
    inner join directory_tree dt on d.ParentDirectory = dt.FullName
    where dt.Level < 5  -- Prevent infinite recursion
)
select 
    replicate('  ', Level) + Name as IndentedName,
    FullName,
    Level
from directory_tree
order by FullName;
```

## Performance Optimization with CTEs

### Efficient Data Processing
```sql
-- Pre-filter data to reduce processing overhead
with filtered_commits as (
    select 
        c.Sha,
        c.AuthorEmail,
        c.Date,
        c.Message
    from #git.repository('/large/repo') r
    cross apply r.Commits c
    where c.Date >= DateAdd('month', -6, GetDate())  -- Only recent commits
      and c.AuthorEmail like '%@company.com'          -- Only company emails
),
author_metrics as (
    select 
        AuthorEmail,
        count(*) as CommitCount,
        count(distinct DatePart('week', Date)) as ActiveWeeks
    from filtered_commits
    group by AuthorEmail
    having count(*) > 10  -- Only active contributors
)
select 
    AuthorEmail,
    CommitCount,
    ActiveWeeks,
    round(CommitCount / cast(ActiveWeeks as decimal), 2) as CommitsPerWeek
from author_metrics
order by CommitsPerWeek desc;
```

### Memory-Efficient Processing
```sql
-- Process large datasets in chunks
with batch_processing as (
    select 
        ((RowNumber() - 1) / 1000) as BatchId,
        Name,
        Length,
        Extension
    from #os.files('/very/large/directory', true)
    where Length > 0
),
batch_summary as (
    select 
        BatchId,
        count(*) as FileCount,
        sum(Length) as TotalSize,
        max(Length) as MaxSize
    from batch_processing
    group by BatchId
)
select 
    BatchId,
    FileCount,
    round(TotalSize / 1024.0 / 1024.0, 2) as TotalSizeMB,
    round(MaxSize / 1024.0 / 1024.0, 2) as MaxSizeMB
from batch_summary
order by BatchId;
```

## Best Practices

### 1. Meaningful CTE Names
```sql
-- Good - descriptive names
with large_image_files as (...),
     image_metadata as (...),
     processed_images as (...)

-- Avoid - generic names  
with temp1 as (...),
     data as (...),
     result as (...)
```

### 2. Logical Data Flow
```sql
-- Structure CTEs in logical processing order
with 
raw_data as (
    -- Initial data extraction
    select * from #source.data()
),
cleaned_data as (
    -- Data cleaning and normalization
    select CleanField1, CleanField2 from raw_data where IsValid = true
),
enriched_data as (
    -- Add calculated fields
    select *, CalculatedField from cleaned_data
),
final_result as (
    -- Final transformations
    select FinalField1, FinalField2 from enriched_data
)
select * from final_result;
```

### 3. Appropriate Filtering
```sql
-- Filter early to improve performance
with filtered_source as (
    select * 
    from #large.dataset()
    where RelevantField = @parameter  -- Filter at source level
      and Date >= @startDate
),
processed_data as (
    select ProcessedField 
    from filtered_source
    -- Additional processing on smaller dataset
)
select * from processed_data;
```

## Error Handling and Troubleshooting

### Common CTE Issues

1. **CTE Not Found**
```sql
-- ERROR: Referencing undefined CTE
select * from undefined_cte;

-- FIX: Define CTE first
with undefined_cte as (select 1 as value)
select * from undefined_cte;
```

2. **Circular References**
```sql
-- ERROR: CTEs cannot reference each other circularly
with cte1 as (select * from cte2),
     cte2 as (select * from cte1)
select * from cte1;

-- FIX: Remove circular dependency
with cte1 as (select * from #source.data()),
     cte2 as (select * from cte1)
select * from cte2;
```

3. **Column Ambiguity**
```sql
-- ERROR: Ambiguous column names
with ambiguous as (
    select Name from #os.files('/path', true)
    union all
    select Name from #git.repository('/repo') r cross apply r.Files f
)
select Name from ambiguous;  -- Which Name?

-- FIX: Use aliases to clarify
with clarified as (
    select Name as FileName from #os.files('/path', true)
    union all
    select f.Name as GitFileName from #git.repository('/repo') r cross apply r.Files f
)
select FileName from clarified;
```

## Integration with Other Features

### CTEs with Table Definitions
```sql
table ProcessedTable {
    Id 'System.String',
    ProcessedValue 'System.String'
};

couple #data.source with table ProcessedTable as DataSource;

with processed_data as (
    select Id, ProcessedValue
    from DataSource(@inputPath)
    where ProcessedValue is not null
)
select * from processed_data
order by Id;
```

### CTEs with Cross Apply
```sql
with document_content as (
    select 
        f.Name as FileName,
        f.GetFileContent() as Content
    from #os.files('/documents', true) f
    where f.Extension = '.txt'
)
select 
    dc.FileName,
    w.Word,
    count(*) as WordCount
from document_content dc
cross apply Split(dc.Content, ' ') w
group by dc.FileName, w.Word
having count(*) > 3
order by count(*) desc;
```

## Next Steps

- Learn about [joins](./join-operations.md) for combining data from multiple sources
- Explore [cross apply operations](./cross-outer-apply.md) for advanced data relationships
- See [practical examples](./examples-git-insights.md) of CTEs in real-world scenarios