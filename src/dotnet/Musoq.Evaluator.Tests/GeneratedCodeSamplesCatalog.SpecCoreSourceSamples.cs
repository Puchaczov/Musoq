namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateSpecificationCoreSourceSamples()
    {
        return
        [
            Basic(
                "Q271_SpecCoreAnySomeSubqueries",
                "Subquery",
                "select a.City, " +
                "a.Country = ANY (select b.Country from #B.entities() b) as AnyMatch, " +
                "a.Population > SOME (select c.Population from #C.entities() c) as SomeMatch " +
                "from #A.entities() a"),
            Basic(
                "Q272_SpecCoreNotInSetSubquery",
                "Subquery",
                "select Count(a.City) as NotMemberCount from #A.entities() a " +
                "where a.City NOT IN (select b.City from #B.entities() b)"),
            Basic(
                "Q273_SpecCoreCorrelatedScalarSetOperation",
                "Subquery",
                "SELECT a.City, (" +
                "SELECT b.City FROM #B.entities() b " +
                "WHERE b.Country = a.Country AND (b.City = 'KRAKOW' OR b.City = 'PARIS') " +
                "UNION (City) " +
                "SELECT c.City FROM #C.entities() c WHERE c.Country = a.Country" +
                ") AS MatchCity FROM #A.entities() a"),
            Basic(
                "Q277_SpecCoreNamedDatasourceArguments",
                "Parameters",
                "param(label: string = 'parameter'); " +
                "table NamedShape { Value: int, First: string, Second: int }; " +
                "couple #named.any with table NamedShape as Data; " +
                "select d.Value, d.First, d.Second, p.Value, p.First, p.Second " +
                "from Data(second: 4, first: $label) d " +
                "cross join Data(first: 'positional') p") with
            {
                CreateSchemaProvider = CreateNamedDatasourceSampleProvider
            }
        ];
    }
}
