using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal static class ByteBufferPlanRewrite
{
    internal static MethodCall? Projection(
        MethodCall call,
        IReadOnlyList<IrExpression> arguments,
        Func<IrExpression, IrExpression?> visit
    )
    {
        if (call.BufferPlan?.Program is not { } program)
            return ReferenceEquals(arguments, call.Arguments) ? call : call with { Arguments = arguments };
        var inputs = ImmutableArray.CreateBuilder<IrExpression>(program.Inputs.Length);
        foreach (var input in program.Inputs)
        {
            if (visit(input) is not { } rewritten)
                return null;
            inputs.Add(rewritten);
        }
        if (ReferenceEquals(arguments, call.Arguments) && inputs.SequenceEqual(program.Inputs))
            return call;
        return call with
        {
            Arguments = arguments,
            BufferPlan = call.BufferPlan with { Program = program with { Inputs = inputs.ToImmutable() } },
        };
    }

    internal static MethodCall Method(
        MethodCall call,
        IReadOnlyList<IrExpression> arguments,
        Func<IrExpression, IrExpression> visit
    ) =>
        Projection(call, arguments, visit)
        ?? throw new InvalidOperationException("A total expression rewrite returned no value.");
}
