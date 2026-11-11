using System.Collections.Generic;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal enum ExecutionBufferEmptyResult
{
    EmptyArray,
    Null
}

/// <summary>An identity-sensitive owned snapshot selected before executable lowering.</summary>
public sealed record ExecutionByteBufferSnapshot : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _arguments = [];
    internal ExecutionByteBufferSnapshot(ExecutionByteBufferProgram program)
        : base(ExecutionClrBindingFactory.FromClr(typeof(byte[])))
    {
        Program = program;
        Arguments = program.Inputs;
        MaximumReadBytes = program.MaximumReadBytes;
    }

    internal ExecutionByteBufferProgram? Program { get; init; }
    internal ExecutionByteBufferSnapshot(IReadOnlyList<ExecutionExpression> arguments, BinaryBufferInputPlan plan)
        : base(ExecutionClrBindingFactory.FromClr(typeof(byte[])))
    {
        Arguments = arguments;
        MaximumReadBytes = plan.MaximumReadBytes;
        EmptyResult = plan.EmptyResult == BinaryBufferEmptyResult.Null
            ? ExecutionBufferEmptyResult.Null : ExecutionBufferEmptyResult.EmptyArray;
        HasRange = plan.HasRange;
    }

    public IReadOnlyList<ExecutionExpression> Arguments
    {
        get => Program?.Inputs ?? _arguments;
        init => _arguments = ExecutionIrCollections.Freeze(value);
    }
    public int? MaximumReadBytes { get; init; }
    internal ExecutionBufferEmptyResult EmptyResult { get; init; }
    internal bool HasRange { get; init; }
}
