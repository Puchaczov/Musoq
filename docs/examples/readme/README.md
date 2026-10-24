# README executable examples

This collection contains 28 runnable examples, with 24 displayed in the repository [README](../../../README.md). Each example has a SQL file; the four additional Search recipes include their SQL below. `binary-default.sql` remains supplemental coverage.

## Prerequisites

Use a running Musoq instance supporting these constructions. `search-*.sql` requires the Search provider. All `binary*.sql` files and `table-couple.sql` require the System provider. Other examples use inline inputs. No credentials or external downloads are required.

Run commands from the repository root. Search resolves relative roots from the server's working directory, which may differ from the CLI's directory. Each Search file declares a root parameter; the commands below pass an absolute root. The paths example reads the actual parser directory; other Search examples read the fixtures below.

All queries order their result explicitly or return one row. Compare column names, values, nulls, ordering, and duplicates. JSON null is not the string "NULL".

## Search inputs

The checked-in fixtures are synthetic log records. Their local `.gitignore` makes them eligible despite the repository-wide log exclusion. Each file uses UTF-8 and CRLF.

### app.log

```text
INFO service=checkout code=I01 message=ready
ERROR service=checkout code=E42 message=retry failed
ERROR service=checkout code=E42 message=timeout
```

### auth.log

```text
INFO service=auth code=I01 message=ready
```

### worker.log

```text
WARN service=payments code=W01 message=queue busy
ERROR service=payments code=E17 message=payment failed
```

### Portable path comparison

Search outputs retain the provider's Path. For comparisons and the small README tables, convert backslashes to `/` and remove only the known input-root prefix, so fixture paths become app.log or worker.log. For the repository query, remove only the parser-root prefix. Do not discard arbitrary directories or compare only basenames outside these scopes. Counts, line numbers, and full line text must remain unchanged. search.lines returns LineText with its CRLF terminator; the JSON oracle preserves it, while the README table omits it for display.

The actual-repository query is a five-row preview, not a completeness claim. Its expected path snapshot depends on the checkout and must be refreshed from successful execution.

## Running Search queries

Resolve source roots from this checkout once before running the Search commands below:

```powershell
$parserRoot = (Resolve-Path -LiteralPath 'src/dotnet/Musoq.Parser').Path
$fixtureRoot = (Resolve-Path -LiteralPath 'docs/examples/readme/fixtures').Path
$parserParameters = @{ root = $parserRoot } | ConvertTo-Json -Compress
$fixtureParameters = @{ root = $fixtureRoot } | ConvertTo-Json -Compress
```

The root defaults in the SQL files are convenient when the server runs from this repository root. Explicit overrides work when it runs elsewhere. Query files execute verbatim; only the input parameter changes.

## Main examples

### Search paths

```sql
param(root: string = './src/dotnet/Musoq.Parser')

select p.Path as Path
from search.paths($root, options: (scope: (
    include: array { '*.cs', '**/*.cs' },
    exclude: array { '**/obj/**', '**/bin/**' }
))) p
order by p.Path
take 5
```

```shell
musoq run docs/examples/readme/search-paths.sql --from-file --params-json $parserParameters --format json
```

**Observed checkout snapshot (2026-10-13):**

```json
[{"Path":"BinarySchemaParser.FieldValidation.cs"},{"Path":"BinarySchemaParser.FieldValidationLiterals.cs"},{"Path":"BinarySchemaParser.Schema.cs"},{"Path":"BinarySchemaParser.SizedTypes.cs"},{"Path":"BinarySchemaParser.Validation.cs"}]
```

**Provenance:** Adapts the musoq skill paths recipe to the actual parser directory. Source paths are lexical discovery, not symbol analysis; excludes bin/obj and returns an ordered five-row preview.

### Find matching lines · Search

```sql
param(root: string = './docs/examples/readme/fixtures')

select s.Path as Path, s.LineNumber as Line, s.LineText as Text
from search.lines($root, 'ERROR',
    options: (scope: (include: array { '*.log' }))) s
order by s.Path, s.LineNumber
```

```shell
musoq run docs/examples/readme/search-lines.sql --from-file --params-json $fixtureParameters --format json
```

```json
[{"Path":"app.log","Line":2,"Text":"ERROR service=checkout code=E42 message=retry failed\r\n"},{"Path":"app.log","Line":3,"Text":"ERROR service=checkout code=E42 message=timeout\r\n"},{"Path":"worker.log","Line":2,"Text":"ERROR service=payments code=E17 message=payment failed\r\n"}]
```

**Provenance:** Adapts the skill lines recipe to the six repository-owned log records. ERROR occurs once in each of three matching physical lines.

