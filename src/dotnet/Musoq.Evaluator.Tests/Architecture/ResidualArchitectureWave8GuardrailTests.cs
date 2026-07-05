using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave8GuardrailTests
{
    [TestMethod]
    public void TableCompletionAndPostOperationProjection_ShouldLiveUnderLoweringTables()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var tablesDirectory = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "Lowering", "Tables");

        Assert.IsTrue(File.Exists(Path.Combine(tablesDirectory, "TableCompletionModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(tablesDirectory, "TableCompletionPlanner.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(tablesDirectory, "PostOperationProjectionPlanner.cs")));

        var materializationText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.Windows.Materialization.cs"));
        var projectionText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.PostOperationProjection.cs"));
        var orderText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.OrderRecords.cs"));

        Assert.Contains("TableCompletionPlanner.Default.Complete", materializationText);
        Assert.DoesNotContain("new ExecutionDistinctTable", materializationText);
        Assert.DoesNotContain("new ExecutionProjectTable", materializationText);
        Assert.Contains("PostOperationProjectionPlanner", projectionText);
        Assert.DoesNotContain("CreateHiddenSortFieldName", projectionText);
        Assert.Contains("PostOperationProjectionPlanner.CreateHiddenSortFields", orderText);
        Assert.Contains("PostOperationProjectionPlanner.ReplaceSortProjectedFields", orderText);
    }
}
