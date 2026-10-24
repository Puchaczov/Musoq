using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class GeneratedRowContextPruner
{
    private enum ContextReadPolicy { DirectOnly, IncludeVariables }

    private readonly record struct ContextConsumerState(
        IReadOnlyDictionary<string, HashSet<string>> VariableTypes,
        IReadOnlySet<string> ContextTypes,
        HashSet<string> Result);

    private readonly struct NonContextWriteVisitor(ContextConsumerState state) : IExecutionNodeVisitor
    {
        public bool Visit(ExecutionNode node)
        {
            var visitor = new ContextExpressionVisitor(state, ContextReadPolicy.DirectOnly);
            if (node is ExecutionAppendRow append)
                return VisitValues(append.Values, ref visitor);
            if (node is ExecutionCreateGeneratedRow create)
                return VisitValues(create.Values, ref visitor);
            if (node is ExecutionParallelFilterProjectLoop parallel)
            {
                ExecutionIrTraversal.VisitExpressions(parallel.SourceRows, ref visitor);
                ExecutionIrTraversal.VisitExpressions(parallel.Predicate, ref visitor);
                return VisitValues(parallel.AppendRow.Values, ref visitor);
            }
            var expressions = new ExecutionLocalExpressionCursor(node);
            while (expressions.MoveNext())
                ExecutionIrTraversal.VisitExpressions(expressions.Current, ref visitor);
            return true;
        }

        private static bool VisitValues(IReadOnlyList<ExecutionRowValue> values, ref ContextExpressionVisitor visitor)
        {
            for (var index = 0; index < values.Count; index++)
                ExecutionIrTraversal.VisitExpressions(values[index].Value, ref visitor);
            return true;
        }
    }

    private readonly struct ContextExpressionVisitor(ContextConsumerState state, ContextReadPolicy policy) : IExecutionExpressionVisitor
    {
        public bool Visit(ExecutionExpression expression)
        {
            if (policy == ContextReadPolicy.IncludeVariables && expression is ExecutionVariableRead variableRead)
                TryAddVariableGeneratedRowTypes(variableRead.Variable, state.VariableTypes, state.ContextTypes, state.Result);
            AddDirectContextConsumer(expression, state.VariableTypes, state.ContextTypes, state.Result);
            return true;
        }
    }
}
