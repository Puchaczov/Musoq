using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(AccessMethodNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitAccessMethod(node,
            (token, modifiedNode, exArgs, arg3, alias, canSkipInjectSource) =>
                new AccessMethodNode(token, modifiedNode, exArgs, canSkipInjectSource, arg3, alias,
                    default, node.IsDistinct)
                {
                    HasFilter = node.HasFilter,
                    FilterExpression = node.FilterExpression,
                    FilterExpressionText = node.FilterExpressionText,
                    IsPivotGenerated = node.IsPivotGenerated,
                    IsScalarSubqueryValueWrapper = node.IsScalarSubqueryValueWrapper
                });
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
        var span = node.SpanOrEmpty();
        var message = $"FILTER clause can only be applied to aggregate functions, but '{node.Name}' is not an aggregate function.";

        var exception = new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "CreateAccessMethod",
            message,
            DiagnosticCode.MQ3051_FilterOnNonAggregate,
            span);

        if (TryReportException(exception, node))
            return;

        throw exception;
    }

    private void ThrowPivotUsingOnNonAggregate(AccessMethodNode node)
    {
        var span = node.SpanOrEmpty();
        var message = $"PIVOT USING accepts aggregate function calls only, but '{node.Name}' is not an aggregate function.";

        var exception = new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "CreateAccessMethod",
            message,
            DiagnosticCode.MQ3051_FilterOnNonAggregate,
            span);

        if (TryReportException(exception, node))
            return;

        throw exception;
    }
}
