using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Build;

internal static class OptimizationFallbackWarningReporter
{
    public static void ReportFallbackWarnings(
        PlanningResult planningResult,
        CompilationOptions compilationOptions,
        DiagnosticContext diagnosticContext)
    {
        ArgumentNullException.ThrowIfNull(planningResult);
        ArgumentNullException.ThrowIfNull(compilationOptions);
        ArgumentNullException.ThrowIfNull(diagnosticContext);

        var reportedKeys = new HashSet<OptimizationFallbackWarningKey>();
        var reportedMessages = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in CollectFallbackWarningCandidates(planningResult, compilationOptions)
                     .OrderBy(static candidate => candidate.Priority)
                     .ThenBy(static candidate => candidate.Optimization, StringComparer.Ordinal)
                     .ThenBy(static candidate => candidate.Target, StringComparer.Ordinal)
                     .ThenBy(static candidate => candidate.Category, StringComparer.Ordinal)
                     .ThenBy(static candidate => candidate.Reason, StringComparer.Ordinal))
        {
            var message = candidate.CreateMessage();
            var key = new OptimizationFallbackWarningKey(candidate.Optimization, candidate.Target, candidate.Category);
            if (!reportedKeys.Add(key) || !reportedMessages.Add(message))
                continue;

            diagnosticContext.ReportWarning(
                DiagnosticCode.MQ5012_OptimizationFallback,
                message,
                TextSpan.Empty);
        }
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectFallbackWarningCandidates(
        PlanningResult planningResult,
        CompilationOptions compilationOptions)
    {
        foreach (var candidate in CollectSourceDiagnosticWarnings(planningResult))
            yield return candidate;

        foreach (var candidate in CollectAutomaticSourcePlanFallbacks(planningResult))
            yield return candidate;

        foreach (var candidate in CollectSourcePredicateRuntimeFallbacks(planningResult.Decisions))
            yield return candidate;

        foreach (var candidate in CollectJoinStrategyFallbacks(planningResult.Decisions, compilationOptions))
            yield return candidate;

        foreach (var candidate in CollectParallelStrategyFallbacks(planningResult.Decisions, compilationOptions))
            yield return candidate;

        foreach (var candidate in CollectSetOperationFallbacks(planningResult.Decisions))
            yield return candidate;

        foreach (var candidate in CollectCteReuseFallbacks(planningResult.Decisions))
            yield return candidate;

        foreach (var candidate in CollectSubqueryLoweringFallbacks(planningResult.Decisions))
            yield return candidate;

        foreach (var candidate in CollectCteSidecarIndexFallbacks(planningResult.Decisions, compilationOptions))
            yield return candidate;
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectSourceDiagnosticWarnings(PlanningResult planningResult)
    {
        foreach (var sourcePlan in planningResult.Properties.SourcePlanResultsBySourceId
                     .OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            foreach (var diagnostic in sourcePlan.Value.Diagnostics)
            {
                if (diagnostic.Severity != OptimizationDiagnosticSeverity.Warning)
                    continue;

                yield return CreateSourceDiagnosticCandidate(sourcePlan.Key, sourcePlan.Value, diagnostic);
            }
        }
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectAutomaticSourcePlanFallbacks(PlanningResult planningResult)
    {
        foreach (var sourcePlan in planningResult.Properties.SourcePlanResultsBySourceId
                     .OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            if (planningResult.Properties.SourcePlanRequestsBySourceId.TryGetValue(sourcePlan.Key, out var request) &&
                TryCreateAutomaticSourceFallbackWarning(request, sourcePlan.Value, out var fallbackWarning))
            {
                yield return CreateSourceDiagnosticCandidate(sourcePlan.Key, sourcePlan.Value, fallbackWarning, priority: 10);
            }
        }
    }

    private static bool TryCreateAutomaticSourceFallbackWarning(
        SourcePlanRequest request,
        SourcePlanResult result,
        out OptimizationDiagnostic warning)
    {
        var predicateFallback = request.Predicate != null && result.ResidualPredicate != null;
        var orderingFallback = request.OrderBy.Count > 0 && result.ResidualOrderBy.Count > 0;
        var skipFallback = request.Skip.HasValue && result.ResidualSkip.HasValue;
        var takeFallback = request.Take.HasValue && result.ResidualTake.HasValue;

        if (!predicateFallback && !orderingFallback && !skipFallback && !takeFallback)
        {
            warning = null!;
            return false;
        }

        var residuals = new[]
            {
                predicateFallback ? "predicate" : null,
                orderingFallback ? "ordering" : null,
                skipFallback ? "skip" : null,
                takeFallback ? "take" : null
            }
            .Where(static item => item != null)
            .ToArray();
        var residualText = string.Join(", ", residuals);

        warning = OptimizationDiagnostic.FallbackWarning(
            "SourcePlan",
            FormatSourceTarget(request.Identity.SourceContextId, request.Identity),
            $"Source planning left residual {residualText} work after the source declined part of the request. Requested {FormatRequest(request)}; accepted {FormatAccepted(result)}; residual {FormatResidual(result)}",
            $"Residual {residualText} work remains in the physical plan.");
        return true;
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectSourcePredicateRuntimeFallbacks(
        IReadOnlyList<PlanningDecision> decisions)
    {
        foreach (var decision in decisions.OrderBy(static decision => decision.Target, StringComparer.Ordinal))
        {
            if (IsUnsupportedSourcePredicatePlanFallback(decision))
            {
                yield return CreateDecisionCandidate(
                    "SourcePredicatePushdown",
                    decision.Target,
                    decision.Reason,
                    "Runtime filter remains in the physical plan.",
                    "PredicatePushdown");
                continue;
            }

            if (IsUnsupportedMovedPredicateFallback(decision))
            {
                yield return CreateDecisionCandidate(
                    "SourcePredicateMovement",
                    decision.Target,
                    decision.Reason,
                    "Moved predicate remains evaluated by runtime filter work.",
                    "PredicateMovement");
            }
        }
    }

    private static bool IsUnsupportedSourcePredicatePlanFallback(PlanningDecision decision)
    {
        return decision is
               {
                   Category: PlanningDecisionCategory.PredicatePushdown,
                   RuleName: "SourcePredicatePlan",
                   Outcome: "RetainedRuntimeOnly"
               } &&
               decision.Reason.StartsWith("Predicate expression could not be converted:", StringComparison.Ordinal);
    }

    private static bool IsUnsupportedMovedPredicateFallback(PlanningDecision decision)
    {
        return decision is
               {
                   Category: PlanningDecisionCategory.PredicatePushdown,
                   RuleName: "SourcePredicateMovementExpansion",
                   Outcome: "Skipped"
               } &&
               decision.Reason.Contains("cannot be represented by the source predicate DTO", StringComparison.Ordinal);
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectJoinStrategyFallbacks(
        IReadOnlyList<PlanningDecision> decisions,
        CompilationOptions compilationOptions)
    {
        var riskDecisionsByTarget = CreateNestedLoopRiskLookup(decisions);
        foreach (var decision in decisions.OrderBy(static decision => decision.Target, StringComparer.Ordinal))
        {
            if (!IsJoinStrategyNestedLoopFallback(decision, compilationOptions))
                continue;

            var fallback = CreateJoinFallbackText(decision, riskDecisionsByTarget);
            yield return CreateDecisionCandidate(
                "JoinStrategySelection",
                decision.Target,
                decision.Reason,
                fallback,
                "JoinStrategy",
                priority: 30);
        }
    }

    private static bool IsJoinStrategyNestedLoopFallback(
        PlanningDecision decision,
        CompilationOptions compilationOptions)
    {
        if (decision is not
            {
                Category: PlanningDecisionCategory.JoinStrategy,
                RuleName: "JoinStrategySelection",
                Outcome: "NestedLoop"
            })
        {
            return false;
        }

        if (!compilationOptions.UseHashJoin && !compilationOptions.UseSortMergeJoin)
            return false;

        return !IsSemanticNestedLoopJoin(decision);
    }

    private static bool IsSemanticNestedLoopJoin(PlanningDecision decision)
    {
        return decision.Target is "Cross" or "AsofInner" or "AsofLeft" ||
               decision.Reason.StartsWith("CROSS JOIN semantics require", StringComparison.Ordinal) ||
               decision.Reason.StartsWith("ASOF join semantics require", StringComparison.Ordinal);
    }

    private static IReadOnlyDictionary<string, PlanningDecision> CreateNestedLoopRiskLookup(
        IReadOnlyList<PlanningDecision> decisions)
    {
        return decisions
            .Where(static decision => decision is
            {
                Category: PlanningDecisionCategory.JoinStrategy,
                RuleName: "NestedLoopCardinalityRisk"
            })
            .Where(static decision => decision.Outcome is "HighRisk" or "UnknownRisk")
            .GroupBy(static decision => decision.Target, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderBy(static decision => decision.Outcome == "HighRisk" ? 0 : 1)
                    .ThenBy(static decision => decision.Reason, StringComparer.Ordinal)
                    .First(),
                StringComparer.Ordinal);
    }

    private static string CreateJoinFallbackText(
        PlanningDecision decision,
        IReadOnlyDictionary<string, PlanningDecision> riskDecisionsByTarget)
    {
        var fallback = $"{decision.Target} join remains a nested-loop join in the physical plan.";
        return riskDecisionsByTarget.TryGetValue(decision.Target, out var risk)
            ? $"{fallback} {risk.Reason}"
            : fallback;
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectParallelStrategyFallbacks(
        IReadOnlyList<PlanningDecision> decisions,
        CompilationOptions compilationOptions)
    {
        if (compilationOptions.ParallelizationMode != ParallelizationMode.Full)
            yield break;

        foreach (var decision in decisions.OrderBy(static decision => decision.RuleName, StringComparer.Ordinal)
                     .ThenBy(static decision => decision.Target, StringComparer.Ordinal))
        {
            if (!IsParallelStrategyFallback(decision))
                continue;

            yield return CreateDecisionCandidate(
                decision.RuleName,
                decision.Target,
                decision.Reason,
                CreateParallelFallbackText(decision.RuleName),
                "ParallelEligibility",
                priority: 40);
        }
    }

    private static bool IsParallelStrategyFallback(PlanningDecision decision)
    {
        return decision is
               {
                   Category: PlanningDecisionCategory.ParallelEligibility,
                   Outcome: "Skipped"
               } &&
               decision.RuleName is "ParallelSingleKeyAggregate" or "ParallelFilterProject" or "ParallelCte" &&
               !IsSilentParallelSkip(decision);
    }

    private static bool IsSilentParallelSkip(PlanningDecision decision)
    {
        if (decision.Reason.StartsWith("Compilation option", StringComparison.Ordinal))
            return true;

        return decision.RuleName switch
        {
            "ParallelFilterProject" => IsSilentParallelFilterProjectSkip(decision.Reason),
            "ParallelSingleKeyAggregate" => IsSilentParallelSingleKeyAggregateSkip(decision.Reason),
            _ => false
        };
    }

    private static bool IsSilentParallelFilterProjectSkip(string reason)
    {
        return reason.StartsWith("Post-operations are present", StringComparison.Ordinal) ||
               reason.StartsWith("Distinct projection requires", StringComparison.Ordinal) ||
               reason.StartsWith("Unsupported row source", StringComparison.Ordinal) ||
               reason.StartsWith("Source shape is dynamic", StringComparison.Ordinal) ||
               reason.Contains("through dynamic or reflected access", StringComparison.Ordinal) ||
               reason.Contains("no method-heavy expression worth parallelizing", StringComparison.Ordinal);
    }

    private static bool IsSilentParallelSingleKeyAggregateSkip(string reason)
    {
        return reason.StartsWith("Unsupported row source", StringComparison.Ordinal) ||
               reason.StartsWith("Source shape is dynamic", StringComparison.Ordinal) ||
               reason.StartsWith("No aggregate set operations are present", StringComparison.Ordinal);
    }

    private static string CreateParallelFallbackText(string ruleName)
    {
        return ruleName switch
        {
            "ParallelSingleKeyAggregate" => "Serial single-key aggregate execution remains for the candidate.",
            "ParallelFilterProject" => "Serial filter/project execution remains for the candidate.",
            "ParallelCte" => "Serial CTE execution remains for the candidate.",
            _ => "Serial execution remains for the candidate."
        };
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectSetOperationFallbacks(
        IReadOnlyList<PlanningDecision> decisions)
    {
        foreach (var decision in decisions.OrderBy(static decision => decision.Target, StringComparer.Ordinal))
        {
            if (decision is not
                {
                    Category: PlanningDecisionCategory.SetOperationStrategy,
                    RuleName: "SetOperationStrategy",
                    Outcome: "RowComparer"
                })
            {
                continue;
            }

            yield return CreateDecisionCandidate(
                "SetOperationStrategy",
                decision.Target,
                decision.Reason,
                CreateSetOperationFallbackText(decision.Target),
                "SetOperationStrategy",
                priority: 50);
        }
    }

    private static string CreateSetOperationFallbackText(string target)
    {
        return string.Equals(target, "UnionAll", StringComparison.Ordinal)
            ? "Materialized row-comparer set operation remains because streaming UnionAll lowering was not usable."
            : "Materialized row-comparer set operation remains because HashSet lowering was not usable.";
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectCteReuseFallbacks(
        IReadOnlyList<PlanningDecision> decisions)
    {
        foreach (var decision in decisions.OrderBy(static decision => decision.Target, StringComparer.Ordinal))
        {
            if (!IsSingleUseCteMaterializationFallback(decision))
                continue;

            yield return CreateDecisionCandidate(
                "CteReuseStrategy",
                decision.Target,
                decision.Reason,
                "Materialized CTE table remains for the single-use CTE.",
                "CteReuseStrategy",
                priority: 60);
        }
    }

    private static bool IsSingleUseCteMaterializationFallback(PlanningDecision decision)
    {
        return decision is
               {
                   Category: PlanningDecisionCategory.CteStrategy,
                   RuleName: "CteReuseStrategy",
                   Outcome: "MaterializeSingleUse"
               } &&
               !IsGeneratedSubqueryCteTarget(decision.Target) &&
               decision.Reason.StartsWith("Single-use CTE is not the terminal read-once projection candidate", StringComparison.Ordinal);
    }

    private static bool IsGeneratedSubqueryCteTarget(string target)
    {
        return target.StartsWith("cte:_sq_", StringComparison.OrdinalIgnoreCase) ||
               target.StartsWith("cte:_dt_", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectSubqueryLoweringFallbacks(
        IReadOnlyList<PlanningDecision> decisions)
    {
        foreach (var decision in decisions.OrderBy(static decision => decision.Target, StringComparer.Ordinal))
        {
            if (decision is not
                {
                    Category: PlanningDecisionCategory.SubqueryStrategy,
                    RuleName: "SubqueryLoweringStrategy",
                    Outcome: "PredicateOuterJoinFallback"
                })
            {
                continue;
            }

            yield return CreateDecisionCandidate(
                "SubqueryLoweringStrategy",
                decision.Target,
                decision.Reason,
                "Predicate subquery remains lowered through an outer-join fallback.",
                "SubqueryLoweringStrategy",
                priority: 70);
        }
    }

    private static IEnumerable<OptimizationFallbackWarningCandidate> CollectCteSidecarIndexFallbacks(
        IReadOnlyList<PlanningDecision> decisions,
        CompilationOptions compilationOptions)
    {
        if (!compilationOptions.UseCteSidecarIndexes || !compilationOptions.UseHashJoin)
            yield break;

        var groupedSkips = decisions
            .Where(IsCteSidecarIndexFallback)
            .GroupBy(static decision => ResolveCteSidecarTarget(decision.Target), StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal);

        foreach (var group in groupedSkips)
        {
            var reasons = group
                .Select(static decision => decision.Reason)
                .Where(static reason => !string.IsNullOrWhiteSpace(reason))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static reason => reason, StringComparer.Ordinal)
                .ToArray();
            var reason = reasons.Length == 0
                ? "CTE sidecar index planning skipped an eligible hash-build consumer."
                : string.Join("; ", reasons);

            yield return CreateDecisionCandidate(
                "CteSidecarIndexStrategy",
                group.Key,
                reason,
                "Post-materialization hash or keyset build remains for the CTE consumer.",
                "CteSidecarIndexStrategy",
                priority: 80);
        }
    }

    private static bool IsCteSidecarIndexFallback(PlanningDecision decision)
    {
        return decision is
               {
                   Category: PlanningDecisionCategory.CteSidecarIndexStrategy,
                   RuleName: "CteSidecarIndexStrategy",
                   Outcome: "Skipped"
               } &&
               decision.Target.Contains(":hashJoin:", StringComparison.Ordinal) &&
               IsRelevantCteSidecarIndexSkipReason(decision.Reason);
    }

    private static bool IsRelevantCteSidecarIndexSkipReason(string reason)
    {
        return reason.StartsWith("The hash-build key", StringComparison.Ordinal) ||
               reason.StartsWith("The hash-build side is ambiguous", StringComparison.Ordinal);
    }

    private static string ResolveCteSidecarTarget(string target)
    {
        var hashJoinMarker = target.IndexOf(":hashJoin:", StringComparison.Ordinal);
        return hashJoinMarker > 0 ? target[..hashJoinMarker] : target;
    }

    private static OptimizationFallbackWarningCandidate CreateSourceDiagnosticCandidate(
        string sourceContextId,
        SourcePlanResult sourcePlan,
        OptimizationDiagnostic diagnostic,
        int priority = 0)
    {
        var optimization = ResolvePart(diagnostic.Optimization, "SourcePlan");
        var target = ResolvePart(diagnostic.Target, FormatSourceTarget(sourceContextId, sourcePlan.ExecutionPlan.Identity));
        var reason = ResolvePart(diagnostic.Reason, diagnostic.Message);
        var fallback = ResolvePart(diagnostic.Fallback, "See source planning diagnostics.");

        return new OptimizationFallbackWarningCandidate(
            optimization,
            target,
            reason,
            fallback,
            optimization,
            priority);
    }

    private static OptimizationFallbackWarningCandidate CreateDecisionCandidate(
        string optimization,
        string target,
        string reason,
        string fallback,
        string category,
        int priority = 20)
    {
        return new OptimizationFallbackWarningCandidate(
            optimization,
            target,
            reason,
            fallback,
            category,
            priority);
    }

    private static string FormatRequest(SourcePlanRequest request)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"predicate={FormatPredicate(request.Predicate)}, orderBy={request.OrderBy.Count}, skip={FormatValue(request.Skip)}, take={FormatValue(request.Take)}");
    }

    private static string FormatAccepted(SourcePlanResult result)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"predicate={FormatPredicate(result.AcceptedPredicate)}, orderBy={result.AcceptedOrderBy.Count}, skip={FormatValue(result.AcceptedSkip)}, take={FormatValue(result.AcceptedTake)}");
    }

    private static string FormatResidual(SourcePlanResult result)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"predicate={FormatPredicate(result.ResidualPredicate)}, orderBy={result.ResidualOrderBy.Count}, skip={FormatValue(result.ResidualSkip)}, take={FormatValue(result.ResidualTake)}");
    }

    private static string FormatSourceTarget(string sourceContextId, SourceIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(identity.SchemaName) || string.IsNullOrWhiteSpace(identity.MethodName))
            return sourceContextId;

        var alias = string.IsNullOrWhiteSpace(identity.Alias)
            ? string.Empty
            : $" as {identity.Alias}";
        var id = string.IsNullOrWhiteSpace(identity.SourceContextId)
            ? sourceContextId
            : identity.SourceContextId;

        return $"#{identity.SchemaName}.{identity.MethodName}(){alias} [{id}]";
    }

    private static string ResolvePart(string? preferred, string fallback)
    {
        return string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
    }

    private static string FormatValue(long? value)
    {
        return value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : "null";
    }

    private static string FormatPredicate(SourcePredicateExpression? predicate)
    {
        return predicate == null ? "none" : "yes";
    }

    private sealed record OptimizationFallbackWarningCandidate(
        string Optimization,
        string Target,
        string Reason,
        string Fallback,
        string Category,
        int Priority)
    {
        public string CreateMessage()
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"Optimization fallback in {Optimization} for {Target}: {Reason}. Fallback: {Fallback}. Inspect planning/logical/physical plan text for details.");
        }
    }

    private readonly record struct OptimizationFallbackWarningKey(
        string Optimization,
        string Target,
        string Category);
}
