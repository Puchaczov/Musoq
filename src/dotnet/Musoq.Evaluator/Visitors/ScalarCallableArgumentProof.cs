using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

internal static class ScalarCallableArgumentProof
{
    internal static ArgsListNode? NarrowStrictCasts(ArgsListNode arguments)
    {
        Node[]? adapted = null;
        for (var index = 0; index < arguments.Args.Length; index++)
        {
            if (
                arguments.Args[index] is not CastNode { ReturnType: { } type } cast
                || Nullable.GetUnderlyingType(type) is not { } valueType
                || !IsKnownNonNull(cast.Expression)
            )
                continue;

            adapted ??= (Node[])arguments.Args.Clone();
            adapted[index] = new CastNode(cast.Expression, cast.TargetTypeName, valueType)
                .WithSpan(cast.Span)
                .WithFullSpan(cast.FullSpan);
        }

        return adapted == null
            ? null
            : (ArgsListNode)
                new ArgsListNode(adapted, arguments.ArgumentNames, arguments.Span).WithFullSpan(arguments.FullSpan);
    }

    internal static bool TryResolveGeneric(
        ISchema schema,
        string name,
        Type[] argumentTypes,
        Type entityType,
        [NotNullWhen(true)] out MethodInfo? method
    )
    {
        if (
            schema.TryResolveMethod(name, argumentTypes, entityType, out var candidate)
            && candidate.IsGenericMethod
            && !candidate.IsDefined(typeof(AggregateFunctionAttribute))
        )
        {
            method = candidate;
            return true;
        }

        method = null;
        return false;
    }

    private static bool IsKnownNonNull(Node expression) =>
        expression switch
        {
            ConstantValueNode constant => constant.ObjValue != null,
            CastNode cast => IsKnownNonNull(cast.Expression),
            { ReturnType: { IsValueType: true } type } => Nullable.GetUnderlyingType(type) == null,
            _ => false,
        };
}
