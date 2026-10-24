using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class PreviousExecutionIrAnalysis
{
    private static IReadOnlyList<ExecutionBlock> PreviousNodeBlocks(ExecutionNode node) =>
        PreviousNodeCatalog.Definitions.FirstOrDefault(definition => definition.NodeType == node.GetType())?.GetChildBlocks(node) ?? [];
}
