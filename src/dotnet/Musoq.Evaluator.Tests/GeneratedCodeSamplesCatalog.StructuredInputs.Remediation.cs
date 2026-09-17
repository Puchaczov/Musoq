using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateStructuredInputRemediationSamples()
    {
        return
        [
            Structured(
                "Q354_StructuredNumericOverload",
                "StructuredInputs",
                @"select o.Kind, o.Value, o.Count
from #inputs.overloaded(42.5) o"),
            Structured(
                "Q355_StructuredStructuralOverload",
                "StructuredInputs",
                @"select o.Kind, o.Value, o.Count
from #inputs.overloaded(
    patterns: array {
        (Id: 'todo', Pattern: 'TODO'),
        (Id: 'fixme', Pattern: 'FIXME'),
    }
) o"),
            Structured(
                "Q356_StructuredDeclarationDefaultConflict",
                "StructuredInputs",
                @"param(input: (
    Id: string,
    Pattern: string,
    Mode: string = 'regex'
) = (Id: 'todo', Pattern: 'TODO'))

select d.Id, d.Mode
from #inputs.defaultconflict(input: $input) d"),
            Structured(
                "Q357_StructuredExplicitNullDescription",
                "Description",
                @"desc arguments #inputs.defaultconflict(
    input: (Id: 'todo', Pattern: 'TODO', Mode: null)
)"),
            Structured(
                "Q358_StructuredContextLifecycle",
                "StructuredInputs",
                @"select c.QueryId, c.SourceContextId, c.Alias
from #inputs.contextprobe() c"),
            Structured(
                "Q359_StructuredMutableOwnership",
                "StructuredInputs",
                @"select left.Value, right.Value
from #inputs.mutable(items: array { (Value: 1) }) left
cross join #inputs.mutable(items: array { (Value: 1) }) right"),
            Structured(
                "Q360_StructuredTypedParameterSnapshot",
                "StructuredInputs",
                @"param(
    input: (
        Id: string,
        Pattern: string,
        Mode: string = 'regex'
    )[] = array { (Id: 'todo', Pattern: 'TODO') }
)

select m.PatternId, m.MatchText
from #inputs.match('TODO', patterns: $input) m"),
            Structured(
                "Q361_StructuredRecursiveCteArgument",
                "StructuredInputs",
                @"with recursive numbers (Value) as (
    select Value
    from values { (Value: 1) } seed
    union all
    select n.Value + 1
    from numbers n
    where n.Value < 3
)
select n.Value
from #inputs.numbers(values: numbers) n"),
            StructuredWithOptions(
                "Q362_StructuredParallelDirectCte",
                "StructuredInputs",
                @"with patterns as (
    select p1.Id, p1.Pattern
    from values { (Id: 'todo', Pattern: 'TODO') } p1
), numbers as (
    select p2.Value
    from values { (Value: 1), (Value: 2) } p2
)
select m.PatternId, n.Value
from #inputs.match('TODO', patterns: patterns) m
cross join #inputs.numbers(values: numbers) n",
                new CompilationOptions(useCteParallelization: true).WithStabilityAwareScalarReuse()),
            Structured(
                "Q363_StructuredStrictReceiverDemand",
                "StructuredInputs",
                @"select s.Value
from #inputs.strict(values: array { 1, 2 }) s"),
            Structured(
                "Q364_StructuredFilteredCteArgument",
                "StructuredInputs",
                @"with numbers as (
    select p.Value
    from values { (Value: 3), (Value: 1), (Value: 2) } p
    where p.Value > 1
)
select n.Value
from #inputs.numbers(values: numbers) n"),
            Structured(
                "Q365_StructuredValuesAndArrayInOneScript",
                "StructuredInputs",
                @"let values = array { 1, 2, 3 };

select n.Value
from values { (Label: 'numbers') } p
cross apply #inputs.numbers(values: $values) n"),
            Structured(
                "Q366_StructuredOverloadInventory",
                "Description",
                @"desc arguments #inputs.overloaded"),
            Structured(
                "Q367_StructuredEmptyCteArgument",
                "StructuredInputs",
                @"with empty as (
    select p.Value
    from values { (Value: 1) } p
    where p.Value < 0
)
select n.Value
from #inputs.numbers(values: empty) n"),
            Structured(
                "Q368_StructuredPrimitiveEmptyContext",
                "Description",
                @"desc arguments #inputs.numbers(values: array {})")
        ];
    }
}
