
namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{

    private static CompilationOptions CreateParallelCteSidecarOptions()
    {
        return new CompilationOptions(
            parallelizationMode: ParallelizationMode.Full,
            useHashJoin: true,
            useSortMergeJoin: false,
            useCteParallelization: true,
            useCteSidecarIndexes: true);
    }
}
