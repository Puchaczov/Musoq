using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(AccessMethodNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (TryBindEnumIntrinsic(node) || TryRejectUnsupportedEnumMethod(node))
            return;

        VisitAccessMethod(node,
            (token, modifiedNode, exArgs, arg3, alias, canSkipInjectSource) =>
                (AccessMethodNode)(new AccessMethodNode(
                        token,
                        modifiedNode,
                        exArgs,
                        canSkipInjectSource,
                        arg3,
                        alias,
                        node.Span,
                        node.IsDistinct)
                    {
                        HasFilter = node.HasFilter,
                        FilterExpression = node.FilterExpression,
                        FilterExpressionText = node.FilterExpressionText,
                        IsPivotGenerated = node.IsPivotGenerated,
                        IsScalarSubqueryValueWrapper = node.IsScalarSubqueryValueWrapper
                    }).WithFullSpan(node.FullSpan));
    }

    public override void Visit(InterpretCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = PopSemanticNode(VisitorOperationNames.VisitInterpretCallNode);


        PushSemanticNode(new InterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(ParseCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = PopSemanticNode(VisitorOperationNames.VisitParseCallNode);


        PushSemanticNode(new ParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(TryInterpretCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = PopSemanticNode(VisitorOperationNames.VisitTryInterpretCallNode);


        PushSemanticNode(new TryInterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(TryParseCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = PopSemanticNode(VisitorOperationNames.VisitTryParseCallNode);


        PushSemanticNode(new TryParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(PartialInterpretCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = PopSemanticNode(VisitorOperationNames.VisitPartialInterpretCallNode);


        PushSemanticNode(new PartialInterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(InterpretAtCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var offset = PopSemanticNode(VisitorOperationNames.VisitInterpretAtCallNodeOffset);
        var dataSource = PopSemanticNode(VisitorOperationNames.VisitInterpretAtCallNodeDataSource);


        PushSemanticNode(new InterpretAtCallNode(dataSource, offset, node.SchemaName, node.ReturnType));
    }

    public override void Visit(AccessRefreshAggregationScoreNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitAccessMethod(node,
            (token, node1, exArgs, arg3, alias, _) =>
                new AccessRefreshAggregationScoreNode(token, node1, exArgs ?? ArgsListNode.Empty, node.CanSkipInjectSource,
                    arg3, alias)
                {
                    HasFilter = node.HasFilter,
                    FilterExpression = node.FilterExpression,
                    FilterExpressionText = node.FilterExpressionText,
                    IsPivotGenerated = node.IsPivotGenerated,
                    IsScalarSubqueryValueWrapper = node.IsScalarSubqueryValueWrapper
                });
    }

    private void ThrowFilterOnNonAggregate(AccessMethodNode node)
    {
        var exception = new FilterOnNonAggregateException(node.Name, node.SpanOrEmpty());

        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(exception.Code, exception.Message, node);
            return;
        }

        throw exception;
    }

    private void ThrowPivotUsingOnNonAggregate(AccessMethodNode node)
    {
        var exception = new PivotUsingNonAggregateException(node.Name, node.SpanOrEmpty());

        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(exception.Code, exception.Message, node);
            return;
        }

        throw exception;
    }
}
