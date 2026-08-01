using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.RuntimeDynamic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static IReadOnlyList<GeneratedCodeSample> CreateRuntimeDynamicSamples()
    {
        return
        [
            RuntimeDynamicInspection(
                "Q231_PublicDynamicRootConstant",
                "select 1 as Marker from #runtime.events()"),
            RuntimeDynamicInspection(
                "Q232_PublicDynamicRootFilterProjection",
                "select label, metric, payload from #runtime.events() where 2 = runtimekey and runtimekey = 2 and true = enabled"),
            RuntimeDynamicInspection(
                "Q233_PublicDynamicNestedNullable",
                "select branch.measurement, branch.raw from #runtime.events() where branch is not null"),
            RuntimeDynamicInspection(
                "Q234_PublicDynamicJoinMethod",
                "select Scale(e.metric, l.factor) from #runtime.events() e inner join #runtime.lookup() l on e.runtimekey = l.id")
        ];
    }

    private static GeneratedCodeSample RuntimeDynamicInspection(string name, string query)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "RuntimeDynamic",
            Format = GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode,
            CreateSchemaProvider = static () => new RuntimeDynamicSchemaProvider()
        };
    }
}
