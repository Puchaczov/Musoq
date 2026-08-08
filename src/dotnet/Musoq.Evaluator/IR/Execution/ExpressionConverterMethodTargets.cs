using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionExpression ConvertMethodCall(
        MethodCall method,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        IReadOnlyDictionary<string, int>? cteTableIndexes,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets)
    {
        return CreateMethodCall(
            method.Method,
            method.Arguments.Select(argument => Convert(argument, sourceShapes, cteTableIndexes, methodTargets)).ToArray(),
            method.Alias,
            method.ReturnType,
            CreateInjectedSourceExpression(method.Method, method.Alias, sourceShapes),
            sourceShapes);
    }

    private static ExecutionExpression CreateMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType,
        ExecutionExpression? injectedSource,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        var call = new ExecutionMethodCall(
            method,
            arguments,
            alias,
            returnType,
            injectedSource);

        return injectedSource != null &&
               IsNullableInjectedSource(injectedSource, sourceShapes)
            ? new ExecutionCaseWhen(
                [
                    new ExecutionCaseWhenBranch(
                        new ExecutionIsNullCheck(injectedSource, false, typeof(bool)),
                        new ExecutionLiteral(null, LiftNullableType(returnType)))
                ],
                call,
                LiftNullableType(returnType))
            : call;
    }

    private static bool IsNullableInjectedSource(
        ExecutionExpression injectedSource,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        if (injectedSource is not ExecutionFieldRead fieldRead ||
            string.IsNullOrWhiteSpace(fieldRead.Alias) ||
            !sourceShapes.TryGetValue(fieldRead.Alias, out var shape) ||
            shape is not TableRowShape tableRow)
        {
            return false;
        }

        var context = tableRow.Contexts.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, fieldRead.FieldName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, fieldRead.FieldName, StringComparison.OrdinalIgnoreCase));

        return context?.Nullability == FieldNullability.Nullable;
    }

    private static Type LiftNullableType(Type type)
    {
        return type.IsValueType && Nullable.GetUnderlyingType(type) == null
            ? typeof(Nullable<>).MakeGenericType(type)
            : type;
    }
}
