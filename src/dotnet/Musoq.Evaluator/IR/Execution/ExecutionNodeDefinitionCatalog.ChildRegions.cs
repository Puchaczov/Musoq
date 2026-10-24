namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionNodeDefinitionCatalog
{
    private static ExecutionChildRegion OptionalRegion(ExecutionBlock body, ExecutionBlock? optional, int index) =>
        index switch
        {
            0 => new(body),
            1 when optional is not null => new(optional),
            _ => default
        };

    private static ExecutionChildRegion RecursiveRegion(ExecutionRecursiveCte node, int index) =>
        index switch
        {
            0 => new(node.Anchor),
            1 => new(node.InvariantSetup),
            2 => new(node.RecursiveMember),
            _ => default
        };

    private static ExecutionChildRegion ParallelRegion(ExecutionParallelBlock node, int index)
    {
        if (index < node.Tasks.Count)
            return new(node.Tasks[index].Body);
        return index == node.Tasks.Count ? new(node.Merge.Body) : default;
    }
}
