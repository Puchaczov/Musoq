---
title: Analysis of the EFCore Repository
layout: default
parent: Practical Examples and Applications
nav_order: 5
---

# Analysis of the EFCore Repository

Since my memory is good but short, I'll perform an example analysis of the EFCore repository using git-analysis-tool here. At the time of writing, the repository has about 16 thousand commits, so it should give us plenty of interesting data to look at. This analysis will serve as my reference point for future repository queries - when I need to check something later, I can come back here to remember how to structure these queries.

## How many commits exactly does the repository have at the time of writing?

There are exactly `15992` commits, we can count this with a simple query:

```sql
select
    Count(1) as CommitsCount
from #git.repository('D:\repos\efcore') r
cross apply r.Commits c
group by 'fake'
```

| CommitsCount |
| ------------ |
| 15992        |

## Looking at recent commits

Here are the 10 newest commits:

```sql
select
    c.Sha,
    c.MessageShort,
    c.Author,
    c.AuthorEmail,
    c.CommittedWhen
from #git.repository('D:\repos\efcore') r
cross apply r.Commits c
order by c.CommittedWhen desc
take 10
```

| c.Sha                                    | c.MessageShort                                                                                                                   | c.Author            | c.AuthorEmail                                                                         | c.CommittedWhen     |
| ---------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- | ------------------- | ------------------------------------------------------------------------------------- | ------------------- |
| 147264444aca02fcc86544893e3890411f3bc2bb | Update dependencies from [https://github.com/dotnet/arcade](https://github.com/dotnet/arcade) build 20241101.1 (#35035)          | dotnet-maestro[bot] | 42748379+dotnet-maestro[bot]@users.noreply.github.com                                 | 11/04/2024 14:41:55 |
| 507e7f196a11aebaf26d1cf34e5d6a3e7044bb52 | Use the proper type mapping for the result of AVG (#35023)                                                                       | Christopher Jolly   | [chris-jolly_au@hotmail.com](mailto:chris-jolly_au@hotmail.com)                       | 10/31/2024 12:18:57 |
| 17ed217ebb494392e08d8f5634db7eb3bb4ab5ed | Update dependencies from [https://github.com/dotnet/runtime](https://github.com/dotnet/runtime) build 20241027.3 (#34998)        | dotnet-maestro[bot] | 42748379+dotnet-maestro[bot]@users.noreply.github.com                                 | 10/29/2024 20:24:02 |
| 86060f9f5fccea1b9a4c24c64d0516e636fbeb7f | Bump Azure.Identity from 1.13.0 to 1.13.1 (#35001)                                                                               | dependabot[bot]     | 49699333+dependabot[bot]@users.noreply.github.com                                     | 10/28/2024 23:37:52 |
| 463d2d1069a1254e8c6866787a5844f2747ce70e | [TINY] Do not return null namespace for Types (#34994)                                                                           | Shay Rojansky       | [roji@roji.org](mailto:roji@roji.org)                                                 | 10/28/2024 22:16:25 |
| af2ef93ebbdf0ea74ac988f1f932758ade83d7c8 | Update dependencies from [https://github.com/dotnet/arcade](https://github.com/dotnet/arcade) build 20241027.1 (#34997)          | dotnet-maestro[bot] | 42748379+dotnet-maestro[bot]@users.noreply.github.com                                 | 10/28/2024 14:03:20 |
| 96d1997063fbe096741dc9a2bd56edbf6f55dce5 | Fix to #34960 - System.Text.Json.JsonReaderException: '0x00' is invalid after a single JSON value. Expected end of data (#34969) | Maurycy Markowski   | [maumar@microsoft.com](mailto:maumar@microsoft.com)                                   | 10/28/2024 10:07:57 |
| b6c4576370e681af7701f151cbdc1745e10cbbcc | Return the default schema for owned entity types when not mapped to the same table as the owner. (#34974)                        | Andriy Svyryd       | [AndriySvyryd@users.noreply.github.com](mailto:AndriySvyryd@users.noreply.github.com) | 10/26/2024 02:27:20 |
| 2e9e879746c3a75ad71b1c3732469c25f01bb8c7 | enabling test for scenario that was fixed earlier (#34975)                                                                       | Maurycy Markowski   | [maumar@microsoft.com](mailto:maumar@microsoft.com)                                   | 10/25/2024 03:14:54 |
| 931a67c0d6dc1738faf0b2ecf04f242a3789c4e7 | Bump Azure.Identity from 1.12.1 to 1.13.0 (#34948)                                                                               | dependabot[bot]     | 49699333+dependabot[bot]@users.noreply.github.com                                     | 10/22/2024 12:03:30 |

## Available branches in the repository

The repository has quite a few branches. Here's how to list them all:

```sql
select
    b.FriendlyName,
    b.CanonicalName,
    b.IsRemote,
    b.IsTracking,
    b.IsCurrentRepositoryHead,
    b.UpstreamBranchCanonicalName,
    b.RemoteName
from #git.repository('D:\repos\efcore') r
cross apply r.Branches b
```

There are `111` branches at the time of writing. Let's look at those containing 'release' in their name:

```sql
select
    b.FriendlyName,
    b.CanonicalName,
    b.IsRemote,
    b.IsTracking,
    b.IsCurrentRepositoryHead,
    b.UpstreamBranchCanonicalName,
    b.RemoteName
from #git.repository('D:\repos\efcore') r
cross apply r.Branches b
where b.FriendlyName like '%release%'
```

| b.FriendlyName                   | b.CanonicalName                               | b.IsRemote | b.IsTracking | b.IsCurrentRepositoryHead | b.UpstreamBranchCanonicalName        | b.RemoteName |
| -------------------------------- | --------------------------------------------- | ---------- | ------------ | ------------------------- | ------------------------------------ | ------------ |
| origin/merge/release/8.0-to-main | refs/remotes/origin/merge/release/8.0-to-main | True       | False        | False                     | refs/heads/merge/release/8.0-to-main | origin       |
| origin/release/2.1               | refs/remotes/origin/release/2.1               | True       | False        | False                     | refs/heads/release/2.1               | origin       |
| origin/release/2.3               | refs/remotes/origin/release/2.3               | True       | False        | False                     | refs/heads/release/2.3               | origin       |
| origin/release/6.0               | refs/remotes/origin/release/6.0               | True       | False        | False                     | refs/heads/release/6.0               | origin       |
| origin/release/7.0               | refs/remotes/origin/release/7.0               | True       | False        | False                     | refs/heads/release/7.0               | origin       |
| origin/release/7.0-rc1           | refs/remotes/origin/release/7.0-rc1           | True       | False        | False                     | refs/heads/release/7.0-rc1           | origin       |
| origin/release/7.0-rc2           | refs/remotes/origin/release/7.0-rc2           | True       | False        | False                     | refs/heads/release/7.0-rc2           | origin       |
| origin/release/8.0               | refs/remotes/origin/release/8.0               | True       | False        | False                     | refs/heads/release/8.0               | origin       |
| origin/release/8.0-preview1      | refs/remotes/origin/release/8.0-preview1      | True       | False        | False                     | refs/heads/release/8.0-preview1      | origin       |
| origin/release/8.0-preview2      | refs/remotes/origin/release/8.0-preview2      | True       | False        | False                     | refs/heads/release/8.0-preview2      | origin       |
| origin/release/8.0-preview3      | refs/remotes/origin/release/8.0-preview3      | True       | False        | False                     | refs/heads/release/8.0-preview3      | origin       |
| origin/release/8.0-preview4      | refs/remotes/origin/release/8.0-preview4      | True       | False        | False                     | refs/heads/release/8.0-preview4      | origin       |
| origin/release/8.0-preview5      | refs/remotes/origin/release/8.0-preview5      | True       | False        | False                     | refs/heads/release/8.0-preview5      | origin       |
| origin/release/8.0-preview6      | refs/remotes/origin/release/8.0-preview6      | True       | False        | False                     | refs/heads/release/8.0-preview6      | origin       |
| origin/release/8.0-preview7      | refs/remotes/origin/release/8.0-preview7      | True       | False        | False                     | refs/heads/release/8.0-preview7      | origin       |
| origin/release/8.0-rc1           | refs/remotes/origin/release/8.0-rc1           | True       | False        | False                     | refs/heads/release/8.0-rc1           | origin       |
| origin/release/8.0-rc2           | refs/remotes/origin/release/8.0-rc2           | True       | False        | False                     | refs/heads/release/8.0-rc2           | origin       |
| origin/release/8.0-staging       | refs/remotes/origin/release/8.0-staging       | True       | False        | False                     | refs/heads/release/8.0-staging       | origin       |
| origin/release/9.0               | refs/remotes/origin/release/9.0               | True       | False        | False                     | refs/heads/release/9.0               | origin       |
| origin/release/9.0-preview1      | refs/remotes/origin/release/9.0-preview1      | True       | False        | False                     | refs/heads/release/9.0-preview1      | origin       |
| origin/release/9.0-preview2      | refs/remotes/origin/release/9.0-preview2      | True       | False        | False                     | refs/heads/release/9.0-preview2      | origin       |
| origin/release/9.0-preview3      | refs/remotes/origin/release/9.0-preview3      | True       | False        | False                     | refs/heads/release/9.0-preview3      | origin       |
| origin/release/9.0-preview4      | refs/remotes/origin/release/9.0-preview4      | True       | False        | False                     | refs/heads/release/9.0-preview4      | origin       |
| origin/release/9.0-preview5      | refs/remotes/origin/release/9.0-preview5      | True       | False        | False                     | refs/heads/release/9.0-preview5      | origin       |
| origin/release/9.0-preview6      | refs/remotes/origin/release/9.0-preview6      | True       | False        | False                     | refs/heads/release/9.0-preview6      | origin       |
| origin/release/9.0-preview7      | refs/remotes/origin/release/9.0-preview7      | True       | False        | False                     | refs/heads/release/9.0-preview7      | origin       |
| origin/release/9.0-rc1           | refs/remotes/origin/release/9.0-rc1           | True       | False        | False                     | refs/heads/release/9.0-rc1           | origin       |
| origin/release/9.0-rc2           | refs/remotes/origin/release/9.0-rc2           | True       | False        | False                     | refs/heads/release/9.0-rc2           | origin       |
| origin/release/9.0-staging       | refs/remotes/origin/release/9.0-staging       | True       | False        | False                     | refs/heads/release/9.0-staging       | origin       |

## Contributors analysis

The repository has `439` authors in total, found using this query:

```sql
with AllAuthors as
(
    select
        Count(c.AuthorEmail) as AuthorsCommits
    from #git.repository('D:\repos\efcore') r
    cross apply r.Commits c
    group by c.AuthorEmail
)
select Count(1) as AuthorsCount from AllAuthors group by 'fake';
```

Here's the top 10 by number of commits:

```sql
select
    c.AuthorEmail,
    Count(c.Sha) as CommitCount
from #git.repository('D:\repos\efcore') r
cross apply r.Commits c
group by c.AuthorEmail
having Count(c.Sha) > 10
order by Count(c.Sha) desc
take 10
```

| c.AuthorEmail                                                                         | CommitCount |
| ------------------------------------------------------------------------------------- | ----------- |
| [ajcvickers@hotmail.com](mailto:ajcvickers@hotmail.com)                               | 2360        |
| [smitpatel@users.noreply.github.com](mailto:smitpatel@users.noreply.github.com)       | 1693        |
| 42748379+dotnet-maestro[bot]@users.noreply.github.com                                 | 1523        |
| [Andriy.Svyryd@microsoft.com](mailto:Andriy.Svyryd@microsoft.com)                     | 907         |
| [maumar@microsoft.com](mailto:maumar@microsoft.com)                                   | 779         |
| [roji@roji.org](mailto:roji@roji.org)                                                 | 748         |
| [anpete@microsoft.com](mailto:anpete@microsoft.com)                                   | 739         |
| [AndriySvyryd@users.noreply.github.com](mailto:AndriySvyryd@users.noreply.github.com) | 570         |
| [bricelam@microsoft.com](mailto:bricelam@microsoft.com)                               | 514         |
| [dotnet-bot@microsoft.com](mailto:dotnet-bot@microsoft.com)                           | 445         |

## Monthly bugfix analysis

This query shows how many commits in each of the last 10 months contained the words "fix" or "bug":

```sql
with MentionedBugFix as (
    select
        ToString(c.CommittedWhen, 'yyyy-MM') as Month,
        c.Sha as CommitSha,
        c.MessageShort as CommitMessage
    from #git.repository('D:\repos\efcore') s
    cross apply s.Commits c
    where
        ToLowerInvariant(c.MessageShort) like '%fix%' or
        ToLowerInvariant(c.MessageShort) like '%bug%'
)
select
    MentionedBugFix.Month,
    Count(MentionedBugFix.CommitSha) as BugFixCommits,
    AggregateValues(MentionedBugFix.CommitMessage) as BugFixMessages
from MentionedBugFix
group by MentionedBugFix.Month
order by MentionedBugFix.Month desc
take 10
```

|MentionedBugFix.Month|BugFixCommits|BugFixMessages|
|---|---|---|
|2024-10|12|Fix to #34960 - System.Text.Json.JsonReaderException: '0x00' is invalid after a single JSON value. Expected end of data (#34969),enabling test for scenario that was fixed earlier (#34975),Fix to #34749 - Created query for projection with ownedMany and join is wrong (#34858),[release/9.0] Fix Cosmos enum partition keys (#34922),Nanoseconds and microseconds processing fix on Cosmos (#34901),Update MicrosoftNETCoreBrowserDebugHostTransportVersion to 8.0.11-servicing.24508.13,Fix to #34760 - NullReferenceException for a custom ValueConverter in EF Core 9 RC 1. (#34894),Fix conditional test evaluation in funcletizer (#34886),small xml doc fix (copy paste error) (#34857) (#34888),small xml doc fix (copy paste error) (#34857),Fix tags for final GroupBy queries (#34787),Fix: Value cannot be null parameter name key (#34730)|
|2024-09|7|Update azure-pipelines.yml to fix BinSkim (#34800),Fix to #34728 - Split query with AsNoTrackingWithIdentityResolution() throws ArgumentOutOfRangeException (#34742) (#34743),Fix to #34728 - Split query with AsNoTrackingWithIdentityResolution() throws ArgumentOutOfRangeException (#34742),Fix test and a bit of code cleanup (#34731),Fix Azure SQL tests (#34676),Fix type mapping management for JsonScalarExpression (#34663),Fix doc typo (#34664)|
|2024-08|12|(RC2) (Test only) Fix async void tests (#34529),(RC2) Fix ESCAPE clause for Azure Synapse. (#34463) (#34510),(RC2) Fix ESCAPE clause for Azure Synapse. (#34463) (#34509),Fix ESCAPE clause for Azure Synapse. (#34463),Fix non-NativeAOT compiled model (#34455),Fix github-merge-flow (#34452),[release/9.0-rc1] Fix OptimisticConcurrencyCosmosTest (#34443),API review fixes (#34408),Fix some Cosmos tests on CI (#34390),Fix to #34211 - Issue occurs when trying to generate query string when mapping entity with json to a view with a custom schema (#34239),fixing cosmos baselines to expect $ type as discriminator,[release/8.0] Use placeholder value to fix CredScan (#34342)|
|2024-07|5|Fix to #34056 - AOT/Query: for queries with JSON, interceptors generate code with labels that are not uniquified (#34323),Fix build break,Nullability-related fixes to LEAST/GREATEST (#32458),Fix `Nullable<bool>.ToString()` conversion (#33940),Fix `Nullable<T>.ToString()` (#34014)|
|2024-06|17|Cosmos: Fixes around array projection (#34061),Fix build regression (#34078),fixing cosmos test failures on linux (#34058) (#34059),Cosmos: Fix Project_multiple_collections (#34003),Fix for #33702 Translation for the SQL Server PATINDEX function (#33868),Fix exception assertions in Cosmos tests (#33984),Fix to #33073 - JSON columns throws Invalid token type: 'StartObject' exception with AsNoTrackingWithIdentityResolution() (#33101),Fix merge,Fix UpdateExpression.VisitChildren to visit the setter column (#33948),Fix SQL Server translation of `IndexOf` (#33876),Fix `unhex()` nullability (#33870),Fix to #33886 - Query/Json: additional small fixes for JSON escaping (#33888),Fix optimization of `CASE op WHEN` (#33869),Fix to #33443 - JSON path is not properly formatted (#33771),Fix and enable `Bitwise_and_on_expression_with_like_and_null_check_being_compared_to_false` (#33872),Fix SQL Server operator precedence table (#33875),Re-enable fixed tests (#33871)|
|2024-05|6|Fix comparison of nullable values (#33757),Fix merge,Fix to #33547 - Breaking Change in 8.0.4: System.InvalidOperationException: The data is NULL at ordinal 0. This method can't be called on NULL values. Check using IsDBNull before calling. (#33692),Fix to #33547 - Breaking Change in 8.0.4: System.InvalidOperationException: The data is NULL at ordinal 0. This method can't be called on NULL values. Check using IsDBNull before calling. (#33559),Fix to #33678 - TimeOnly.FromDateTime() could not be translated in EF Core 8 (#33689),[release/8.0] Fix parameter names for nested types in complex type equality (#33527) (#33548)|
|2024-04|10|Fix to #33590 - Test ordering issue (#33609),Fix string.Length translation after SqlQuery (#33580),Use placeholder value to fix CredScan (#33550),Fix parameter names for nested types in complex type equality (#33527),Various fixes for issues in efcore-in-VMR build (#33540),Small fix to IInjectableService and lazy loading that removes constants of ParameterBindingInfo objects. This is needed for precompiled query work (#33534),Debug messages intended for shadow properties should not be logged for indexer properties (#33488),Fix docs/names (#33475),Fix funcletizer for VisitListInit (#33466),Fix documentation comment about ShouldConvertToInlineQueryRoot (#33455)|
|2024-03|13|Fixes around identifier and type mappings for ValuesExpression (#33439),Fix key generation validation for TPC. (#33371),Fix NRT directives in tests (#33387),Fix missing type mapping for CONVERT (#33311),Merge fixup,Merge pull request #33291 from cincuranet/fix-local-locale,Fix to #32911 - Incorrect apply projection for complex properties (#33212),Fix to #33004 - Unfulfillable nullable contraints are set for complex types with TPH entities (#33054),Fix to #32972 - The database default created by Migrations for primitive collections mapped to JSON is invalid (#33048),Fix OPENJSON postprocessing with split query (#32978) (#33027),Fix MemberExpression funcletization (#33241),Fix inferred mapping application on Sqlite JsonEachExpression (#33209),Another tiny fix to #32911 (#33213)|
|2024-02|15|Merge pull request #33197 from dotnet/fix33183,Fix to #33183 - Complex property causes The multi-part identifier xxx could not be bound after GroupBy + Select(x => x.First()),Fixed typo in getting-and-building-the-code.md (#33166),Merge pull request #33110 from dotnet/fix33046,Fix to #33046 - ArgumentException thrown when building queries involving owned entities mapped with .ToJson(),Fix to #32911 - ComplexProperty with AsSplitQuery (#33020),Small fixes done during EFCore.PG 9.0.0-preview.1 sync (#33060),Fix to #33004 - Unfulfillable nullable contraints are set for complex types with TPH entities (#33052),Fix to #32972 - The database default created by Migrations for primitive collections mapped to JSON is invalid (#33039),Fix merge and remove quirks,Fix lock loosing in concurrency detector for multiple contexts on one thread. - In ConcurrencyDetector lock acquisition mark changes to locks held count Fixes #31890,Fix to #32984 - Query/Test: change query test infra to output SQL whenever we encounter an exception during query execution (#33000),Fix to #32939 - Accidentally using field instead of property for JSON metadata produce hard to understand exception (#32966),Fix OPENJSON postprocessing with split query (#32978),Fix daily build selection image. (#32975)|
|2024-01|20|Fix typo (#32973),Fix failing test related to SQL Server identity reseeding (#32949),Fix copy paste typo (#32951),Fix incremental builds (#32860),Fix SelectExpression cloning when client projections are present (#32824),Fix IncludeExpression pruning for nested owned entities in ExecuteUpdate/Delete,Fix test baseline due to bad merge (#32789),Fix typos on DeleteBehavior.cs (#32781),Fix merge.,Remove quirk and fix merge,Fix complex assignment operator handling in LinqToCSharpSyntaxTranslator (#32715),Fix T4 files for VS 17.8 (#32651),Preserve unicodeness and fixed-lengthiness in compiled model. (#32662),Preserve unicodeness and fixed-lengthness in compiled model. (#32683),Fixup merge and remove quirks,Fix expression cloning when table changes in SelectExpression.VisitChildren (#32504),Add method postfix when rewriting parameters for StartsWith/EndsWith/Contains (#32440),Fix Contains within SQL Server aggregate functions (#32543),[release/8.0] Fix splitting migrations SQL by GO (#32572),Fix to #32570 - Test/Cleanup: some adhoc tests that are provider only because hardcoded column types etc can be moved from provider to relational/core (#32706)|

## Long-term contributors

Here's a query to find people who have been contributing the longest, based on the time span between their first and last commit:

```sql
with ContributionLength as (
    select
        c.AuthorEmail as Author,
        c.Count(1) as TotalCommits,
        c.MinDateTimeOffset(c.CommittedWhen) as FirstCommit,
        c.MaxDateTimeOffset(c.CommittedWhen) as LastCommit
    from #git.repository('D:\repos\efcore') r
    cross apply r.Commits c
    group by c.AuthorEmail
    having Count(c.AuthorEmail) > 1
    order by Count(1) desc
)
select
    cl.Author
from ContributionLength cl
order by SubtractDateTimeOffsets(cl.LastCommit, cl.FirstCommit) desc
take 10
```

|cl.Author|ContributionLength|
|---|---|
|[ajcvickers@hotmail.com](mailto:ajcvickers@hotmail.com)|3866.06:30:41|
|[Andriy.Svyryd@microsoft.com](mailto:Andriy.Svyryd@microsoft.com)|3823.20:08:53|
|[maumar@microsoft.com](mailto:maumar@microsoft.com)|3785.14:16:16|
|[roji@roji.org](mailto:roji@roji.org)|3475.02:17:04|
|[Tratcher@Outlook.com](mailto:Tratcher@Outlook.com)|3261.18:46:07|
|[ErikEJ@users.noreply.github.com](mailto:ErikEJ@users.noreply.github.com)|3076.16:28:44|
|[brecon@microsoft.com](mailto:brecon@microsoft.com)|3015.22:10:43|
|[smitpatel@users.noreply.github.com](mailto:smitpatel@users.noreply.github.com)|2933.10:56:46|
|[nimullen@microsoft.com](mailto:nimullen@microsoft.com)|2835.15:35:45|
|[Eilon@users.noreply.github.com](mailto:Eilon@users.noreply.github.com)|2778.18:55:27|

## Most frequently modified files

This query analyzes the differences between all sequential commits to find the most frequently changed files. It takes about 3 minutes to run since it processes the entire commit history:

```sql
WITH FileChanges AS (
    SELECT
        d.Path,
        c.Count(c.Sha) as TimesModified
    FROM #git.repository('D:\repos\efcore') r
    CROSS APPLY r.Head.Commits c
    CROSS APPLY r.DifferenceBetween(r.CommitFrom(c.Sha), r.CommitFrom(c.Sha + '^')) d
    GROUP BY d.Path
)
SELECT
    *
FROM FileChanges
ORDER BY TimesModified DESC
TAKE 10
```

|d.Path|TimesModified|
|---|---|
|eng/Version.Details.xml|2664|
|eng/Versions.props|2585|
|global.json|1073|
|NuGet.config|750|
|test/EFCore.SqlServer.FunctionalTests/Query/GearsOfWarQuerySqlServerTest.cs|545|
|src/EFCore/Properties/CoreStrings.Designer.cs|527|
|src/EFCore/Properties/CoreStrings.resx|517|
|test/EFCore.SqlServer.FunctionalTests/Query/QueryBugsTest.cs|461|
|src/EFCore.Relational/Query/SqlExpressions/SelectExpression.cs|421|
|src/EFCore.Relational/Query/RelationalQueryableMethodTranslatingExpressionVisitor.cs|391|

## Recent file changes

Here are the files changed in the last 10 commits:

```sql
SELECT
    c.Sha,
    c.MessageShort,
    c.Author,
    c.CommittedWhen,
    Count(d.Path) as FilesChanged
FROM #git.repository('D:\repos\efcore') r
CROSS APPLY r.Take(r.Head.Commits, 10) c
CROSS APPLY r.DifferenceBetween(r.CommitFrom(c.Sha), r.CommitFrom(c.Sha + '^')) d
GROUP BY c.Sha, c.MessageShort, c.Author, c.CommittedWhen
ORDER BY c.CommittedWhen DESC
```

|c.Sha|c.MessageShort|c.Author|c.CommittedWhen|FilesChanged|
|---|---|---|---|---|
|147264444aca02fcc86544893e3890411f3bc2bb|Update dependencies from [https://github.com/dotnet/arcade](https://github.com/dotnet/arcade) build 20241101.1 (#35035)|dotnet-maestro[bot]|11/04/2024 14:41:55|3|
|507e7f196a11aebaf26d1cf34e5d6a3e7044bb52|Use the proper type mapping for the result of AVG (#35023)|Christopher Jolly|10/31/2024 12:18:57|2|
|17ed217ebb494392e08d8f5634db7eb3bb4ab5ed|Update dependencies from [https://github.com/dotnet/runtime](https://github.com/dotnet/runtime) build 20241027.3 (#34998)|dotnet-maestro[bot]|10/29/2024 20:24:02|2|
|86060f9f5fccea1b9a4c24c64d0516e636fbeb7f|Bump Azure.Identity from 1.13.0 to 1.13.1 (#35001)|dependabot[bot]|10/28/2024 23:37:52|1|
|463d2d1069a1254e8c6866787a5844f2747ce70e|[TINY] Do not return null namespace for Types (#34994)|Shay Rojansky|10/28/2024 22:16:25|1|
|af2ef93ebbdf0ea74ac988f1f932758ade83d7c8|Update dependencies from [https://github.com/dotnet/arcade](https://github.com/dotnet/arcade) build 20241027.1 (#34997)|dotnet-maestro[bot]|10/28/2024 14:03:20|4|
|96d1997063fbe096741dc9a2bd56edbf6f55dce5|Fix to #34960 - System.Text.Json.JsonReaderException: '0x00' is invalid after a single JSON value. Expected end of data (#34969)|Maurycy Markowski|10/28/2024 10:07:57|4|
|b6c4576370e681af7701f151cbdc1745e10cbbcc|Return the default schema for owned entity types when not mapped to the same table as the owner. (#34974)|Andriy Svyryd|10/26/2024 02:27:20|4|
|2e9e879746c3a75ad71b1c3732469c25f01bb8c7|enabling test for scenario that was fixed earlier (#34975)|Maurycy Markowski|10/25/2024 03:14:54|2|
|931a67c0d6dc1738faf0b2ecf04f242a3789c4e7|Bump Azure.Identity from 1.12.1 to 1.13.0 (#34948)|dependabot[bot]|10/22/2024 12:03:30|1|

## Branch-specific analysis

Let's first find branches with more than one commit:

```sql
select
    b.FriendlyName,
    b.IsRemote,
    b.IsTracking,
    b.IsCurrentRepositoryHead,
    b.RemoteName,
    Count(1) as NumberOfCommits
from #git.repository('D:\repos\efcore') r
cross apply r.Branches b
cross apply r.GetBranchSpecificCommits(r.Self, b.Self) c
group by b.FriendlyName, b.IsRemote, b.IsTracking, b.IsCurrentRepositoryHead, b.RemoteName
having Count(1) > 1
```

|b.FriendlyName|b.IsRemote|b.IsTracking|b.IsCurrentRepositoryHead|b.RemoteName|NumberOfCommits|
|---|---|---|---|---|---|
|origin/release/2.3|True|False|False|origin|15|
|origin/release/6.0|True|False|False|origin|47|
|origin/release/7.0|True|False|False|origin|10|

Version `6.0` had 47 commits, let's look at them:

```sql
with BranchInfo as (
    select
        c.Sha,
        c.Message,
        c.Author,
        c.AuthorEmail,
        c.CommittedWhen
    from #git.repository('D:\repos\efcore') r
    cross apply r.SearchForBranches('origin/release/6.0') b
    cross apply b.GetBranchSpecificCommits(r.Self, b.Self) c
)
select * from BranchInfo;
```

Most are merge commits. Let's see what files were changed:

```sql
with DifferenceInfo as (
    select
        d.Path
    from #git.repository('D:\repos\efcore') r
    cross apply r.SearchForBranches('origin/release/6.0') b
    cross apply b.GetBranchSpecificCommits(r.Self, b.Self) c
    cross apply r.DifferenceBetween(r.CommitFrom(c.Sha), r.CommitFrom(c.Sha + '^')) d
    group by d.Path
)
select * from DifferenceInfo;
```

|d.Path|
|---|
|NuGet.config|
|eng/Version.Details.xml|
|global.json|
|eng/Versions.props|
|src/EFCore.Cosmos/EFCore.Cosmos.csproj|
|src/EFCore.SqlServer/EFCore.SqlServer.csproj|
|eng/common/post-build/add-build-to-channel.ps1|
|eng/common/post-build/check-channel-consistency.ps1|
|eng/common/post-build/nuget-validation.ps1|
|eng/common/post-build/nuget-verification.ps1|
|eng/common/post-build/post-build-utils.ps1|
|eng/common/post-build/publish-using-darc.ps1|
|eng/common/post-build/sourcelink-validation.ps1|
|eng/common/post-build/symbols-validation.ps1|
|eng/common/post-build/trigger-subscriptions.ps1|
|eng/common/templates-official/job/publish-build-assets.yml|
|eng/common/templates-official/post-build/common-variables.yml|
|eng/common/templates-official/post-build/post-build.yml|
|eng/common/templates-official/post-build/setup-maestro-vars.yml|
|eng/common/templates-official/post-build/trigger-subscription.yml|
|eng/common/templates-official/steps/add-build-to-channel.yml|
|eng/common/templates/job/publish-build-assets.yml|
|eng/common/templates/post-build/common-variables.yml|
|eng/common/templates/post-build/post-build.yml|
|eng/common/templates/post-build/setup-maestro-vars.yml|
|eng/common/templates/post-build/trigger-subscription.yml|
|eng/common/templates/steps/add-build-to-channel.yml|
|azure-pipelines-public.yml|
|azure-pipelines.yml|
|eng/common/templates-official/job/source-build.yml|
|eng/common/templates-official/job/source-index-stage1.yml|
|eng/common/templates-official/jobs/source-build.yml|
|eng/common/templates-official/steps/enable-internal-runtimes.yml|
|eng/common/templates-official/steps/get-delegation-sas.yml|
|eng/common/templates-official/steps/get-federated-access-token.yml|
|eng/common/templates/job/source-build.yml|
|eng/common/templates/job/source-index-stage1.yml|
|eng/common/templates/jobs/source-build.yml|
|eng/common/templates/steps/enable-internal-runtimes.yml|
|eng/common/templates/steps/get-delegation-sas.yml|
|eng/common/templates/steps/get-federated-access-token.yml|
|eng/helix.proj|
|.github/CODEOWNERS|
|eng/common/templates-official/job/job.yml|
|eng/common/templates-official/job/onelocbuild.yml|
|eng/common/templates-official/jobs/jobs.yml|
|eng/common/templates-official/steps/component-governance.yml|
|eng/common/templates/steps/component-governance.yml|
|.config/guardian/.gdnbaselines|
|.config/tsaoptions.json|
|test/EFCore.Cosmos.FunctionalTests/ConfigPatternsCosmosTest.cs|
...well, mostly `yaml` configuration files
## I'm going to try out different way...

Let's check what files changed between v5.0.0 and v6.0.0:

```sql
with CommitsFromTags as (
    select
        t.MinCommit(t.Commit) as FirstCommit,
        t.MaxCommit(t.Commit) as LastCommit
    from #git.repository('D:\repos\efcore') r
    cross apply r.Tags t
    where t.FriendlyName = 'v5.0.0' or t.FriendlyName = 'v6.0.0'
    group by 'fake'
)
select
    b.FirstCommit.CommittedWhen as FirstCommitDate,
    b.LastCommit.CommittedWhen as LastCommitDate,
    d.Path
from #git.repository('D:\repos\efcore') r2
inner join CommitsFromTags b on 1 = 1
cross apply r2.DifferenceBetween(b.FirstCommit, b.LastCommit) d
```

The query found **4085** changed files. Here are 10 random examples:

| d.Path                                                                  |
| ----------------------------------------------------------------------- |
| benchmark/EFCore.Benchmarks/Models/Orders/OrdersFixtureBase.cs          |
| benchmark/EFCore.Benchmarks/Models/Orders/OrdersFixtureSeedBase.cs      |
| benchmark/EFCore.Benchmarks/Models/Orders/Product.cs                    |
| benchmark/EFCore.Benchmarks/ParamsSummaryColumn.cs                      |
| benchmark/EFCore.Benchmarks/Query/FuncletizationTests.cs                |
| benchmark/EFCore.Benchmarks/Query/NavigationsQueryTests.cs              |
| benchmark/EFCore.Benchmarks/Query/QueryCompilationTests.cs              |
| benchmark/EFCore.Benchmarks/Query/RawSqlQueryTests.cs                   |
| benchmark/EFCore.Benchmarks/Query/SimpleQueryTests.cs                   |
| benchmark/EFCore.Benchmarks/UpdatePipeline/SimpleUpdatePipelineTests.cs |

## Most frequently changed files between versions

To find which files changed most often between v5.0.0 and v6.0.0, we need to check all commits in that period. This query takes about 3 minutes to run:

```sql
with CommitsFromTags as (
    select
        t.MinCommit(t.Commit) as FirstCommit,
        t.MaxCommit(t.Commit) as LastCommit
    from #git.repository('D:\repos\efcore') r
    cross apply r.Tags t
    where t.FriendlyName = 'v5.0.0' or t.FriendlyName = 'v6.0.0'
    group by 'fake'
), AllChangesBetweenTwoTags as (
    select
        d.Path,
        d.Count(1) as NumberOfChanges
    from #git.repository('D:\repos\efcore') r2
    inner join CommitsFromTags b on 1 = 1
    cross apply r2.Commits c1
    cross apply r2.DifferenceBetween(r2.CommitFrom(c1.Sha), r2.CommitFrom(c1.Sha + '^')) d
    where c1.CommittedWhen > b.FirstCommit.CommittedWhen and c1.CommittedWhen <= b.LastCommit.CommittedWhen
    group by d.Path
)
select * from AllChangesBetweenTwoTags
```

Found **4732** files that were modified at least once. Here are 10 random examples:

|d.Path|NumberOfChanges|
|---|---|
|test/EFCore.SqlServer.FunctionalTests/Query/NorthwindAggregateOperatorsQuerySqlServerTest.cs|18|
|global.json|195|
|test/EFCore.Specification.Tests/TestUtilities/TestHelpers.cs|26|
|test/EFCore.Sqlite.FunctionalTests/Query/NorthwindAggregateOperatorsQuerySqliteTest.cs|5|
|src/EFCore.Relational/Infrastructure/RelationalModelValidator.cs|33|
|src/EFCore.Relational/Properties/RelationalStrings.Designer.cs|42|
|src/EFCore.Relational/Properties/RelationalStrings.resx|35|
|src/EFCore/ChangeTracking/Internal/InternalEntityEntry.cs|49|
|src/EFCore/Infrastructure/ModelValidator.cs|51|
|src/EFCore/Metadata/Internal/ConstructorBindingFactory.cs|15|

That's all for now, I hope that when I look at this again after some time, it will help me in some way :)

## Bonus

For visualizing the codebase evolution, we can count added and deleted lines for each commit. This query takes about 10 minutes for 16k commits:

```sql
select
    p.LinesAdded,
    p.LinesDeleted
from #git.repository('D:\repos\efcore') r
cross apply r.Commits c
cross apply r.PatchBetween(c.Self, r.CommitFrom(c.Self.Sha + '^')) p
```