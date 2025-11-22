namespace Musoq.Evaluator;

/// <summary>
/// Compilation options for query execution.
/// </summary>
/// <param name="parallelizationMode">The parallelization mode to use. Defaults to Full if not specified.</param>
/// <param name="useHashJoin">Whether hash join optimization should be used for eligible joins. Defaults to true.</param>
public class CompilationOptions(ParallelizationMode? parallelizationMode = null, bool useHashJoin = true)
{
    /// <summary>
    /// Gets the parallelization mode for query execution.
    /// </summary>
    public ParallelizationMode ParallelizationMode { get; } = parallelizationMode ?? ParallelizationMode.Full;
    
    /// <summary>
    /// Gets a value indicating whether hash join optimization should be used for eligible joins.
    /// </summary>
    public bool UseHashJoin { get; } = useHashJoin;
}