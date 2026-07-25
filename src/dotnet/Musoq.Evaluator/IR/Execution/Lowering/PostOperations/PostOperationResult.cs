namespace Musoq.Evaluator.IR.Execution.Lowering.PostOperations;

internal sealed record PostOperationResult(
    bool IsBuilt,
    ExecutionNode Node,
    ExecutionVariable Target,
    string UnsupportedReason)
{
    public static PostOperationResult Success(ExecutionNode node, ExecutionVariable target)
    {
        return new PostOperationResult(true, node, target, string.Empty);
    }

    public static PostOperationResult Unsupported(string reason)
    {
        var emptyTable = new ExecutionVariable(string.Empty, typeof(object));

        return new PostOperationResult(false, new ExecutionReturnTable(emptyTable), emptyTable, reason);
    }
}
