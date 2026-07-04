using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static IEnumerable<ExecutionBlock> GetChildBlocks(ExecutionNode node)
    {
        return ExecutionNodeRegistry.GetChildBlocks(node);
    }
}
