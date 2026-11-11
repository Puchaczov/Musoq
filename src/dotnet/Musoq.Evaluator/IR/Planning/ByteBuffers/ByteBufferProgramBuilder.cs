using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal static class ByteBufferProgramBuilder
{
    internal static ByteBufferProgram? Build(MethodCall producer, ByteBufferRequirement requirement,
        ByteBufferResultKind result = ByteBufferResultKind.Snapshot)
    {
        var state = producer.BufferSemantics?.Kind switch
        {
            ByteBufferCallableKind.Materialize => Sequence(producer.Arguments[0]),
            ByteBufferCallableKind.Concatenate => Concatenation(producer),
            ByteBufferCallableKind.Decode => Composition(producer),
            _ => null
        };
        if (state is null)
            return null;
        var allocation = producer.BufferSemantics!.Kind == ByteBufferCallableKind.Concatenate
            ? ByteBufferEmptyAllocation.Fresh : ByteBufferEmptyAllocation.Shared;
        return new(state.Inputs.ToImmutableArray(), state.SegmentCount, state.Steps.ToImmutableArray(),
            requirement, result, allocation, state.Concatenation);
    }

    private static State? Sequence(IrExpression expression)
    {
        if (expression is not MethodCall call)
            return Array(expression);
        var kind = call.BufferSemantics?.Kind;
        if (kind is ByteBufferCallableKind.Skip or ByteBufferCallableKind.Take or ByteBufferCallableKind.SkipAndTake)
            return Range(call);
        if (kind == ByteBufferCallableKind.Materialize)
        {
            var input = Sequence(call.Arguments[0]);
            if (input is not null)
                input.HasBarrier = true;
            return input ?? Array(expression);
        }
        if (kind == ByteBufferCallableKind.Concatenate)
            return Concatenation(call) ?? Array(expression);
        if (kind == ByteBufferCallableKind.Decode)
            return Composition(call) ?? Array(expression);
        return Array(expression);
    }

    private static State? Array(IrExpression expression) => expression.ReturnType == typeof(byte[])
        ? new State(expression) : null;

    private static State? Range(MethodCall call)
    {
        var state = Sequence(call.Arguments[0]);
        if (state is null)
            return null;
        if (state.HasBarrier && call.Arguments.Skip(1).Any(argument => argument is not Literal))
            state = Array(call.Arguments[0]);
        if (state is null)
            return null;
        if (call.BufferSemantics!.Kind is ByteBufferCallableKind.Skip or ByteBufferCallableKind.SkipAndTake)
            state.Step(ByteBufferStepKind.Skip, call.Arguments[1]);
        if (call.BufferSemantics.Kind == ByteBufferCallableKind.Take)
            state.Step(ByteBufferStepKind.Take, call.Arguments[1]);
        if (call.BufferSemantics.Kind == ByteBufferCallableKind.SkipAndTake)
            state.Step(ByteBufferStepKind.Take, call.Arguments[2]);
        return state;
    }

    private static State? Concatenation(MethodCall call)
    {
        if (call.Arguments.Count == 0 || call.Arguments.Any(argument => argument.ReturnType != typeof(byte[])))
            return null;
        return new State(call.Arguments, call);
    }

    private static State? Composition(MethodCall decode)
    {
        if (decode.Arguments is not [MethodCall { BufferSemantics.Kind: ByteBufferCallableKind.Encode } encode] ||
            decode.BufferSemantics!.Encoding != encode.BufferSemantics.Encoding ||
            encode.Arguments.Count == 0)
            return null;
        var semantics = encode.BufferSemantics;
        if (semantics.DelimiterArgument >= 0 && encode.Arguments.Count > semantics.DelimiterArgument &&
            encode.Arguments[semantics.DelimiterArgument] is not Literal { Value: "" })
            return null;
        var state = Sequence(encode.Arguments[0]);
        if (state is null)
            return null;
        if (semantics.EncodingRange == ByteBufferEncodingRange.CheckedSlice)
        {
            if (state.SegmentCount > 0 || state.HasBarrier && encode.Arguments.Skip(1).Any(argument => argument is not Literal))
                state = Array(encode.Arguments[0]);
            if (state is null)
                return null;
            var start = state.Inputs.Count;
            state.Inputs.Add(encode.Arguments[1]);
            state.Inputs.Add(encode.Arguments[2]);
            state.Steps.Add(new(ByteBufferStepKind.CheckedRange, start, start + 1));
        }
        state.Steps.Add(new(ByteBufferStepKind.NullWhenEmpty));
        state.HasBarrier = true;
        return state;
    }

    private sealed class State
    {
        internal State(IrExpression input) => Inputs = [input];
        internal State(IReadOnlyList<IrExpression> segments, MethodCall concatenation)
        {
            Inputs = [..segments];
            SegmentCount = segments.Count;
            Concatenation = concatenation;
            HasBarrier = true;
        }
        internal List<IrExpression> Inputs { get; }
        internal List<ByteBufferStep> Steps { get; } = [];
        internal int SegmentCount { get; }
        internal MethodCall? Concatenation { get; }
        internal bool HasBarrier { get; set; }
        internal void Step(ByteBufferStepKind kind, IrExpression argument)
        {
            Steps.Add(new(kind, Inputs.Count));
            Inputs.Add(argument);
        }
    }
}