### Count labeled matches · Search and GROUP BY

```shell
musoq run docs/examples/readme/search-patterns.sql --from-file --params-json $fixtureParameters --format json
```

```json
[{"Level":"ERROR","Occurrences":3},{"Level":"WARN","Occurrences":1}]
```

**Provenance:** Adapts the skill many recipe. Counts occurrences per label: three ERROR and one WARN; it does not count distinct files.

### Find files containing both patterns · CTE and HAVING

```sql
param(root: string = './docs/examples/readme/fixtures')

with topics as (
    select m.Path as Path, m.PatternId as Level
    from search.many($root, patterns: array {
        (id: 'ERROR', pattern: 'ERROR'),
        (id: 'WARN', pattern: 'WARN')
    }, options: (scope: (include: array { '*.log' }))) m
    group by m.Path, m.PatternId
)
select t.Path as Path, Count(*) as Levels
from topics t
group by t.Path
having Count(*) = 2
order by Path
```

```shell
musoq run docs/examples/readme/search-both.sql --from-file --params-json $fixtureParameters --format json
```

```json
[{"Path":"worker.log","Levels":2}]
```

**Provenance:** Adapts the skill search-both recipe. Inner grouping deduplicates (Path, Level); HAVING selects two distinct labels, regardless of repeated ERROR matches.

### Extract values from matches · Captures and APPLY

```sql
param(root: string = './docs/examples/readme/fixtures')

select c.Text as Service, Count(*) as Records
from search.matches($root,
    'service=(?<service>[a-z]+)', options: (
        text: (mode: 'regex'),
        records: (mode: 'physical-line', maxBytes: 1048576),
        scope: (include: array { '*.log' })
    )) m
cross apply m.Captures c
where c.GroupName = 'service' and c.Success = true
group by c.Text
order by Service
```

```shell
musoq run docs/examples/readme/search-captures.sql --from-file --params-json $fixtureParameters --format json
```

```json
[{"Service":"auth","Records":1},{"Service":"checkout","Records":3},{"Service":"payments","Records":2}]
```

**Provenance:** Adapts the skill search-captures recipe to service=(?<service>[a-z]+), physical-line framing, and the named service group. Six records yield auth 1, checkout 3, payments 2.

### Inline VALUES and expressions

```shell
musoq run docs/examples/readme/values.sql --from-file --format json
```

```json
[{"Name":"A","Total":24}]
```

**Provenance:** Uses the inline VALUES and expression constructs defined by the core specification; the two-row numeric fixture is explicit synthetic input.

### Match names and allowed values · LIKE and IN

```shell
musoq run docs/examples/readme/like-in.sql --from-file --format json
```

```json
[{"Name":"api.cs"},{"Name":"app.cs"}]
```

**Provenance:** Uses documented SQL LIKE (%) and IN semantics. The three explicit inputs select api.cs and app.cs; executed during planning and rechecked.

### Choose values conditionally · CASE

```shell
musoq run docs/examples/readme/case.sql --from-file --format json
```

```json
[{"Name":"Ada","Band":"high"},{"Name":"Ben","Band":"low"}]
```

**Provenance:** Uses the documented CASE construction, including its required ELSE. Ada/30 and Ben/10 are explicit inline inputs; executed during planning and rechecked for this collection.

### Keep rows with a matching record · EXISTS

```shell
musoq run docs/examples/readme/exists.sql --from-file --format json
```

```json
[{"Name":"Ada"}]
```

**Provenance:** Uses documented correlated EXISTS over explicit customer/order inputs. CustomerId 1 matches Ada; Ben has no match. Executed during planning and rechecked.

### Filter group totals · HAVING

```shell
musoq run docs/examples/readme/having.sql --from-file --format json
```

```json
[{"Team":"api","Total":25}]
```

**Provenance:** Uses documented GROUP BY/HAVING semantics. api amounts 10 and 15 total 25; worker amount 5 is excluded. Executed during planning and rechecked.

### Conditional aggregates

```shell
musoq run docs/examples/readme/aggregates.sql --from-file --format json
```

```json
[{"Component":"api","Total":2,"Errors":1},{"Component":"worker","Total":1,"Errors":0}]
```

**Provenance:** Adapts the musoq skill conditional.sql fixture, preserving all three input rows and the FILTER condition. Both total and conditional counts are checked.

### Window ranking and QUALIFY

```shell
musoq run docs/examples/readme/window.sql --from-file --format json
```

```json
[{"Category":"Books","Name":"A","Score":30,"Position":1},{"Category":"Games","Name":"C","Score":20,"Position":1}]
```

