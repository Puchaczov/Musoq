# Query-scoped CLR rows for dynamic sources

Dynamic sources do not have to materialize `object[]` or another declared entity when query
planning already knows the exact row shape. The optional query-scoped row contract lets a
source fill a private CLR carrier emitted into the collectible compiled-query assembly. The
existing `GetRowSource<T>` contract remains unchanged and is the fallback for old providers,
inexact metadata, entity-dependent queries, and targets that do not support this transfer mode.

This is a format-neutral row-transfer contract. CSV ordinals, JSON properties, XML paths, and
future native representations all use the same logical slot API while keeping their parser and
mapping implementation private.

## Contracts and transfer lifecycle

The public opt-in surface in `Musoq.Schema` consists of:

- `SourceTransferCapabilities.QueryScopedRows` on `SourceDescriptor`;
- `IQueryScopedRowSourceSchema.GetQueryScopedRowSource<TRow,TMaterializer>`;
- immutable `QueryScopedRowSourceRequest`, `QueryRowShape`, and `QueryRowField` metadata;
- `IQuerySourceFieldReader.Read<T>(int slot)`; and
- `IQueryRowMaterializer<TRow>.Materialize<TReader>(scoped ref TReader)`.

`SourceDescriptor.TransferCapabilities` defaults to `None`, so existing datasource binaries and
source code remain on `GetRowSource<T>` until they explicitly advertise the optional capability.

The decision is made once and carried through the compiler:

1. Source discovery reports exact columns and advertises `QueryScopedRows` for that source
   invocation. Metadata may come from `TABLE`/`COUPLE`, a header, a schema document, or another
   deterministic discovery mechanism.
2. The planner intersects the source capability, target capability, exact projected metadata,
   source-entity usage, and row lifetime. It records either `DeclaredRows` or `QueryScopedRows`
   plus a stable diagnostic reason.
3. Physical planning and Execution IR carry the selected mode, carrier kind, lifetime, logical
   fields, and shape fingerprint. The target does not repeat or override this analysis.
4. The C# target emits the selected private readonly struct or sealed class and a private
   readonly materializer. It also emits one immutable shape field per fingerprint in the
   collectible query type.
5. Execution opens the source through the selected API. A query-scoped source builds its native
   slot mapping once, applies accepted source work, and calls the static materializer with its
   concrete reader for each accepted row.

`QueryRowField` carries a dense logical slot, the original source ordinal and name, exact CLR
type, nullability, and immutable read modifiers. `QueryRowShape` requires slots `0..N-1`, copies
the field list, and computes a deterministic SHA-256 fingerprint over all semantic field data.
Names and source ordinals are mapping metadata; the dense slot is the hot-loop contract.

## Exact CLR-type eligibility

`QueryRowField.IsSupportedFieldType(Type)` is the shared public policy used by schema validation
and planning. The planner adds one exactness rule: `object` is referenceable C#, but it is not an
exact source field type, so it selects the declared-row path.

| Accepted | Rejected or legacy fallback |
| --- | --- |
| Public primitive, enum, and value types | `void` |
| Public arrays | Function-pointer types |
| Nullable value types | Pointer and byref types |
| Closed visible generic types | Byref-like types such as `Span<T>` |
| Public reference types with an exact runtime type | Open generics or types containing generic parameters |
| Exact nullable/reference metadata | Non-visible private or internal CLR types |
|  | `object`, because it is insufficiently exact for planning |

Column names must be non-empty and unique case-insensitively. Source ordinals must be
non-negative and unique. An unresolved intended type, duplicate metadata, or unresolved required
field causes a deterministic legacy fallback; the planner never guesses which field was meant.
Arrays and closed generic types are eligible only when the complete constructed type is visible
to generated C#.

## Exact projections, empty shapes, and special names

Planning distinguishes `Unavailable` projection metadata from an `Exact` projection containing
zero fields. An exact empty shape is valid when the query needs no source field and does not need
the declared entity, for example a source-side `COUNT(*)` path. Its materializer constructs a
zero-field carrier and emits no `Read<T>` calls.

Projection analysis includes fields required by accepted predicates, ordering, and other
source-plan work before it decides that a shape is empty. If any required field cannot be
resolved, the planner falls back instead of producing an incomplete empty carrier.

Source names are never turned into carrier member identifiers. Generated carrier members use
dense names such as `Field0`; punctuation, Unicode, spaces, keywords, and bracket-quoted names
remain string metadata in `QueryRowField`. This keeps names such as `display name`, `na-me`, and
`select` safe without lossy identifier rewriting.

