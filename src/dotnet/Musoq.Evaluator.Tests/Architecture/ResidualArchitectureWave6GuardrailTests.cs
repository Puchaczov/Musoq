using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave6GuardrailTests
{
    [TestMethod]
    public void PhysicalLoweringRegistry_ShouldExposeTopLevelDispatchInventoryInOrder()
    {
        string[] expected =
        [
            "multi-statement",
            "cte",
            "desc",
            "set-operation",
            "aggregate",
            "window",
            "pipeline"
        ];

        CollectionAssert.AreEqual(expected, PhysicalToExecutionPlanBuilder.PlanLoweringDescriptorNames.ToArray());
    }

    [TestMethod]
    public void PhysicalLoweringRegistry_ShouldExposeTableDispatchInventoryInOrder()
    {
        string[] expected =
        [
            "multi-statement-table",
            "set-operation-table",
            "window-table",
            "pipeline-table",
            "aggregate-table"
        ];

        CollectionAssert.AreEqual(expected, PhysicalToExecutionPlanBuilder.TableLoweringDescriptorNames.ToArray());
    }

    [TestMethod]
    public void PhysicalToExecutionBuilderEntrypoints_ShouldDelegateDispatchToRegistry()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var planDispatchFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.cs");
        var tableDispatchFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.TableDispatch.cs");
        var registryFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.DispatchRegistry.cs");

        var planDispatchText = File.ReadAllText(planDispatchFile);
        var tableDispatchText = File.ReadAllText(tableDispatchFile);
        var registryText = File.ReadAllText(registryFile);

        Assert.Contains("CreatePhysicalLoweringRegistry().TryBuildPlan", planDispatchText);
        Assert.Contains("CreatePhysicalLoweringRegistry().TryBuildTable", tableDispatchText);
        Assert.Contains("TryBuildMultiStatementPlan", registryText);
        Assert.Contains("TryBuildCtePlan", registryText);
        Assert.Contains("TryBuildDescPlan", registryText);
        Assert.Contains("TryBuildSetOperationPlan", registryText);
        Assert.Contains("TryBuildAggregatePlan", registryText);
        Assert.Contains("TryBuildWindowPlan", registryText);
        Assert.Contains("TryBuildPipelinePlan", registryText);
        Assert.Contains("TryBuildMultiStatementTable", registryText);
        Assert.Contains("TryBuildSetOperationTable", registryText);
        Assert.Contains("TryBuildWindowTable", registryText);
        Assert.Contains("TryBuildPipelineTable", registryText);
        Assert.Contains("TryBuildAggregateTable", registryText);

        Assert.IsFalse(
            planDispatchText.Contains("CteLoweringCoordinator", StringComparison.Ordinal) ||
            planDispatchText.Contains("CreateAggregateLoweringCoordinator", StringComparison.Ordinal) ||
            planDispatchText.Contains("CreateWindowLoweringCoordinator", StringComparison.Ordinal),
            "Top-level plan dispatch should stay in PhysicalToExecutionPlanBuilder.DispatchRegistry.cs.");
        Assert.IsFalse(
            tableDispatchText.Contains("CreateAggregateLoweringCoordinator", StringComparison.Ordinal) ||
            tableDispatchText.Contains("CreateWindowLoweringCoordinator", StringComparison.Ordinal),
            "Table-producing dispatch should stay in PhysicalToExecutionPlanBuilder.DispatchRegistry.cs.");
    }
}
