using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class RenderedQueryInspectorCatalogTests
{
    [TestMethod]
    public void Resolve_WhenTargetIsCSharpClr_ShouldReturnCSharpInspector()
    {
        var inspector = ExecutionTargetCatalog.ResolveInspector(ExecutionTargetIds.CSharpClr);

        Assert.IsInstanceOfType<CSharpRenderedQueryInspector>(inspector);
    }

    [TestMethod]
    public void Inspect_WhenArtifactIsCSharp_ShouldFormatGeneratedCSharp()
    {
        var artifact = new CSharpRenderedQueryArtifact(
            CSharpCompilation.Create(
                "Inspection",
                [CSharpSyntaxTree.ParseText("public sealed class Query { }")]),
            "Inspection.CompiledQuery");

        var inspection = ExecutionTargetCatalog
            .ResolveInspector(ExecutionTargetIds.CSharpClr)
            .Inspect(artifact);

        Assert.AreEqual(ExecutionTargetIds.CSharpClr, inspection.TargetId);
        Assert.Contains("public sealed class Query", inspection.GeneratedCSharpCode ?? string.Empty);
        Assert.AreEqual("csharp", inspection.SourceMetadata["language"]);
        Assert.AreEqual("Inspection.CompiledQuery", inspection.SourceMetadata["runnableType"]);
    }

    [TestMethod]
    public void Resolve_WhenTargetIsTestOnlyNonClr_ShouldRejectWithoutTemporaryInspector()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => ExecutionTargetCatalog.ResolveInspector(TestExecutionTargetIds.TestOnlyNonClr));

        Assert.Contains("TestOnlyNonClr", exception.Message);
    }

    [TestMethod]
    public void InspectGeneratedCSharpCode_WhenInspectorReturnsNonClrInspection_ShouldRejectPublicCSharpCompatibility()
    {
        var artifact = new TestOnlyRenderedQueryArtifact("bytecode:noop");
        using var _ = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(inspector: new TestOnlyRenderedQueryInspector()));
        var method = typeof(InstanceCreator).GetMethod(
            "InspectGeneratedCSharpCode",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        var exception = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(null, [artifact]));

        Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("Generated C# inspection requires execution target", exception.InnerException!.Message);
        Assert.Contains("TestOnlyNonClr", exception.InnerException.Message);
    }

    [TestMethod]
    public void Inspect_WhenTemporaryNonClrInspectorIsRegistered_ShouldExposeTargetMetadataWithoutCSharpCompilation()
    {
        var artifact = new TestOnlyRenderedQueryArtifact("bytecode:noop");
        using var _ = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(inspector: new TestOnlyRenderedQueryInspector()));

        var inspection = ExecutionTargetCatalog
            .ResolveInspector(TestExecutionTargetIds.TestOnlyNonClr)
            .Inspect(artifact);

        Assert.AreEqual(TestExecutionTargetIds.TestOnlyNonClr, inspection.TargetId);
        Assert.IsNull(inspection.GeneratedCSharpCode);
        Assert.AreEqual("bytecode:noop", inspection.SourceMetadata["payload"]);
    }

    private sealed record TestOnlyRenderedQueryArtifact(string Payload)
        : RenderedQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);

    private sealed class TestOnlyRenderedQueryInspector : IRenderedQueryInspector
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public RenderedQueryInspection Inspect(RenderedQueryArtifact artifact)
        {
            var testArtifact = (TestOnlyRenderedQueryArtifact)artifact;
            return new RenderedQueryInspection(
                TargetId,
                null,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["payload"] = testArtifact.Payload
                });
        }
    }
}
