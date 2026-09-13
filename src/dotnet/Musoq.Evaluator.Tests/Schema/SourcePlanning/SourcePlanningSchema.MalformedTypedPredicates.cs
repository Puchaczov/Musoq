using System;
using System.Collections.Generic;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public sealed partial class SourcePlanningSchema
{
    private enum MalformedTypedResultKind
    {
        MissingApplication,
        DuplicateApplication,
        AlteredApplication,
        UnadvertisedPhase
    }

    private static SourcePlanResult CreateMalformedTypedResult(
        SourcePlanRequest request,
        MalformedTypedResultKind kind)
    {
        var match = (SourcePredicateStringMatch)request.Predicate!;
        IReadOnlyList<SourcePredicateApplication> applications = kind switch
        {
            MalformedTypedResultKind.MissingApplication => [],
            MalformedTypedResultKind.DuplicateApplication =>
            [
                new SourcePredicateApplication(match, SourcePredicateEvaluationPhase.RowFiltering),
                new SourcePredicateApplication(match, SourcePredicateEvaluationPhase.RowFiltering)
            ],
            MalformedTypedResultKind.AlteredApplication =>
            [
                new SourcePredicateApplication(
                    new SourcePredicateStringMatch(
                        match.Column,
                        match.Kind,
                        match.OriginalPattern,
                        string.Concat(match.Needle, "-altered"),
                        match.Comparison,
                        match.IsNegated),
                    SourcePredicateEvaluationPhase.RowFiltering)
            ],
            MalformedTypedResultKind.UnadvertisedPhase =>
            [new SourcePredicateApplication(match, SourcePredicateEvaluationPhase.CandidateMetadata)],
            _ => throw new InvalidOperationException($"Unknown malformed typed result kind '{kind}'.")
        };

        return CreateTypedResult(request, match, applications);
    }

    private static SourcePlanResult CreateMalformedNestedTypedResult(SourcePlanRequest request)
    {
        var first = CreateMatch("Name", SourceStringMatchKind.Prefix, "item-0%", "item-0");
        var nested = new SourcePredicateLogical(
            SourcePredicateLogicalOperator.Or,
            first,
            CreateMatch("Category", SourceStringMatchKind.Exact, "alpha", "alpha"));
        return CreateTypedResult(
            request,
            nested,
            [new SourcePredicateApplication(first, SourcePredicateEvaluationPhase.RowFiltering)]);
    }

    private static SourcePlanResult CreateMalformedUnknownVersionResult(SourcePlanRequest request)
    {
        var match = CreateMatch("Name", SourceStringMatchKind.Prefix, "item-0%", "item-0");
        return CreateTypedResult(
            request,
            acceptedPredicate: null,
            applications: [new SourcePredicateApplication(match, SourcePredicateEvaluationPhase.RowFiltering)]);
    }

    private static SourcePlanResult CreateTypedResult(
        SourcePlanRequest request,
        SourcePredicateExpression? acceptedPredicate,
        IReadOnlyList<SourcePredicateApplication> applications)
    {
        return new SourcePlanResult
        {
            ExecutionPlan = new SourceExecutionPlan
            {
                Identity = request.Identity,
                AcceptedColumns = request.RequiredColumns,
                AcceptedPredicate = acceptedPredicate,
                PredicateApplications = applications
            },
            AcceptedColumns = request.RequiredColumns,
            AcceptedPredicate = acceptedPredicate
        };
    }

    private static SourcePredicateStringMatch CreateMatch(
        string column,
        SourceStringMatchKind kind,
        string pattern,
        string needle) => new(new SourceColumnRef(column), kind, pattern, needle);
}
