# Git History Data Source Example

This example shows how to expose a fixed-schema custom data source to Musoq
through a small, deterministic Git-history plugin. It does not call the Git CLI
and does not use LibGit2Sharp. All rows come from `InMemoryGitHistoryStore`, so
tests and sample queries are stable on every machine.

Use this example when your datasource owns a stable typed row model. Use the CSV
example when your datasource needs TABLE/COUPLE metadata, user-declared column
types, and read modifiers.

## Schema

Register `GitSchemaProvider` and query the schema as `#git`.

```sql
select ShortSha, AuthorName, Subject
from #git.commits()
order by AuthoredAt;
```

The source exposes one table:

```sql
#git.commits()
#git.commits('musoq')
```

`commits(repository)` filters to one repository. When no repository argument is
provided, the optional runtime setting `GIT_EXAMPLE_REPOSITORY` can provide the
default repository name. The explicit argument wins over the runtime setting.

## Columns

`commits` returns typed `GitCommitRow` rows with these columns:

```text
Repository, Branch, Sha, ShortSha, AuthorName, AuthorEmail, AuthoredAt,
Subject, Message, ChangedFiles, Additions, Deletions, Churn, IsMerge
```

Commit metadata columns are cheap. Stats columns (`ChangedFiles`, `Additions`,
`Deletions`, `Churn`) are intentionally treated as expensive and are loaded
lazily through `IGitHistoryStore.GetStats`.

## Runtime v2 Features

`GitSchema.TryPlanSource` demonstrates source planning for:

- accepted projection columns
- accepted predicates over cheap commit metadata columns
- partial `and` predicate pushdown, where cheap conjuncts run in the source and
  expensive conjuncts remain evaluator residual work
- all-or-nothing `or` predicate pushdown
- accepted comparisons, `in`/`not in`, and null checks over cheap columns
- accepted `order by`, `skip`, and `take` when semantics stay correct
- residual predicates/orderings/slicing when the source cannot evaluate them
  cheaply or safely
- exact cardinality estimates from deterministic in-memory data
- immutable `SourceExecutionPlan.Properties`, including resolved repository
  scope and planning notes
- source planning diagnostics that explain expensive stats fallbacks and
  rejected slicing

`GitCommitsSource` uses `SourceExecutionContext.Plan`, reports data-source
progress, honors cancellation, and inherits `DiagnosticChunkedRowSource` so
source diagnostics can observe chunk production and consumption.

Stats columns are still accepted for projection, so selecting `Additions` or
`Churn` works naturally. Predicates and orderings over stats columns are left as
evaluator residual work, which keeps compile-time planning from forcing lazy
stats loading.

The tests include an internal recorder that proves the engine passes source
identity, metadata context, runtime settings, static and runtime arguments,
planning requests, accepted execution plans, cancellation, progress callbacks,
diagnostics, and source contract diagnostics into the plugin API.

## Example Queries

Recent commits for the default repository:

```sql
select ShortSha, AuthoredAt, AuthorName, Subject
from #git.commits()
order by AuthoredAt desc
take 5;
```

Commits scoped by repository argument:

```sql
select ShortSha, Branch, Subject
from #git.commits('musoq')
where IsMerge = false
order by AuthoredAt;
```

Top authors by commit count:

```sql
select AuthorName, Count(1) as Commits
from #git.commits()
group by AuthorName
order by Commits desc;
```

Churn by repository:

```sql
select Repository, Sum(Additions) as Added, Sum(Deletions) as Deleted, Sum(Churn) as Churn
from #git.commits()
group by Repository
order by Churn desc;
```

Partial predicate pushdown with residual stats work:

```sql
select Subject
from #git.commits()
where AuthorName = 'Bob Evaluator' and Additions > 100
order by AuthoredAt asc
take 1;
```

Runtime setting example:

```csharp
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Examples.DataSources.Git;
using Musoq.Schema.Optimization;

var options = new CompilationOptions(
    usePrimitiveTypeValidation: false,
    sourceRuntimeSettingsResolver: new StaticSettingsResolver("docs"));

var query = "select Subject from #git.commits() order by AuthoredAt";
var compiled = InstanceCreator.CompileForExecution(
    query,
    Guid.NewGuid().ToString(),
    new GitSchemaProvider(),
    loggerResolver,
    options);

sealed class StaticSettingsResolver(string repository) : ISourceRuntimeSettingsResolver
{
    public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
    {
        return new Dictionary<string, string>
        {
            [GitSchema.RepositoryRuntimeSetting] = repository
        };
    }
}
```

## Tests

Run only this example:

```powershell
dotnet test src/dotnet/examples/data-sources/git/tests/Musoq.Examples.DataSources.Git.Tests.csproj --configuration Release --nologo --verbosity minimal
```

Run the full solution:

```powershell
dotnet test src/dotnet/Musoq.sln --configuration Release --nologo --verbosity minimal
```
