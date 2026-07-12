using System.Collections.Generic;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool IsVariableUsedAfter(
        IReadOnlyList<ExecutionNode> nodes,
        int index,
        string variableName)
    {
        return ExecutionIrAnalysis.IsVariableUsedAfter(nodes, index, variableName);
    }
}
