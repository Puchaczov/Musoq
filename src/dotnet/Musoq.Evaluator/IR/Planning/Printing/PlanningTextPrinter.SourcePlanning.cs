using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Planning.Printing;

internal static partial class PlanningTextPrinter
{
    private static void AppendSourceInteractionPlan(
        StringBuilder builder,
        PlanProperties properties,
        string sourceContextId)
    {
        if (!properties.SourceInteractionPlansBySourceId.TryGetValue(sourceContextId, out var plan))
        {
            builder.AppendLine("      interaction: none");
            return;
        }

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      interaction shape: {plan.ShapeKind} ({plan.Confidence}) - {plan.ShapeReason}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      interaction columns: {plan.ColumnContract} [{FormatColumnNames(plan.QuerySourceColumns)}] - {plan.ColumnReason}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      interaction predicate: {plan.PredicateContract} - {plan.PredicateReason}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      interaction source request: orderBy={plan.SourcePlanRequest.OrderBy.Count}, skip={FormatHintValue(plan.SourcePlanRequest.Skip)}, take={FormatHintValue(plan.SourcePlanRequest.Take)} - {plan.SourcePlanRequestReason}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      interaction arguments: {plan.ArgumentMode} - {plan.ArgumentReason}");
    }

    private static void AppendSourcePlanResult(
        StringBuilder builder,
        PlanProperties properties,
        string sourceContextId)
    {
        if (!properties.SourcePlanResultsBySourceId.TryGetValue(sourceContextId, out var plan))
        {
            builder.AppendLine("      source plan: none");
            return;
        }

        if (properties.SourcePlanRequestsBySourceId.TryGetValue(sourceContextId, out var request))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source plan requested: columns={FormatColumnRefs(request.RequiredColumns)}, orderBy={request.OrderBy.Count}, skip={FormatHintValue(request.Skip)}, take={FormatHintValue(request.Take)}, predicate={FormatPredicate(request.Predicate)}");
            AppendSourceCapabilityDiagnostics(builder, request, plan);
        }

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source plan accepted: columns={FormatColumnRefs(plan.AcceptedColumns)}, orderBy={plan.AcceptedOrderBy.Count}, skip={FormatHintValue(plan.AcceptedSkip)}, take={FormatHintValue(plan.AcceptedTake)}, predicate={FormatPredicate(plan.AcceptedPredicate)}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source plan residual: orderBy={plan.ResidualOrderBy.Count}, skip={FormatHintValue(plan.ResidualSkip)}, take={FormatHintValue(plan.ResidualTake)}, predicate={FormatPredicate(plan.ResidualPredicate)}");

