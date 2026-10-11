using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

internal static class WindowContextOperationBinding
{
    internal static ExecutionWindowContextBuffers? CreateBuffers(WindowRegistration registration,
        ExecutionWindowFrame? frame, ExecutionCallableRef? operation, string prefix)
    {
        if (operation is null)
            return null;
        var parameters = operation.ResolveClrMethod().GetParameters();
        var contextType = parameters[0].ParameterType.GetElementType()!;
        var types = contextType.GetGenericArguments();
        var values = new ExecutionVariable($"{prefix}Values", types[0].MakeArrayType());
        var arguments = parameters.Skip(1).Select((parameter, index) =>
            new ExecutionVariable($"{prefix}Argument{index}", parameter.ParameterType.MakeArrayType())).ToArray();
        var rangeKeys = frame is { Kind: ExecutionWindowFrameKind.Range } &&
                        (frame.Start.Kind is ExecutionWindowFrameBoundKind.OffsetPreceding or ExecutionWindowFrameBoundKind.OffsetFollowing ||
                         frame.End.Kind is ExecutionWindowFrameBoundKind.OffsetPreceding or ExecutionWindowFrameBoundKind.OffsetFollowing)
            ? new ExecutionVariable($"{prefix}RangeKeys", registration.OrderKeys[0].Expression.ReturnType.MakeArrayType())
            : null;
        var orderValues = types.Length == 2 ? new ExecutionVariable($"{prefix}OrderValues", types[1].MakeArrayType()) : null;
        return new ExecutionWindowContextBuffers(values, arguments, rangeKeys, orderValues,
            ExecutionClrBindingFactory.FromClr(contextType));
    }

    internal static Type? ResolveRawOrderKeyType(IReadOnlyList<Type> keyTypes)
    {
        if (keyTypes.Count == 0)
            return null;
        if (keyTypes.Count == 1)
            return keyTypes[0];
        return ValueTupleTypeShape.TryCreate(keyTypes, out var tupleType) ? tupleType : null;
    }

}
