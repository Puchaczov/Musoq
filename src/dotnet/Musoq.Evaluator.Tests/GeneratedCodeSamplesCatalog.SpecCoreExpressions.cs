namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateSpecificationCoreExpressionSamples()
    {
        return
        [
            Basic(
                "Q268_SpecCoreOperatorsAndRawLiterals",
                "Scalar",
                @"select R'C:\new\test' as RawPath, '\'' as Escaped,
                     -Population as UnaryValue,
                     Population + 2 * 3 % 2 as ArithmeticValue,
                     (Id & 3) | (Id << 2) as BitwiseValue,
                     Id >> 1 as ShiftedValue,
                     0x10 as HexValue,
                     0b1010 as BinaryValue,
                     0o17 as OctalValue,
                     18.5d as DecimalValue,
                     Population between 1 and 2000000 as InRange,
                     case when Population > 0 then 'positive' else 'zero' end as CaseValue,
                     null + Population as NullPropagation,
                     null ?? Population as NullFallback
              from #A.entities()
              where Match('\\d+', Name)"),
            Basic(
                "Q269_SpecCorePatternPredicates",
                "Scalar",
                "select Name, Name like 'A%' as IsLike, Name not like 'Z%' as IsNotLike, " +
                "Name rlike '^[A-Z]' as IsRlike, Name not rlike '^$' as IsNotRlike " +
                "from #A.entities() where any(Name, City) like '%a%' " +
                "and all(Name, City) not like '%z%'"),
            Basic(
                "Q270_SpecCoreMembershipPredicates",
                "InClause",
                "param(names: string[]) " +
                "select Name from #A.entities() " +
                "where Name in ('Alice', 'Bob', 'Cara', 'Dora', 'Eve', 'Fay', 'Gina', 'Hana', 'Ivy', 'Jill', " +
                "'Kara', 'Liam', 'Mona', 'Nora', 'Owen', 'Pia', 'Quin', 'Rita', 'Sara', 'Tara') " +
                "and Name not in $names and Name contains ('a')"),
            Basic(
                "Q274_SpecCoreNullFallback",
                "Scalar",
                "select Population ?? 'unused' as NonNullableFallback, " +
                "NullableValue ?? 0 as NullableFallback, " +
                "Name ?? City ?? 'fallback' as ReferenceFallback, null ?? Name as NullFallback " +
                "from #A.entities()"),
            Basic(
                "Q275_SpecCoreStarFilterModifiers",
                "Scan",
                "select * like '%o%' exclude (Money) replace (Population * 2 as Population) " +
                "rename (Country as Nation, Population as WeightedPopulation) from #A.entities()"),
            Basic(
                "Q276_SpecCoreRowAndMemberAccess",
                "Scalar",
                "select RowNumber() as RowNo, Self.Array[0] as FirstItem, Self.Array[-1] as LastItem, " +
                "Self.Array[10] as MissingItem, Name[0] as FirstCharacter, " +
                "Self.Dictionary['A'] as DictionaryValue, Self.Self.Name as NestedName, " +
                "ToString(Self) as EntityText from #A.entities()"),
            Basic(
                "Q299_SpecCoreStringNumericCoercion",
                "Scalar",
                "select Name from #A.entities() where Name = 42"),
            Basic(
                "Q300_SpecCoreTemporalCoercion",
                "Scalar",
                "select Name from #A.entities() where Time >= '2024-01-01'"),
            Basic(
                "Q301_SpecCoreObjectNumericCoercion",
                "Scalar",
                "select Label from #object.items() where Value > 10") with
            {
                CreateSchemaProvider = CreateObjectCoercionSampleProvider
            }
        ];
    }
}
