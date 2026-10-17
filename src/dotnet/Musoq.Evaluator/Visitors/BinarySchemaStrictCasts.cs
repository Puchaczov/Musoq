using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class BinarySchemaStrictCasts
{
    internal static (string CanonicalName, Type ReturnType) Resolve(CastNode cast)
    {
        if (!StrictCastRuntime.TryResolveTarget(cast.TargetTypeName, out var name, out var type))
            throw new QuerySyntaxException(StrictCastRuntime.CreateUnsupportedTargetMessage(cast.TargetTypeName),
                cast.SpanOrEmpty(), DiagnosticCode.MQ3090_UnsupportedCastTarget);

        if (cast.Expression is ConstantValueNode constant &&
            !StrictCastRuntime.TryValidateConstant(name, constant.ObjValue, out var failure))
            throw new QuerySyntaxException($"Constant cannot be cast to '{name}': {failure}",
                cast.SpanOrEmpty(), DiagnosticCode.MQ3091_InvalidConstantCast);

        return (name, type);
    }
}