## Declared-entity dependence

The optimization is valid only when a query consumes column values. A resolved method with an
`InjectSource` or `InjectSpecificSource` parameter requires the provider's declared entity and
therefore selects `GetRowSource<DeclaredType>`. The analysis is keyed by source identity and
alias, so one source in a multi-source query can remain query-scoped while another falls back.
An unresolved source-targeted injection is handled conservatively. Ordinary scalar methods over
projected fields do not create entity dependence.

Any future logical expression that exposes or retains a whole source entity must record the same
declared-entity requirement. Carrier lifetime is considered only after query-scoped transfer is
otherwise eligible; selecting a class cannot repair an entity-dependent query.

## Carrier lifetime policy

The planner first determines whether the source row is replaced before a retaining boundary,
then applies the 64-byte payload threshold:

| Plan shape before row replacement | Lifetime | Carrier result |
| --- | --- | --- |
| Filter, having/qualify filter, skip, or take followed by direct projection | `ScanLocal` | Readonly struct when estimated payload is at most 64 bytes |
| Direct aggregation replacing the source row | `ScanLocal` | Readonly struct when estimated payload is at most 64 bytes |
| Any scan-local shape wider than 64 bytes | `ScanLocal` | Sealed class |
| Join or outer-null extension, apply, sort before projection, window, set operation, or unpivot | `EscapesScan` | Sealed class |
| Unprojected CTE storage/reuse, recursive flow, explicit materialization, or multiple consumers | `EscapesScan` | Sealed class |
| Unknown operator, missing lifetime path, logical cycle, or root reached before replacement | `EscapesScan` | Sealed class |

A projection can replace the source carrier before a later sort, distinct, or CTE boundary; in
that case the source row remains scan-local. The policy is about where the source carrier itself
lives, not whether the complete query contains a retaining operator.

The payload estimate uses known primitive widths, 16 bytes for `decimal` and `Guid`, and pointer
width for references and nullable values. An unrecognized value type is treated as wider than
the struct threshold. This is deliberately conservative and deterministic.

## Implementing a format

A provider keeps its existing declared-row implementation and adds the optional interface. Its
query-scoped implementation should follow this sequence:

1. During discovery, report exact `ISchemaColumn` values and advertise the capability only when
   the invocation can honor every reported type, nullability rule, and read modifier.
2. In `GetQueryScopedRowSource`, inspect `request.Shape.Fields` once and build a dense-slot to
   native-location mapping owned by that row-source instance.
3. Enumerate native records and apply accepted predicate/order/skip/take work before constructing
   a generated carrier whenever the accepted source plan permits it.
4. Create a concrete reader, preferably a private `ref struct`, and invoke
   `TMaterializer.Materialize<ConcreteReader>(ref reader)` only for accepted rows.
5. Return ordinary `RowSource<TRow>` chunks while preserving the source's diagnostics,
   cancellation, conversion, and disposal behavior.

The hot-path shape is:

```csharp
private ref struct NativeReader(NativeRecord record, NativeMap map) : IQuerySourceFieldReader
{
    public T Read<T>(int slot) => /* native typed read through a prebuilt slot mapping */;
}

private static TRow MaterializeAccepted<TRow, TMaterializer>(NativeRecord record, NativeMap map)
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    var reader = new NativeReader(record, map);
    return TMaterializer.Materialize<NativeReader>(ref reader);
}
```

Do not store the reader in an `IQuerySourceFieldReader` variable, invoke per-field delegates,
reflect over `TRow`, compile runtime expressions, or box native values. The optional schema
interface is used once when the source opens; the warmed per-row/per-field loop remains a static
generic call specialized for the concrete reader.

Typical mapping strategies are:

| Format | Discovery metadata | Private mapping built once per source |
| --- | --- | --- |
| CSV or delimited text | Header name, declared `source.name`, or source ordinal | Dense slot to parsed field ordinal |
| JSON | Property name or provider-defined path modifier | Dense slot to property/path token or native lookup handle |
| XML | Element, attribute, namespace, or provider-defined path | Dense slot to element/attribute path and native value reader |
| Binary/key-value/future formats | Exact schema field or native key | Dense slot to parser offset, token, or key handle |

The core engine does not prescribe parsing, buffering, path syntax, or missing-value semantics.
Those remain provider responsibilities, but `Read<T>` must honor the exact requested field type
and the shape's nullability/modifier contract.

