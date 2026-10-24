using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static IEnumerable<ExecutionVariable> GetDirectVariableReferences(ExecutionNode node)
    {
        for (var index = 0; PagingCountExecution.ReadAt(node, index) is { } read; index++)
            if (read is ExecutionVariableRead variableRead)
                yield return variableRead.Variable;
        if (TryGetTablePostOperation(node, out var postOperation))
        {
            yield return postOperation.Source;
            yield return postOperation.Target;
            if (CapacityVariable(postOperation.CapacityHint) is { } capacity)
                yield return capacity;
            yield break;
        }
        var referenceIndex = 0;
        while (DirectVariableAt(node, referenceIndex++) is { } variable)
            yield return variable;
    }

    internal static IEnumerable<ExecutionVariable> GetCapacityHintVariables(ExecutionCapacityHint? capacityHint)
    {
        if (CapacityVariable(capacityHint) is { } variable)
            yield return variable;
    }
}