**Provenance:** Adapts the musoq skill rank.sql fixture to QUALIFY. All four input rows, the score/name ordering, and category ordering are preserved; the CTE/outer filter is replaced by a same-block window filter.

### LEFT JOIN

```shell
musoq run docs/examples/readme/left-join.sql --from-file --format json
```

```json
[{"Name":"A","Score":30},{"Name":"B","Score":null}]
```

**Provenance:** Uses documented LEFT JOIN semantics with two left rows and one matching right row. The unmatched B row must have a null Score.

### ANTI JOIN

```shell
musoq run docs/examples/readme/anti-join.sql --from-file --format json
```

```json
[{"Name":"B"}]
```

**Provenance:** Uses the left-sided ANTI JOIN construction covered by the repository semi/anti join tests; the test providers are replaced with inline rows.

### ASOF matching

```shell
musoq run docs/examples/readme/asof.sql --from-file --format json
```

```json
[{"ErrorTime":"2026-10-11T10:05:00","Version":"v1"},{"ErrorTime":"2026-10-11T10:20:00","Version":"v2"}]
```

**Provenance:** Retains the previously executed timestamp fixture, adapted from AsOfJoinTests.InlineValues.cs (InlineAsOf_ShouldPartitionEqualityKeys). Fixed ISO timestamp strings are cast to datetime. Backward matching associates 10:05 with v1 and 10:20 with v2; this does not establish causation.

### CROSS APPLY over collections

```shell
musoq run docs/examples/readme/apply.sql --from-file --format json
```

```json
[{"Name":"A","Value":1},{"Name":"A","Value":2}]
```

**Provenance:** Uses the collection expansion verified during planning. The array is constructed in a CTE projection; direct array expressions in VALUES fields are rejected by the running engine.

### UNION ALL

```shell
musoq run docs/examples/readme/set-operations.sql --from-file --format json
```

```json
[{"Value":1},{"Value":2},{"Value":2},{"Value":3}]
```

**Provenance:** Uses UNION ALL over two explicit relations, then orders their combined output. The duplicate value 2 is intentional and must remain.

### Parameters and local variables

```shell
musoq run docs/examples/readme/script-values.sql --from-file --format json
```

```json
[{"Name":"A"}]
```

**Provenance:** Uses the typed parameter/let construction verified during planning, with explicit Books/A/B inputs. Defaults select A; overriding minScore with 35 produces no rows.

Override minScore while retaining the default local category:

```shell
musoq run docs/examples/readme/script-values.sql --from-file --params-json '{"minScore":35}' --format json
```

```json
[]
```

### CTE composition

```shell
musoq run docs/examples/readme/ctes.sql --from-file --format json
```

```json
[{"Name":"A","Total":25}]
```

**Provenance:** Combines documented CTE and aggregation constructs over an explicit three-row fixture. Expected total 25 is the sum of A amounts 10 and 15.

### Recursive CTEs and keyed UNION

```shell
musoq run docs/examples/readme/recursive.sql --from-file --format json
```

```json
[{"Service":"checkout","Depth":0},{"Service":"payments","Depth":1},{"Service":"ledger","Depth":2}]
```

**Provenance:** Retains the previously executed three-service cycle, adapted from RecursiveCteJoinExecutionTests.cs. Service is the keyed identity and Depth 0 is the starting service. The fixture does not assert a shortest-path contract for arbitrary graphs.

### Inline text formats

```shell
musoq run docs/examples/readme/text.sql --from-file --format json
```

```json
[{"Level":"ERROR","Message":"retry failed"},{"Level":"INFO","Message":"ready"}]
```

**Provenance:** Adapts the musoq skill parse-text fixture into two explicit input rows. The token/whitespace/rest declaration and Parse construction are retained; projection replaces the prior incident aggregation.

### Tolerant text handling

```shell
musoq run docs/examples/readme/text-tolerant.sql --from-file --format json
```

```json
[{"Id":1,"Message":"ready"},{"Id":2,"Message":null}]
```

**Provenance:** Retains the repository-owned copy of the musoq skill tolerant-text fixture. The second input is rejected and OUTER APPLY preserves its original Id with a null Message.

### Inline binary formats

```shell
musoq run docs/examples/readme/binary.sql --from-file --format json
```

```json
[{"Kind":1,"Size":3,"PayloadBytes":3}]
```

**Provenance:** Uses the compact Header declaration verified during planning and the exact 01 03 00 AA BB CC input. The Size field controls byte[Size], and the expected payload length is three.

### Read a record inside a record · Nested binary formats

```shell
musoq run docs/examples/readme/binary-nested.sql --from-file --format json
```

```json
[{"Kind":1,"UserId":42}]
```