        if (plan.Cardinality != null)
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source plan cardinality: {plan.Cardinality.Kind}, exact={FormatHintValue(plan.Cardinality.ExactRows)}, lower={FormatHintValue(plan.Cardinality.LowerBound)}, upper={FormatHintValue(plan.Cardinality.UpperBound)}, confidence={plan.Cardinality.Confidence}, reason={FormatReason(plan.Cardinality.Reason)}");
            AppendCardinalityCapability(builder, plan.Cardinality);
        }

        foreach (var diagnostic in plan.Diagnostics)
        {
            var origin = string.IsNullOrWhiteSpace(diagnostic.Origin)
                ? string.Empty
                : $" [{diagnostic.Origin}]";
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source plan diagnostic{origin}: {diagnostic.Severity} - {diagnostic.Message}");
        }

        foreach (var diagnostic in plan.ContractDiagnostics)
        {
            var origin = string.IsNullOrWhiteSpace(diagnostic.Origin)
                ? string.Empty
                : $" [{diagnostic.Origin}]";
            var code = string.IsNullOrWhiteSpace(diagnostic.Code)
                ? string.Empty
                : $" {diagnostic.Code}";
            var details = FormatContractDiagnosticDetails(diagnostic);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source contract diagnostic{origin}: {diagnostic.Severity}{code} - {diagnostic.Message}{details}");
        }
    }

    private static void AppendSourceCapabilityDiagnostics(
        StringBuilder builder,
        SourcePlanRequest request,
        SourcePlanResult plan)
    {
        var projectionResidual = Math.Max(0, request.RequiredColumns.Count - plan.AcceptedColumns.Count);
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source capability projection: requested={request.RequiredColumns.Count}, accepted={plan.AcceptedColumns.Count}, residual={projectionResidual} -> {ResolveCapabilityStatus(request.RequiredColumns.Count, plan.AcceptedColumns.Count, projectionResidual)}");

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source capability predicate: requested={FormatPredicate(request.Predicate)}, accepted={FormatPredicate(plan.AcceptedPredicate)}, residual={FormatPredicate(plan.ResidualPredicate)} -> {ResolvePredicateStatus(request, plan)}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source capability ordering: requested={request.OrderBy.Count}, accepted={plan.AcceptedOrderBy.Count}, residual={plan.ResidualOrderBy.Count} -> {ResolveCapabilityStatus(request.OrderBy.Count, plan.AcceptedOrderBy.Count, plan.ResidualOrderBy.Count)}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source capability slicing: requested=skip={FormatHintValue(request.Skip)}, take={FormatHintValue(request.Take)}, accepted=skip={FormatHintValue(plan.AcceptedSkip)}, take={FormatHintValue(plan.AcceptedTake)}, residual=skip={FormatHintValue(plan.ResidualSkip)}, take={FormatHintValue(plan.ResidualTake)} -> {ResolveSliceStatus(request, plan)}");
    }

    private static void AppendCardinalityCapability(StringBuilder builder, CardinalityEstimate cardinality)
    {
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      source capability cardinality: {cardinality.Kind} confidence={cardinality.Confidence} usableForHashBuild={FormatYesNo(IsUsableHashBuildCardinality(cardinality))} reason={FormatReason(cardinality.Reason)}");
    }

    private static string ResolveCapabilityStatus(int requested, int accepted, int residual)
    {
        if (requested == 0)
            return "NotRequested";

        if (accepted > 0 && residual == 0)
            return "Accepted";

        return accepted > 0 ? "Partial" : "Rejected";
    }

    private static string ResolvePredicateStatus(SourcePlanRequest request, SourcePlanResult plan)
    {
        if (request.Predicate == null)
            return "NotRequested";

        if (plan.AcceptedPredicate != null && plan.ResidualPredicate == null)
            return "Accepted";

        return plan.AcceptedPredicate != null ? "Partial" : "Rejected";
    }

    private static string ResolveSliceStatus(SourcePlanRequest request, SourcePlanResult plan)
    {
        var requested = request.Skip.HasValue || request.Take.HasValue;
        if (!requested)
            return "NotRequested";

        var accepted = plan.AcceptedSkip.HasValue || plan.AcceptedTake.HasValue;
        var residual = plan.ResidualSkip.HasValue || plan.ResidualTake.HasValue;
        if (accepted && !residual)
            return "Accepted";

        return accepted ? "Partial" : "Rejected";
    }

    private static bool IsUsableHashBuildCardinality(CardinalityEstimate cardinality)
    {
        return cardinality.Kind switch
        {
            CardinalityKind.Exact => cardinality.ExactRows is >= 0,
            CardinalityKind.Bounded => cardinality is { Confidence: >= 0.8d, UpperBound: >= 0 },
            _ => false
        };
    }

    private static string FormatYesNo(bool value)
    {
        return value ? "yes" : "no";
    }

    private static string FormatColumnRefs(System.Collections.Generic.IReadOnlyList<SourceColumnRef> columns)
    {
        if (columns.Count == 0)
            return "[]";

        return $"[{string.Join(", ", columns.Select(static column => column.Name))}]";
    }

    private static string FormatPredicate(SourcePredicateExpression? predicate)
    {
        return predicate == null ? "none" : "yes";
    }

    private static string FormatReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? "none" : reason;
    }

    private static string FormatContractDiagnosticDetails(SourceContractDiagnostic diagnostic)
    {
        var parts = new[]
        {
            string.IsNullOrWhiteSpace(diagnostic.ColumnName) ? null : $"column={diagnostic.ColumnName}",
            string.IsNullOrWhiteSpace(diagnostic.ModifierKey) ? null : $"modifier={diagnostic.ModifierKey}"
        }.Where(static part => part != null).ToArray();

        return parts.Length == 0
            ? string.Empty
            : $" ({string.Join(", ", parts)})";
    }
}
