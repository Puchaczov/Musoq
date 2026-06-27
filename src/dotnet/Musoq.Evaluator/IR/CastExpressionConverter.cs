using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private StrictCast ConvertCast(CastNode node)
    {
        if (!StrictCastRuntime.TryGetReturnType(node.TargetTypeName, out _))
            throw new NotSupportedException(StrictCastRuntime.CreateUnsupportedTargetMessage(node.TargetTypeName));

        return new StrictCast(
            Convert(node.Expression),
            node.TargetTypeName,
            Expressions.ExpressionConverter.RequireReturnType(node));
    }
}
