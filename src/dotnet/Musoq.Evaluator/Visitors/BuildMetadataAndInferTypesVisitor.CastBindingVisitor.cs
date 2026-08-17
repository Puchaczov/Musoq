using Musoq.Evaluator.Helpers;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CastNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var expression = PopSemanticNode(nameof(Visit) + nameof(CastNode));

        if (!StrictCastRuntime.TryResolveTarget(node.TargetTypeName, out var canonicalTypeName, out var returnType))
        {
            if (TryReportSemanticError<NotSupportedException>(
                    DiagnosticCode.MQ3090_UnsupportedCastTarget,
                    StrictCastRuntime.CreateUnsupportedTargetMessage(node.TargetTypeName),
                    node))
            {
                PushSemanticNode(new CastNode(expression, node.TargetTypeName, typeof(object)).WithSpan(node.Span));
                return;
            }
        }

        if (TryGetConstantValue(expression, out var constantValue) &&
            !StrictCastRuntime.TryValidateConstant(canonicalTypeName, constantValue, out var failure))
        {
            var message = $"Constant value '{expression}' cannot be cast to '{canonicalTypeName}': {failure}";
            if (TryReportSemanticError<InvalidCastException>(DiagnosticCode.MQ3091_InvalidConstantCast, message, expression))
            {
                PushSemanticNode(new CastNode(expression, canonicalTypeName, returnType).WithSpan(node.Span));
                return;
            }
        }

        PushSemanticNode(new CastNode(expression, canonicalTypeName, returnType).WithSpan(node.Span));
    }

    private static bool TryGetConstantValue(Node expression, out object? value)
    {
        if (expression is ConstantValueNode constant)
        {
            value = constant.ObjValue;
            return true;
        }

        if (expression is NullNode)
        {
            value = null;
            return true;
        }

        value = null;
        return false;
    }
}
