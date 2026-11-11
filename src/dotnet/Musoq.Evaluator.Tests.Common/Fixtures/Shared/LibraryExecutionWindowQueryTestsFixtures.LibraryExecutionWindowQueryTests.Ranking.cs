using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{
    public sealed class RankingLibrary : LibraryBase
    {
        [WindowFunction(
            Name = "PeerScore",
            KernelType = typeof(PeerScoreKernel),
            RankingKind = WindowRankingKind.Rank,
            RequiresOrderBy = true,
            Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Batch
        )]
        public decimal DeclarePeerScore() => AggregateFunction.NotInvoked<decimal>();
    }

    public static class PeerScoreKernel
    {
        public static decimal Get(in WindowRankingContext context) => 100.5m + context.PeerStart + context.DenseRank;
    }
}
