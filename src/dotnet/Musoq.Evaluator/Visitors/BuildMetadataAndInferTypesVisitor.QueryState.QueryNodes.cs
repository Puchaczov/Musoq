using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(QueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var orderBy = node.OrderBy != null ? PopSemanticNode() as OrderByNode : null;
        var qualify = node.Qualify != null ? PopSemanticNode() as QualifyNode : null;
        var window = node.Window != null ? PopSemanticNode() as WindowNode : null;
        var take = node.Take != null ? PopSemanticNode() as TakeNode : null;
        var skip = node.Skip != null ? PopSemanticNode() as SkipNode : null;
        var select = PopSemanticNode() as SelectNode
                     ?? throw new VisitorException(
                         VisitorName,
                         "VisitQueryNode",
                         "Expected SELECT node on visitor stack.");
        var groupBy = node.GroupBy != null ? PopSemanticNode() as GroupByNode : null;
        var where = node.Where != null ? PopSemanticNode() as WhereNode : null;
        var from = PopSemanticNode() as FromNode;

        if (from is null)
        {
            var span = node.SpanOrEmpty();
            throw new FromNodeIsNull(span);
        }

        groupBy = ExpandGroupByAllIfNeeded(select, NormalizeGroupByOrdinals(select, groupBy, node.GroupBy));
        if (groupBy == null && _methodResolution.RefreshMethods.Count > 0)
            groupBy = new GroupByNode([new FieldNode(new IntegerNode("1", "s"), 0, string.Empty)], null);

        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(from.Alias.ToRefreshMethodsSymbolName(),
            new RefreshMethodsSymbol(_methodResolution.RefreshMethods));
        _methodResolution.RefreshMethods.Clear();

        if (_sourceBinding.CurrentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(string.Empty))
            _sourceBinding.CurrentScope.ScopeSymbolTable.MoveSymbol(string.Empty, from.Alias);

        TraversalFrame.PushMethod(from.Alias);

        var queryNode = new QueryNode(select, from, where, groupBy, orderBy, skip, take, window, qualify, default);

        ValidateSelectFieldsArePrimitive(queryNode.Select.Fields, "SELECT");

        if (where != null)
        {
            ValidateExpressionIsPrimitive(where.Expression, "WHERE");
            ValidateExpressionIsBoolean(where.Expression, "WHERE");
        }

        if (groupBy != null)
        {
            foreach (var field in groupBy.Fields)
                ValidateExpressionIsPrimitive(field.Expression, "GROUP BY");

            if (groupBy.Having != null)
            {
                ValidateExpressionIsPrimitive(groupBy.Having.Expression, "HAVING");
                ValidateExpressionIsBoolean(groupBy.Having.Expression, "HAVING");
            }
        }

        if (orderBy != null)
            foreach (var field in orderBy.Fields)
            {
                ValidateOrderByExpression(field);
                ValidateExpressionIsPrimitive(field.Expression, "ORDER BY");
            }

        if (skip != null)
            ValidateExpressionIsPrimitive(skip.Expression, "SKIP");

        if (take != null)
            ValidateExpressionIsPrimitive(take.Expression, "TAKE");

        if (qualify != null)
        {
            ValidateExpressionIsPrimitive(qualify.Expression, "QUALIFY");
            ValidateExpressionIsBoolean(qualify.Expression, "QUALIFY");
            ValidateQualifyReferencesWindowFunction(qualify);
        }
        if (groupBy != null)
            ValidateGroupBySemantics(select, groupBy);

        long? skipValue = skip?.Expression is IntegerNode skipInt ? Convert.ToInt64(skipInt.ObjValue, System.Globalization.CultureInfo.InvariantCulture) : null;
        long? takeValue = take?.Expression is IntegerNode takeInt ? Convert.ToInt64(takeInt.ObjValue, System.Globalization.CultureInfo.InvariantCulture) : null;
        //    Note: DISTINCT creates an implicit GROUP BY
        var isSingleTableQuery = _sourceBinding.AliasToSchemaFromNodeMap.Count == 1;
        var hasOrderBy = orderBy != null;
        var hasGroupBy = groupBy != null;
        var canPassHints = isSingleTableQuery && !hasOrderBy && !hasGroupBy;

        foreach (var schemaFromNode in _sourceBinding.AliasToSchemaFromNodeMap.Values)
        {
            var identity = SourceIdentityFactory.Create(schemaFromNode);
            var sourceRuntimeSettings = GetResolvedSourceRuntimeSettings(schemaFromNode.Id);
            _sourceBinding.SourcePlanRequestsPerSchema[schemaFromNode] = canPassHints
                ? new SourcePlanRequest
                {
                    Identity = identity,
                    SourceRuntimeSettings = sourceRuntimeSettings,
                    Skip = skipValue,
                    Take = takeValue
                }
                : SourcePlanRequest.Empty(identity) with
                {
                    SourceRuntimeSettings = sourceRuntimeSettings
                };
        }

        SemanticNodeResult.From(queryNode).ApplyTo(TraversalFrame);

        _sourceBinding.AliasToSchemaFromNodeMap.Clear();
        _sourceBinding.SchemaFromInfo.Clear();
        _sourceBinding.AliasMapToInMemoryTableMap.Clear();
        _sourceBinding.UsedSchemasQuantity = 0;
    }

    public override void Visit(InternalQueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var span = node.SpanOrEmpty();
        throw new NotSupportedException(
            $"Internal Query Node is not supported in this context at position {span.Start}");
    }

    public override void Visit(RootNode node)
    {
        SemanticNodeResult
            .From(new RootNode(TraversalFrame.PopNode(VisitorName, "Visit(RootNode)")))
            .ApplyTo(TraversalFrame);
    }

    public override void Visit(SingleSetNode node)
    {
    }

    public override void Visit(RefreshNode node)
    {
    }

    public override void Visit(QueryScope node)
    {
    }
}
