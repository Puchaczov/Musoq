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

        if (!StrictCastRuntime.TryGetReturnType(node.TargetTypeName, out var returnType))
        {
            if (TryReportSemanticError<NotSupportedException>(
                    DiagnosticCode.MQ2030_UnsupportedSyntax,
                    StrictCastRuntime.CreateUnsupportedTargetMessage(node.TargetTypeName),
                    node))
            {
                PushSemanticNode(new CastNode(expression, node.TargetTypeName, typeof(object)).WithSpan(node.Span));
                return;
            }
        }

        PushSemanticNode(new CastNode(expression, node.TargetTypeName, returnType).WithSpan(node.Span));
    }
}
