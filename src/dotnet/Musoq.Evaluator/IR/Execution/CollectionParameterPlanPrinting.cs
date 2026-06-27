namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatCollectionInCheck(ExecutionCollectionInCheck collectionInCheck)
    {
        return $"{FormatExpression(collectionInCheck.Expression)} IN {FormatExpression(collectionInCheck.Collection)}";
    }
}
