using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class TwoModeRendererGuardrailTests
{
    [TestMethod]
    public void FinalSinkRenderers_ShouldUseSharedSetupAndProjectionInvocationHelpers()
    {
        var rendererRoot = FindRendererRoot();
        var typedDirect = File.ReadAllText(Path.Combine(rendererRoot, "CSharpRenderer.TypedDirectProjection.cs"));
        var tableDirect = File.ReadAllText(Path.Combine(rendererRoot, "CSharpRenderer.TableDirectProjection.cs"));
        var typedPost = File.ReadAllText(Path.Combine(rendererRoot, "CSharpRenderer.TypedPostOperations.cs"));
        var finalRows = File.ReadAllText(Path.Combine(rendererRoot, "CSharpRenderer.FinalSinkRows.cs"));
        var setup = File.ReadAllText(Path.Combine(rendererRoot, "CSharpRenderer.FinalSinkSetup.cs"));
        var invocations = File.ReadAllText(Path.Combine(rendererRoot, "CSharpRenderer.FinalProjectionInvocations.cs"));

        StringAssert.Contains(typedDirect, "TryCreateFinalSinkMethod(");
        StringAssert.Contains(tableDirect, "TryCreateFinalSinkMethod(");
        StringAssert.Contains(typedPost, "TryCreateFinalSinkMethod(");
        StringAssert.Contains(setup, "CreateTypedSinkSetupArtifacts");
        StringAssert.Contains(invocations, "FinalProjectionInvocationSpec");
        StringAssert.Contains(typedDirect, "CreateFinalProjectionInvocation");
        StringAssert.Contains(finalRows, "CreateFinalProjectionInvocation");
        StringAssert.Contains(typedDirect, "CreateQueryRowsShardInvocation");
        StringAssert.Contains(finalRows, "CreateQueryRowsShardInvocation");
        Assert.IsFalse(typedDirect.Contains("RenderSourceScanForTypedSink", StringComparison.Ordinal));
        Assert.IsFalse(tableDirect.Contains("RenderSourceScanForTypedSink", StringComparison.Ordinal));
        Assert.IsFalse(typedPost.Contains("RenderSourceScanForTypedSink", StringComparison.Ordinal));
        Assert.IsFalse(setup.Contains("RenderSourceScanForTypedSink", StringComparison.Ordinal));
    }

    [TestMethod]
    public void FinalSinkRenderers_ShouldNotCallSinkPlannersOrAnalyzers()
    {
        var rendererRoot = FindRendererRoot();
        foreach (var file in Directory.EnumerateFiles(rendererRoot, "CSharpRenderer*.cs"))
        {
            var text = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);

            Assert.IsFalse(text.Contains("FinalProjectionSinkPlanner", StringComparison.Ordinal), fileName);
            Assert.IsFalse(text.Contains("FinalProjectionDirectProjectionAnalyzer", StringComparison.Ordinal), fileName);
            Assert.IsFalse(text.Contains("FinalProjectionPostOperationAnalyzer", StringComparison.Ordinal), fileName);
        }
    }

    private static string FindRendererRoot()
    {
        var root = FindRepositoryRoot();
        return Path.Combine(root, "src", "dotnet", "Musoq.Evaluator", "IR", "CodeGeneration");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "dotnet", "Musoq.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing src/dotnet/Musoq.sln.");
    }
}
