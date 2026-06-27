using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static BuildResult<ExecutionExpression> ConvertWindowCollectionInCheck(
        CollectionInCheck collectionInCheck,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var expression = ConvertWindowExpression(collectionInCheck.Expression, sourceLookup, windowResults, windowIndex);
        return expression.Supported
            ? BuildResult<ExecutionExpression>.Success(new ExecutionCollectionInCheck(
                expression.Value,
                new ExecutionScriptParameterRead(
                    collectionInCheck.Collection.Name,
                    PrimitiveTypeResolver.CreateReadOnlyCollectionType(collectionInCheck.ElementType)),
                collectionInCheck.ElementType,
                collectionInCheck.ReturnType))
            : expression;
    }
}
