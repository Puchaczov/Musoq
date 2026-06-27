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
        ProfiledFromExisting("P07_ParallelCte_Full", "Q82_ParallelIndependentCtes.cs")
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

    private static GeneratedCodeSample ProfiledFromExisting(string name, string sourceFileName)
    {
        var source = GeneratedCodeSamplesCatalog.GetByFileName(sourceFileName);

        return source with
        {
            Name = name,
            FileName = $"{name}.cs",
            Category = "Profiled",
            CompilationOptions = source.CompilationOptions.WithInstrumentationMode(QueryInstrumentationMode.Full)
        };
    }
}