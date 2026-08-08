namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample NullableProviderMethodLeftJoin()
    {
        return Basic(
            "Q235_NullableProviderMethodLeftJoin",
            "Join",
            "select a.Id, b.GetCountry() as RightCountry from #A.entities() a left outer join #B.entities() b on a.Id = b.Id order by a.Id");
    }
}
