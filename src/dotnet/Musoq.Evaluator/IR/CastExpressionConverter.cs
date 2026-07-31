using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private StrictCast ConvertCast(CastNode node)
    {
        if (!StrictCastRuntime.TryResolveTarget(node.TargetTypeName, out var canonicalTypeName, out _))
            throw new NotSupportedException(StrictCastRuntime.CreateUnsupportedTargetMessage(node.TargetTypeName));

        return new StrictCast(
            Convert(node.Expression),
            canonicalTypeName,
            Expressions.ExpressionConverter.RequireReturnType(node));
    }
}
