---
title: Querying Git Repositories
layout: default
parent: Practical Examples and Applications
nav_order: 6
---

# Git Operations with SQL Queries

There are many Git commands that can be represented using SQL queries in more or less useful ways. This section will progressively showcase examples of such queries.

## Working with Patches 

To see the overall changes introduced in a given branch, we need to find the `branching point` and examine the difference between it and the last commit in that branch. Let's consider a repository with the following structure:

```
ff067d6177e3c94970e8ae308412c6d1bed0d42a (HEAD -> feature/feature_1) fiveth commit for feature_1
f3414f10b6cce06910dfb06bd9a96f52a73b9e91 fourth commit for feature_1
0933e9b9846a8bb81b66fd94c0c666c9ed2914fc third commit for feature_1
eac5aec2c1f9ac2dca8c7537226bb9c4fe9ccabd second commit for feature_1
edd4d2c2f95b1d8b6a202ee150ca8277b9cf6373 first commit for feature_1
2cdb0d0d384ba387a0d05c62054139a18f3bda91 (master) initial commit
```

### Using Git Diff

Using the `git diff` command, we can calculate the difference like this:

```bash
git diff 2cdb0d0d384ba387a0d05c62054139a18f3bda91 ff067d6177e3c94970e8ae308412c6d1bed0d42a --shortstat
```

Here, `2cdb0d0d384ba387a0d05c62054139a18f3bda91` represents the "branching point" and `ff067d6177e3c94970e8ae308412c6d1bed0d42a` is the last commit in the branch.

### Using SQL Queries

The equivalent SQL query would be:

```sql
with BranchCommits as (
    select
        r.MinCommit(c.Self) as FirstCommit,
        r.MaxCommit(c.Self) as LastCommit
    from #git.repository('/some/git/repo') r
    cross apply r.SearchForBranches('feature/feature_1') b 
    cross apply r.GetBranchSpecificCommits(r.Self, b.Self, false) c
    group by 'fake'
)
select
    b.FirstCommit.Sha,
    b.FirstCommit.CommittedWhen,
    b.FirstCommit.MessageShort,
    b.LastCommit.Sha,
    b.LastCommit.CommittedWhen,
    b.LastCommit.MessageShort,
    p.LinesAdded,
    p.LinesDeleted
from BranchCommits b 
inner join #git.repository('/some/git/repo') r on 1 = 1
cross apply r.PatchBetween(r.CommitFrom(b.FirstCommit.Sha), r.CommitFrom(b.LastCommit.Sha)) p
```

This query consists of two main parts:

1. A CTE (Common Table Expression) query that retrieves the first and last commit in the branch
2. The main query that shows the changes between these commits

Important note: `GetBranchSpecificCommits` with the `false` parameter returns branch-specific commits **including** the "branching point" (which doesn't belong to the branch). You can exclude this commit by changing the flag to `true`.

The query returns results in this format:

| FirstCommit.Sha | FirstCommit.CommittedWhen | FirstCommit.MessageShort | LastCommit.Sha | LastCommit.CommittedWhen | LastCommit.MessageShort | LinesAdded | LinesDeleted |
|----------------|--------------------------|------------------------|---------------|------------------------|----------------------|------------|--------------|
| 2cdb0d0d384... | 12/14/2024 21:45:49 | initial commit | ff067d6177... | 12/14/2024 22:02:33 | fiveth commit for feature_1 | 5 | 0 |

### Viewing Changed Files

To see which specific files were modified, we can modify the query to use a private table for the calculated patch:

```sql
with BranchCommits as (
    select
        r.MinCommit(c.Self) as FirstCommit,
        r.MaxCommit(c.Self) as LastCommit
    from #git.repository('/some/git/repo') r
    cross apply r.SearchForBranches('feature/feature_1') b 
    cross apply r.GetBranchSpecificCommits(r.Self, b.Self, false) c
    group by 'fake'
)
select
    c.Path,
    c.LinesAdded,
    c.LinesDeleted
from BranchCommits b 
inner join #git.repository('/some/git/repo') r on 1 = 1
cross apply r.PatchBetween(r.CommitFrom(b.FirstCommit.Sha), r.CommitFrom(b.LastCommit.Sha)) p
cross apply p.Changes c
```

This query shows the changes by file:

| Path | LinesAdded | LinesDeleted |
|------|------------|--------------|
| application/main.py | 2 | 0 |
| application/meta.py | 0 | 0 |
| ci/test_1.py | 3 | 0 |