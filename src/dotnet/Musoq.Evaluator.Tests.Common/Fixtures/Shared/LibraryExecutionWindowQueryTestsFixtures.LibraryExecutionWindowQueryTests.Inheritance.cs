using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{
    public sealed class InvoiceWindowLibrary : LibraryBase
    {
        [WindowFunction(
            typeof(LibraryExecutionLibrary.ScaledTotalKernel),
            WindowEvaluationModes.WholePartition
                | WindowEvaluationModes.Running
                | WindowEvaluationModes.RecomputeFrames,
            AggregateEmptyResultBehavior.Null,
            Name = "Sum"
        )]
        public decimal? SumScaled(decimal? value, int factor) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(
            typeof(InvoiceTotalKernel),
            WindowEvaluationModes.WholePartition
                | WindowEvaluationModes.Running
                | WindowEvaluationModes.RecomputeFrames,
            AggregateEmptyResultBehavior.Null
        )]
        public new decimal? Sum(decimal? value, [AggregateParent] int parent = 0) =>
            AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(
            typeof(InvoiceTotalKernel),
            WindowEvaluationModes.WholePartition,
            AggregateEmptyResultBehavior.Null,
            Name = "InvoiceTotal"
        )]
        public decimal? TotalAmount(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
    }

    public static class InvoiceTotalKernel
    {
        public struct State
        {
            public bool HasValue;
            public decimal Total;
        }

        public static void Reset(ref State state) => state = new State { Total = 100m };

        public static void Set(ref State state, decimal? value)
        {
            if (!value.HasValue)
                return;
            state.Total = checked(state.Total + value.Value);
            state.HasValue = true;
        }

        public static decimal? Get(in State state) => state.HasValue ? state.Total : null;
    }
}
