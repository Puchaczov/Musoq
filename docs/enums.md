# First-class enum contract

This document freezes the first implementation profile for enum values in
Musoq Runtime v2. It is normative together with the enum sections in the core
language and TABLE/COUPLE specifications.

The contract is intentionally narrow: an enum is a nominal compile-time type
whose execution carrier is one of the eight CLR integral primitives. A CLR
enum object is allowed in compilation-scoped binding state, but never in a
generated query row, key, source predicate constant, or final table value.

## Version and repository boundary

This contract is rebased to the current Runtime v2 Core package graph. The
separate Musoq.DataSources checkout currently consumes the preceding
compatible Core train. Core is implemented and qualified first; DataSources
work starts only after the user publishes the resulting immutable Core
prerelease packages. Cloud is not modified in this campaign and stays on its
previous compatible graph until both prerelease trains exist.

## Language surface

Query-local declarations are statement-level declarations and are visible
only to later statements in the same batch:

```sql
enum JobStatus : int {
    Queued = 10,
    Running = 20,
    Finished = 30
};

flags enum FileAccess : uint {
    None = 0ui,
    Read = 1ui,
    Write = 2ui,
    ReadWrite = 3ui,
};
```

`enum` and `flags enum` are contextual at statement boundaries. They remain
valid identifiers elsewhere. The backing type must be `byte`, `sbyte`,
`short`, `ushort`, `int`, `uint`, `long`, or `ulong`. Every member requires an
explicit integral literal representable by that backing type. Empty
declarations, auto-numbering, forward references, and non-integral values are
invalid. A trailing comma is accepted.

Query-local type names are compared case-insensitively. Member names are
compared with ordinal, case-sensitive semantics. Duplicate names, including
names that differ only by case, are invalid. Duplicate values are valid
aliases; the first declared name is canonical for `EnumName`.

Flags declarations do not impose a powers-of-two validation rule. Zero,
atomic values, aliases, and named composite values are all valid. The
`flags` marker controls helper availability and participates in logical
identity and fingerprints.

Native public CLR enums are discovered only through types reachable from the
query: source columns, selected public properties, public method results, or
an exact fully qualified TABLE type reference. Musoq does not scan loaded
assemblies or maintain a process-wide CLR enum cache.

## Member syntax and helpers

A quoted string literal becomes an enum member only in a context that already
requires one specific enum identity:

```sql
where Status = 'Running'
where Status <> 'Finished'
where Status in ('Queued', 'Running')

select case Status
    when 'Queued' then 'waiting'
    when 'Running' then 'active'
    else 'done'
end
from Jobs();
```

Member names are exact and case-sensitive. Bare members, static member syntax,
and implicit numeric comparison are deliberately rejected:

```sql
where Status = Running
where Status = JobStatus.Running
where Status = 20
```

The intrinsic helper set is:

```sql
EnumValue(Status)
EnumName(Status)
IsDefined(Status)
HasAnyFlags(Access, 'Read', 'Write')
HasAllFlags(Access, 'Read', 'Write')
```

`EnumValue` returns the nullable backing carrier. `EnumName` returns the first
declared name whose complete raw value matches, or `NULL` for an unknown
value. It does not synthesize comma-separated flag names. `IsDefined` tests
for any declared raw value. Flag member arguments are bound into one typed
mask before execution.

For a null enum value, `EnumValue` and `EnumName` return `NULL`, while
`IsDefined`, `HasAnyFlags`, and `HasAllFlags` return `false`. For a non-null
value and a zero mask, `HasAllFlags` returns `true` and `HasAnyFlags` returns
`false`.

Unknown numeric values that fit the backing type remain valid. They compare,
group, and flow through a query as their primitive carrier while retaining
the enum identity in metadata. `EnumName` returns `NULL` and `IsDefined`
returns `false` for such a value.

## Nominal relational semantics

Two enums are compatible only when their logical identities match. Equal
backing types and equal member sets do not make independently declared enums
compatible.

The initial profile supports:

- `=`, `<>`, `!=`, `IS DISTINCT FROM`, and `IS NOT DISTINCT FROM` for one
  enum identity;
- `IN` and `NOT IN` with contextual member literals;
- simple and searched `CASE`;
- equality joins, including SEMI and ANTI joins;
- projection, aliases, `SELECT *`, CTEs, and derived tables;
- `GROUP BY`, `DISTINCT`, and set operations when identities agree;
- enum expressions as window partition keys.

The initial profile rejects ordering, `BETWEEN`, ASOF inequality keys,
arithmetic, general bitwise syntax, `SUM`, `AVG`, `MIN`, `MAX`, pattern
operators, implicit enum/string/numeric conversion, cross-enum operations,
enum script parameters, arbitrary enum casts, and enum-valued typed-output DTO
members.

