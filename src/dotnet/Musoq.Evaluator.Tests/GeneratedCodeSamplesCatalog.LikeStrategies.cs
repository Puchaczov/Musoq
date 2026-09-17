namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateLikeStrategySamples()
    {
        return
        [
            Basic(
                "Q369_DynamicColumnLike",
                "Scalar",
                "select Id, Name from #A.entities() where Name like City order by Id"),
            new GeneratedCodeSample
            {
                Name = "Q370_CorrelatedApplyDynamicLike",
                FileName = "Q370_CorrelatedApplyDynamicLike.cs",
                Query = "select i.Name, s.Value from #apply.items() i " +
                        "cross apply i.JustReturnArrayOfString() s where i.Name like s.Value order by i.Name, s.Value",
                Category = "Apply",
                Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
                CreateSchemaProvider = CreateGeneratedApplySchemaProvider
            },
            Basic(
                "Q371_UnicodeLikeClauseContexts",
                "Grouping",
                "select City, City like 'Ł%' as IsPolish, " +
                "Count(Name) filter (where Name like 'Ż%') as MatchingNames " +
                "from #A.entities() where Name like '%ó%' or City like 'Ł%' " +
                "group by City having City like 'Ł%' order by City"),
            Basic(
                "Q372_ConstantRLike",
                "Scalar",
                "select Id, Name from #A.entities() where Name rlike r'\\A[A-Z][a-z]+\\z' order by Id"),
            Basic(
                "Q373_DynamicColumnRLike",
                "Scalar",
                "select Id, Name from #A.entities() where Name rlike City order by Id"),
            new GeneratedCodeSample
            {
                Name = "Q374_CorrelatedApplyRLike",
                FileName = "Q374_CorrelatedApplyRLike.cs",
                Query = "select i.Name, s.Value from #apply.items() i " +
                        "cross apply i.JustReturnArrayOfString() s where s.Value rlike i.Name",
                Category = "Apply",
                Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
                CreateSchemaProvider = CreateGeneratedApplySchemaProvider
            },
            Basic(
                "Q375_LiteralAndFallbackRLike",
                "Scalar",
                "select Name, Name rlike r'\\AAlex\\z' as IsExact, " +
                "Name rlike r'^[A-Z]' as UsesRegex from #A.entities() order by Name")
        ];
    }
}
