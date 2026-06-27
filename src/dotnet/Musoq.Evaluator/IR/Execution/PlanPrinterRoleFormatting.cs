using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatTablePostOperationFlow(ExecutionNode node)
    {
        return ExecutionNodeFacts.TryGetTablePostOperation(node, out var operation)
            ? $"{operation.Source.Name} -> {operation.Target.Name}"
            : "? -> ?";
    }

    private static string FormatWindowComputationTarget(ExecutionNode node)
    {
        return ExecutionNodeFacts.TryGetWindowComputation(node, out var window)
            ? $"{window.Results.Name} <- {window.Buffer.Name}"
            : "? <- ?";
    }
}
