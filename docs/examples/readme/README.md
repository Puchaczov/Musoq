# README syntax examples

The repository [README](../../../README.md) contains fifteen visible syntax examples. Each is backed by one SQL file here and appears verbatim in the README. `binary-default.sql` is an additional preserved fixture linked from the binary section; it is not one of the fifteen main examples.

## Prerequisites

Use a running Musoq instance supporting the language constructs shown. `binary.sql`, `binary-default.sql`, and `table-couple.sql` require the System provider. All other examples use inline VALUES and no external datasource provider. No example requires credentials, input data files, or user-local skill files.

Run commands below from the repository root, or use an absolute SQL path when invoking from elsewhere. Every input is embedded in its query. All examples order their output explicitly or return one row; compare column names, values, ordering, and duplicate rows. JSON null is a missing/rejected value, not the string "NULL".

## Main examples

### Inline VALUES and expressions

```shell
musoq run docs/examples/readme/values.sql --from-file --format json
```

```json
[{"Name":"A","Total":24}]
```

**Provenance:** Uses the inline VALUES and expression constructs defined by the core specification; the two-row numeric fixture is explicit synthetic input.

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

### UNION ALL

```shell
musoq run docs/examples/readme/set-operations.sql --from-file --format json
```

```json
[{"Value":1},{"Value":2},{"Value":2},{"Value":3}]
```

**Provenance:** Uses UNION ALL over two explicit relations, then orders their combined output. The duplicate value 2 is intentional and must remain.

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

Execute all fifteen main files and the supplemental fixture verbatim; compare them with the JSON above. Also verify the parameter override. This covers filtering/projection, composition, unmatched join rows, backward temporal matching, array expansion, aggregate filtering, ranking, set multiplicity, cycle termination, strict and tolerant text, length-dependent binary input, and source-contract binding.

Installed-runtime SQL validation and qualification of the source checkout are separate checks. Record observed runtime/provider versions when refreshing this collection; a successful query against an installed host does not prove that host contains current checkout binaries.

On 2026-10-11, all fifteen main queries and binary-default.sql matched the recorded JSON, including column names, values, and row order. Overriding minScore to 35 returned no rows. The installed command and server both reported development; the enabled System provider reported 8.0.3-alpha.7. These results validate the installed runtime separately from checkout qualification.
