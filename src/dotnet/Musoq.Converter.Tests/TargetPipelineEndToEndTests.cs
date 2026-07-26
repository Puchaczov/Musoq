using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TargetPipelineEndToEndTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CSharpClrTarget_ShouldCompilePackageLoadExecuteAndInspect()
    {
        const string query = "select i.Value from #artifact.items() i";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("single"));

        var artifactResult = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "TargetPipelineE2E",
            provider,
            _loggerResolver);

        Assert.IsTrue(artifactResult.Succeeded, FormatDiagnostics(artifactResult.Diagnostics));
        Assert.IsNotNull(artifactResult.Artifact);
        Assert.IsTrue(artifactResult.Artifact.AssemblyBytes.Length > 0);
        Assert.AreEqual("TargetPipelineE2E.CompiledQuery", artifactResult.Artifact.RunnableTypeName);
        Assert.AreEqual(
            ExecutionTargetIds.CSharpClr.ToString(),
            artifactResult.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutionTarget]);
        Assert.AreEqual(
            CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
            artifactResult.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutableArtifactKind]);
        Assert.AreEqual(
            "1",
            artifactResult.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion]);

        var packageResult = InstanceCreator.CompileTargetPackageWithDiagnostics(
            query,
            "TargetPipelineE2EPackage",
            provider,
            _loggerResolver,
            ExecutionTargetIds.CSharpClr);

        Assert.IsTrue(packageResult.Succeeded, FormatDiagnostics(packageResult.Diagnostics));
        Assert.IsNotNull(packageResult.BuildItems);
        Assert.IsNotNull(packageResult.BuildItems.ExecutionTargetCompatibilityReport);
        Assert.IsNotNull(packageResult.BuildItems.TargetRuntimeContract);
        Assert.IsNotNull(packageResult.BuildItems.ExecutionTargetReadinessReport);
        Assert.IsNotNull(packageResult.Package);
        Assert.AreEqual(1, packageResult.Package.SemanticsContract.Version);

        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifactResult.Artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("loaded")),
            _loggerResolver,
            new CompiledQueryArtifactLoadOptions
            {
                ValidationMode = CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash
            });

        Assert.IsTrue(loaded.Succeeded, FormatDiagnostics(loaded.Diagnostics));
        Assert.IsNotNull(loaded.BuildItems);
        Assert.AreEqual(
            artifactResult.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256],
            CSharpClrArtifactCompatibility.ComputeGeneratedCodeHash(loaded.BuildItems.RenderingArtifact));
        var table = loaded.CompiledQuery.Run();
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("loaded", table[0][0]);

        var inspection = InstanceCreator.CompileForInspection(
            query,
            "TargetPipelineE2EInspection",
            provider,
            _loggerResolver);

        Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode));
        Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.ExecutionPlanText));
        Assert.IsNotNull(inspection.ExecutionPlan);
        StringAssert.Contains(inspection.GeneratedCSharpCode, "CompiledQuery");
    }

    [TestMethod]
    public void CSharpClrTarget_WhenCompilationFails_ShouldReturnDiagnosticsWithoutArtifact()
    {
        var result = InstanceCreator.CompileArtifactWithDiagnostics(
            "select Missing from #artifact.items() i",
            "TargetPipelineInvalid",
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.Artifact);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Code == DiagnosticCode.MQ3001_UnknownColumn));
    }

    [TestMethod]
    public void CSharpClrTarget_WhenExecutionCompilationCacheIsWarm_ShouldActivateFromCachedArtifactAfterPlanning()
    {
        if (Debugger.IsAttached)
            return;

        var query = $"select d.Dummy from #system.dual() d where d.Dummy = 'single'";
        var provider = new SystemSchemaProvider();
        var options = new CompilationOptions();

        var first = InstanceCreator.CompileWithDiagnostics(
            query,
            "TargetPipelineCacheFirst",
            provider,
            _loggerResolver,
            options);
        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics));
        Assert.IsNotNull(first.BuildItems);

        var second = InstanceCreator.CompileWithDiagnostics(
            query,
            "TargetPipelineCacheSecond",
            provider,
            _loggerResolver,
            options);

        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics));
        Assert.IsNotNull(second.BuildItems);
        Assert.IsTrue(second.BuildItems.StopAfterPlanning);
        Assert.AreEqual("single", second.CompiledQuery.Run()[0][0]);
    }

    [TestMethod]
    public void ExecutionCompilationCache_WhenSourceSettingsResolverChanges_ShouldNotReuseCachedExecution()
    {
        if (Debugger.IsAttached)
            return;

        const string query = "select i.Token from #settings.items() i";
        var first = InstanceCreator.CompileWithDiagnostics(
            query,
            "TargetPipelineSettingsFirst",
            new SettingsArtifactSchemaProvider(),
            _loggerResolver,
            new CompilationOptions(sourceRuntimeSettingsResolver: new TokenSettingsResolver("first")));
        var second = InstanceCreator.CompileWithDiagnostics(
            query,
            "TargetPipelineSettingsSecond",
            new SettingsArtifactSchemaProvider(),
            _loggerResolver,
            new CompilationOptions(sourceRuntimeSettingsResolver: new TokenSettingsResolver("second")));

        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics));
        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics));
        Assert.IsNotNull(second.BuildItems);
        Assert.IsFalse(second.BuildItems.StopAfterPlanning);
        Assert.AreEqual("second", second.CompiledQuery.Run()[0][0]);
    }

    private static string FormatDiagnostics(System.Collections.Generic.IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }
}
