using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionScriptParameterRead ConvertScriptParameter(ScriptParameterRef parameter)
    {
        return new ExecutionScriptParameterRead(parameter.Name, GetScriptParameterExecutionType(parameter.ReturnType));
    }

    private static ExecutionCollectionInCheck ConvertCollectionInCheck(
        CollectionInCheck collectionInCheck,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        IReadOnlyDictionary<string, int>? cteTableIndexes,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets)
    {
        return new ExecutionCollectionInCheck(
            Convert(collectionInCheck.Expression, sourceShapes, cteTableIndexes, methodTargets),
            ConvertScriptParameter(collectionInCheck.Collection),
            collectionInCheck.ElementType,
            collectionInCheck.ReturnType);
    }

    private static Type GetScriptParameterExecutionType(Type parameterType)
    {
        if (parameterType.IsArray && parameterType.GetArrayRank() == 1)
            return PrimitiveTypeResolver.CreateReadOnlyCollectionType(parameterType.GetElementType()!);

        return parameterType;
    }
}
