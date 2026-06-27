namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateRuntimeV2CastGroupingSamples()
    {
        return
        [
            RuntimeV2CastGroupingFeature(
                "Q150_RuntimeV2CastProjection",
                @"SELECT Population::Int32 as PopulationInt,
                         Amount::Decimal as AmountDecimal,
                         Id::Guid as EntityGuid
                  FROM #features.items()"),
            RuntimeV2CastGroupingFeature(
                "Q151_RuntimeV2CastExpressions",
                @"SELECT (Quantity + 1)::Int64 as QuantityNext,
                         Population::Int32::String as PopulationText,
                         CreatedAt::DateTimeOffset as CreatedOffset
                  FROM #features.items()
                  WHERE Population::Int32 > 1000
                  ORDER BY Amount::Decimal"),
            RuntimeV2CastGroupingFeature(
                "Q152_RuntimeV2CastAggregateGrouping",
                @"SELECT City, Sum(Amount::Decimal) as TotalAmount
                  FROM #features.items()
                  WHERE Population::Int32 > 0
                  GROUP BY City
                  HAVING Sum(Amount::Decimal) > '10.00'::Decimal"),
            RuntimeV2CastGroupingFeature(
                "Q153_RuntimeV2GroupByOrdinal",
                @"SELECT City, Department, Count(*) as Cnt
                  FROM #features.items()
                  GROUP BY 1, 2"),
            RuntimeV2CastGroupingFeature(
                "Q154_RuntimeV2GroupByAllCasts",
                @"SELECT City, Population::Int32 as PopulationInt, Count(*) as Cnt
                  FROM #features.items()
                  GROUP BY ALL"),
            RuntimeV2CastGroupingFeature(
                "Q155_RuntimeV2AliasWhereGroupBy",
                @"SELECT City as c, Count(*) as Cnt
                  FROM #features.items()
                  WHERE c <> ''
                  GROUP BY c"),
            RuntimeV2CastGroupingFeature(
                "Q156_RuntimeV2HavingAggregateAlias",
                @"SELECT City, Count(*) as cnt
                  FROM #features.items()
                  GROUP BY City
                  HAVING cnt > 1"),
            RuntimeV2CastGroupingFeature(
                "Q157_RuntimeV2AliasSourceConflict",
                @"SELECT City as Department, Department as SourceDepartment, Count(*) as cnt
                  FROM #features.items()
                  GROUP BY Department, City"),
            RuntimeV2CastGroupingFeature(
                "Q158_RuntimeV2CombinedGrouping",
                @"SELECT City as c,
                         Population::Int32 as pop,
                         Count(*) as cnt,
                         Sum(Amount::Decimal) as total
                  FROM #features.items()
                  WHERE pop > 0
                  GROUP BY ALL
                  HAVING cnt > 1 AND total > '10.00'::Decimal
                  ORDER BY c")
        ];
    }

    private static GeneratedCodeSample RuntimeV2CastGroupingFeature(string name, string query)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "RuntimeV2CastGrouping",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateRuntimeV2CastGroupingFeatureSchemaProvider,
            CompilationOptions = new CompilationOptions(useCommonSubexpressionElimination: true)
        };
    }
}
