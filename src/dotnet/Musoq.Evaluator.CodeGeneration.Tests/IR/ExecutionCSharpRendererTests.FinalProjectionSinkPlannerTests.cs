using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenProjectionIsDirect_ShouldAcceptDirectRows()
    {
        var plan = CreateProjectionPlan(
            "Q_DirectProjection",
            "Name",
            typeof(string),
            new ExecutionFieldRead("p", "Name", typeof(string)));

        var sinkPlan = AnalyzeDirectProjection(plan);

        AssertAccepted(sinkPlan);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, sinkPlan.ResultMetadata.RowPathKind);
        Assert.AreEqual(FinalResultSinkKind.TableRowsMaterialized, sinkPlan.ResultMetadata.FinalResultSinkKind);
        Assert.IsFalse(sinkPlan.ResultMetadata.RequiresComputeTableMethod);
        Assert.IsNull(sinkPlan.ProjectionLoop!.Predicate);
        Assert.AreEqual(0, sinkPlan.PostOperations.Count);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, sinkPlan.RejectionKind);
    }

    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenProjectionIsFiltered_ShouldKeepPredicate()
    {
        var sinkPlan = AnalyzeDirectProjection(CreatePlan());

        AssertAccepted(sinkPlan);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, sinkPlan.ResultMetadata.RowPathKind);
        Assert.IsNotNull(sinkPlan.ProjectionLoop!.Predicate);
        Assert.AreEqual("result", sinkPlan.AppendTarget!.Name);
    }

    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenProjectionHasMethodTargetSetup_ShouldAcceptSetupNode()
    {
        var plan = CreateProjectionPlan(
            "Q_MethodTargetSetup",
            "Name",
            typeof(string),
            CreateToUpperNameCall());

        var sinkPlan = AnalyzeDirectProjection(plan);

        AssertAccepted(sinkPlan);
        Assert.AreEqual(1, sinkPlan.SetupNodes.Count);
        Assert.IsInstanceOfType(sinkPlan.SetupNodes[0], typeof(ExecutionCreateObject));
    }

    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenProjectionIsParallel_ShouldAcceptShardRows()
    {
        var sinkPlan = AnalyzeDirectProjection(CreateParallelProjectionPlan());

        AssertAccepted(sinkPlan);
        Assert.AreEqual(QueryResultRowPathKind.ShardRows, sinkPlan.ResultMetadata.RowPathKind);
        Assert.AreEqual(FinalResultSinkKind.GeneratedRowParallelShards, sinkPlan.ResultMetadata.FinalResultSinkKind);
        Assert.IsTrue(sinkPlan.ProjectionLoop!.CanUseParallel);
    }

    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenParallelProjectionHasRowLocalCse_ShouldAcceptOptionalProjector()
    {
        var sinkPlan = AnalyzeDirectProjection(CreateParallelProjectionPlanWithRowLocalMethodCse());

        AssertAccepted(sinkPlan);
        Assert.AreEqual(QueryResultRowPathKind.ShardRows, sinkPlan.ResultMetadata.RowPathKind);
        Assert.IsTrue(sinkPlan.ProjectionLoop!.CanUseParallel);
        Assert.IsNotNull(sinkPlan.ProjectionLoop.OptionalProjectionBody);
    }

    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenPostOperationChainIsSupported_ShouldAcceptOperations()
    {
        var sinkPlan = AnalyzePostOperations(CreateProjectionPostOperationPlan());

        AssertAccepted(sinkPlan);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, sinkPlan.ResultMetadata.RowPathKind);
        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, sinkPlan.ResultMetadata.FinalResultSinkKind);
        Assert.AreEqual(4, sinkPlan.PostOperations.Count);
        Assert.IsInstanceOfType(sinkPlan.PostOperations[0], typeof(TypedPostOperation.Distinct));
        Assert.IsInstanceOfType(sinkPlan.PostOperations[1], typeof(TypedPostOperation.Order));
        Assert.IsInstanceOfType(sinkPlan.PostOperations[2], typeof(TypedPostOperation.Skip));
        Assert.IsInstanceOfType(sinkPlan.PostOperations[3], typeof(TypedPostOperation.Take));
    }

    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenHiddenSortColumnRequiresRenumbering_ShouldReject()
    {
        var sinkPlan = AnalyzePostOperations(CreateHiddenSortProjectionPostOperationPlan());

        AssertRejected(sinkPlan);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.UnsupportedPostOperationChain, sinkPlan.RejectionKind);
        StringAssert.Contains(sinkPlan.RejectionReason!, "Post-operation chain");
    }

    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenPlanHasJoinShape_ShouldReject()
    {
        var sinkPlan = AnalyzeDirectProjection(CreateMultipleSourceScanProjectionPlan());

        AssertRejected(sinkPlan);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.ExpectedOneSourceScan, sinkPlan.RejectionKind);
        StringAssert.Contains(sinkPlan.RejectionReason!, "Expected one source scan");
    }

    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenPlanHasGroupingShape_ShouldReject()
    {
        var sinkPlan = AnalyzeDirectProjection(CreateKernelAggregatePlan());

        AssertRejected(sinkPlan);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.ExpectedOneSourceScan, sinkPlan.RejectionKind);
        StringAssert.Contains(sinkPlan.RejectionReason!, "Expected one source scan");
    }

    [TestMethod]
    public void FinalProjectionSinkPlanner_WhenPlanHasUnsupportedExtraNodes_ShouldReject()
    {
        var plan = CreateProjectionPlan(
            "Q_UnsupportedExtraNode",
            "Name",
            typeof(string),
            new ExecutionFieldRead("p", "Name", typeof(string)));
        var unsupported = new ExecutionLet(
            new ExecutionVariable("__extra", typeof(int)),
            new ExecutionLiteral(1, typeof(int)));
        plan = plan with
        {
            Body = new ExecutionBlock([
                .. plan.Body.Nodes.Take(2),
                unsupported,
                .. plan.Body.Nodes.Skip(2)
            ])
        };

        var sinkPlan = AnalyzeDirectProjection(plan);

        AssertRejected(sinkPlan);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.UnexpectedPlanNodes, sinkPlan.RejectionKind);
        StringAssert.Contains(sinkPlan.RejectionReason!, "outside the direct projection sink");
    }

    [TestMethod]
    public void TableViaRowsResultInfoResolver_WhenFinalResultExists_ShouldIgnoreReturnTable()
    {
        var plan = CreatePlan();
        var staleTable = new ExecutionVariable("staleResult", typeof(object));
        var nodes = plan.Body.Nodes
            .Select(node => node is ExecutionReturnTable
                ? new ExecutionReturnTable(staleTable)
                : node)
            .ToArray();
        var rewritten = plan with { Body = new ExecutionBlock(nodes) };

        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolve(rewritten, out var resultInfo));
        Assert.AreEqual("result", resultInfo.TableName);
        Assert.AreEqual("ResultRow0", resultInfo.RowTypeName);
        Assert.AreEqual("Name", resultInfo.Columns.Single().Name);
    }

    [TestMethod]
    public void TableViaRowsResultInfoResolver_WhenOnlyReturnTableExists_ShouldReject()
    {
        var plan = CreatePlan();
        var legacyOnly = new ExecutionPlan(plan.Identifier, plan.Shapes, plan.Body);

        Assert.IsFalse(TableViaRowsResultInfoResolver.TryResolve(legacyOnly, out _));
    }

    private static FinalProjectionSinkPlan AnalyzeDirectProjection(ExecutionPlan plan)
    {
        return FinalProjectionSinkPlanner.AnalyzeDirectProjection(
            plan,
            ResolveResultInfo(plan),
            FinalProjectionSinkTarget.TableRows);
    }

    private static FinalProjectionSinkPlan AnalyzePostOperations(ExecutionPlan plan)
    {
        return FinalProjectionSinkPlanner.AnalyzePostOperations(
            plan,
            ResolveResultInfo(plan),
            FinalProjectionSinkTarget.TypedRows);
    }

    private static TableViaRowsResultInfo ResolveResultInfo(ExecutionPlan plan)
    {
        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolve(plan, out var resultInfo));
        return resultInfo;
    }

    private static void AssertAccepted(FinalProjectionSinkPlan sinkPlan)
    {
        Assert.IsTrue(sinkPlan.IsAccepted, sinkPlan.RejectionReason);
        Assert.IsNotNull(sinkPlan.ProjectionLoop);
        Assert.IsNotNull(sinkPlan.AppendTarget);
        Assert.AreEqual(1, sinkPlan.SourceScans.Count);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, sinkPlan.RejectionKind);
        Assert.IsNull(sinkPlan.RejectionReason);
    }

    private static void AssertRejected(FinalProjectionSinkPlan sinkPlan)
    {
        Assert.IsFalse(sinkPlan.IsAccepted);
        Assert.IsNull(sinkPlan.ProjectionLoop);
        Assert.AreNotEqual(FinalProjectionSinkRejectionKind.None, sinkPlan.RejectionKind);
        Assert.IsNotNull(sinkPlan.RejectionReason);
    }

    private static ExecutionPlan CreateHiddenSortProjectionPostOperationPlan()
    {
        var plan = CreateProjectionPostOperationPlan();
        var nodes = plan.Body.Nodes
            .Select(static node => node is ExecutionSortTable sort
                ? sort with { RenumberFieldIndexes = [0] }
                : node)
            .ToArray();

        return plan with { Body = new ExecutionBlock(nodes) };
    }

    private static ExecutionPlan CreateMultipleSourceScanProjectionPlan()
    {
        var plan = CreatePlan();
        return plan with { Body = new ExecutionBlock([plan.Body.Nodes[0], ..plan.Body.Nodes]) };
    }
}
