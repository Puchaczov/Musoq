using System;
using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Schema.CandidatePayload;

public sealed class CandidatePayloadSchema(
    string schemaName,
    IReadOnlyList<CandidatePayloadSeed> rows,
    CandidatePayloadMode mode,
    CandidatePayloadRecorder recorder)
    : SchemaBase(schemaName, CreateLibrary())
{
    public const string Items = "items";

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        return string.Equals(name, Items, StringComparison.OrdinalIgnoreCase)
            ? new CandidatePayloadTable()
            : base.GetTableByName(name, metadataContext, parameters);
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        var descriptor = base.DescribeSource(name, context, parameters);
        return mode switch
        {
            CandidatePayloadMode.CandidateMetadata or
                CandidatePayloadMode.MalformedMissingApplication or
                CandidatePayloadMode.MalformedDuplicateApplication or
                CandidatePayloadMode.MalformedAlteredApplication => descriptor with
                {
                    PredicateCapabilities = CreateCapabilities(
                        nameof(CandidatePayloadEntity.Path),
                        SourcePredicateEvaluationPhases.CandidateMetadata)
                },
            CandidatePayloadMode.RowFiltering or
                CandidatePayloadMode.MalformedUnadvertisedPhase => descriptor with
                {
                    PredicateCapabilities = CreateCapabilities(
                        nameof(CandidatePayloadEntity.Path),
                        SourcePredicateEvaluationPhases.RowFiltering)
                },
            CandidatePayloadMode.UnknownVersion or
                CandidatePayloadMode.MalformedUnknownVersionApplication => descriptor with
                {
                    PredicateCapabilities = CreateCapabilities(
                        nameof(CandidatePayloadEntity.Path),
                        SourcePredicateEvaluationPhases.CandidateMetadata) with
                    {
                        ContractVersion = SourcePredicateCapabilities.CurrentContractVersion + 1
                    }
                },
            CandidatePayloadMode.PayloadOnlyCapability => descriptor with
            {
                PredicateCapabilities = CreateCapabilities(
                    nameof(CandidatePayloadEntity.Payload),
                    SourcePredicateEvaluationPhases.CandidateMetadata)
            },
            _ => descriptor
        };
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        if (!string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
            return SourcePlanResult.RejectAll(request);

        return mode switch
        {
            CandidatePayloadMode.CandidateMetadata => CreateAcceptedResult(
                request,
                SourcePredicateEvaluationPhase.CandidateMetadata),
            CandidatePayloadMode.RowFiltering => CreateAcceptedResult(
                request,
                SourcePredicateEvaluationPhase.RowFiltering),
            CandidatePayloadMode.MalformedMissingApplication => CreateMalformedResult(request, []),
            CandidatePayloadMode.MalformedDuplicateApplication => CreateMalformedResult(
                request,
                [
                    CreateApplication(request, SourcePredicateEvaluationPhase.CandidateMetadata),
                    CreateApplication(request, SourcePredicateEvaluationPhase.CandidateMetadata)
                ]),
            CandidatePayloadMode.MalformedAlteredApplication => CreateMalformedResult(
                request,
                [CreateAlteredApplication(request)]),
            CandidatePayloadMode.MalformedUnadvertisedPhase => CreateMalformedResult(
                request,
                [CreateApplication(request, SourcePredicateEvaluationPhase.CandidateMetadata)]),
            CandidatePayloadMode.MalformedUnknownVersionApplication => CreateUnknownVersionApplication(request),
            _ => SourcePlanResult.RejectAll(request)
        };
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (!string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
            return base.GetRowSource<T>(name, executionContext, parameters);

        recorder.RecordExecutionPlan(executionContext.Plan);
        return EnsureSourceType<T, CandidatePayloadEntity>(
            name,
            new CandidatePayloadRowSource(rows, executionContext, recorder));
    }

    private static SourcePredicateCapabilities CreateCapabilities(
        string column,
        SourcePredicateEvaluationPhases phases)
    {
        return new SourcePredicateCapabilities
        {
            StringMatches =
            [
                new SourceStringMatchCapability(
                    new SourceColumnRef(column),
                    SourceStringMatchOperations.All,
                    SourceStringComparison.LikeIgnoreCase,
                    supportsNegation: true,
                    phases)
            ]
        };
    }

    private static SourcePlanResult CreateAcceptedResult(
        SourcePlanRequest request,
        SourcePredicateEvaluationPhase phase)
    {
        if (request.Predicate is not SourcePredicateStringMatch match)
            return SourcePlanResult.RejectAll(request);

        return CreateResult(
            request,
            match,
            [new SourcePredicateApplication(match, phase)]);
    }

    private static SourcePlanResult CreateMalformedResult(
        SourcePlanRequest request,
        IReadOnlyList<SourcePredicateApplication> applications)
    {
        return CreateResult(request, request.Predicate, applications);
    }

    private static SourcePlanResult CreateUnknownVersionApplication(SourcePlanRequest request)
    {
        var match = new SourcePredicateStringMatch(
            new SourceColumnRef(nameof(CandidatePayloadEntity.Path)),
            SourceStringMatchKind.Prefix,
            "/root/%",
            "/root/");
        return CreateResult(
            request,
            acceptedPredicate: null,
            applications: [new SourcePredicateApplication(match, SourcePredicateEvaluationPhase.CandidateMetadata)]);
    }

    private static SourcePredicateApplication CreateApplication(
        SourcePlanRequest request,
        SourcePredicateEvaluationPhase phase)
    {
        return new SourcePredicateApplication((SourcePredicateStringMatch)request.Predicate!, phase);
    }

    private static SourcePredicateApplication CreateAlteredApplication(SourcePlanRequest request)
    {
        var match = (SourcePredicateStringMatch)request.Predicate!;
        var altered = new SourcePredicateStringMatch(
            match.Column,
            match.Kind,
            match.OriginalPattern,
            string.Concat(match.Needle, "-altered"),
            match.Comparison,
            match.IsNegated);
        return new SourcePredicateApplication(altered, SourcePredicateEvaluationPhase.CandidateMetadata);
    }

    private static SourcePlanResult CreateResult(
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

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}