**Provenance:** Adapts the nested-schema construction covered by BinaryOrTextual.CoreBinaryTests.NestedSchemas.cs. Exact bytes 01 2A 00 00 00 contain Kind 1 and nested UserId 42.

### Read repeated values · Binary arrays

```shell
musoq run docs/examples/readme/binary-array.sql --from-file --format json
```

```json
[{"Value":7},{"Value":42}]
```

**Provenance:** Uses the counted primitive-array contract in binary specification sections 4.4 and 7.3. Bytes 02 07 00 2A 00 contain two little-endian short values, 7 and 42.

### Read different payload kinds · Binary switch

```shell
musoq run docs/examples/readme/binary-tagged.sql --from-file --format json
```

```json
[{"PayloadKind":"Login","UserId":42},{"PayloadKind":"Raw","UserId":null}]
```

**Provenance:** Reuses the Login/Raw schemas covered by BinaryOrTextual.SwitchTests.cs. Login bytes 01 04 2A 00 00 00 yield 42; Raw bytes 09 03 AA BB CC select Raw. CASE guards inactive-branch field access. Direct unguarded projection failed on the installed host; no engine fix is included.

### Keep malformed binary rows · TryInterpret

```shell
musoq run docs/examples/readme/binary-tolerant.sql --from-file --format json
```

```json
[{"Id":1,"Kind":1,"Size":3},{"Id":2,"Kind":null,"Size":null}]
```

**Provenance:** Adapts the OUTER APPLY/TryInterpret behavior covered by BinaryOrTextual.SchemaFeaturesTests.TryInterpretCore.cs. The first Header is complete; the second declares three payload bytes but supplies one.

### TABLE and COUPLE

```shell
musoq run docs/examples/readme/table-couple.sql --from-file --format json
```

```json
[{"Dummy":"X"}]
```

**Provenance:** Uses the installed System provider contract observed with describe source system.dual(): Dummy has type string. The TABLE/COUPLE binding was executed during planning; no provider signature is invented.

## Supplemental binary default branch

```shell
musoq run docs/examples/readme/binary-default.sql --from-file --format json
```

```json
[{"Type":9,"PayloadKind":"Raw","PayloadBytes":3}]
```

**Provenance:** Preserved unchanged from the earlier README collection. It adapts Query_SwitchPayload_DefaultCase_ShouldSelectRawBranch in BinaryOrTextual.SwitchTests.cs, using 09 03 AA BB CC and projecting the three-byte fallback length.

See the [binary switch tests](../../../src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.SwitchTests.cs) and [tagged-union specification](../../../specs/musoq-binary-text-spec.md#412-switch-tagged-union-payloads) for the related schemas and semantics.

## Inspection

```shell
musoq inspect docs/examples/readme/asof.sql --from-file --stage logical --no-pager
musoq inspect docs/examples/readme/asof.sql --from-file --stage physical --no-pager
musoq inspect docs/examples/readme/asof.sql --from-file --stage execution --no-pager
musoq inspect docs/examples/readme/asof.sql --from-file --stage generated-code --no-pager
```

These commands expose the same query's planned and lowered representations. Identifiers and complete plan text may vary between versions.

## Validation record

Expected JSON above is the acceptance oracle; it is not, by itself, evidence of successful execution. Fixture results are derived from the six explicit input records. Re-execute all 28 main files and binary-default.sql, compare their columns and ordered values, and verify the parameter override.

Installed-runtime SQL validation and Release qualification of the source checkout are separate checks. Successful installed-host queries do not prove that host contains current checkout binaries.

On 2026-10-13, the command/server report development; core libraries report 17.0.12-alpha.1+a32f3aaa714ef12fdcdfe310fd482c5c46dc463d, Search reports 1.0.2-alpha.1, and System reports 8.0.5-alpha.1. The earlier Search version mismatch was resolved externally. All five Search files execute successfully with absolute root overrides; the JSON above matches their observed results. No host or provider replacement was performed as part of this documentation change.

Supplemental binary checks reuse the published queries with one input substitution: binary-array.sql with 0207002A00 replaced by 00 returns []; binary.sql with 010300AABBCC replaced by 010300AA fails with MQ7101 (interpretationCode ISE0001), byte offset 3. binary-tolerant.sql preserves the truncated row with NULL fields. The specification document is unchanged.

All 28 main files and the preserved binary-default fixture matched their recorded columns and ordered JSON values on the corrected installed runtime. Search roots were passed as absolute paths. The minScore=35 override returned []; a zero-count binary array returned []; strict truncated input reported MQ7101/ISE0001; tolerant input retained the malformed row.
