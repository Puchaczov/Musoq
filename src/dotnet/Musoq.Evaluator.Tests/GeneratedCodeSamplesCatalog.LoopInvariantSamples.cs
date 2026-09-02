using System.Collections.Generic;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static IReadOnlyList<GeneratedCodeSample> CreateLoopInvariantSamples()
    {
        return
        [
            LoopInvariantStableApplyProjection(),
            LoopInvariantVolatileApplyProjection(),
            LoopInvariantStableAndVolatileFunctions(),
            LoopInvariantEmptyApply()
        ];
    }

    private static GeneratedCodeSample LoopInvariantStableApplyProjection()
    {
        return new GeneratedCodeSample
        {
            Name = "Q248_LoopInvariantStableApplyProjection",
            FileName = "Q248_LoopInvariantStableApplyProjection.cs",
            Query = "select a.Value, b.Value, c.Value from #licm.outers() a cross apply a.Middles b cross apply b.Leaves c",
            Category = "LoopInvariant",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateLoopInvariantSampleSchemaProvider
        };
    }

    private static GeneratedCodeSample LoopInvariantVolatileApplyProjection()
    {
        return new GeneratedCodeSample
        {
            Name = "Q249_LoopInvariantVolatileApplyProjection",
            FileName = "Q249_LoopInvariantVolatileApplyProjection.cs",
            Query = "select a.VolatileValue, b.Value, c.Value from #licm.outers() a cross apply a.Middles b cross apply b.Leaves c",
            Category = "LoopInvariant",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateLoopInvariantSampleSchemaProvider
        };
    }

    private static GeneratedCodeSample LoopInvariantStableAndVolatileFunctions()
    {
        return new GeneratedCodeSample
        {
            Name = "Q250_LoopInvariantStableAndVolatileFunctions",
            FileName = "Q250_LoopInvariantStableAndVolatileFunctions.cs",
            Query = "select a.StableOf(a.Value), a.StablePair(a.Value, b.Value), a.VolatileOf(b.Value) from #licm.outers() a cross apply a.Middles b",
            Category = "LoopInvariant",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateLoopInvariantSampleSchemaProvider,
            CompilationOptions = new CompilationOptions(useCommonSubexpressionElimination: false)
        };
    }

    private static GeneratedCodeSample LoopInvariantEmptyApply()
    {
        return new GeneratedCodeSample
        {
            Name = "Q251_LoopInvariantEmptyApply",
            FileName = "Q251_LoopInvariantEmptyApply.cs",
            Query = "select a.Value, b.VolatileValue from #licm.outers() a cross apply a.EmptyMiddles b",
            Category = "LoopInvariant",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateLoopInvariantSampleSchemaProvider
        };
    }
}
