using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionExpression ConvertMethodCall(
        MethodCall method,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        IReadOnlyDictionary<string, int>? cteTableIndexes,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets)
    {
        if (method.BufferPlan is { Program: not null } program)
            return ByteBufferExecutionLowering.Lower(program,
                argument => Convert(argument, sourceShapes, cteTableIndexes, methodTargets));
        var arguments = method.Arguments.Select((argument, index) => WrapBufferArgument(
            Convert(argument, sourceShapes, cteTableIndexes, methodTargets), method.BufferPlan, index)).ToArray();
        if (method.BufferSemantics?.Kind == ByteBufferCallableKind.Length && ByteBufferViewShape.IsView(arguments[0]))
            return new ExecutionByteBufferOperation(arguments[0], ExecutionByteBufferViewAccess.LogicalLength);
        if (method.BufferSemantics?.Kind == ByteBufferCallableKind.FixedWidthRead)
            arguments[0] = ByteBufferViewShape.Storage(arguments[0]);
        var converted = CreateMethodCall(
            method.Method,
            arguments,
            method.Alias,
            method.ReturnType,
            CreateInjectedSourceExpression(method.Method, method.Alias, sourceShapes),
            sourceShapes);

        if (converted is ExecutionMethodCall call)
            converted = call with
            {
                BufferSemantics = method.BufferSemantics,
                IsOpaqueBufferProducer = method.ReturnType == typeof(byte[]) && method.BufferSemantics is null,
                EnumIntrinsic = method.EnumIntrinsic,
                OperandEnumType = method.OperandEnumType,
                EnumMask = method.EnumMask
            };
        return method.BufferPlan is { } bufferPlan
            ? new ExecutionByteBufferOperation(converted, bufferPlan)
            : converted;
    }

    private static ExecutionExpression WrapBufferArgument(ExecutionExpression converted, ByteBufferValuePlan? plan, int index)
    {
        return plan is not null && index < plan.ArgumentPlans.Length && plan.ArgumentPlans[index] is { } input
            ? new ExecutionByteBufferOperation(converted, input)
            : converted;
    }

    private static ExecutionExpression CreateMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType,
        ExecutionExpression? injectedSource,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        var generatedBindings = GeneratedScalarGenericBinding.Resolve(method, arguments);
        var call = new ExecutionMethodCall(
            method,
            arguments,
            alias,
            returnType,
            injectedSource)
        {
            GeneratedGenericArguments = generatedBindings,
            GeneratedResultTypeName = GeneratedScalarGenericBinding.ResolveResult(method, generatedBindings)
        };

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
