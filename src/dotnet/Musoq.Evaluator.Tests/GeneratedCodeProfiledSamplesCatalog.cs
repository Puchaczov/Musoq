using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tests;

internal static class GeneratedCodeProfiledSamplesCatalog
{
    private const string SimpleSelectWhereQuery =
        "select Name, Population from #A.entities() where Population > 0";

    public static IReadOnlyList<GeneratedCodeSample> Samples { get; } =
    [
        ProfiledBasic(
            "P01_SimpleSelectWhere_Disabled",
            SimpleSelectWhereQuery,
            QueryInstrumentationMode.Disabled),
        ProfiledBasic(
            "P02_SimpleSelectWhere_SourceBoundaries",
            SimpleSelectWhereQuery,
            QueryInstrumentationMode.SourceBoundaries),
        ProfiledBasic(
            "P03_SimpleSelectWhere_Full",
            SimpleSelectWhereQuery,
            QueryInstrumentationMode.Full),
        ProfiledFromExisting("P04_InnerJoin_Full", "Q03_InnerJoin.cs"),
        ProfiledFromExisting("P05_GroupBySingle_Full", "Q05_GroupBySingle.cs"),
        ProfiledFromExisting("P06_OrderBySkipTake_Full", "Q22_OrderBySkipTake.cs"),
        ProfiledFromExisting("P07_ParallelCte_Full", "Q82_ParallelIndependentCtes.cs"),
        ProfiledFromExisting(
            "P08_RecursiveUnionAll_Disabled",
            "Q188_RecursiveUnionAllCounter.cs",
            QueryInstrumentationMode.Disabled),
        ProfiledFromExisting(
            "P09_RecursiveUnionAll_SourceBoundaries",
            "Q188_RecursiveUnionAllCounter.cs",
            QueryInstrumentationMode.SourceBoundaries),
        ProfiledFromExisting("P10_RecursiveUnionAll_Full", "Q188_RecursiveUnionAllCounter.cs"),
        ProfiledFromExisting("P11_RecursiveKeyedUnion_Full", "Q193_RecursiveUnionSingleKeyCycle.cs"),
        ProfiledFromExisting("P12_RecursiveInvariantIndexedEdges_Full", "Q203_RecursiveInvariantHashLookup.cs"),
        ProfiledFromExisting(
            "P13_RecursiveTypedInvariantDirectIndex_Full",
            "Q224_RecursiveCompositeInvariantSubplan.cs"),
        ProfiledFromExisting("P14_StableFilterProjectionReuse_Full", "Q252_StableFilterProjectionReuse.cs"),
        ProfiledFromExisting("P15_VolatileFilterProjectionReuse_Full", "Q253_VolatileFilterProjectionReuse.cs"),
        ProfiledFromExisting("P16_SharedStableWindowInputs_Full", "Q254_SharedStableWindowInputs.cs"),
        ProfiledFromExisting("P17_ParallelAggregateSharedArguments_Full", "Q256_ParallelAggregateSharedArguments.cs"),
        ProfiledFromExisting("P18_PivotPredicateDispatch_Full", "Q257_PivotPredicateDispatch.cs"),
        ProfiledFromExisting("P19_RecursiveStableScalarInvariant_Full", "Q267_RecursiveStableScalarInvariant.cs")
    ];

    public static GeneratedCodeSample GetByFileName(string fileName)
    {
        return Samples.Single(sample => sample.FileName == fileName);
    }

    private static GeneratedCodeSample ProfiledBasic(
        string name,
        string query,
        QueryInstrumentationMode instrumentationMode)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "Profiled",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = GeneratedCodeSamplesCatalog.CreateBasicSchemaProvider,
            CompilationOptions = new CompilationOptions(instrumentationMode: instrumentationMode)
        };
    }

    private static GeneratedCodeSample ProfiledFromExisting(
        string name,
        string sourceFileName,
        QueryInstrumentationMode instrumentationMode = QueryInstrumentationMode.Full)
    {
        var source = GeneratedCodeSamplesCatalog.GetByFileName(sourceFileName);

        return source with
        {
            Name = name,
            FileName = $"{name}.cs",
            Category = "Profiled",
            CompilationOptions = source.CompilationOptions.WithInstrumentationMode(instrumentationMode)
        };
    }
}
