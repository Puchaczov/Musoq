using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal static class ByteBufferDemandPreimage
{
    internal static ByteBufferRequirement For(ByteBufferProgram program, ByteBufferRequirement requirement)
    {
        if (requirement.RequiresCompleteArray || program.SegmentCount != 0)
            return ByteBufferRequirement.Complete;
        var prefix = requirement.PrefixBytes;
        foreach (var step in program.Steps.Reverse())
        {
            if (step.Kind == ByteBufferStepKind.CheckedRange)
                return ByteBufferRequirement.Complete;
            if (step.Kind == ByteBufferStepKind.NullWhenEmpty || prefix == 0)
                continue;
            if (program.Inputs[step.Argument] is not Literal { Value: int count })
                return ByteBufferRequirement.Complete;
            var bounded = Math.Max(0, count);
            prefix = step.Kind == ByteBufferStepKind.Take
                ? Math.Min(prefix, bounded) : (int)Math.Min(int.MaxValue, (long)prefix + bounded);
        }
        return new(prefix, true, false);
    }
}
