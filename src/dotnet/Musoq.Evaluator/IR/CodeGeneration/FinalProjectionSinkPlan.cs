using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal sealed record FinalProjectionSinkPlan
{
    private FinalProjectionSinkPlan(
        bool isAccepted,
        IReadOnlyList<ExecutionSourceScan> sourceScans,
        IReadOnlyList<ExecutionNode> setupNodes,
        TypedProjectionLoop? projectionLoop,
        ExecutionVariable? appendTarget,
        QueryMethodRenderMetadata resultMetadata,
        IReadOnlyList<TypedPostOperation> postOperations,
        FinalProjectionSinkRejectionKind rejectionKind,
        string? rejectionReason)
    {
        IsAccepted = isAccepted;
        SourceScans = sourceScans.ToArray();
        SetupNodes = setupNodes.ToArray();
        ProjectionLoop = projectionLoop;
        AppendTarget = appendTarget;
        ResultMetadata = resultMetadata;
        PostOperations = postOperations.ToArray();
        RejectionKind = rejectionKind;
        RejectionReason = rejectionReason;
    }

    public bool IsAccepted { get; }

    public IReadOnlyList<ExecutionSourceScan> SourceScans { get; }

    public IReadOnlyList<ExecutionNode> SetupNodes { get; }

    public TypedProjectionLoop? ProjectionLoop { get; }

    public ExecutionVariable? AppendTarget { get; }

    public QueryMethodRenderMetadata ResultMetadata { get; }

    public IReadOnlyList<TypedPostOperation> PostOperations { get; }

    public FinalProjectionSinkRejectionKind RejectionKind { get; }

    public string? RejectionReason { get; }

    public static FinalProjectionSinkPlan Accepted(
        IReadOnlyList<ExecutionSourceScan> sourceScans,
        TypedProjectionLoop projectionLoop,
        ExecutionVariable appendTarget,
        QueryMethodRenderMetadata resultMetadata,
        IReadOnlyList<TypedPostOperation> postOperations,
        IReadOnlyList<ExecutionNode>? setupNodes = null)
    {
        return new FinalProjectionSinkPlan(
            true,
            sourceScans,
            setupNodes ?? [],
            projectionLoop,
            appendTarget,
            resultMetadata,
            postOperations,
            FinalProjectionSinkRejectionKind.None,
            null);
    }

    public static FinalProjectionSinkPlan Rejected(string reason)
    {
        return Rejected(FinalProjectionSinkRejectionKind.Unknown, reason);
    }

    public static FinalProjectionSinkPlan Rejected(FinalProjectionSinkRejectionKind kind, string reason)
    {
        return new FinalProjectionSinkPlan(false, [], [], null, null, QueryMethodRenderMetadata.Unknown, [], kind, reason);
    }
}
