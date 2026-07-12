using System.Collections.Generic;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPlan
{
    public ExecutionPlan(
        string identifier,
        IReadOnlyList<RowShape> shapes,
        ExecutionBlock body,
        FinalShapeResult? finalResult = null,
        ExecutionSemanticsContract? semanticsContract = null,
        int executionIrVersion = TargetContractVersions.ExecutionIr)
    {
        if (executionIrVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(executionIrVersion));

        Identifier = identifier;
        Shapes = shapes;
        Body = body;
        FinalResult = finalResult;
        SemanticsContract = semanticsContract ?? ExecutionSemanticsContract.Version1;
        ExecutionIrVersion = executionIrVersion;
    }

    public string Identifier { get; init; }

    public IReadOnlyList<RowShape> Shapes { get; init; }

    public ExecutionBlock Body { get; init; }

    public FinalShapeResult? FinalResult { get; init; }

    public ExecutionSemanticsContract SemanticsContract { get; init; }

    public int ExecutionIrVersion { get; init; }
}