## Execution and lifecycle obligations

Query-scoped transfer changes carrier construction, not datasource semantics. Implementations
must preserve the same behavior as their declared-row path:

- Apply accepted pushdown before materialization. A rejected raw row must not invoke the
  materializer; accepted `TAKE` must stop upstream enumeration at the accepted limit.
- Dispose native streams, readers, enumerators, and buffers promptly on normal completion,
  residual engine termination, failure, and cancellation.
- Check the request cancellation token before opening expensive resources and while enumerating.
  Propagate `OperationCanceledException` unchanged for pre-cancelled and mid-stream cancellation.
- Emit a consistent begin, cumulative rows, failure or cancellation, and end lifecycle. The end
  path must run once even when reading or materialization fails.
- Preserve the original reader/materializer exception as the inner cause. Execution errors must
  include enough schema, source, alias, and query context to identify the failing boundary.
- Treat a null row source, open failure, invalid mapping, missing required value, or conversion
  failure as an execution error; do not silently switch carrier modes at runtime.
- Keep chunking observationally equivalent across chunk boundaries and propagate materializer
  exceptions without continuing to enumerate later rows.

The reusable test provider in `Musoq.Converter.Tests` exercises ordinal-, property-, and
path-style ref-struct readers through compiled queries, including pushdown, early termination,
cancellation, failures, disposal, joins, outer joins, grouping, windows, CTEs, and set operations.

## Fallback and runtime mismatch behavior

Planning selects declared rows when the source or target lacks the capability, metadata is not
exact and usable, the declared entity is required, or another eligibility rule fails. This is a
compile-time choice with a recorded reason, not a runtime retry strategy.

If an artifact planned `QueryScopedRows` but the runtime schema no longer implements
`IQueryScopedRowSourceSchema`, execution fails deterministically with the source identity and
shape fingerprint. It does not invoke legacy `GetRowSource<T>` because doing so would violate
the compiled carrier and artifact identity. Recompile against the current provider metadata or
restore the advertised runtime capability.

## Cache and collectible-context ownership

Transfer mode, carrier choice, lifetime, and shape fingerprint are semantic artifact identity.
The same SQL may reuse an artifact only when these facts match; fresh provider/runtime instances
are still bound for each execution. A metadata-shape or capability change must force a distinct
artifact. Artifacts compiled for legacy and query-scoped modes cannot cross-reuse.

Generated carriers, materializers, closed generics, delegates, and row instances belong to the
compiled query's collectible `AssemblyLoadContext`. Never retain them in a process-static cache.
The generated query may own static readonly immutable shape metadata because that field lives in
the same collectible type. Provider caches must remain unload-neutral: primitive fingerprints,
native mapping data, and metadata are safe only when they retain no provider instance, request,
generated `Type`, `MethodInfo`, closed generic, delegate, or generated row.

Cache and unload tests compile, execute, and collect query-scoped artifacts, then require weak
references to the generated assembly, carrier, and materializer to clear. A reflection ratchet
also rejects process-static retention of those generated objects.

## Architecture and generated-code ratchets

The planner owns selection and fallback reasons. Physical scans and Execution IR carry the
immutable result. Target feature analysis requires the separate
`query-row-source-access-v1` host import; legacy `source-access-v1` and global Host ABI v2 remain
unchanged. The C# renderer consumes the planned transfer and cannot query provider capabilities
or choose a different carrier.

The canonical generated-code corpus under `generated-code-samples/current` verifies the emitted
forms:

| Sample | Contract covered |
| --- | --- |
| `Q236_QueryRowLegacyFallback.cs` | Unchanged declared `GetRowSource<T>` path |
| `Q237_QueryRowReadonlyStruct.cs` | Scan-local readonly struct and static materializer |
| `Q238_QueryRowSealedClass.cs` | Wide sealed-class carrier |
| `Q239_QueryRowZeroField.cs` | Exact empty carrier with no field reads |
| `Q240_QueryRowSpecialNames.cs` | Unicode, punctuation, spaces, and keyword metadata |
| `Q241_QueryRowLifetimeBoundary.cs` | Escaping lifetime and shared immutable shape metadata |

