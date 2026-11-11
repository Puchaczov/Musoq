using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal static class ByteBufferExecutionLowering
{
    internal static ExecutionExpression Lower(ByteBufferValuePlan plan, Func<IrExpression, ExecutionExpression> convert)
    {
        var source =
            plan.Program ?? throw new InvalidOperationException("Buffer program was not selected by planning.");
        var inputs = source.Inputs.Select(input => ConvertInput(input, convert)).ToArray();
        foreach (var step in source.Steps.Where(step => step.Kind == ByteBufferStepKind.CheckedRange))
        {
            RetypeNull(step.Argument);
            RetypeNull(step.LengthArgument);
        }
        var steps = source
            .Steps.Select(step => new ExecutionByteBufferStep(
                step.Kind switch
                {
                    ByteBufferStepKind.Skip => ExecutionByteBufferStepKind.Skip,
                    ByteBufferStepKind.Take => ExecutionByteBufferStepKind.Take,
                    ByteBufferStepKind.CheckedRange => ExecutionByteBufferStepKind.CheckedRange,
                    ByteBufferStepKind.NullWhenEmpty => ExecutionByteBufferStepKind.NullWhenEmpty,
                    _ => throw new ArgumentOutOfRangeException(nameof(plan)),
                },
                step.Argument,
                step.LengthArgument
            ))
            .ToArray();
        var fallback = source.Concatenation is { } concat ? CreateFallback(concat, inputs.Length) : null;
        var result = source.Result switch
        {
            ByteBufferResultKind.Snapshot => ExecutionByteBufferResultKind.Snapshot,
            ByteBufferResultKind.Length => ExecutionByteBufferResultKind.Length,
            ByteBufferResultKind.View => ExecutionByteBufferResultKind.View,
            _ => throw new ArgumentOutOfRangeException(nameof(plan)),
        };
        var encodedSteps = steps.Select(step =>
            FormattableString.Invariant($"{step.Kind}:{step.Argument}:{step.LengthArgument}")
        );
        var signature =
            string.Join("|", inputs.Select(input => input.ReturnType.StableId))
            + FormattableString.Invariant(
                $"|{source.SegmentCount}|{source.InputIsView}|{string.Join(";", encodedSteps)}|{source.Requirement.MaximumReadBytes}|{result}|{source.EmptyAllocation}|{fallback?.Method.Descriptor.StableName}"
            );
        var program = new ExecutionByteBufferProgram
        {
            HelperName =
                "__ByteBuffer_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature)))[..16],
            Inputs = inputs,
            Steps = steps,
            SegmentCount = source.SegmentCount,
            InputIsView = source.InputIsView,
            MaximumReadBytes = source.Requirement.MaximumReadBytes,
            Result = result,
            EmptyAllocation =
                source.EmptyAllocation == ByteBufferEmptyAllocation.Fresh
                    ? ExecutionByteBufferEmptyAllocation.Fresh
                    : ExecutionByteBufferEmptyAllocation.Shared,
            Concatenation = fallback,
        };
        if (result == ExecutionByteBufferResultKind.Snapshot)
            return new ExecutionByteBufferSnapshot(program);
        return new ExecutionByteBufferOperation(
            new ExecutionLiteral(null, result == ExecutionByteBufferResultKind.View ? typeof(byte[]) : typeof(int?)),
            plan
        )
        {
            Program = program,
            Materialization = ExecutionByteBufferMaterialization.PlannedOperations,
        };

        void RetypeNull(int index)
        {
            if (source.Inputs[index] is Literal { Value: null })
                inputs[index] = new ExecutionLiteral(null, typeof(int?));
        }
    }

    private static ExecutionExpression ConvertInput(IrExpression input, Func<IrExpression, ExecutionExpression> convert)
    {
        var converted = convert(input);
        return input is MethodCall { BufferSemantics: null } && converted is ExecutionMethodCall call
            ? call with
            {
                IsOpaqueBufferProducer = true,
            }
            : converted;
    }

    private static ExecutionMethodCall CreateFallback(MethodCall call, int argumentIndex)
    {
        var parameter = call.Method.GetParameters()[0].ParameterType;
        return new ExecutionMethodCall(
            call.Method,
            [new ExecutionByteBufferArgument(argumentIndex, ExecutionClrBindingFactory.FromClr(parameter))],
            call.Alias,
            typeof(byte[])
        )
        {
            BufferSemantics = call.BufferSemantics,
        };
    }
}
