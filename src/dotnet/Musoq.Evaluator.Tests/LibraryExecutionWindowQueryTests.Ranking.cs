using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void RankingKernel_WhenDeclaredUnderAnAlias_ShouldReceiveConcretePeerContext()
    {
        const string query = """
            select Position, PeerScore() over (order by Value) as Score,
                Rank() over (order by Value) as Rank, DenseRank() over (order by Value) as Dense
            from #schema.first() order by Position
            """;
        NumericRow<int>[] input = [new(1, 1), new(2, 2), new(3, 2)];
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, RankingLibrary>(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Score", typeof(decimal)),
            ("Rank", typeof(long)), ("Dense", typeof(long)));
        Assert.HasCount(3, rows);
        decimal[] scores = [101.5m, 103.5m, 103.5m];
        long[] ranks = [1L, 2L, 2L];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(scores[index], rows[index].GetValue<decimal>(1));
            Assert.AreEqual(ranks[index], rows[index].GetValue<long>(2));
            Assert.AreEqual(ranks[index], rows[index].GetValue<long>(3));
        }
    }

    public sealed class RankingLibrary : LibraryBase
    {
        [WindowFunction(Name = "PeerScore", KernelType = typeof(PeerScoreKernel),
            RankingKind = WindowRankingKind.Rank, RequiresOrderBy = true,
            Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Batch)]
        public decimal DeclarePeerScore() => AggregateFunction.NotInvoked<decimal>();
    }

    public static class PeerScoreKernel
    {
        public static decimal Get(in WindowRankingContext context)
            => 100.5m + context.PeerStart + context.DenseRank;
    }
}
