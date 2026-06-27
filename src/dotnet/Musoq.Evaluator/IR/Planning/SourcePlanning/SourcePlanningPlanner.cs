using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning.OptimizationDiagnostics;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static partial class SourcePlanningPlanner
{
    public static SourcePlanningResult Plan(
        PlanningContext context,
        IReadOnlyList<SchemaScanNode> scans,
        IReadOnlyDictionary<string, RequiredColumnUsage[]> requiredColumnUsagesBySourceId,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlansBySourceId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(scans);
        ArgumentNullException.ThrowIfNull(requiredColumnUsagesBySourceId);
        ArgumentNullException.ThrowIfNull(sourcePredicatePlansBySourceId);
        var sourceLocalRequests = BuildSourceLocalRequests(
            context,
            requiredColumnUsagesBySourceId,
            sourcePredicatePlansBySourceId);
        var requests = new Dictionary<string, SourcePlanRequest>(StringComparer.Ordinal);
        var results = new Dictionary<string, SourcePlanResult>(StringComparer.Ordinal);
        var decisions = new List<PlanningDecision>();

        foreach (var scan in scans)
        {
            if (string.IsNullOrWhiteSpace(scan.SourceContextId))
                continue;

            var sourceNode = ResolveSourceNode(context, scan);
            var identity = sourceNode != null
                ? SourceIdentityFactory.Create(sourceNode)
                : new SourceIdentity(scan.SchemaName, scan.MethodName, scan.SourceContextId, scan.Alias);
            var request = sourceLocalRequests.TryGetValue(scan.SourceContextId, out var sourceLocalRequest)
                ? sourceLocalRequest
                : CreateEmptyRequest(context, identity, scan, requiredColumnUsagesBySourceId, sourcePredicatePlansBySourceId);
            var result = PlanSource(context, scan, sourceNode, request);

            requests[scan.SourceContextId] = request;
            results[scan.SourceContextId] = result;
            decisions.Add(CreateDecision(scan, request, result));
        }

        return new SourcePlanningResult(requests, results, decisions);
    }

    private static SourcePlanResult PlanSource(
        PlanningContext context,
        SchemaScanNode scan,
        SchemaFromNode? sourceNode,
        SourcePlanRequest request)
    {
        var schema = context.SchemaProvider.GetSchema(scan.SchemaName);
        var parameters = sourceNode != null
            ? SchemaArgumentBinder.BindStaticArguments(sourceNode.Parameters)
            : [];
        var metadataContext = new SourceMetadataContext(
            request.Identity.SourceContextId,
            CancellationToken.None,
            ResolveColumns(context, scan),
            request.SourceRuntimeSettings,
            NullLogger.Instance);

        var descriptor = schema.DescribeSource(
            scan.MethodName,
            new SourceDescribeContext(request.Identity, metadataContext),
            parameters);

        var result = schema.TryPlanSource(scan.MethodName, request, parameters) ?? SourcePlanResult.RejectAll(request);
        result = OptimizationDiagnosticOriginMarker.Mark(result, "TryPlanSource");
        result = SourceContractDiagnosticOriginMarker.Mark(result, "TryPlanSource");
        result = OptimizationDiagnosticOriginMarker.Prepend(result, descriptor.Diagnostics, "DescribeSource");
        return SourceContractDiagnosticOriginMarker.Prepend(result, descriptor.ContractDiagnostics, "DescribeSource");
    }

    private static Dictionary<string, SourcePlanRequest> BuildSourceLocalRequests(
        PlanningContext context,
        IReadOnlyDictionary<string, RequiredColumnUsage[]> requiredColumnUsagesBySourceId,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlansBySourceId)
    {
        var result = new Dictionary<string, SourcePlanRequest>(StringComparer.Ordinal);

        AddSourceLocalRequests(
            context.LogicalPlan,
            context,
            requiredColumnUsagesBySourceId,
            sourcePredicatePlansBySourceId,
            result);

        return result;
    }

    private static void AddSourceLocalRequests(
        LogicalNode node,
        PlanningContext context,
        IReadOnlyDictionary<string, RequiredColumnUsage[]> requiredColumnUsagesBySourceId,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlansBySourceId,
        IDictionary<string, SourcePlanRequest> requests)
    {
        if (TryBuildSourceLocalRequest(
                node,
                context,
                requiredColumnUsagesBySourceId,
                sourcePredicatePlansBySourceId,
                out var sourceContextId,
                out var request))
        {
            requests[sourceContextId] = request;
            return;
        }

        foreach (var child in node.Children)
            AddSourceLocalRequests(
                child,
                context,
                requiredColumnUsagesBySourceId,
                sourcePredicatePlansBySourceId,
                requests);
    }

    private static bool TryBuildSourceLocalRequest(
        LogicalNode node,
        PlanningContext context,
        IReadOnlyDictionary<string, RequiredColumnUsage[]> requiredColumnUsagesBySourceId,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlansBySourceId,
        out string sourceContextId,
        [NotNullWhen(true)] out SourcePlanRequest? request)
    {
        var orderFields = Array.Empty<OrderField>();
        long? skip = null;
        long? take = null;
        var filterPredicates = new List<IrExpression>();
        var current = node;

        while (true)
        {
            switch (current)
            {
                case MultiStatementNode { Statements.Length: 1 } multiStatement:
                    current = multiStatement.Statements[0];
                    continue;
                case TakeNode takeNode when take == null:
                    take = takeNode.Count;
                    current = takeNode.Input;
                    continue;
                case SkipNode skipNode when skip == null:
                    skip = skipNode.Count;
                    current = skipNode.Input;
                    continue;
                case SortNode sortNode when orderFields.Length == 0:
                    orderFields = sortNode.Keys;
                    current = sortNode.Input;
                    continue;
                case FilterNode filter:
                    filterPredicates.Add(filter.Predicate);
                    current = filter.Input;
                    continue;
                case ProjectNode { IsDistinct: false } project:
                    current = project.Input;
                    continue;
                case SchemaScanNode scan when !string.IsNullOrWhiteSpace(scan.SourceContextId):
                    if (!CanTraverseSourceLocalFilters(filterPredicates, scan) ||
                        !TryConvertOrderBy(orderFields, scan, out var orderBy))
                    {
                        sourceContextId = string.Empty;
                        request = null;
                        return false;
                    }

                    sourceContextId = scan.SourceContextId;
                    request = new SourcePlanRequest
                    {
                        Identity = ResolveIdentity(context, scan),
                        SourceRuntimeSettings = ResolveSourceRuntimeSettings(context, scan),
                        RequiredColumns = ResolveRequiredColumns(context, scan, requiredColumnUsagesBySourceId),
                        Predicate = ResolvePredicate(scan, sourcePredicatePlansBySourceId),
                        OrderBy = orderBy,
                        Skip = skip,
                        Take = take
                    };
                    return true;
                default:
                    sourceContextId = string.Empty;
                    request = null;
                    return false;
            }
        }
    }

    private static SourcePredicateExpression? ResolvePredicate(
        SchemaScanNode scan,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlansBySourceId)
    {
        if (string.IsNullOrWhiteSpace(scan.SourceContextId) ||
            !sourcePredicatePlansBySourceId.TryGetValue(scan.SourceContextId, out var plan) ||
            plan.PushedPredicates.Length == 0)
        {
            return null;
        }

        var predicates = new List<SourcePredicateExpression>();

        foreach (var predicate in plan.PushedPredicates)
            if (SourcePredicateExpressionConverter.TryConvertPredicate(predicate, scan.Alias, out var sourcePredicate))
                predicates.Add(sourcePredicate);

        return predicates.Count switch
        {
            0 => null,
            1 => predicates[0],
            _ => predicates.Aggregate(static (left, right) =>
                new SourcePredicateLogical(SourcePredicateLogicalOperator.And, left, right))
        };
    }

    private static bool TryConvertOrderBy(
        OrderField[] fields,
        SchemaScanNode scan,
        out OrderByExpression[] orderBy)
    {
        if (fields.Length == 0)
        {
            orderBy = [];
            return true;
        }

        var sourceColumns = scan.OutputSchema.Columns
            .Select(static column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<OrderByExpression>(fields.Length);

        foreach (var field in fields)
        {
            if (field.NullOrdering != NullOrdering.Default || field.Expression is not ColumnRef columnRef ||
                !string.Equals(columnRef.Alias, scan.Alias, StringComparison.OrdinalIgnoreCase) ||
                !sourceColumns.Contains(columnRef.ColumnName))
            {
                orderBy = [];
                return false;
            }

            result.Add(new OrderByExpression(
                new SourceColumnRef(columnRef.ColumnName),
                field.Descending ? OrderDirection.Descending : OrderDirection.Ascending));
        }

        orderBy = result.ToArray();
        return true;
    }

    private static SourceIdentity ResolveIdentity(PlanningContext context, SchemaScanNode scan)
    {
        var sourceNode = ResolveSourceNode(context, scan);
        return sourceNode != null
            ? SourceIdentityFactory.Create(sourceNode)
            : new SourceIdentity(scan.SchemaName, scan.MethodName, scan.SourceContextId ?? string.Empty, scan.Alias);
    }

    private static SchemaFromNode? ResolveSourceNode(PlanningContext context, SchemaScanNode scan)
    {
        foreach (var sourceNode in context.SourcePlanRequestsBySource.Keys)
        {
            if (string.Equals(sourceNode.Id, scan.SourceContextId, StringComparison.Ordinal))
                return sourceNode;
        }

        foreach (var sourceNode in context.UsedSchemaColumns.Keys)
        {
            if (string.Equals(sourceNode.Id, scan.SourceContextId, StringComparison.Ordinal))
                return sourceNode;
        }

        return null;
    }

    private static ISchemaColumn[] ResolveColumns(PlanningContext context, SchemaScanNode scan)
    {
        if (context.InferredColumns.TryGetValue(scan.Alias, out var inferredColumns) && inferredColumns.Length > 0)
            return inferredColumns;

        return scan.OutputSchema.Columns
            .Select(static column => (ISchemaColumn)new SchemaColumn(column.Name, column.Index, column.Type))
            .ToArray();
    }

    private static PlanningDecision CreateDecision(
        SchemaScanNode scan,
        SourcePlanRequest request,
        SourcePlanResult result)
    {
        var requested = $"columns={request.RequiredColumns.Count}, predicate={FormatPredicate(request.Predicate)}, orderBy={request.OrderBy.Count}, skip={FormatValue(request.Skip)}, take={FormatValue(request.Take)}";
        var accepted = $"columns={result.AcceptedColumns.Count}, predicate={FormatPredicate(result.AcceptedPredicate)}, orderBy={result.AcceptedOrderBy.Count}, skip={FormatValue(result.AcceptedSkip)}, take={FormatValue(result.AcceptedTake)}";
        var residual = $"predicate={FormatPredicate(result.ResidualPredicate)}, orderBy={result.ResidualOrderBy.Count}, skip={FormatValue(result.ResidualSkip)}, take={FormatValue(result.ResidualTake)}";
        var acceptedAny = result.AcceptedColumns.Count > 0 || result.AcceptedPredicate != null || result.AcceptedOrderBy.Count > 0 || result.AcceptedSkip.HasValue || result.AcceptedTake.HasValue;

        return new PlanningDecision(
            PlanningDecisionCategory.SourcePlanning,
            "SourcePlan",
            scan.SourceContextId ?? scan.Alias,
            acceptedAny ? "PartiallyAccepted" : "Rejected",
            PlanningConfidence.Medium,
            $"Requested {requested}; accepted {accepted}; residual {residual}.");
    }

    private static string FormatValue(long? value) => value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";

    private static string FormatPredicate(SourcePredicateExpression? predicate) => predicate == null ? "none" : "yes";
}
