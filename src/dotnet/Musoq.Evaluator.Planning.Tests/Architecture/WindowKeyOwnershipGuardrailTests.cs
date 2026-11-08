using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class WindowKeyOwnershipGuardrailTests
{
    [TestMethod]
    public void WindowKeyCarriers_ShouldBeSelectedByLoweringAndRequiredByTheRenderer()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var lowering = File.ReadAllText(Path.Combine(root, "src", "dotnet", "Musoq.Evaluator",
            "IR", "Execution", "PhysicalLoweringImplementation.WindowRegistrationKeys.cs"));
        var renderer = File.ReadAllText(Path.Combine(root, "src", "dotnet", "Musoq.Targets.CSharpClr",
            "Rendering", "Execution", "ExecutionCSharpRenderer.WindowGeneratedKeys.cs"));
        var start = renderer.IndexOf("private static ExecutionWindowKeyArray ResolveGeneratedWindowOrderKeyArray", System.StringComparison.Ordinal);
        var end = renderer.IndexOf("private static ExecutionWindowKeyArray? ResolveRankingPartitionKeyArray", start, System.StringComparison.Ordinal);
        var resolver = renderer[start..end];
        Assert.Contains("WindowKeyShapePolicy.CreateOrderKeyArray", lowering);
        Assert.Contains("WindowKeyShapePolicy.CreatePartitionKeyArray", lowering);
        Assert.DoesNotContain("CreateGeneratedWindowKeyArray(", renderer);
        Assert.DoesNotContain("ResolveWindowKeyArray(", renderer);
        Assert.Contains("has no lowered concrete carrier", resolver);
        Assert.DoesNotContain("CreateGeneratedWindowKeyArray(", resolver);
        Assert.DoesNotContain("ResolveWindowKeyArray(", resolver);
    }
    [TestMethod]
    public void RankingExecution_ShouldRequireLibraryOperationsAndConcreteKeyMetadata()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var keys = File.ReadAllText(Path.Combine(root, "src/dotnet/Musoq.Evaluator/IR/Execution/Lowering/Windows/WindowKeyShapePolicy.cs"));
        var metadata = File.ReadAllText(Path.Combine(root, "src/dotnet/Musoq.Evaluator/IR/Execution/Lowering/Windows/RankingWindowMetadata.cs"));
        var renderer = File.ReadAllText(Path.Combine(root, "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.WindowRankingKernels.cs"));
        var plans = File.ReadAllText(Path.Combine(root, "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.WindowKernelPlans.cs"));
        Assert.DoesNotContain("typeof(object[])", keys);
        Assert.DoesNotContain("new ExecutionWindowKeyShape(typeof(object)", keys);
        Assert.DoesNotContain("ResolveFunction(string", metadata);
        Assert.DoesNotContain("CreateFusedRankingKernelLoop", plans);
        Assert.DoesNotContain("CreateRankKernelBody", renderer);
        Assert.Contains("registered library operation", renderer);
    }
}
