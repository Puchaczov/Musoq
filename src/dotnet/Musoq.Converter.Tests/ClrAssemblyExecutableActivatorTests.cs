using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class ClrAssemblyExecutableActivatorTests
{
    [TestMethod]
    public void Resolve_WhenTargetIsCSharpClr_ShouldReturnClrAssemblyActivator()
    {
        var activator = ExecutionTargetCatalog.ResolveActivator(ExecutionTargetIds.CSharpClr);

        Assert.IsInstanceOfType<ClrAssemblyExecutableActivator>(activator);
    }

    [TestMethod]
    public void ActivateTable_WhenArtifactIsNotClrAssembly_ShouldReject()
    {
        var activator = new ClrAssemblyExecutableActivator();
        var artifact = new TestOnlyExecutableQueryArtifact("payload");

        var exception = Assert.Throws<InvalidOperationException>(
            () => activator.ActivateTable(artifact, CreateBinding()));

        Assert.Contains("CLR executable artifact", exception.Message);
        Assert.Contains("TestOnlyNonClr", exception.Message);
    }

    [TestMethod]
    public void LoadRunnableType_WhenArtifactIsLoadedClrArtifact_ShouldReturnLoadedType()
    {
        var activator = new ClrAssemblyExecutableActivator();
        var artifact = new ClrLoadedExecutableArtifact(typeof(object));

        var type = activator.LoadRunnableType(artifact);

        Assert.AreEqual(typeof(object), type);
    }

    [TestMethod]
    public void CreateLoadedExecutableArtifact_ShouldReturnCSharpClrLoadedArtifact()
    {
        var activator = new ClrAssemblyExecutableActivator();

        var artifact = activator.CreateLoadedExecutableArtifact(typeof(object));

        Assert.AreEqual(ExecutionTargetIds.CSharpClr, artifact.TargetId);
        var loadedArtifact = Assert.IsInstanceOfType<ClrLoadedExecutableArtifact>(artifact);
        Assert.AreEqual(typeof(object), loadedArtifact.RunnableType);
    }

    private static QueryRuntimeBinding CreateBinding()
    {
        return new QueryRuntimeBinding(
            new SystemSchemaProvider(),
            new Dictionary<string, IReadOnlyDictionary<string, string>>(),
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>(),
            new Dictionary<string, SourceExecutionPlan>());
    }

    private sealed record TestOnlyExecutableQueryArtifact(string Payload)
        : ExecutableQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);
}
