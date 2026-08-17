using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static LoweringAttempt<ExecutionExpression> ConvertWindowCollectionInCheck(
        CollectionInCheck collectionInCheck,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var expression = ConvertWindowExpression(collectionInCheck.Expression, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        return expression.IsBuilt
            ? LoweringAttempt<ExecutionExpression>.Built(new ExecutionCollectionInCheck(
                expression.Value,
                new ExecutionScriptParameterRead(
                    collectionInCheck.Collection.Name,
                    PrimitiveTypeResolver.CreateReadOnlyCollectionType(collectionInCheck.ElementType)),
                collectionInCheck.ElementType,
                collectionInCheck.ReturnType))
            : expression;
    }
}
