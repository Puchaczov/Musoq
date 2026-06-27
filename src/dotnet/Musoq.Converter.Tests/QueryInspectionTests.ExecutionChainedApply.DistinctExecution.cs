using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderDistinctMinMaxGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select i.Name as Name, RowNumber() over (order by Max(distinct n.Value) desc, Min(distinct n.Value), i.Name) as DistinctMinMaxRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name",
            CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Sum(n.Value) as RepeatedSum, Sum(distinct n.Value) as DistinctSum, RowNumber() over (order by Sum(distinct n.Value) desc, Sum(n.Value) desc, i.Name) as MixedRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedRowNo", CreateMixedRegularAndDistinctAggregateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("right", table[0][0]);
        Assert.AreEqual(2, table[0][1]);
        Assert.AreEqual(2, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(4, table[1][1]);
        Assert.AreEqual(1, table[1][2]);
        Assert.AreEqual(2L, table[1][3]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctGroupedAggregateSortChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Sum(n.Value) as RepeatedSum, Sum(distinct n.Value) as DistinctSum from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Sum(distinct n.Value) desc, Sum(n.Value) desc, i.Name", CreateMixedRegularAndDistinctAggregateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("right", table[0][0]);
        Assert.AreEqual(2, table[0][1]);
        Assert.AreEqual(2, table[0][2]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(4, table[1][1]);
        Assert.AreEqual(1, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctMinMaxGroupedAggregateSortChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Min(n.Value) as RepeatedMin, Min(distinct n.Value) as DistinctMin, Max(n.Value) as RepeatedMax, Max(distinct n.Value) as DistinctMax from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Max(distinct n.Value) desc, Max(n.Value) desc, Min(distinct n.Value), Min(n.Value), i.Name", CreateMixedDistinctAggregateFamilySchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual(4, table[0][3]);
        Assert.AreEqual(4, table[0][4]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(3, table[1][2]);
        Assert.AreEqual(3, table[1][3]);
        Assert.AreEqual(3, table[1][4]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctAvgGroupedAggregateSortChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Avg(n.Value) as RepeatedAvg, Avg(distinct n.Value) as DistinctAvg from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Avg(distinct n.Value) desc, Avg(n.Value) desc, i.Name", CreateMixedDistinctAggregateFamilySchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("right", table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual(3, table[0][2]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(2, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctMinMaxGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Min(n.Value) as RepeatedMin, Min(distinct n.Value) as DistinctMin, Max(n.Value) as RepeatedMax, Max(distinct n.Value) as DistinctMax, RowNumber() over (order by Max(distinct n.Value) desc, Max(n.Value) desc, Min(distinct n.Value), Min(n.Value), i.Name) as MixedMinMaxRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedMinMaxRowNo", CreateMixedDistinctAggregateFamilySchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual(4, table[0][3]);
        Assert.AreEqual(4, table[0][4]);
        Assert.AreEqual(1L, table[0][5]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(3, table[1][2]);
        Assert.AreEqual(3, table[1][3]);
        Assert.AreEqual(3, table[1][4]);
        Assert.AreEqual(2L, table[1][5]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctAvgGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, Avg(n.Value) as RepeatedAvg, Avg(distinct n.Value) as DistinctAvg, RowNumber() over (order by Avg(distinct n.Value) desc, Avg(n.Value) desc, i.Name) as MixedAvgRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedAvgRowNo", CreateMixedDistinctAggregateFamilySchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("right", table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual(3, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(2, table[1][2]);
        Assert.AreEqual(2L, table[1][3]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderAvgParentGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, RowNumber() over (order by Avg(n.Value, 0) desc, i.Name) as ParentAvgRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderMinMaxParentGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name as Name, RowNumber() over (order by Max(n.Value, 0) desc, Min(n.Value, 0), i.Name) as ParentMinMaxRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderQualifiedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m qualify RowNumber() over (partition by i.Name order by n.Value, m.Value) <= 1 order by i.Name", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(3, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererDisabledForChainedCrossApplyPropertySource_ShouldStillRunThroughExecutionBackend()
    {
        var compiled = CompileForExecution(
            "select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m",
            CreateApplyCandidateSchemaProvider(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(1, table[1][1]);
        Assert.AreEqual(2, table[1][2]);
        Assert.AreEqual("right", table[4][0]);
        Assert.AreEqual(3, table[4][1]);
        Assert.AreEqual(3, table[4][2]);
    }

}
