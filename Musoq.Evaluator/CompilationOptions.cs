namespace Musoq.Evaluator;

public class CompilationOptions(ParallelizationMode? parallelizationMode = null, bool useHashJoin = true)
{
    public ParallelizationMode ParallelizationMode { get; } = parallelizationMode ?? ParallelizationMode.Full;
    
    public bool UseHashJoin { get; } = useHashJoin;
}