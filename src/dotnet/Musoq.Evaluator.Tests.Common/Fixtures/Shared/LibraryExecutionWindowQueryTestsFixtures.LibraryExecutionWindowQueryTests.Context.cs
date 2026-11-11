using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{
    public sealed class FrameLibrary : LibraryBase
    {
        [WindowFunction(
            KernelType = typeof(PositiveAverageKernel),
            ContextType = typeof(WindowFrameContext<>),
            Modes = WindowEvaluationModes.RecomputeFrames
                | WindowEvaluationModes.Running
                | WindowEvaluationModes.WholePartition,
            EmptyResultBehavior = AggregateEmptyResultBehavior.Null
        )]
        public decimal? PositiveAverage(int? value) => AggregateFunction.NotInvoked<decimal?>();
    }

    public static class PositiveAverageKernel
    {
        public static decimal? Get(in WindowFrameContext<int?> context)
        {
            var total = 0m;
            var count = 0L;
            for (var position = context.FrameStart; position <= context.FrameEnd; position++)
            {
                var value = context.Read(position);
                if (value is not > 0)
                    continue;
                total += value.Value;
                count++;
            }
            return count == 0 ? null : total / count;
        }
    }
}