Refresh these files only through
`GeneratedCodeSamplesSnapshotTests.Refresh_All_Local_Generated_Samples`, then refresh
`GeneratedCodeSamplesManifestTests.Refresh_Tracked_Generated_Code_Sample_Manifest`. Snapshot,
shape, execution, profiled, interpretation, and manifest tests must all remain green. Syntax and
IL ratchets verify that the materializer contains no `object[]`, reflection, delegate-held
reader, or `box`, and that every interface call is constrained. Warm JIT disassembly verifies
that no interface or virtual dispatch survives in the specialized field loop.

## Qualified CSV evidence

The corrected benchmark uses identical files, metadata, projection, accepted source plans,
filter/take semantics, and correctness oracles for legacy, readonly-struct, and sealed-class
modes. Before timing, every case compares row count, checksum, ordering, and failure behavior.
The source/carrier matrix has 96 identities; the warm/cold compiled-query matrix has 36. Three
independent reports are combined by median, and the checked-in gate rejects missing scenarios or
inconsistent cohorts.

Activation requires at least 2x legacy throughput and at least 90% lower row-overhead allocation
for every 2/8/32/64-field typed numeric carrier case, at least 20% lower end-to-end numeric CSV
allocation at every width, zero struct-carrier allocation, at most one class carrier per accepted
row, no warmed boxing/interface/virtual dispatch, no ordinary warm regression above 3%, and no
string-heavy or high-rejection regression above 5%.

On Windows 11, .NET SDK 10.0.303, .NET 10.0.11, and an Intel Core Ultra 9 285K, the Wave 10 run
passed all 30 activation checks:

- Numeric readonly-struct carrier throughput was 15.3825x, 12.7635x, 2.4194x, and 2.6422x legacy
  for 2, 8, 32, and 64 fields.
- Struct carrier allocation was 0 B and 100% below legacy at every width. The sealed-class path
  stayed below the one-carrier-per-accepted-row ceiling.
- End-to-end nullable numeric CSV allocation fell 54.39%, 47.96%, 45.25%, and 44.18%.
- All ordinary warm scenarios stayed within the 3% regression ceiling. String-full and
  high-rejection cases stayed within 5%; the closest case was high rejection at 1.0484x legacy.
- Pinned warm disassembly of the concrete eight-field reader specialization contained no boxing
  helper, `callvirt`, interface-dispatch marker, or virtual-function-pointer marker.

The CSV example therefore advertises query-scoped rows by default and retains
`new CsvSchemaProvider(enableQueryScopedRows: false)` as the explicit legacy opt-out. Full raw
results, commands, medians, allocations, and disassembly methodology are recorded in
[the benchmark baseline](../src/dotnet/Musoq.Benchmarks/Baselines/QueryScopedSourceMaterializationBaseline.md).
These measurements qualify this core CSV example and tested scenarios only. They are not a
performance claim for JSON, XML, external providers, other machines, or future runtime versions.

## Provider activation checklist

Before enabling query-scoped rows by default for another provider:

1. Preserve and test the declared-row path and an explicit legacy opt-out.
2. Test every supported/rejected CLR-type category, duplicate/ambiguous metadata, unresolved
   required fields, exact empty shapes, and special names.
3. Test entity-dependent method fallback and narrow/wide carriers across transparent, retaining,
   multi-use, and unknown operators.
4. Add generated-code samples or equivalent syntax/IL checks for the provider's materializer and
   mapping shape; refresh snapshots and manifests through their utilities.
5. Exercise compiled-query lifecycle behavior: null/missing values, pushdown before materialize,
   take/early termination, cancellation, diagnostics, exceptions, disposal, joins, outer-null
   extension, grouping, windows, CTEs, set operations, and chunk boundaries.
6. Prove cache isolation for shape, capability, carrier, and provider changes. Prove collectible
   unload and process-static retention ratchets.
7. Run a like-for-like three-sample median benchmark with correctness oracles. Meet the allocation,
   throughput, warm-regression, class-allocation, and warmed-disassembly gates before activation.
8. Run `git diff --check`, the warning-clean Release solution build, and the complete
   normal-parallel Release test suite before committing the activation.

Benchmark timing thresholds are manual qualification gates, not machine-dependent CI timing
assertions. Matrix completeness and gate arithmetic remain deterministic unit tests.

## Boundaries and deferred work

This feature is a core-engine row-transfer optimization. The separate `Musoq.DataSources`
repository is unchanged; its CSV, JSON, XML, and other providers must opt in and qualify
independently. Columnar typed batches remain deferred until real provider evidence justifies a
contract, and no unused batch API or capability flag is reserved. Package-version changes,
releases, publication, and pushes are also outside this work.
