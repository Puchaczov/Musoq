---
title: Querying Multiple Git Repositories at Once
layout: default
parent: Practical Examples and Applications
nav_order: 7
---

# Querying Multiple Repositories at Once

A quite useful feature of the tool is the ability to query multiple Git repositories at once. Sometimes I need to track what I did and when in different projects. Currently, I'm involved in several projects (actively contributing to around ~10), so I need a general overview of what I did in them and when.

## Simply listing commits from all repositories

```sql
with ProjectsToAnalyze as (
    select
        dir2.FullName as FullName
    from #os.directories('D:\repos', false) dir1
    cross apply #os.directories(dir1.FullName, false) dir2
    where
        dir2.Name = '.git'
)
select
    c.Message,
    c.Author,
    c.CommittedWhen
from ProjectsToAnalyze p cross apply #git.repository(p.FullName) r 
cross apply r.Commits c
where c.AuthorEmail = 'my-email@email.ok'
order by c.CommitedWhen desc
```

I keep all repositories at the same level, and I identify whether a folder is a repository by checking for the existence of a `.git` subfolder.

The CTE expression naturally returns matching folders:

|FullName                       |
|-------------------------------|
|D:\repos\Musoq.DataSources\.git|
|D:\repos\Musoq\.git           |

The second part lists all commits for the found repositories. Of course, in descending order as that's likely what I'll be most interested in.

|p.RepositoryName   |c.Message                                                           |c.Author |c.AuthorEmail         |c.CommittedWhen    |
|-------------------|---------------------------------------------------------------------|---------|---------------------|-------------------|
|Musoq.DataSources |Update runtime, rework directories source, new tests for multi repo... |puchacz  |puchala.czwa@gmail.com|11/21/2024 21:50:25|
|Musoq             |Merge remote-tracking branch 'origin/master'                          |puchacz  |puchala.czwa@gmail.com|11/13/2024 21:23:20|
|Musoq             |Fixes around the documentation                                        |puchacz  |puchala.czwa@gmail.com|11/13/2024 21:23:13|
|Musoq.DataSources |1. Runtime Update 2. Git library update                              |puchacz  |puchala.czwa@gmail.com|11/11/2024 15:51:31|
|Musoq.DataSources |further git plugin implementations                                    |puchacz  |puchala.czwa@gmail.com|11/09/2024 00:02:07|
|Musoq.DataSources |1. Evaluator raised up + changes within git plugin                   |puchacz  |puchala.czwa@gmail.com|11/06/2024 23:54:05|

## How many commits do all repositories have

Sometimes we want to learn some basic statistics about repositories - how many commits they have, when was the last commit, who are the authors. We can do this with the following query:

```sql
with Repositories as (
    select
        dir2.FullName as GitPath,
        dir2.Parent.Name as RepositoryName
    from #os.directories('D:\repos', false) dir1
    cross apply #os.directories(dir1.FullName, false) dir2
    where
        dir2.Name = '.git'
)
select
    r.RepositoryName,
    repo.Count(c.Sha) as CommitCount,
    repo.Length(repo.Distinct(repo.Split(repo.AggregateValues(c.AuthorEmail), ','))) as AuthorsCount,
    repo.StringsJoin(',', repo.Distinct(repo.Split(repo.AggregateValues(c.AuthorEmail), ','))) as Authors,
    repo.MaxDateTimeOffset(c.CommittedWhen) as LastCommitDate
from Repositories r
cross apply #git.repository(r.GitPath) repo
cross apply repo.Commits c
group by r.RepositoryName
order by repo.Count(c.Sha) desc
```

|r.RepositoryName   |CommitCount |AuthorsCount |Authors                                                  |LastCommitDate     |
|-------------------|------------|-------------|--------------------------------------------------------|-------------------|
|Musoq             |529         |7            |pu...ail.com,pu...om,4969...github.com,jth...com,...    |11/19/2024 06:23:14|
|Musoq.DataSources |70          |2            |pu...@gmail.com,pu...@gmail.com                         |11/21/2024 21:50:25|

Worth noting here is the somewhat enigmatic:

```sql
repo.Length(repo.Distinct(repo.Split(repo.AggregateValues(c.AuthorEmail), ','))) as AuthorsCount,
```

and

```sql
repo.StringsJoin(',', repo.Distinct(repo.Split(repo.AggregateValues(c.AuthorEmail), ',')))
```

which is simply a workaround for the current lack of support for the `distinct` operator. I therefore used the aggregating method `AggregateValues(...)` to join all emails in a given repository, then this aggregated string was split using the `Split` method which transformed the string into an array of strings, and using the `Distinct` method our emails were limited to only unique ones. On this we can count either `Length` or `StringsJoin` which counts how many elements are in the array and joins the array of strings using a comma character.