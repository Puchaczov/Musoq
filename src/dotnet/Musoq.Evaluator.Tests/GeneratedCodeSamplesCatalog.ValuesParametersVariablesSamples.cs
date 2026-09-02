namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateValuesParametersAndVariablesSamples()
    {
        return
        [
            Basic(
                "Q117_ValuesRowLiterals",
                "Values",
                @"from values {
                  { Name: 'Newtonsoft.Json', Approved: true, Score: 10ui },
                  { Name: 'Legacy.Package', Approved: false, Score: 20ui }
              } packages
              where packages.Approved = false
              select packages.Name, packages.Score"),
            Basic(
                "Q118_ValuesCteReuse",
                "Values",
                @"with policy as (
                  from values {
                      { Name: 'Newtonsoft.Json', Approved: true },
                      { Name: 'Legacy.Package', Approved: false }
                  } p
                  select p.Name, p.Approved
              )
              select leftPolicy.Name, rightPolicy.Approved
              from policy leftPolicy
              inner join policy rightPolicy on leftPolicy.Name = rightPolicy.Name
              where rightPolicy.Approved = false"),
            Basic(
                "Q119_ValuesNumericLiterals",
                "Values",
                @"from values {
                  {
                      PlainInt: 10,
                      UIntValue: 11ui,
                      LongValue: 12l,
                      ULongValue: 13ul,
                      ShortValue: 14s,
                      UShortValue: 15us,
                      SByteValue: 16b,
                      ByteValue: 17ub,
                      DecimalValue: 18.5d,
                      HexValue: 0x10,
                      BinaryValue: 0b1010,
                      OctalValue: 0o17
                  }
              } literals
              select literals.PlainInt,
                     literals.UIntValue,
                     literals.LongValue,
                     literals.ULongValue,
                     literals.ShortValue,
                     literals.UShortValue,
                     literals.SByteValue,
                     literals.ByteValue,
                     literals.DecimalValue,
                     literals.HexValue,
                     literals.BinaryValue,
                     literals.OctalValue"),
            Basic(
                "Q171_ValuesStaticParametersAndLets",
                "Values",
                @"param(baseScore: int, suffix: string = '-ok')
              let bonus: int = 5
              from values {
                  { Name: 'first' + $suffix, Score: $baseScore },
                  { Name: 'second' + $suffix, Score: $baseScore + $bonus }
              } scores
              select scores.Name, scores.Score"),
            Basic(
                "Q172_CollectionParameterInMembership",
                "Parameters",
                @"param(ids: int[])
              select Name, Id
              from #A.entities()
              where Id in $ids
              order by Name"),
            Basic(
                "Q120_ScriptParametersWhereSelect",
                "Parameters",
                @"param(country: string, minPopulation: int = 100)
              select Name, Population, $country as RequestedCountry
              from #A.entities()
              where Country = $country and Population > $minPopulation"),
            Basic(
                "Q121_ScriptParameterPrimitiveDefaults",
                "Parameters",
                @"param(
                  flag: bool = true,
                  code: char = 'x',
                  limit: int? = null,
                  id: guid = '2ffcf6fa-3369-4300-946a-bb131a037985',
                  created: datetime = '2024-01-02T03:04:05.0000000Z')
              select $flag, $code, $limit, $id, $created
              from #A.entities()"),
            ScriptParameterSourceArgument(),
            Basic(
                "Q123_ScriptParameterGroupByHelperCapture",
                "Parameters",
                @"param(suffix: string, minCount: int)
              select Country + $suffix as CountryKey, Count(Name) as NameCount
              from #A.entities()
              group by Country + $suffix
              having Count(Name) >= $minCount"),
            BasicWithOptions(
                "Q124_ScriptParameterJoinHelperCapture",
                "Parameters",
                @"param(suffix: string, fallback: string)
              select a.Name, Coalesce(b.Name + $suffix, $fallback) as MatchedName
              from #A.entities() a
              left outer join #B.entities() b on a.City + $suffix = b.City + $suffix",
                new CompilationOptions(useHashJoin: true, useSortMergeJoin: false)
                    .WithStabilityAwareScalarReuse()),
            Basic(
                "Q125_ScriptParameterCteHelperCapture",
                "Parameters",
                @"param(country: string)
              with filtered as (
                  select Name, City, $country as RequestedCountry
                  from #A.entities()
                  where Country = $country
              )
              select l.Name, r.RequestedCountry
              from filtered l
              inner join filtered r on l.Name = r.Name"),
            Basic(
                "Q126_ScriptParameterWindowHelperCapture",
                "Parameters",
                @"param(country: string, label: string)
              select Name,
                     RowNumber() over (
                         partition by case when Country = $country then $country else Country end
                         order by Name + $label
                     ) as rn,
                     $label as WindowLabel
              from #A.entities()"),
            RuntimeV2ScriptParameter(
                "Q127_ScriptParameterParallelHelperCapture",
                @"param(threshold: int, label: string)
              SELECT Name, $label as Label, HeavyComputation(Value) as Heavy
              FROM #test.entities()
              WHERE Value > $threshold"),
            Basic(
                "Q128_ScriptParameterTypedComparison",
                "Parameters",
                @"param(country: string)
              select Name, Country
              from #A.entities()
              where Country = $country"),
            Basic(
                "Q129_ScriptParameterNumericWideningComparison",
                "Parameters",
                @"param(minPopulation: int)
              select Name, Population
              from #A.entities()
              where Population >= $minPopulation"),
            Basic(
                "Q130_ScriptVariableWhereSelect",
                "Variables",
                @"let country: string = 'Poland'
                            let minPopulation: int = 100
                            select Name, Population, $country as RequestedCountry
                            from #A.entities()
                            where Country = $country and Population > $minPopulation"),
            Basic(
                "Q131_ScriptVariablePrimitiveValues",
                "Variables",
                @"let flag: bool = true
                            let code: char = 'x'
                            let limit: int? = null
                            let id: guid = '2ffcf6fa-3369-4300-946a-bb131a037985'
                            let created: datetime = '2024-01-02T03:04:05.0000000Z'
                            let elapsed: timespan = '01:30:00'
                            select $flag, $code, $limit, $id, $created, $elapsed
                            from #A.entities()"),
            ScriptVariableSourceArgument(),
            Basic(
                "Q133_ScriptVariableGroupByHavingCapture",
                "Variables",
                @"let suffix: string = '_country'
                            let minCount: int = 2
                            select Country + $suffix as CountryKey, Count(Name) as NameCount
                            from #A.entities()
                            group by Country + $suffix
                            having Count(Name) >= $minCount"),
            BasicWithOptions(
                "Q134_ScriptVariableJoinHelperCapture",
                "Variables",
                @"let suffix: string = '_joined'
                            let fallback: string = 'missing'
                            select a.Name, Coalesce(b.Name + $suffix, $fallback) as MatchedName
                            from #A.entities() a
                            left outer join #B.entities() b on a.City + $suffix = b.City + $suffix",
                new CompilationOptions(useHashJoin: true, useSortMergeJoin: false)
                    .WithStabilityAwareScalarReuse()),
            Basic(
                "Q135_ScriptVariableCteHelperCapture",
                "Variables",
                @"let country: string = 'Poland'
                            with filtered as (
                                    select Name, City, $country as RequestedCountry
                                    from #A.entities()
                                    where Country = $country
                            )
                            select l.Name, r.RequestedCountry
                            from filtered l
                            inner join filtered r on l.Name = r.Name"),
            Basic(
                "Q136_ScriptVariableWindowHelperCapture",
                "Variables",
                @"let country: string = 'Poland'
                            let label: string = '_window'
                            select Name,
                                         RowNumber() over (
                                                 partition by case when Country = $country then $country else Country end
                                                 order by Name + $label
                                         ) as rn,
                                         $label as WindowLabel
                            from #A.entities()"),
            RuntimeV2ScriptVariable(
                "Q137_ScriptVariableParallelHelperCapture",
                @"let threshold: int = 100
                            let label: string = 'static'
                            SELECT Name, $label as Label, HeavyComputation(Value) as Heavy
                            FROM #test.entities()
                            WHERE Value > $threshold")
        ];
    }
}
