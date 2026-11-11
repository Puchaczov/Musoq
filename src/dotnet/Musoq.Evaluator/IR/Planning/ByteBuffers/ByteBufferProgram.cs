using System.Collections.Immutable;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal enum ByteBufferStepKind { Skip, Take, CheckedRange, NullWhenEmpty }
internal enum ByteBufferResultKind { Snapshot, Length, View }
internal enum ByteBufferEmptyAllocation { Shared, Fresh }

internal readonly record struct ByteBufferStep(ByteBufferStepKind Kind, int Argument = -1, int LengthArgument = -1);

/// <summary>A normalized program with operands captured in their original evaluation order.</summary>
internal sealed record ByteBufferProgram(
    ImmutableArray<IrExpression> Inputs,
    int SegmentCount,
    ImmutableArray<ByteBufferStep> Steps,
    ByteBufferRequirement Requirement,
    ByteBufferResultKind Result,
    ByteBufferEmptyAllocation EmptyAllocation,
    MethodCall? Concatenation)
{
    internal bool InputIsView { get; init; }
}