## Public Schema contract

`ISchemaColumn` deliberately distinguishes three concerns:

- `ColumnType` is the primitive type used in generated query rows;
- `SourceReadType` is the exact type presented by the source boundary;
- `EnumType` is optional portable logical metadata.

For an ordinary column, `SourceReadType` equals `ColumnType` and `EnumType` is
`null`. For a native enum source property, `SourceReadType` is the native enum
type, `ColumnType` is its integral carrier, and `EnumType` describes the
logical enum. A dynamic query-local enum normally uses the carrier for both
CLR types and carries its identity only through `EnumType`.

The portable descriptor contains the display name, origin, backing kind,
flags marker, ordered members, aliases, and deterministic fingerprint. It
does not contain a `System.Type`, assembly path, runtime-generated enum type,
or process-global cache handle. Native CLR `Type` objects stay in
compilation-scoped binding state.

The distinction propagates through query-row fields, logical and physical
schemas, Execution IR field bindings, source-transfer fingerprints, target
compatibility identities, artifacts, and final columns. Descriptor
fingerprints are calculated outside row processing.

This is an intentional Schema ABI break. A Core package containing this
contract must not run against an older datasource package generation. Mixed
generations fail at package or ABI validation rather than falling back to an
object-valued enum path.

## Source boundary and dynamic values

Every enum is normalized immediately after a source read:

- a typed native enum read is converted directly to its backing primitive;
- a dynamic or query-scoped read requests the backing primitive from
  `IQuerySourceFieldReader.Read<T>`;
- nullable values use typed, lifted conversions.

The generated hot path may not use reflection, `Enum.Parse`, `Enum.ToObject`,
`Convert.ChangeType`, an object cast, or a boxed enum. Existing boxing at the
final `Row.this[int] : object` boundary remains part of table materialization
and is measured separately.

Dynamic `object`, dictionary, and `ExpandoObject` values are never inspected
row by row to infer an enum. A TABLE descriptor freezes the logical identity
and backing type at compilation. Changing it is schema drift and requires
recompilation. A query-scoped dynamic source must advertise both
`QueryScopedRows` and `LogicalScalarReads`; otherwise enum TABLE planning
fails without an object-path fallback.

Binary and text interpretation-schema enum fields are outside this profile.
Their representation and byte-order contracts require a separate extension.

## Source predicate planning

The source-planning representation carries enum constants as
`SourcePredicateEnumLiteral`: an allocation-free `EnumScalarValue` plus the
enum fingerprint. It never contains a boxed CLR enum.

Core may offer a source:

- enum equality and inequality;
- `IN` and `NOT IN`, preserving negation in Expression IR;
- enum null checks;
- positive `HasAnyFlags` and `HasAllFlags` terms represented by
  `SourcePredicateFlags` and an explicit match mode.

Ordering is rejected before planning. Null-safe distinct comparisons,
negated flag helpers, unsupported `OR` shapes, and any predicate form the
source cannot represent stay as Core residuals.

A Core filter is removed only when the datasource returns the exact predicate
as accepted. Core validates the accepted/residual partition and matches enum
fingerprints, operators, negation, and flag modes. A corrupt or stale
descriptor is a source-contract failure.

## Output contract

Direct enum projection writes the primitive carrier into the final row.
`Column.ColumnType` is the carrier type and `Column.EnumType` contains the
portable descriptor. No final row cell or column type contains `System.Enum`
or a CLR enum `Type`.

Consumers that need stable text must select `EnumName(...)` explicitly. The
descriptor is discovery metadata, not a promise that enum values will be
serialized as names.

## Performance qualification

Generated enum comparison and helper loops must add `0 B/row`. Equality must
remain within 2% of its integral equivalent; enum `IN`, joins, grouping, and
`DISTINCT` within 3%; flag helpers within 2% of handwritten masks. Enum-free
generated code must remain unchanged apart from unavoidable contract-version
metadata and must show no allocation regression.

The forbidden row-loop operations are `box`, reflection, `Enum.Parse`,
`Enum.ToObject`, `Convert.ChangeType`, string member parsing, locks,
fingerprint calculation, descriptor iteration, and repeated decoding of one
source field. Compiled-query-owned name lookup data is immutable and created
once outside execution loops.

## Deferred Cloud contract

Cloud remains on the previous package graph until compatible Core and
DataSources prereleases exist. A later atomic Cloud upgrade must keep cell
values numeric, optionally expose a versioned portable `enumType` column
descriptor, invalidate compiled-query caches across the artifact version, and
avoid `JsonStringEnumConverter` and CLR enum serialization. Old clients must
be able to ignore the metadata. Enum string parameters remain unsupported in
the first profile.
