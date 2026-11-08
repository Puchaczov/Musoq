using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionConcurrencyQueryTestsFixtures
{

    public sealed partial class ConcurrencyLibrary
    {
        [WindowFunction(typeof(CancellableValueKernel), WindowEvaluationModes.WholePartition,
            AggregateEmptyResultBehavior.Custom, ContextType = typeof(WindowFrameContext<>))]
        public int CancellableValue(int value, ConcurrencyProbe probe) => AggregateFunction.NotInvoked<int>();

        public static class CancellableValueKernel
        {
            public static int Get(in WindowFrameContext<int> context, ConcurrencyProbe probe)
            {
                probe.EnterWindow(context.CancellationToken);
                return context.Read(context.Position);
            }
        }
    }

}
