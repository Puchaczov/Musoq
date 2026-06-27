using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool HasExplicitNullOrdering(IReadOnlyList<ExecutionOrderField> keys)
    {
        return keys.Any(static key => key.NullOrdering != NullOrdering.Default);
    }

    private InvocationExpressionSyntax CreateExplicitNullOrderedRowsExpression(
        ExecutionVariable source,
        IReadOnlyList<ExecutionOrderField> keys)
    {
        return CreateEvaluationHelperInvocation(
            nameof(EvaluationHelper.OrderRows),
            CreateRowsRead(source),
            CreateArrayCreation(nameof(RowOrderKey), keys.Select(CreateRowOrderKeyCreation)));
    }
}
