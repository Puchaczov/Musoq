using System;

namespace Musoq.Evaluator;

internal static class CompilationParallelism
{
    public static int ResolveMaxDegreeOfParallelism(CompilationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(1, options.MaxDegreeOfParallelismOverride ?? Environment.ProcessorCount);
    }
}
