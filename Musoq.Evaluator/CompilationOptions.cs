namespace Musoq.Evaluator;

public class CompilationOptions(ParallelizationMode? parallelizationMode = null)
{
    public ParallelizationMode ParallelizationMode { get; } = parallelizationMode ?? ParallelizationMode.Full;
}