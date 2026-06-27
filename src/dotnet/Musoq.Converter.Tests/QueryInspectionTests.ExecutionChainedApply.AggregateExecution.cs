using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name, n.Value as FirstValue, m.Value as SecondValue, RowNumber() over (partition by i.Name order by n.Value, m.Value) as RowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m order by i.Name, RowNo", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.AreEqual("left", table[3][0]);
        Assert.AreEqual(2, table[3][1]);
        Assert.AreEqual(2, table[3][2]);
        Assert.AreEqual(4L, table[3][3]);
        Assert.AreEqual("right", table[4][0]);
        Assert.AreEqual(3, table[4][1]);
        Assert.AreEqual(3, table[4][2]);
        Assert.AreEqual(1L, table[4][3]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderGroupedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Count(1) as PairCount from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(4L, table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderGroupedHavingChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Count(1) as PairCount from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name having Count(1) > 1 order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(4L, table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderGroupedWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Count(1) as PairCount, RowNumber() over (order by i.Name) as GroupRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by GroupRowNo", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(4L, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
        Assert.AreEqual(2L, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderGroupedQualifiedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Count(1) as PairCount from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name qualify RowNumber() over (order by i.Name) <= 1 order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(4L, table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderGroupedSumWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Sum(n.Value) as ValueSum, RowNumber() over (order by Sum(n.Value) desc) as GroupRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by GroupRowNo", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(6, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(2L, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderGroupedHavingQualifiedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Sum(n.Value) as ValueSum from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name having Sum(n.Value) > 2 qualify RowNumber() over (order by Sum(n.Value) desc) <= 1 order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(6, table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderPartitionedGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, RowNumber() over (partition by Count(1) order by Avg(n.Value) desc, Min(n.Value), Max(n.Value) desc) as AggregateRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderGroupedAvgMinMaxHavingQualifiedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Avg(n.Value) as ValueAvg, Min(n.Value) as ValueMin, Max(n.Value) as ValueMax from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name having Max(n.Value) >= 2 qualify RowNumber() over (order by Avg(n.Value) desc, Min(n.Value), Max(n.Value) desc) <= 1 order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("right", table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual(3, table[0][2]);
        Assert.AreEqual(3, table[0][3]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderFilteredGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, RowNumber() over (order by Sum(n.Value) filter (where m.Value > 1) desc, i.Name) as FilteredSumRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderMultiArgumentGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, RowNumber() over (order by Sum(n.Value, 0) desc, i.Name) as ParentSumRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderAliasDistinctGroupedAggregateChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Sum(n.Value) as NumberTotal, Sum(b.Value) as ByteTotal from #apply.items() i cross apply i.Numbers n cross apply i.Content b group by i.Name order by Name", CreateAliasDistinctAggregateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(6, table[0][1]);
        Assert.AreEqual((byte)60, table[0][2]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual((byte)7, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderAliasDistinctGroupedAggregateSortChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Sum(n.Value) as NumberTotal, Sum(b.Value) as ByteTotal from #apply.items() i cross apply i.Numbers n cross apply i.Content b group by i.Name order by Sum(b.Value) desc, Sum(n.Value) desc", CreateAliasDistinctAggregateSortSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("right", table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual((byte)100, table[0][2]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(10, table[1][1]);
        Assert.AreEqual((byte)1, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderAliasDistinctGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Sum(n.Value) as NumberTotal, Sum(b.Value) as ByteTotal, RowNumber() over (order by Sum(b.Value) desc, Sum(n.Value) desc, i.Name) as AggregateRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Content b group by i.Name order by AggregateRowNo", CreateAliasDistinctAggregateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(6, table[0][1]);
        Assert.AreEqual((byte)60, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual((byte)7, table[1][2]);
        Assert.AreEqual(2L, table[1][3]);
    }

}
