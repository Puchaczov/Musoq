using Musoq.Evaluator.Helpers;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CastNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var expression = PopSemanticNode(nameof(Visit) + nameof(CastNode));

        if (TryGetEnumExpressionType(expression, out var sourceEnum) ||
            TryResolveEnumCastTarget(node.TargetTypeName, out sourceEnum))
        {
            ReportEnumSemanticError(
                DiagnosticCode.MQ3110_UnsupportedEnumOperator,
                $"CAST is not supported for enum type '{sourceEnum.DisplayName}' in v1. Use EnumValue(...) to project its backing number.",
                node);
            PushSemanticNode(new CastNode(expression, node.TargetTypeName, typeof(object)).WithSpan(node.Span));
            return;
        }

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
            var message = $"Constant value '{System.Convert.ToString(constantValue, System.Globalization.CultureInfo.InvariantCulture) ?? "null"}' cannot be cast to '{canonicalTypeName}': {failure}";
            if (TryReportSemanticError<InvalidCastException>(DiagnosticCode.MQ3091_InvalidConstantCast, message, expression))
            {
                PushSemanticNode(new CastNode(expression, canonicalTypeName, returnType).WithSpan(node.Span));
                return;
            }
        }

        PushSemanticNode(new CastNode(expression, canonicalTypeName, returnType).WithSpan(node.Span));
    }

    private bool TryResolveEnumCastTarget(string targetTypeName, out EnumTypeDescriptor descriptor)
    {
        if (_enumBinding.QueryLocalTypes.TryGetValue(targetTypeName, out descriptor!))
            return true;

        foreach (var nativeDescriptor in _enumBinding.NativeTypes.Values)
        {
            if (!string.Equals(nativeDescriptor.DisplayName, targetTypeName, StringComparison.Ordinal))
                continue;

            descriptor = nativeDescriptor;
            return true;
        }

        descriptor = null!;
        return false;
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
