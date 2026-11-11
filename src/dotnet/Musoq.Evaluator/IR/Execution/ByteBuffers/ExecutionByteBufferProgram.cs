using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal enum ExecutionByteBufferStepKind
{
    Skip,
    Take,
    CheckedRange,
    NullWhenEmpty,
}

internal enum ExecutionByteBufferResultKind
{
    Snapshot,
    Length,
    View,
}

internal enum ExecutionByteBufferEmptyAllocation
{
    Shared,
    Fresh,
}

internal readonly record struct ExecutionByteBufferStep(
    ExecutionByteBufferStepKind Kind,
    int Argument,
    int LengthArgument
);

internal sealed record ExecutionByteBufferProgram
{
    private IReadOnlyList<ExecutionExpression> _inputs = [];
    private IReadOnlyList<ExecutionByteBufferStep> _steps = [];
    internal required string HelperName { get; init; }
    internal IReadOnlyList<ExecutionExpression> Inputs
    {
        get => _inputs;
        init => _inputs = ExecutionIrCollections.Freeze(value);
    }
    internal IReadOnlyList<ExecutionByteBufferStep> Steps
    {
        get => _steps;
        init => _steps = ExecutionIrCollections.Freeze(value);
    }
    internal int SegmentCount { get; init; }
    internal bool InputIsView { get; init; }
    internal int? MaximumReadBytes { get; init; }
    internal ExecutionByteBufferResultKind Result { get; init; }
    internal ExecutionByteBufferEmptyAllocation EmptyAllocation { get; init; }
    internal ExecutionMethodCall? Concatenation { get; init; }
    internal bool RequiresSnapshotService =>
        Steps.Any(step => step.Kind == ExecutionByteBufferStepKind.CheckedRange)
        || SegmentCount == 0 && Result != ExecutionByteBufferResultKind.Length;

    internal IEnumerable<ExecutionExpression> Children => Concatenation is null ? Inputs : Inputs.Append(Concatenation);

    internal ExecutionExpression? ChildAt(int index) =>
        index < Inputs.Count ? Inputs[index]
        : index == Inputs.Count ? Concatenation
        : null;

    internal ExecutionByteBufferProgram Rewrite(Func<ExecutionExpression, ExecutionExpression> rewrite) =>
        this with
        {
            Inputs = Inputs.Select(rewrite).ToArray(),
            Concatenation = Concatenation is null ? null : (ExecutionMethodCall)rewrite(Concatenation),
        };
}
