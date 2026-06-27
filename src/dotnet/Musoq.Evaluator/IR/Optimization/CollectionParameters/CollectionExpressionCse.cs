using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization;

internal static partial class ExpressionCseSubstitution
{
    private static ExecutionCollectionInCheck ReplaceCollectionInCheck(
        ExecutionCollectionInCheck collectionInCheck,
        IReadOnlyDictionary<string, ExecutionVariable> variablesBySignature)
    {
        return collectionInCheck with
        {
            Expression = ExpressionCseSubstitution.Replace(collectionInCheck.Expression, variablesBySignature)
        };
    }
}
