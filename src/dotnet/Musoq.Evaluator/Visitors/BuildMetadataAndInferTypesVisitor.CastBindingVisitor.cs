using Musoq.Evaluator.Helpers;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CastNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var expression = SafePop(Nodes, nameof(Visit) + nameof(CastNode));

        if (!StrictCastRuntime.TryGetReturnType(node.TargetTypeName, out var returnType))
        {
            if (TryReportSemanticError<NotSupportedException>(
                    DiagnosticCode.MQ2030_UnsupportedSyntax,
                    StrictCastRuntime.CreateUnsupportedTargetMessage(node.TargetTypeName),
                    node))
            {
                Nodes.Push(new CastNode(expression, node.TargetTypeName, typeof(object)).WithSpan(node.Span));
                return;
            }
        }

        Nodes.Push(new CastNode(expression, node.TargetTypeName, returnType).WithSpan(node.Span));
    }
}
