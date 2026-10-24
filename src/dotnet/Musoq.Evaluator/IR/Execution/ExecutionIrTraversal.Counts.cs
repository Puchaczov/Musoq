namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionIrTraversal
{
    internal static long CountNodes(ExecutionBlock block)
    {
        var visitor = new NodeCountVisitor();
        VisitNodes(block, ref visitor);
        return visitor.Count;
    }

    internal static long CountExpressions(ExecutionBlock block)
    {
        var visitor = new ExpressionCountVisitor();
        VisitExpressions(block, ref visitor);
        return visitor.Count;
    }

    private struct NodeCountVisitor : IExecutionNodeVisitor
    {
        internal long Count;
        public bool Visit(ExecutionNode node) { Count++; return true; }
    }

    private struct ExpressionCountVisitor : IExecutionExpressionVisitor
    {
        internal long Count;
        public bool Visit(ExecutionExpression expression) { Count++; return true; }
    }
}
