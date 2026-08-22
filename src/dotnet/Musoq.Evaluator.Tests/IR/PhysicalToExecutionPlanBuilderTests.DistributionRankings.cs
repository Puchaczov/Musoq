using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenPlanHasDistributionRankings_ShouldReturnTypedWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        OrderField[] orderFields = [new(new ColumnRef("p", "Age", typeof(int)), Descending: false)];
        IrExpression[] partitionKeys = [new ColumnRef("p", "Name", typeof(string))];
        var percentRank = CreatePercentRankRegistration(orderFields, partitionKeys);
        var cumeDist = CreateCumeDistRegistration(orderFields, partitionKeys, windowIndex: 1);
        var window = new PhysicalWindowNode([percentRank, cumeDist], materialize);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("PercentRank", new WindowFunctionRef(0, typeof(double)), 0),
                new ProjectedField("CumeDist", new WindowFunctionRef(1, typeof(double)), 1)
            ],
            window);

        var plan = RequireExecutionPlan(CreateBuilder().Build(project, "Q_DistributionRankings"));
        var expected = string.Join("\n",
            "ExecutionPlan [Q_DistributionRankings]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      PercentRank: double <- field PercentRank",
            "      CumeDist: double <- field CumeDist",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    WindowKernelPlan [hash partition/per-partition sort; kernels 2; ranking|resultWindowRows|resultPercentRanks0Partitions|resultPercentRanks0Partitions|resultPercentRanks0PartitionKeys|resultPercentRanks0OrderKeys]",
            "      ComputePercentRankWindow [resultPercentRanks0 <- resultWindowRows partition by p.Name order by p.Age ASC]",
            "      ComputeCumeDistWindow [resultCumeDists1 <- resultWindowRows partition by p.Name order by p.Age ASC]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(PercentRank: resultPercentRanks0[windowIndex], CumeDist: resultCumeDists1[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }
}
