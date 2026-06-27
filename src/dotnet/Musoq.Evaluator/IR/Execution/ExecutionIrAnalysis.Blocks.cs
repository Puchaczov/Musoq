using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionIrAnalysis
{
    internal static IEnumerable<ExecutionNode> FlattenNodes(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            yield return node;
            foreach (var childBlock in GetChildBlocks(node))
            {
                foreach (var childNode in FlattenNodes(childBlock))
                    yield return childNode;
            }
        }
    }

    internal static IEnumerable<ExecutionBlock> GetChildBlocks(ExecutionNode node)
    {
        return ExecutionNodeFacts.GetChildBlocks(node);
    }

    internal static IEnumerable<string> CollectDeclaredVariableNames(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            foreach (var variable in ExecutionNodeFacts.GetDeclaredVariables(node))
                yield return variable.Name;

            foreach (var childBlock in GetChildBlocks(node))
            {
                foreach (var variableName in CollectDeclaredVariableNames(childBlock))
                    yield return variableName;
            }
        }
    }
}
