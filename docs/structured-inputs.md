# Structured inputs and VALUES migration

Core structured source arguments use one named record notation across expressions,
arrays, and inline relational rows. The syntax migration is present in Core 1.7; contract and performance remediation is tracked in the qualification ledger.

## Final syntax

Use a parenthesized row for every VALUES row:

~~~sql
select p.Id
from values {
    (Id: 'todo'),
    (Id: 'fixme'),
} p
~~~

Use array { ... } for collection values and (Name: expression) for records:

~~~sql
param(
    patterns: (
        Id: string,
        Pattern: string,
        Mode: string = 'literal'
    )[]
)

let localPatterns = array {
    (Id: 'todo', Pattern: 'TODO'),
    (Pattern: 'FIXME', Id: 'fixme'),
};

select m.PatternId, m.MatchText
from #inputs.match('TODO FIXME', patterns: $localPatterns) m
~~~

The former brace-row spelling is a breaking change and is rejected:

~~~sql
-- rejected
values { { Id: 'todo' } } p
~~~

Rows still require an alias, at least one field, and the same
case-insensitive field set in every row. Field order may differ, and a trailing
comma is allowed. Ordinary parenthesized arithmetic remains grouping, so
(Value: (1 + 2) * 3) contains a record field whose expression is evaluated
normally.

## Source and host boundaries

Structural arguments are bound once against the receiving typed contract. Core
accepts Core-owned structural values, string-keyed dictionaries, arrays,
List<T>, and IReadOnlyList<T>. It snapshots host values before opening a
query datasource. Missing fields, explicit nulls, empty arrays, and defaults
remain distinct. A source is registered through its typed construction binding;
Core never falls back to an object[] source call or arbitrary POCO/JSON
discovery.

A complete visible CTE can be passed directly as a collection argument:

~~~sql
with patterns as (
    select p.Id, p.Pattern
    from values {
        (Id: 'todo', Pattern: 'TODO'),
    } p
)
select m.PatternId
from #inputs.match('TODO', patterns: patterns) m
~~~

The receiver validates the complete CTE shape before column pruning. Primitive
receivers require one output column; record receivers match columns by name and
preserve duplicate rows and order supplied by the CTE.

## Metadata

DESC ARGUMENTS reports the selected receiving contract without evaluating
values, opening a source, or enumerating a CTE:

~~~sql
desc arguments #inputs.match
desc arguments #inputs.match(
    'TODO',
    patterns: array { (Id: 'todo', Pattern: 'TODO') }
)
~~~

The result has Overload, Path, Kind, Type, Required, Nullable,
HasDefault, Default, MaxDepth, MaxNodes, and MaxStringBytes. An explicit\nnull default is reported as HasDefault=true with the text `null`; an absent\ndefault uses a database-null cell. Defaults
and limits are metadata; no host parameter value is required for a description.

## Maintained example and qualification

The runnable demonstration datasource is under
src/dotnet/examples/data-sources/structured-inputs/. Its --all runner and
test resources cover inline records and primitive/nested arrays, inferred and
annotated lets, host parameters, CTE arguments, correlated and coupled calls,
and all DESC families:

~~~powershell
dotnet run --project src/dotnet/examples/data-sources/structured-inputs/runner/Musoq.Examples.DataSources.StructuredInputs.Runner.csproj -c Release -- --all
~~~

The Wave 20 deterministic populations use fixed seeds and contain at least
2,000 parser/recovery cases, 512 binding/type/default cases, and 128 compiled
execution cases. They include valid near misses beside malformed inputs and
metamorphic checks for field order/casing, defaults, host representation,
per-run isolation, and observable array order. Generated code samples Q330–Q353
exercise the same contracts through plans and emitted C#.