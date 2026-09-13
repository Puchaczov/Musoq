using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateStructuredInputSamples()
    {
        return
        [
            Structured(
                "Q330_StructuredPrimitiveArray",
                "StructuredInputs",
                @"select n.Value
from #inputs.numbers(values: array { 1, 2, 2, 3 }) n"),
            Structured(
                "Q331_StructuredRecordArray",
                "StructuredInputs",
                @"select m.PatternId, m.MatchText
from #inputs.match(
    'TODO FIXME',
    patterns: array {
        (Id: 'todo', Pattern: 'TODO'),
        (Pattern: 'FIXME', Id: 'fixme'),
    }
) m"),
            Structured(
                "Q332_StructuredNestedRecord",
                "StructuredInputs",
                @"select c.Enabled, c.Before, c.After
from #inputs.configure(
    options: (
        Enabled: true,
        Codes: array { 10, 20 },
        Window: (Before: 2, After: 3),
    )
) c"),
            Structured(
                "Q333_StructuredNestedArrays",
                "StructuredInputs",
                @"select m.Value, m.Row, m.Column
from #inputs.matrix(
    values: array {
        array { 1, 2 },
        array { 3 },
        array {},
    }
) m"),
            Structured(
                "Q334_StructuredReorderedDefaults",
                "StructuredInputs",
                @"select w.Value, w.Weight, w.Enabled
from #inputs.weighted(
    items: array {
        (Value: 10, Weight: 1.5),
        (Weight: 2.5, Value: 20, Enabled: false),
    }
) w"),
            Structured(
                "Q335_StructuredInferredLetPresence",
                "StructuredInputs",
                @"let todo = (Id: 'todo', Pattern: 'TODO');
let fixme = (Id: 'fixme', Pattern: 'FIXME');
let patterns = array { $todo, (Id: 'issue', Pattern: 'ISSUE-[0-9]+', Mode: 'regex') };

select m.PatternId
from #inputs.match('TODO FIXME', patterns: $patterns) m"),
            Structured(
                "Q336_StructuredDeclaredLetDefaults",
                "StructuredInputs",
                @"let patterns: (
    Id: string,
    Pattern: string,
    Mode: string = 'regex'
)[] = array {
    (Id: 'todo', Pattern: 'TODO'),
    (Id: 'issue', Pattern: 'ISSUE-[0-9]+'),
};

select m.PatternId, m.MatchText
from #inputs.match('TODO ISSUE-42', patterns: $patterns) m"),
            Structured(
                "Q337_StructuredParameterDefaults",
                "StructuredInputs",
                @"param(
    patterns: (
        Id: string,
        Pattern: string,
        Mode: string = 'literal'
    )[] = array {
        (Id: 'todo', Pattern: 'TODO'),
    },
    emptyNumbers: int[] = array {},
    nullableNumbers: int?[] = array { 1, null }
)

select m.PatternId, m.MatchText
from #inputs.match('TODO', patterns: $patterns) m"),
            Structured(
                "Q338_StructuredPrimitiveCteArgument",
                "StructuredInputs",
                @"with numbers as (
    select p.Value
    from values {
        (Value: 1),
        (Value: 2),
        (Value: 2),
    } p
)
select n.Value
from #inputs.numbers(values: numbers) n"),
            Structured(
                "Q339_StructuredRecordCteArgument",
                "StructuredInputs",
                @"with patterns as (
    select p.Id, p.Pattern
    from values {
        (Id: 'todo', Pattern: 'TODO'),
        (Id: 'fixme', Pattern: 'FIXME'),
    } p
)
select m.PatternId, m.MatchText
from #inputs.match('TODO FIXME', patterns: patterns) m"),
            Structured(
                "Q340_StructuredSharedCteArgument",
                "StructuredInputs",
                @"with patterns as (
    select p.Id, p.Pattern
    from values {
        (Id: 'todo', Pattern: 'TODO'),
    } p
)
select leftMatch.PatternId, rightMatch.PatternId
from #inputs.match('TODO', patterns: patterns) leftMatch
cross join #inputs.match('TODO', patterns: patterns) rightMatch"),
            Structured(
                "Q341_StructuredSortedCteArgument",
                "StructuredInputs",
                @"with numbers as (
    select p.Value
    from values {
        (Value: 3),
        (Value: 1),
        (Value: 2),
    } p
    order by p.Value
)
select n.Value
from #inputs.numbers(values: numbers) n"),
            Structured(
                "Q342_StructuredDistinctCteArgument",
                "StructuredInputs",
                @"with numbers as (
    select distinct p.Value
    from values {
        (Value: 1),
        (Value: 2),
        (Value: 2),
    } p
)
select n.Value
from #inputs.numbers(values: numbers) n"),
            Structured(
                "Q343_StructuredGroupedCteArgument",
                "StructuredInputs",
                @"with numbers as (
    select p.Value
    from values {
        (Value: 1),
        (Value: 2),
        (Value: 2),
    } p
    group by p.Value
)
select n.Value
from #inputs.numbers(values: numbers) n"),
            Structured(
                "Q344_StructuredSetCteArgument",
                "StructuredInputs",
                @"with numbers as (
    select p.Value
    from values { (Value: 1) } p
    union all (Value)
    select q.Value
    from values { (Value: 2) } q
)
select n.Value
from #inputs.numbers(values: numbers) n"),
            Structured(
                "Q345_StructuredPagedCteArgument",
                "StructuredInputs",
                @"with numbers as (
    select p.Value
    from values {
        (Value: 3),
        (Value: 1),
        (Value: 2),
    } p
    order by p.Value
    take 2
)
select n.Value
from #inputs.numbers(values: numbers) n"),
            StructuredWithOptions(
                "Q346_StructuredParallelCteArguments",
                "StructuredInputs",
                @"with patterns as (
    select p1.Id, p1.Pattern
    from values {
        (Id: 'todo', Pattern: 'TODO'),
        (Id: 'fixme', Pattern: 'FIXME'),
    } p1
), numbers as (
    select p2.Value
    from values {
        (Value: 1),
        (Value: 2),
    } p2
)
select m.PatternId, n.Value
from #inputs.match('TODO FIXME', patterns: patterns) m
cross join #inputs.numbers(values: numbers) n",
                new CompilationOptions(useCteParallelization: true).WithStabilityAwareScalarReuse()),
            Structured(
                "Q347_StructuredCorrelatedInvocation",
                "StructuredInputs",
                @"param(suffix: string = '!')

select m.PatternId
from values {
    (Pattern: 'TODO'),
} p
cross apply #inputs.match(
    'TODO!',
    patterns: array {
        (Id: 'todo', Pattern: p.Pattern + $suffix),
    }
) m"),
            Structured(
                "Q348_StructuredEmptyAndSuppressedDemand",
                "StructuredInputs",
                @"with unused as (
    select p.Value
    from values { (Value: 100) } p
)
select n.Value
from #inputs.numbers(values: array {}) n"),
            Structured(
                "Q349_StructuredCoupledSource",
                "StructuredInputs",
                @"table MatchRows {
    PatternId: string
};

couple #inputs.match with table MatchRows as Matches;

select m.PatternId
from Matches(
    'TODO',
    patterns: array { (Id: 'todo', Pattern: 'TODO') }
) m"),
            Structured(
                "Q350_StructuredArgumentsDescription",
                "Description",
                @"desc arguments #inputs.match(
    'TODO',
    patterns: array { (Id: 'todo', Pattern: 'TODO') }
)"),
            Structured(
                "Q351_StructuredMetadataOnlyDescription",
                "Description",
                @"desc query (
    with patterns as (
        select p.Id, p.Pattern
        from values { (Id: 'todo', Pattern: 'TODO') } p
    )
    select m.PatternId, m.MatchText
    from #inputs.match('TODO', patterns: patterns) m
)"),
            StructuredWide(
                "Q352_StructuredWidePresence",
                "StructuredInputs",
                @"
let items = array {
    (F01: 1, F02: 2, F03: 3, F04: 4, F05: 5, F06: 6, F07: 7, F08: 8, F09: 9, F10: 10, F11: 11, F12: 12, F13: 13, F14: 14, F15: 15, F16: 16, F17: 17, F18: 18, F19: 19, F20: 20, F21: 21, F22: 22, F23: 23, F24: 24, F25: 25, F26: 26, F27: 27, F28: 28, F29: 29, F30: 30, F31: 31, F32: 32, F33: 33, F34: 34, F35: 35, F36: 36, F37: 37, F38: 38, F39: 39, F40: 40, F41: 41, F42: 42, F43: 43, F44: 44, F45: 45, F46: 46, F47: 47, F48: 48, F49: 49, F50: 50, F51: 51, F52: 52, F53: 53, F54: 54, F55: 55, F56: 56, F57: 57, F58: 58, F59: 59, F60: 60, F61: 61, F62: 62, F63: 63, F64: 64),
    (F65: 65),
};

select w.Value
from #structured.wide(items: $items) w
"),
            Structured(
                "Q353_StructuredValuesGroupedExpression",
                "Values",
                @"from values {
    (Label: ')', Value: (1 + 2) * 3),
    (Label: '(', Value: 4),
} p
select p.Label, p.Value")
        ];
    }

    private static GeneratedCodeSample Structured(string name, string category, string query)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = category,
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateStructuredInputsSchemaProvider
        };
    }

    private static GeneratedCodeSample StructuredWithOptions(
        string name,
        string category,
        string query,
        CompilationOptions compilationOptions)
    {
        return Structured(name, category, query) with
        {
            CompilationOptions = compilationOptions
        };
    }

    private static GeneratedCodeSample StructuredWide(string name, string category, string query)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = category,
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateStructuredWideSchemaProvider
        };
    }
}
