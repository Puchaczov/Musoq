using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{
    public sealed record NullableOrderKeyRow(int Position, int? Key, int? Value);

    public sealed record OrderKeyRow(int Partition, int Position, int? Value, string Label);

    public sealed class OrderKeyLibrary : LibraryBase
    {
        public static int KeyCalls;
        public static int RangeKeyCalls;

        [BindableMethod, NonDeterministic]
        public static int RangeCountingKey(int value)
        {
            Interlocked.Increment(ref RangeKeyCalls);
            return value;
        }

        [BindableMethod, NonDeterministic]
        public static int CountingKey(int value)
        {
            Interlocked.Increment(ref KeyCalls);
            return value;
        }

        [WindowFunction(
            KernelType = typeof(KeyWeightedKernel),
            ContextType = typeof(WindowFrameContext<,>),
            Modes = WindowEvaluationModes.WholePartition
                | WindowEvaluationModes.Running
                | WindowEvaluationModes.RecomputeFrames,
            EmptyResultBehavior = AggregateEmptyResultBehavior.Null
        )]
        public decimal? KeyWeighted(int? value) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(
            KernelType = typeof(NullableKeyTotalKernel),
            ContextType = typeof(WindowFrameContext<,>),
            Modes = WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames,
            EmptyResultBehavior = AggregateEmptyResultBehavior.Null
        )]
        public decimal? NullableKeyTotal(int? value) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(
            KernelType = typeof(FirstLabelKernel),
            ContextType = typeof(WindowFrameContext<,>),
            Modes = WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames,
            EmptyResultBehavior = AggregateEmptyResultBehavior.Null
        )]
        public string? FirstLabel(int? value) => AggregateFunction.NotInvoked<string?>();
    }

    public static class KeyWeightedKernel
    {
        public static decimal? Get(in WindowFrameContext<int?, int> context)
        {
            decimal? total = null;
            for (var index = context.FrameStart; index <= context.FrameEnd; index++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                var value = context.Read(index);
                if (!value.HasValue)
                    continue;
                var weighted = checked((decimal)value.Value * context.ReadOrderKey(index));
                total = checked((total ?? 0m) + weighted);
            }
            return total;
        }
    }

    public static class NullableKeyTotalKernel
    {
        public static decimal? Get(in WindowFrameContext<int?, int?> context)
        {
            decimal? total = null;
            for (var index = context.FrameStart; index <= context.FrameEnd; index++)
            {
                var value = context.Read(index);
                if (value.HasValue)
                    total = checked((total ?? 0m) + (decimal)value.Value * (context.ReadOrderKey(index) ?? 0));
            }
            return total;
        }
    }

    public static class FirstLabelKernel
    {
        public static string? Get(in WindowFrameContext<int?, (int, string)> context) =>
            context.IsEmpty ? null : context.ReadOrderKey(context.FrameStart).Item2;
    }
}
